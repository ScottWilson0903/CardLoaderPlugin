using DiskCardGame;
using HarmonyLib;
using Pixelplacement;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace InscryptionAPI.Pelts.Patches;

[HarmonyPatch]
internal class TradePeltsDialogue
{
    [HarmonyPostfix, HarmonyPatch(typeof(TradePeltsSequencer), nameof(TradePeltsSequencer.GetTierName))]
    private static void AddCustomTierNames(int tier, ref string __result)
    {
        if (tier < 3)
            return;

        __result = PeltManager.GetTierNameFromData(PeltManager.AllPelts()[tier]);
    }
}

[HarmonyPatch(typeof(TradePeltsSequencer), "GetTradeCardInfos", new Type[] { typeof(int), typeof(bool) })]
internal static class TradePeltsSequencer_GetTradeCardInfos
{
    /// <summary>
    /// Shows cards at the Trader to be traded for real cards
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // === We want to turn this

        // List<CardInfo> list = new List<CardInfo>();
        // List<CardInfo> unlockedCards;
        // int numCards;
        // if (tier != 2)
        // {
        //     unlockedCards = CardLoader.GetUnlockedCards(CardMetaCategory.TraderOffer, CardTemple.Nature);
        //     numCards = 8;
        // }
        // else
        // {
        //     unlockedCards = CardLoader.GetUnlockedCards(CardMetaCategory.Rare, CardTemple.Nature);
        //     numCards = 4;
        // }
        // list = CardLoader.GetDistinctCardsFromPool(SaveManager.SaveFile.GetCurrentRandomSeed() + tier * 1000, numCards, unlockedCards, (tier == 1) ? 1 : 0, false);

        // === Into this

        // List<CardInfo> list = new List<CardInfo>();
        // List<CardInfo> unlockedCards;
        // int numCards;
        // if (tier != 2)
        // {
        //     unlockedCards = CardLoader.GetUnlockedCards(CardMetaCategory.TraderOffer, CardTemple.Nature);
        //     numCards = 8;
        // }
        // else
        // {
        //     unlockedCards = CardLoader.GetUnlockedCards(CardMetaCategory.Rare, CardTemple.Nature);
        //     numCards = 4;
        // }
        // GetCardOptions(tier, ref unlockedCards)
        // numCards(tier, ref numCards)
        // list = CardLoader.GetDistinctCardsFromPool(SaveManager.SaveFile.GetCurrentRandomSeed() + tier * 1000, numCards, unlockedCards, abilityCount((tier == 1) ? 1 : 0, tier), false);

        // ===
        List<CodeInstruction> codes = new(instructions);

        List<CardInfo> y = null;
        MethodInfo GetCardOptionsInfo = SymbolExtensions.GetMethodInfo(() => GetCardOptions(1, ref y));

        int t = 0;
        MethodInfo NumCardsInfo = SymbolExtensions.GetMethodInfo(() => numCards(1, ref t));
        MethodInfo AbilityCountInfo = SymbolExtensions.GetMethodInfo(() => abilityCount(1, 1));

