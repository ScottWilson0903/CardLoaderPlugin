using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionAPI.Dialogue;
using InscryptionAPI.Guid;
using Sirenix.Utilities;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace InscryptionAPI.CardCosts;

[HarmonyPatch]
public static class CardCostManager
{
    public class FullCardCost
    {
        /// <summary>
        /// A unique identifier corresponding to the mod that added this cost.
        /// </summary>
        public string ModGUID;
        /// <summary>
        /// The internal name of this cost. MUST be identical to the CostName used in the CustomCardCost class.
        /// </summary>
        public string CostName;
        /// <summary>
        /// The Type corresponding to this cost's CustomCardCost class.
        /// </summary>
        public Type CostBehaviour;
        /// <summary>
        /// Whether or not the cost value can be negative. If false, negative values will be set to 0 when retrieved using API methods.
        /// </summary>
        public bool CanBeNegative = false;
        /// <summary>
        /// The ResourceType of this cost. Determines whether or not this cost can appear as a choice at card cost choice nodes.
        /// If multiple custom costs share the same ResourceType, they will all be part of the same choice pool.
        /// </summary>
        public ResourceType ResourceType = ResourceType.None;
        /// <summary>
        /// An array of integers representing different resource amounts that can be chosen at the cost choice node in Act 1.
        /// For example, at the choice node you can choose between 1 Blood, 2 Blood, and 3 Blood.
        /// 
        /// If this cost is part of a group (ie have the same ResourceType), then this field will be ignored.
        /// </summary>
        public int[] ChoiceAmounts = null;
        /// <summary>
        /// Retrieves the correct texture corresponding to CardInfo's cost. Used in the 3D Acts (Act 1, Act 3, etc.).
        /// Parameters:
        ///     int: how much of the custom cost the CardInfo/PlayableCard needs to be played.
        ///     CardInfo: the base CardInfo of the current card.
        ///     PlayableCard: the PlayableCard possessing the base CardInfo. Can be null.
        /// Returns:
        ///     The texture representing the custom cost to display on the card.
        /// </summary>
        public Func<int, CardInfo, PlayableCard, Texture2D> GetCostTexture;
        /// <summary>
        /// Retrieves the correct texture corresponding to CardInfo's cost. Used in Act 2.
        /// Parameters:
        ///     int: how much of the custom cost the CardInfo/PlayableCard needs to be played.
        ///     CardInfo: the base CardInfo of the current card.
        ///     PlayableCard: the PlayableCard possessing the base CardInfo. Can be null.
        /// Returns:
        ///     The texture representing the custom cost to display on the card.
        /// </summary>
        public Func<int, CardInfo, PlayableCard, Texture2D> GetPixelCostTexture;
        /// <summary>
        /// The function used to determine this cost's tier when on a card.
        /// For example, the cost tier of Bones is calculated as (amount / 3), so a card that costs 3 Bones will have a tier of 1
        /// Parameters:
        ///     int: the card's custom cost value.
        /// Returns:
        ///     An integer representing the cost tier of this cost.
        /// </summary>
        public Func<int, int> GetCostTier = null;
        /// <summary>
        /// The function used to determine if a card with this cost can be played by turn 2, accounting for other cards in the hand.
        /// Used when creating the starting hand.
        /// Parameters:
        ///     int: the card's custom cost value.
        ///     CardInfo: the card.
        ///     List&lt;CardInfo&gt;: the list of cards in the player's hand.
        /// Returns:
        ///     A bool representing if this card can be played by the second turn.
        /// </summary>
        public Func<int, CardInfo, List<CardInfo>, bool> GetCanBePlayedByTurn2WithHand = null;
        /// <summary>
        /// The texture to use for this cost if it appears in a card cost choice node.
        /// </summary>
        public Func<int, Texture> GetRewardBackTexture = null;

