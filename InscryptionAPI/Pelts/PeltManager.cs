using BepInEx;
using DiskCardGame;
using InscryptionAPI.Dialogue;
using InscryptionAPI.Guid;
using Sirenix.Utilities;
using System.Reflection;
using UnityEngine;

namespace InscryptionAPI.Pelts;

public static class PeltManager
{
    private class VanillaPeltData : PeltData
    {
        public override int BuyPrice => GetBasePeltData().Find((a) => a.Item1 == peltCardName).Item2;
    }

    public class PeltData
    {
        public string pluginGuid;
        public string peltCardName;
        public string peltTierName = null;
        public bool isSoldByTrapper = true;

        public Func<List<CardInfo>> CardChoices;
        public Func<int, int> BuyPriceAdjustment = (int basePrice) => basePrice + RunState.CurrentRegionTier;
        public Action<CardInfo> ModifyCardChoiceAtTrader = null;

        public int baseBuyPrice;
        public int maxBuyPrice;

        public int extraAbilitiesToAdd;

        public int choicesOfferedByTrader = 8;
        public int bossDefeatedPriceReduction = 2;
        public int expensivePeltsPriceMultiplier = 2;

        public virtual int BuyPrice
        {
            get
            {
                int bossDefeatMult = !StoryEventsData.EventCompleted(StoryEvent.TrapperTraderDefeated) ? 1 : (bossDefeatedPriceReduction <= 0 ? 1 : bossDefeatedPriceReduction);
                int challengeMult = !AscensionSaveData.Data.ChallengeIsActive(AscensionChallenge.ExpensivePelts) ? 1 : (expensivePeltsPriceMultiplier <= 0 ? 1 : expensivePeltsPriceMultiplier);
                int finalPrice = BuyPriceAdjustment(baseBuyPrice) / bossDefeatMult * challengeMult;
                if (maxBuyPrice > 0)
                    finalPrice = Mathf.Min(maxBuyPrice, finalPrice);
                return Mathf.Max(1, finalPrice);
            }
        }
    }

    internal static List<PeltData> AllNewPelts = new();
    private static List<PeltData> BasePelts = null;

    internal static string[] BasePeltNames { get; } = new string[]
    {
        "PeltHare",
        "PeltWolf",
        "PeltGolden"
    };

    internal static int[] BasePeltPrices
    {
        get // base w/o modifiers: 2, 4, 7
        {
            int expensivePeltsMult = AscensionSaveData.Data.ChallengeIsActive(AscensionChallenge.ExpensivePelts) ? 2 : 1;
            int defeatedTrapperMult = SpecialNodeHandler.Instance.buyPeltsSequencer.TrapperBossDefeated ? 2 : 1;
            return new int[]
            {
                2 / defeatedTrapperMult * expensivePeltsMult,
                (4 + RunState.CurrentRegionTier) / defeatedTrapperMult * expensivePeltsMult,
                Mathf.Min(20, (7 + RunState.CurrentRegionTier * 2) / defeatedTrapperMult * expensivePeltsMult)
            };
        }
    }

    internal static List<PeltData> AllPelts()
    {
        BasePelts ??= CreateBasePelts();
        return BasePelts.Concat(AllNewPelts).ToList();
    }

    internal static List<Tuple<string, int>> GetBasePeltData()
    {
        List<Tuple<string, int>> data = new();
        for (int i = 0; i < BasePeltNames.Length; i++)
        {
            string peltName = BasePeltNames[i];
            data.Add(new Tuple<string, int>(peltName, BasePeltPrices[i]));
        }

        // Return cache
        return data;
    }

    private static List<PeltData> CreateBasePelts()
    {
        List<PeltData> pelts = new();

        for (int i = 0; i < BasePeltNames.Length; i++)
        {
            VanillaPeltData peltData = new()
            {
                peltCardName = BasePeltNames[i],
                choicesOfferedByTrader = 8,
                extraAbilitiesToAdd = 0,
                isSoldByTrapper = true,
                CardChoices = static () => CardLoader.GetUnlockedCards(CardMetaCategory.TraderOffer, CardTemple.Nature)
            };
            pelts.Add(peltData);
        }

        // Wolf Pelt
        pelts[1].extraAbilitiesToAdd = 1;

        // Golden Pelt
        pelts[2].choicesOfferedByTrader = 4;
        pelts[2].CardChoices = static () => CardLoader.GetUnlockedCards(CardMetaCategory.Rare, CardTemple.Nature);

        return pelts;
    }