        // Find index of
        // list = CardLoader.GetDistinctCardsFromPool(SaveManager.SaveFile.GetCurrentRandomSeed()
        int indexFinishedCalculations = -1;
        int numAbilitiesIndex = -1;
        CodeInstruction numAbilitiesInstruction = null;
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && codes[i].operand != null && codes[i].operand.ToString() == "SaveFile get_SaveFile()")
            {
                indexFinishedCalculations = i + 1;
            }

            if (codes[i].opcode == OpCodes.Call && codes[i].operand != null && codes[i].operand.ToString() == "System.Collections.Generic.List`1[DiskCardGame.CardInfo] GetDistinctCardsFromPool(Int32, Int32, System.Collections.Generic.List`1[DiskCardGame.CardInfo], Int32, Boolean)")
            {
                numAbilitiesIndex = i - 1;
                numAbilitiesInstruction = codes[i - 1];
            }
        }

        if (indexFinishedCalculations < 0)
        {
            InscryptionAPIPlugin.Logger.LogError("[TradePeltsSequencer_GetTradeCardInfos] Did not find index for indexFinishedCalculations!");
        }
        else if (numAbilitiesIndex < 0)
        {
            InscryptionAPIPlugin.Logger.LogError("[TradePeltsSequencer_GetTradeCardInfos] Did not find index for numAbilitiesIndex!");
        }
        else
        {
            int j = 0;

            // void GetCardOptions(int tier, ref List<CardInfo> cards)
            codes.Insert(indexFinishedCalculations + j++, new CodeInstruction(OpCodes.Ldarg_1)); // tier
            codes.Insert(indexFinishedCalculations + j++, new CodeInstruction(OpCodes.Ldloca, 1)); // list
            codes.Insert(indexFinishedCalculations + j++, new CodeInstruction(OpCodes.Call, GetCardOptionsInfo));

            // void numCards(int tier, ref int numCards)
            codes.Insert(indexFinishedCalculations + j++, new CodeInstruction(OpCodes.Ldarg_1)); // tier
            codes.Insert(indexFinishedCalculations + j++, new CodeInstruction(OpCodes.Ldloca, 2)); // numCards
            codes.Insert(indexFinishedCalculations + j++, new CodeInstruction(OpCodes.Call, NumCardsInfo));

            // int abilityCount(int tier, int abilityCount)
            j++;
            numAbilitiesInstruction.opcode = OpCodes.Ldarg_1; // tier
            codes.Insert(numAbilitiesIndex + j++, new CodeInstruction(OpCodes.Call, AbilityCountInfo));
            codes.Insert(numAbilitiesIndex + j++, new CodeInstruction(OpCodes.Ldc_I4_0)); // False
        }

        return codes;
    }

    public static void Postfix(ref List<CardInfo> __result, int tier, bool mergedPelt)
    {
        PeltManager.PeltData pelt = PeltManager.AllPelts()[tier];
        if (pelt.ModifyCardChoiceAtTrader != null)
        {
            foreach (CardInfo cardInfo in __result)
            {
                pelt.ModifyCardChoiceAtTrader(cardInfo);
            }
        }

    }

    private static void GetCardOptions(int tier, ref List<CardInfo> cards)
    {
        PeltManager.PeltData pelt = PeltManager.AllPelts()[tier];
        List<CardInfo> cardOptions = pelt.CardChoices();
        //cardOptions.RemoveAll((a) => a.Health == 0);
        if (cardOptions.Count == 0)
        {
            InscryptionAPIPlugin.Logger.LogWarning("No cards specified for pelt '" + pelt.peltCardName + "', using fallback card.");
            cardOptions.Add(CardLoader.GetCardByName("Amalgam"));
        }

        cards = cardOptions;
    }

    private static void numCards(int tier, ref int numCards)
    {
        PeltManager.PeltData pelt = PeltManager.AllPelts()[tier];
        numCards = Mathf.Min(8, pelt.choicesOfferedByTrader);
    }

    private static int abilityCount(int abilityCount, int tier)
    {
        PeltManager.PeltData pelt = PeltManager.AllPelts()[tier];
        int peltAbilityCount = pelt.extraAbilitiesToAdd;
        return peltAbilityCount;
    }
}

[HarmonyPatch]
public class TradePeltsSequencer_TradePelts
{
    /// <summary>
    /// Patches TradePelts sequence to fix how cards are displayed.
    /// </summary>
    [HarmonyTranspiler, HarmonyPatch(typeof(TradePeltsSequencer), nameof(TradePeltsSequencer.TradePelts), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> CapCreatedPeltCards(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        int searchIndex = codes.FindIndex(0, x => x.opcode == OpCodes.Callvirt && x.operand.ToString() == GetTradeCardInfos);
        if (searchIndex != -1)
        {
            // move to the start of this block of code
            searchIndex -= 7;

            // remove unnecessary opcode
            codes.RemoveAt(searchIndex + 1);

            // update the index
            searchIndex = codes.FindIndex(searchIndex, x => x.opcode == OpCodes.Callvirt && x.operand.ToString() == GetTradeCardInfos);
            int endIndex = codes.FindIndex(searchIndex, x => x.opcode == OpCodes.Stfld && x.operand.ToString() == Current);
            codes.RemoveRange(searchIndex, endIndex - searchIndex);

            MethodInfo customMethod = AccessTools.Method(typeof(TradePeltsSequencer_TradePelts), nameof(CreateTradeCardsFixRows),
                new Type[] { typeof(TradePeltsSequencer), typeof(int), typeof(bool) });

            codes.Insert(searchIndex, new(OpCodes.Callvirt, customMethod));
        }
        return codes;
    }

    private static IEnumerator CreateTradeCardsFixRows(TradePeltsSequencer instance, int tier, bool hasMergedPelt)
    {
        List<CardInfo> cards = instance.GetTradeCardInfos(tier, hasMergedPelt);
        bool useOneRow = cards.Count <= 4;

        yield return instance.CreateTradeCards(cards, useOneRow ? cards.Count : (cards.Count + 1) / 2, useOneRow);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TradePeltsSequencer), nameof(TradePeltsSequencer.CreateTradeCards))]
    private static bool DisableVanillaCreateTradeCards(ref IEnumerator __result, TradePeltsSequencer __instance, List<CardInfo> cards, int cardsPerRow, bool rareCards)
    {
        __result = CentreCreateTradeCards(__instance, cards, cardsPerRow, rareCards);
        return false;
    }