        public Texture RewardBackTexture(int amount)
        {
            if (GetRewardBackTexture == null)
                return null;

            return GetRewardBackTexture(amount);
        }
        /// <param name="amount">The card's cost value.</param>
        /// <param name="cardInfo">The card's CardInfo.</param>
        /// <param name="playableCard">The PlayableCard associated with the card. Can be null.</param>
        /// <returns>The texture for this cost based on the amount. Used in Acts 1 and 3.</returns>
        public Texture2D CostTexture(int amount, CardInfo cardInfo, PlayableCard playableCard)
        {
            if (GetCostTexture == null)
                return null;

            return GetCostTexture(amount, cardInfo, playableCard);
        }
        /// <param name="amount">The cost value of the CardInfo being checked.</param>
        /// <param name="cardInfo">The CardInfo being checked.</param>
        /// <param name="playableCard">The PlayableCard associated with the card. Can be null.</param>
        /// <returns>The texture for this cost based on the amount. Used in Act 2.</returns>
        public Texture2D PixelCostTexture(int amount, CardInfo cardInfo, PlayableCard playableCard)
        {
            if (GetPixelCostTexture == null)
                return null;

            return GetPixelCostTexture(amount, cardInfo, playableCard);
        }
        /// <param name="amount">The cost value of the card being checked.</param>
        /// <returns>This cost tier based on its amount.</returns>
        public int CostTier(int amount)
        {
            if (GetCostTier == null)
                return 0;

            return GetCostTier(amount);
        }
        /// <param name="amount">The cost value of the CardInfo being checked.</param>
        /// <param name="card">The CardInfo being checked.</param>
        /// <param name="hand">The list of other cards in the player's hand.</param>
        /// <returns>Whether or not the provided card can be played by turn 2.</returns>
        public bool CanBePlayedByTurn2WithHand(int amount, CardInfo card, List<CardInfo> hand)
        {
            if (GetCanBePlayedByTurn2WithHand == null)
                return true;

            return GetCanBePlayedByTurn2WithHand(amount, card, hand);
        }

        public FullCardCost(string modGUID, string costName, Type costBehaviour,
            Func<int, CardInfo, PlayableCard, Texture2D> getCostTexture,
            Func<int, CardInfo, PlayableCard, Texture2D> getPixelCostTexture,
            Func<int, int> getCostTier,
            Func<int, CardInfo, List<CardInfo>, bool> getCanBePlayedByTurn2WithHand,
            Func<int, Texture> getRewardBackTexture,
            bool canBeNegative, ResourceType resourceType, int[] choiceAmounts)
        {
            ModGUID = modGUID;
            CostName = costName;
            CostBehaviour = costBehaviour;
            GetCostTexture = getCostTexture;
            GetPixelCostTexture = getPixelCostTexture;
            CanBeNegative = canBeNegative;
            GetCostTier = getCostTier;
            GetCanBePlayedByTurn2WithHand = getCanBePlayedByTurn2WithHand;
            GetRewardBackTexture = getRewardBackTexture;
            ResourceType = resourceType;
            ChoiceAmounts = choiceAmounts;

            TypeManager.Add(costName, costBehaviour);
        }

        public FullCardCost(string modGUID, string costName, Type costBehaviour,
            Func<int, CardInfo, PlayableCard, Texture2D> getCostTexture,
            Func<int, CardInfo, PlayableCard, Texture2D> getPixelCostTexture)
        {
            ModGUID = modGUID;
            CostName = costName;
            CostBehaviour = costBehaviour;
            GetCostTexture = getCostTexture;
            GetPixelCostTexture = getPixelCostTexture;
            CanBeNegative = false;
            GetCostTier = null;
            GetCanBePlayedByTurn2WithHand = null;
            GetRewardBackTexture = null;
            ResourceType = ResourceType.None;
            ChoiceAmounts = null;

            TypeManager.Add(costName, costBehaviour);
        }

        public FullCardCost Clone()
        {
            FullCardCost retval = new(
                this.ModGUID, this.CostName, this.CostBehaviour,
                this.GetCostTexture, this.GetPixelCostTexture,
                this.GetCostTier, this.GetCanBePlayedByTurn2WithHand,
                this.GetRewardBackTexture,
                this.CanBeNegative, this.ResourceType, this.ChoiceAmounts);
            return retval;
        }
    }

    static CardCostManager()
    {
        NewCosts.CollectionChanged += static (_, _) =>
        {
            SyncCustomCostList();
        };
    }

    public static List<FullCardCost> AllCustomCosts { get; private set; } = new();

    internal readonly static ObservableCollection<FullCardCost> NewCosts = new();

    public static event Func<List<FullCardCost>, List<FullCardCost>> ModifyCustomCostList;

    // you don't have the [colour] gem for that.
    // no [colour] gem on the table? then you can't play it.
    // you need at least one [colour] gem on the board in order to play that [card]
    // play the required [colour] gem... then you can play that.
    public static HintsHandler.Hint notEnoughSameColourGemsHint = new("Hint_NotEnoughSameColourGemsHint", 2);
    public static DialogueEvent notEnoghSameColourGemsEvent = DialogueManager.GenerateEvent(
        InscryptionAPIPlugin.ModGUID, "Hint_NotEnoughSameColourGemsHint",
                new() { "You don't have enough [v:1] gems for that." },
                new()
                {
                    new() { "Not enough [v:1] gems on the table? Then you can't play it." },
                    new() { "You need more [v:1] gems on the board in order to play that [v:0]."},
                    new() { "Play the required [v:1] gems... then you can play that." }
                });
    public static void SyncCustomCostList()
    {
        AllCustomCosts = NewCosts.Select(a => a.Clone()).ToList();
        AllCustomCosts = ModifyCustomCostList?.Invoke(AllCustomCosts) ?? AllCustomCosts;
    }