    /// <summary>
    /// Creates a new instance of CustomPeltData then adds it to the game.
    /// </summary>
    /// <param name="pluginGuid">GUID of the mod adding this pelt.</param>
    /// <param name="peltCardInfo">The CardInfo for the actual pelt card.</param>
    /// <param name="getCardChoices">The list of possible cards the Trader will offer for this pelt.</param>
    /// <param name="baseBuyPrice">The starting price of this pelt when buying from the Trapper.</param>
    /// <param name="extraAbilitiesToAdd">The number of extra sigils card choices will have when trading this pelt to the Trader.</param>
    /// <param name="choicesOfferedByTrader">How many cards to offer the player when trading the pelt.</param>
    /// <returns>The newly created CustomPeltData so a chain can continue.</returns>
    public static PeltData New(string pluginGuid, CardInfo peltCardInfo, Func<List<CardInfo>> getCardChoices, int baseBuyPrice, int extraAbilitiesToAdd = 0, int choicesOfferedByTrader = 8)
    {
        return New(pluginGuid, peltCardInfo, baseBuyPrice, extraAbilitiesToAdd, choicesOfferedByTrader, getCardChoices);
    }

    public static PeltData New(string pluginGuid, CardInfo peltCardInfo, int baseBuyPrice, int extraAbilitiesToAdd, int choicesOfferedByTrader, Func<List<CardInfo>> getCardChoices)
    {
        if (getCardChoices == null)
        {
            throw new ArgumentNullException("CardChoices function cannot be null!");
        }

        PeltData peltData = new()
        {
            pluginGuid = pluginGuid,
            peltCardName = peltCardInfo.name,
            peltTierName = GetTierNameFromPelt(peltCardInfo.displayedName),
            CardChoices = getCardChoices,
            baseBuyPrice = baseBuyPrice,
            extraAbilitiesToAdd = extraAbilitiesToAdd,
            choicesOfferedByTrader = choicesOfferedByTrader
        };
        Add(peltData);

        return peltData;
    }

    /// <summary>
    /// Adds a CustomPeltData to the game, enabling it to be usable with the Trapper and Trader.
    /// </summary>
    /// <param name="data">The CustomPeltData to add.</param>
    public static void Add(PeltData data)
    {
        if (data.peltCardName.IsNullOrWhiteSpace())
        {
            InscryptionAPIPlugin.Logger.LogError("Couldn't create CustomPeltData - missing card name!");
            return;
        }

        data.pluginGuid ??= TypeManager.GetModIdFromCallstack(Assembly.GetCallingAssembly());

        if (!AllNewPelts.Contains(data))
            AllNewPelts.Add(data);
    }

    public static List<PeltData> AllPeltsAvailableAtTrader()
    {
        List<PeltData> peltNames = new();
        peltNames.AddRange(AllPelts().Where(x => x.isSoldByTrapper));
        return peltNames;
    }

    public static int GetCostOfPelt(string peltName)
    {
        PeltData pelt = GetPelt(peltName);
        if (pelt == null)
        {
            return 1;
        }

        return pelt.BuyPrice;
    }

    public static PeltData GetPelt(string peltName)
    {
        return AllPelts().Find((a) => a.peltCardName == peltName);
    }

    internal static void CreateDialogueEvents()
    {
        foreach (PeltData peltData in AllPeltsAvailableAtTrader())
        {
            string name = peltData.peltTierName ?? GetTierNameFromData(peltData);
            string dialogueId = "TraderPelts" + name;
            if (!DialogueManager.CustomDialogue.Exists(x => x.DialogueEvent.id == dialogueId))
            {
                if (name.Contains("pelt") || name.Contains("pelt"))
                {
                    DialogueManager.GenerateEvent(InscryptionAPIPlugin.ModGUID, dialogueId,
                        new()
                        {
                            name + "pelts..."
                        }
                    );
                }
                else
                {
                    DialogueManager.GenerateEvent(InscryptionAPIPlugin.ModGUID, dialogueId,
                        new()
                        {
                            name + "..."
                        }
                    );
                }
            }
        }
    }

    public static string GetTierNameFromPelt(string cardName)
    {
        string result = "";
        if (cardName.Contains("pelt") || cardName.Contains("pelt"))
        {
            result = cardName.ToLowerInvariant().Replace("pelt", "").Replace("pelts", "");
            result = result.Split('_').Last().ToTitleCase();
        }
        else
        {
            result = cardName.ToLowerInvariant();
            result = result.Split('_').Last().ToTitleCase();
        }

        return result;
    }
    public static string GetTierNameFromData(PeltData peltData)
    {
        return peltData.peltTierName ?? GetTierNameFromPelt(peltData.peltCardName);
    }
}