    /// <summary>
    /// Centres trade cards when the amount offered isn't 4 or 8.
    /// </summary>
    public static IEnumerator CentreCreateTradeCards(TradePeltsSequencer __instance, List<CardInfo> cards, int cardsPerRow, bool rareCards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            float num = i % cardsPerRow;
            float num2 = (i < cardsPerRow) ? 1 : 0;
            GameObject obj = UnityObject.Instantiate(__instance.selectableCardPrefab, __instance.transform);
            obj.gameObject.SetActive(value: true);
            SelectableCard component = obj.GetComponent<SelectableCard>();
            component.SetInfo(cards[i]);
            SpecialCardBehaviour[] components = obj.GetComponents<SpecialCardBehaviour>();
            for (int j = 0; j < components.Length; j++)
            {
                components[j].OnShownForCardChoiceNode();
            }

            // account for the number of cards in the second row being less than cardsPerRow
            int actualCardsPerRow = (num2 == 0 && cards.Count - cardsPerRow < cardsPerRow) ? cards.Count - cardsPerRow : cardsPerRow;
            float anchorOffset = (4 - actualCardsPerRow) * (__instance.CARD_SPACING.x / 2f);

            Vector3 vector = __instance.CARDS_ANCHOR + new Vector3(anchorOffset + __instance.CARD_SPACING.x * num, 0f, __instance.CARD_SPACING.y * num2);

            // we treat this as a bool indicating whether there's 1 row or 2
            if (rareCards)
                vector.z = -2f;

            Vector3 vector2 = new(90f, 90f, 90f);
            component.transform.position = vector + new Vector3(0f, 0.25f, 3f);
            component.transform.eulerAngles = vector2 + new Vector3(0f, 0f, -7.5f + UnityEngine.Random.value * 7.5f);
            Tween.Position(component.transform, vector, 0.15f, 0f, Tween.EaseOut);
            Tween.Rotation(component.transform, vector2, 0.15f, 0f, Tween.EaseOut);
            __instance.tradeCards.Add(component);
            component.SetEnabled(enabled: false);
            component.Anim.PlayQuickRiffleSound();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private static readonly string Current = "System.Object <>2__current";
    private static readonly string GetTradeCardInfos = "System.Collections.Generic.List`1[DiskCardGame.CardInfo] GetTradeCardInfos(Int32, Boolean)";
}
[HarmonyPatch]
public class TradePeltsSequencer_CreatePeltCards
{
    /// <summary>
    /// The game caps how many Pelts of a single type you can trade in a single visit.
    /// This adds a cap to custom Pelts.
    /// The cap is equal to the number of cards the player will be able to trade.
    /// </summary>
    [HarmonyTranspiler, HarmonyPatch(typeof(TradePeltsSequencer), nameof(TradePeltsSequencer.CreatePeltCards), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> CapCreatedPeltCards(IEnumerable<CodeInstruction> instructions)
    {
        // === We want to turn this:

        // int numPelts = Mathf.Min((tier == 2) ? 4 : 8, cardInfos.Count);
        // int num;

        // === Into this:

        // int numPelts = NumPelts(tier, cardInfos.Count);
        // int num;

        // ===
        List<CodeInstruction> codes = new(instructions);
        int startIndex = codes.FindIndex(0, x => x.opcode == OpCodes.Stfld && x.operand.ToString() == SetCardInfos);
        if (startIndex != -1)
        {
            startIndex = codes.FindIndex(startIndex, x => x.opcode == OpCodes.Ldc_I4_2);
            int endIndex = codes.FindIndex(startIndex, x => x.opcode == OpCodes.Stfld && x.operand.ToString() == SetNumPelts);
            if (endIndex != -1)
            {
                endIndex--;
                codes.RemoveAt(endIndex);

                MethodInfo customMethod = AccessTools.Method(typeof(TradePeltsSequencer_CreatePeltCards), nameof(NumPelts),
                    new Type[] { typeof(int), typeof(int) });

                codes.Insert(endIndex, new(OpCodes.Call, customMethod));
                endIndex -= 3;
                codes.RemoveRange(startIndex, endIndex - startIndex);
            }
        }
        return codes;
    }

    /// <summary>
    /// Returns the number of Pelts to display during the sequence.
    /// </summary>
    /// <param name="tier">The tier/index of the Pelt we're trading.</param>
    /// <param name="cardChoiceCount">How many card choices will be shown for this Pelt.</param>
    public static int NumPelts(int tier, int cardChoiceCount)
    {
        PeltManager.PeltData pelt = PeltManager.AllPelts()[tier];
        return Mathf.Min(pelt.choicesOfferedByTrader, cardChoiceCount);
    }

    private static readonly string SetNumPelts = "System.Int32 <numPelts>5__3";
    private static readonly string SetCardInfos = "System.Collections.Generic.List`1[DiskCardGame.CardInfo] <cardInfos>5__2";

    private static readonly Type classType = Type.GetType("DiskCardGame.TradePeltsSequencer+<CreatePeltCards>d__19, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    private static readonly Type display19ClassType = Type.GetType("DiskCardGame.TradePeltsSequencer+<>c__DisplayClass19_0, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
}