    public static FullCardCost CostByBehaviour(this IEnumerable<FullCardCost> customCosts, Type costBehaviour)
    {
        return customCosts.FirstOrDefault(x => x.CostBehaviour == costBehaviour);
    }
    public static FullCardCost Register(string modGUID, string costName, Type costBehaviour,
        Func<int, CardInfo, PlayableCard, Texture2D> getCostTexture, Func<int, CardInfo, PlayableCard, Texture2D> getPixelCostTexture)
    {
        FullCardCost newCost = new(modGUID, costName, costBehaviour, getCostTexture, getPixelCostTexture);

        NewCosts.Add(newCost);
        return newCost;
    }

    #region Extensions
    /// <summary>
    /// Sets the function used to determine the cost tier for a custom cost.
    /// For example, the cost tier for Bones is equal to (Bones / 3 ), rounded down to the nearest integer.
    /// </summary>
    /// <param name="fullCardCost">The FullCardCost object to modify.</param>
    /// <param name="getCostTier">Parameters: int - cost amount. Returns int representing tier value.</param>
    /// <returns>The same FullCardCost so a chain can continue.</returns>
    public static FullCardCost SetCostTier(this FullCardCost fullCardCost, Func<int, int> getCostTier)
    {
        fullCardCost.GetCostTier = getCostTier;
        return fullCardCost;
    }

    /// <summary>
    /// Defines what cost amounts for this cost will be offered at the cost choice node.
    /// For example, the choice node offers 1 Blood, 2 Blood, and 3 Blood.
    /// 
    /// If this cost is part of a group (ie shares its ResourceType with another custom cost), ChoiceAmounts is ignored.
    /// </summary>
    /// <param name="fullCardCost">The FullCardCost object to modify.</param>
    /// <param name="choiceAmounts">An array of integers representing cost amounts to offer at the cost choice node.</param>
    /// <returns>The same FullCardCost so a chain can continue.</returns>
    public static FullCardCost SetChoiceAmounts(this FullCardCost fullCardCost, params int[] choiceAmounts)
    {
        fullCardCost.ChoiceAmounts = choiceAmounts;
        return fullCardCost;
    }
    /// <summary>
    /// Sets the function used to determine if a card with this cost can be played by the second turn
    /// </summary>
    /// <param name="fullCardCost">The FullCardCost object to modify.</param>
    /// <param name="getCanBePlayedByTurn2WithHand">CardInfo - current CardInfo ; List<CardInfo> - cards in player's hand.</CardInfo>.</param>
    /// <returns>The same FullCardCost so a chain can continue.</returns>
    public static FullCardCost SetCanBePlayedByTurn2WithHand(this FullCardCost fullCardCost, Func<int, CardInfo, List<CardInfo>, bool> getCanBePlayedByTurn2WithHand)
    {
        fullCardCost.GetCanBePlayedByTurn2WithHand = getCanBePlayedByTurn2WithHand;
        return fullCardCost;
    }

    /// <summary>
    /// Whether or not GetCustomCost can return negative values. If False, negative values will be returned as 0.
    /// </summary>
    /// <param name="fullCardCost">The FullCardCost object to modify.</param>
    /// <param name="canBeNegative">If negative cost values should be accepted.</param>
    /// <returns>The same FullCardCost so a chain can continue.</returns>
    public static FullCardCost SetCanBeNegative(this FullCardCost fullCardCost, bool canBeNegative)
    {
        fullCardCost.CanBeNegative = canBeNegative;
        return fullCardCost;
    }

    /// <summary>
    /// Sets the ResourceType and RewardBackTexture for this cost, allowing it to be selected at cost choice nodes in Act 1.
    /// </summary>
    /// <param name="fullCardCost">The FullCardCost object to modify.</param>
    /// <param name="modGUID">The GUID of the mod adding the cost.</param>
    /// <param name="costName">The internal name of the cost.</param>
    /// <param name="isChoice">Whether this card cost should appear at cost choice nodes. Sets the ResourceType to a unique value if true. Sets the ResourceType to None if false.</param>
    /// <param name="rewardBack">The texture to use at the cost choice node.</param>
    /// <returns>The same FullCardCost so a chain can continue.</returns>
    public static FullCardCost SetFoundAtChoiceNodes(this FullCardCost fullCardCost, bool isChoice, Texture rewardBack)
    {
        return fullCardCost.SetFoundAtChoiceNodes(isChoice, (int i) => rewardBack, null);
    }

    public static FullCardCost SetFoundAtChoiceNodes(this FullCardCost fullCardCost, bool isChoice, Func<int, Texture> rewardBackFunc, params int[] choiceAmounts)
    {
        if (isChoice)
            fullCardCost.ResourceType = GuidManager.GetEnumValue<ResourceType>(fullCardCost.ModGUID, fullCardCost.CostName);
        else
            fullCardCost.ResourceType = ResourceType.None;

        fullCardCost.GetRewardBackTexture = rewardBackFunc;
        fullCardCost.SetChoiceAmounts(choiceAmounts);

        return fullCardCost;
    }
    #endregion

    #region Patches
    [HarmonyPostfix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.CanPlay))]
    private static void CanPlayCustomCosts(ref bool __result, ref PlayableCard __instance)
    {
        if (!__result)
            return;

        foreach (CustomCardCost cost in __instance.GetCustomCardCosts())
        {
            FullCardCost fullCost = AllCustomCosts.CostByBehaviour(cost.GetType());
            if (!cost.CostSatisfied(__instance.GetCustomCost(fullCost), __instance))
            {
                __result = false;
                return;
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.GemsCostRequirementMet))]
    private static bool ReplaceCostRequirementMet(PlayableCard __instance, ref bool __result)
    {
        __result = NewGemsCostRequirementMet(__instance);
        return false;
    }

    /// <summary>
    /// A new method that overrides the result of PlayableCard.GemsCostRequirement. Override this if you want to modify GemsCostRequirementMet.
    /// </summary>
    /// <param name="card">The PlayableCard currently being checked.</param>
    /// <returns>Boolean representing if the card's gem cost has been met or not.</returns>
    public static bool NewGemsCostRequirementMet(PlayableCard card)
    {
        List<GemType> gemsOwned = new(ResourcesManager.Instance.gems);
        foreach (GemType item in card.GemsCost())
        {
            if (gemsOwned.Contains(item))
                gemsOwned.Remove(item);
            else
                return false;
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(HintsHandler), nameof(HintsHandler.OnNonplayableCardClicked))]
    private static bool CannotAffordCustomCostHints(ref PlayableCard card)
    {
        // if a card costs multiple of the same colour
        if (card.GemsCost().Count > 0 && !card.GemsCost().Exists(x => !ResourcesManager.Instance.HasGem(x)))
        {
            List<GemType> neededGems = card.GemsCost();
            foreach (GemType gem in ResourcesManager.Instance.gems)
            {
                neededGems.Remove(gem);
            }
            if (neededGems.Count > 0)
            {
                GemType gem = neededGems[0];
                if (SaveManager.SaveFile.IsPart2)
                {
                    string arg = HintsHandler.GetDialogueColorCodeForGem(gem) + Localization.Translate(gem.ToString()) + "[c:]";
                    string message = string.Format(Localization.Translate("You do not have the {0} Gem to play that. Gain Gems by playing Mox cards."), arg);
                    CustomCoroutine.Instance.StartCoroutine(Singleton<TextBox>.Instance.ShowUntilInput(message, TextBox.Style.Neutral, null, TextBox.ScreenPosition.ForceTop, 0f, hideAfter: true, shake: false, null, adjustAudioVolume: false));
                }
                else if (TextDisplayer.m_Instance != null)
                {
                    notEnoughSameColourGemsHint.TryPlayDialogue(new string[2]
                    {
                        card.Info.DisplayedNameLocalized,
                        HintsHandler.GetColorCodeForGem(gem) + Localization.Translate(gem.ToString()) + "</color>"
                    });
                }
                return false;
            }
        }

        foreach (CustomCardCost customCost in card.GetCustomCardCosts())
        {
            FullCardCost fullCost = AllCustomCosts.CostByBehaviour(customCost.GetType());
            int value = card.GetCustomCost(fullCost);
            if (!customCost.CostSatisfied(value, card))
            {
                if (SaveManager.SaveFile.IsPart2)
                {
                    CustomCoroutine.Instance.StartCoroutine(TextBox.Instance.ShowUntilInput(
                        customCost.CostUnsatisfiedHint(value, card),
                        DialogueManager.GetStyleFromAmbition())
                        );
                }
                else if (TextDisplayer.m_Instance != null)
                {
                    if (customCost.PlayHintFrequency > 0 && customCost.playHintAttempts % customCost.PlayHintFrequency == 0)
                    {
                        CustomCoroutine.Instance.StartCoroutine(TextDisplayer.Instance.ShowUntilInput(
                                customCost.CostUnsatisfiedHint(value, card))
                            );
                    }
                    customCost.playHintAttempts++;
                }
                return false;
            }
        }

        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DiskCardGame.Card), nameof(DiskCardGame.Card.SetInfo))]
    private static void AttachCustomCardCosts(DiskCardGame.Card __instance, CardInfo info)
    {
        //InscryptionAPIPlugin.Logger.LogDebug("AttachCustomCardCosts");
        // destroy all pre-existing card cost objects
        __instance.GetComponents<CustomCardCost>().ForEach(x => UnityObject.Destroy(x));

        foreach (Type type in info.GetCustomCosts().Select(x => x.CostBehaviour))
        {
            __instance.gameObject.AddComponent(type);
            //InscryptionAPIPlugin.Logger.LogDebug($"AddCost: {type.Name}");
        }
    }

    // adds custom cost tiers to CostTier - also fixes CostTier to account for vanilla cost adjustments
    [HarmonyPostfix, HarmonyPatch(typeof(CardInfo), nameof(CardInfo.CostTier), MethodType.Getter)]
    private static void CustomCostTiers(CardInfo __instance, ref int __result)
    {
        __result = __instance.BloodCost + Mathf.CeilToInt(__instance.BonesCost / 3f) + Mathf.RoundToInt(__instance.EnergyCost / 2f) + __instance.GemsCost.Count;
        foreach (FullCardCost customCost in __instance.GetCustomCosts())
        {
            __result += customCost.CostTier(__instance.GetCustomCost(customCost));
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Deck), nameof(Deck.CardCanBePlayedByTurn2WithHand))]
    private static void CustomCardsCanBePlayedByTurn2(ref bool __result, CardInfo card, List<CardInfo> hand)
    {
        if (!__result)
            return;

        foreach (FullCardCost fullCost in card.GetCustomCosts())
        {
            if (!fullCost.CanBePlayedByTurn2WithHand(card.GetCustomCost(fullCost), card, hand))
            {
                __result = false;
                return;
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(HintsHandler), nameof(HintsHandler.OnGBCNonPlayableCardPressed))]
    private static bool MultiMonoColourMoxSupport(PlayableCard card)
    {
        if (card.GemsCost().Count > 0)
        {
            List<GemType> ownedGems = card.GemsCost();
            foreach (GemType gem in ResourcesManager.Instance.gems)
                ownedGems.Remove(gem);

            if (ownedGems.Count > 0)
            {
                GemType gem = ownedGems[0];
                string arg = HintsHandler.GetDialogueColorCodeForGem(gem) + Localization.Translate(gem.ToString()) + "[c:]";
                string message = string.Format(Localization.Translate("You do not have the {0} Gem to play that. Gain Gems by playing Mox cards."), arg);
                CustomCoroutine.Instance.StartCoroutine(Singleton<TextBox>.Instance.ShowUntilInput(message, TextBox.Style.Neutral, null, TextBox.ScreenPosition.ForceTop, 0f, true, false, null, false));
                return false;
            }
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(HintsHandler), nameof(HintsHandler.OnNonplayableCardClicked))]
    private static bool MultipleSameColourMoxSupport(PlayableCard card)
    {
        if (card.GemsCost().Count > 0)
        {
            List<GemType> neededGems = card.GemsCost();
            foreach (GemType gem in ResourcesManager.Instance.gems)
            {
                neededGems.Remove(gem);
            }
            if (neededGems.Count > 0)
            {
                GemType gem = neededGems[0];
                HintsHandler.notEnoughGemsHint.TryPlayDialogue(new string[2]
                {
                    card.Info.DisplayedNameLocalized,
                    HintsHandler.GetColorCodeForGem(gem) + Localization.Translate(gem.ToString()) + "</color>"
                });
                return false;
            }
        }

        return true;
    }

    #region SelectedCardToSlot transiler
    [HarmonyTranspiler, HarmonyPatch(typeof(PlayerHand), nameof(PlayerHand.SelectSlotForCard), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> FixDialogueSoftlock(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        //codes.LogCodeInscryptions();

        int methodIndex = codes.FindIndex(x => x.opcode == OpCodes.Callvirt && x.operand.ToString() == "System.Collections.IEnumerator PlayCardOnSlot(DiskCardGame.PlayableCard, DiskCardGame.CardSlot)");
        if (methodIndex > 0)
        {
            MethodInfo newPlayCardToSlot = AccessTools.Method(typeof(CardCostManager), nameof(CardCostManager.PlaySelectedCardToSlot),
                new Type[] { typeof(PlayerHand), typeof(PlayableCard), typeof(CardSlot) });

            codes[methodIndex] = new(OpCodes.Callvirt, newPlayCardToSlot);
            //InscryptionAPIPlugin.Logger.LogInfo($"Added PlaySelectedCardToSlot");
        }

        // offset to the start of the ilcode we want to remove
        int bonesIndex = codes.FindIndex(x => x.opcode == OpCodes.Callvirt && x.operand.ToString() == "Int32 get_BonesCost()") - 3;
        if (bonesIndex > 0)
        {
            int endIndex = codes.FindIndex(bonesIndex, x => x.opcode == OpCodes.Ldc_I4_6) + 4; // offset to the end of the code to remove
            codes.RemoveRange(bonesIndex, endIndex - bonesIndex);
            //InscryptionAPIPlugin.Logger.LogInfo("Removed dupe bone code");
        }
        int energyIndex = codes.FindIndex(x => x.opcode == OpCodes.Callvirt && x.operand.ToString() == "Int32 get_EnergyCost()") - 2;
        if (energyIndex > 0)
        {
            int endIndex = codes.FindIndex(energyIndex, x => x.opcode == OpCodes.Ldc_I4_7) + 4;
            codes.RemoveRange(energyIndex, endIndex - energyIndex);
            //InscryptionAPIPlugin.Logger.LogInfo($"Removed dupe energy code");
        }
        return codes;
    }
    /// <summary>
    /// Does two things:
    ///     1: Caches play costs before playing the card onto the board to prevent incorrect values being used should the card's CardInfo change.
    ///     2: Implements support for custom cards' OnPlay logic.
    /// </summary>
    /// <param name="instance">The PlayerHand object.</param>
    /// <param name="card">The PlayableCard being played to the board.</param>
    /// <param name="lastSelectedSlot">The last selected slot, equals BoardManage.Instance.LastSelectedSlot by default.</param>
    /// <returns></returns>
    public static IEnumerator PlaySelectedCardToSlot(PlayerHand instance, PlayableCard card, CardSlot lastSelectedSlot)
    {
        // keep track of the costs before the card is resolved on the board
        // this is to avoid situations where the card's CardInfo changes before we can actually spend resources
        // which can cause us to spend the incorrect amount
        // example would be a card that costs 1 Blood to play but evolves on resolve into a card that costs 4 Bones
        int bonesCost = card.BonesCost(), energyCost = card.EnergyCost;
        Dictionary<string, int> customCosts = new();
        foreach (CustomCardCost cost1 in card.GetCustomCardCosts())
        {
            customCosts.Add(cost1.CostName, card.GetCustomCostAmount(cost1.CostName));
        }

        yield return instance.PlayCardOnSlot(card, lastSelectedSlot);

        // Update the card cost ONLY IF it is now LOWER
        // This allows cards to dynamically impact price in between when played and
        // when you pay for it.
        // This behavior is somewhat counter-intuitive HOWEVER it is true to how
        // the game was originally coded

        // The game originally just had you pay whatever the card cost when it was
        // on the table; here, you pay the minimum of what it cost on the table
        // and what it cost in your hand. Why? Because someone could conceivably
        // create a card ability that causes cards to cost *more* than normal, and the
        // vanilla way of paying for costs could cause a conflict here.

        // This method stays true to the original game while allowing maximum flexibility

        if (bonesCost > 0)
        {
            int correctValue = Mathf.Min(bonesCost, card.BonesCost());
            yield return Singleton<ResourcesManager>.Instance.SpendBones(correctValue);
        }

        if (energyCost > 0)
        {
            int correctValue = Mathf.Min(energyCost, card.EnergyCost);
            yield return Singleton<ResourcesManager>.Instance.SpendEnergy(correctValue);
        }

        foreach (var cost2 in card.GetCustomCardCosts())
        {
            if (customCosts.ContainsKey(cost2.CostName))
            {
                int correctValue = Mathf.Min(customCosts[cost2.CostName], card.GetCustomCostAmount(cost2.CostName));
                yield return cost2.OnPlayed(correctValue, card);
            }
        }
    }
    #endregion

    #region Card Cost Choice Node

    [HarmonyPostfix, HarmonyPatch(typeof(CardSingleChoicesSequencer), nameof(CardSingleChoicesSequencer.GetCardbackTexture))]
    private static void GetCustomCostCardbackTexture(ref Texture __result, CardChoice choice)
    {
        // custom costs only
        if (choice.resourceType <= ResourceType.Gems)
            return;

        FullCardCost fullCost = AllCustomCosts.FirstOrDefault(x => x.ResourceType == choice.resourceType);
        if (fullCost != null)
            __result = fullCost.RewardBackTexture(choice.resourceAmount);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Part1CardChoiceGenerator), nameof(Part1CardChoiceGenerator.GenerateCostChoices))]
    private static void GenerateCustomCostChoices(ref List<CardChoice> __result, int randomSeed)
    {
        List<CardChoice> list = __result;

        // add energy choice
        if (GetRandomChoosableEnergyCard(randomSeed++) != null)
            list.Add(new CardChoice() { resourceType = ResourceType.Energy });

        // add Mox choice
        int moxIndex = GetRandomMoxIndex(randomSeed++);
        if (moxIndex > 0)
            list.Add(new CardChoice() { resourceType = ResourceType.Gems, resourceAmount = moxIndex });

        // add custom choices
        List<ResourceType> customResourceTypes = GuidManager.GetValues<ResourceType>();
        foreach (ResourceType resource in customResourceTypes)
        {
            List<FullCardCost> fullCosts = AllCustomCosts.FindAll(x => x.ResourceType == resource);
            if (fullCosts.Count > 0)
            {
                // if there's one custom cost and it has different choice amounts
                if (fullCosts.Count == 1 && fullCosts[0].ChoiceAmounts != null && fullCosts[0].ChoiceAmounts.Length > 0)
                {
                    // add a choice for each allowed amount
                    if (GetRandomChoosableCustomCostCardWithAmounts(randomSeed++, fullCosts[0], fullCosts[0].ChoiceAmounts) != null)
                    {
                        foreach (int amount in fullCosts[0].ChoiceAmounts)
                        {
                            list.Add(new() { resourceType = resource, resourceAmount = amount });
                        }
                    }
                }
                else if (GetRandomChoosableCustomCostCard(randomSeed++, fullCosts) != null)
                    list.Add(new() { resourceType = resource });
            }
        }

        while (list.Count > 3)
            list.RemoveAt(SeededRandom.Range(0, list.Count, randomSeed++));

        if (InscryptionAPIPlugin.configRandomChoiceOrder.Value)
            list = list.Randomize().ToList();

        __result = list;

    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardSingleChoicesSequencer), nameof(CardSingleChoicesSequencer.CostChoiceChosen))]
    private static IEnumerator CustomCostChoiceChosen(IEnumerator enumerator, CardSingleChoicesSequencer __instance, SelectableCard card)
    {
        // doesn't cost Energy, Gems, or custom
        if (card.ChoiceInfo.resourceType <= ResourceType.Bone)
        {
            yield return enumerator;
            yield break;
        }

        CardInfo cardInfo = null;
        if (card.ChoiceInfo.resourceType == ResourceType.Energy)
            cardInfo = GetRandomChoosableEnergyCard(SaveManager.SaveFile.GetCurrentRandomSeed());

        else if (card.ChoiceInfo.resourceType == ResourceType.Gems)
        {
            GemType gemType = card.ChoiceInfo.resourceAmount switch { 3 => GemType.Blue, 2 => GemType.Orange, _ => GemType.Green };
            cardInfo = GetRandomChoosableMoxCard(SaveManager.SaveFile.GetCurrentRandomSeed(), gemType);
        }
        else
        {
            List<FullCardCost> fullCosts = AllCustomCosts.FindAll(x => x.ResourceType == card.ChoiceInfo.resourceType);
            if (fullCosts.Count > 0)
            {
                // if there are multiple amounts
                if (fullCosts.Count == 1 && fullCosts[0].ChoiceAmounts != null && fullCosts[0].ChoiceAmounts.Length > 0)
                {
                    cardInfo = GetRandomChoosableCustomCostCardWithAmounts(SaveManager.SaveFile.GetCurrentRandomSeed(), fullCosts[0], card.ChoiceInfo.resourceAmount);
                }
                else
                    cardInfo = GetRandomChoosableCustomCostCard(SaveManager.SaveFile.GetCurrentRandomSeed(), fullCosts);
            }
        }

        card.SetInfo(cardInfo);
        card.SetFaceDown(false, false);
        card.SetInteractionEnabled(false);
        yield return __instance.TutorialTextSequence(card);
        card.SetCardbackToDefault();
        yield return __instance.WaitForCardToBeTaken(card);
        yield break;
    }

    public static CardInfo GetRandomChoosableEnergyCard(int randomSeed)
    {
        List<CardInfo> list = CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature).FindAll(x => x.energyCost > 0);
        return list.Count == 0 ? null : CardLoader.Clone(list[SeededRandom.Range(0, list.Count, randomSeed)]);
    }
    public static CardInfo GetRandomChoosableMoxCard(int randomSeed, GemType gem)
    {
        List<CardInfo> list = CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature).FindAll(x => x.gemsCost.Count > 0 && x.gemsCost.Contains(gem));
        return list.Count == 0 ? null : CardLoader.Clone(list[SeededRandom.Range(0, list.Count, randomSeed)]);
    }

    // chooses a random card that has at least 1 of the provided custom costs
    public static CardInfo GetRandomChoosableCustomCostCard(int randomSeed, List<FullCardCost> customCosts)
    {
        List<CardInfo> list = CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature)
            .FindAll(x => x.GetCustomCosts().Exists(y => customCosts.Contains(y)));

        InscryptionAPIPlugin.Logger.LogDebug($"Custom costs to search for: {customCosts.Count}. Found {list.Count} card(s).");
        return list.Count == 0 ? null : CardLoader.Clone(list[SeededRandom.Range(0, list.Count, randomSeed)]);
    }

    // chooses a random card whose cost value is the same as at least 1 of the provided amounts
    public static CardInfo GetRandomChoosableCustomCostCardWithAmounts(int randomSeed, FullCardCost customCost, params int[] costAmounts)
    {
        List<CardInfo> list = CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature)
            .FindAll(x => costAmounts.Contains(x.GetCustomCost(customCost)));

        InscryptionAPIPlugin.Logger.LogDebug($"Choice amounts for {customCost.CostName}: {costAmounts.Length}. Found {list.Count} card(s).");
        return list.Count == 0 ? null : CardLoader.Clone(list[SeededRandom.Range(0, list.Count, randomSeed)]);
    }
    public static int GetRandomMoxIndex(int randomSeed)
    {
        List<CardInfo> list = CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature).FindAll((CardInfo x) => x.gemsCost.Count > 0);
        if (list.Count == 0)
            return 0;

        List<int> moxIndeces = new();
        if (list.Exists(x => x.gemsCost.Contains(GemType.Green)))
            moxIndeces.Add(1);
        if (list.Exists(x => x.gemsCost.Contains(GemType.Orange)))
            moxIndeces.Add(2);
        if (list.Exists(x => x.gemsCost.Contains(GemType.Blue)))
            moxIndeces.Add(3);

        return moxIndeces[SeededRandom.Range(0, moxIndeces.Count, randomSeed)];
    }

    #endregion

    #endregion
}

/// <summary>
/// An inheritable class for implementing custom card costs.
/// </summary>
public abstract class CustomCardCost : ManagedBehaviour
{
    /// <summary>
    /// The internal name of the cost. Used to check for the cost's extended property by the API.
    /// </summary>
    public abstract string CostName { get; }
    public virtual int PlayHintFrequency { get; } = 1;
    public int playHintAttempts = 0;

    /// <summary>
    /// Whether the current PlayableCard can be played.
    /// </summary>
    /// <param name="cardCost">The resource amount needed to play a card with this cost.</param>
    /// <param name="playableCard">The PlayableCard currently being checked.</param>
    public virtual bool CostSatisfied(int cardCost, PlayableCard playableCard)
    {
        return true;
    }
    /// <summary>
    /// The dialogue string that will be played when you cannot play a card with this custom cost.
    /// </summary>
    /// <param name="cardCost">The resource amount needed to play a card with this cost.</param>
    /// <param name="playableCard">The PlayableCard currently being checked.</param>
    public virtual string CostUnsatisfiedHint(int cardCost, PlayableCard playableCard)
    {
        return null;
    }

    /// <summary>
    /// Represents how many stat points (SP) each cost value will give.
    /// For example, Bones have a 1:1 ratio for cost to stats, eg, 4 Bones will give 4 stat points.
    /// 
    /// DOES NOT ACTUALLY AFFECT ANYTHING ON ITS OWN. You must provide the logic that reads and uses this value.
    /// </summary>
    /// <param name="cardCost">The resource amount needed to play a card with this cost.</param>
    /// <param name="playableCard">The PlayableCard being checked.</param>
    /// <returns>How much SP the PlayableCard's cost value gives.</returns>
    public virtual int CostStatPointValue(int cardCost, PlayableCard playableCard)
    {
        return 0;
    }

    /// <summary>
    /// What the game should do when a card with this cost is played.
    /// Most common use case is implementing the logic for paying for this cost.
    /// </summary>
    /// <param name="cardCost">The resource amount needed to play a card with this cost.</param>
    /// <param name="playableCard">The PlayableCard currently being checked.</param>
    public virtual IEnumerator OnPlayed(int cardCost, PlayableCard playableCard)
    {
        yield break;
    }
}