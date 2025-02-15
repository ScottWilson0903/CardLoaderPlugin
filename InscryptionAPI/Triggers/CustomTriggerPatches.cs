using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Slots;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace InscryptionAPI.Triggers;

[HarmonyPatch]
internal static class CustomTriggerPatches
{
    [HarmonyPatch(typeof(PlayerHand), nameof(PlayerHand.AddCardToHand))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnAddedToHand(IEnumerator result, PlayableCard card)
    {
        yield return result;
        yield return card.TriggerHandler.Trigger<IOnAddedToHand>(x => x.RespondsToAddedToHand(), x => x.OnAddedToHand());
        yield return CustomTriggerFinder.TriggerAll<IOnOtherCardAddedToHand>(false, x => x.RespondsToOtherCardAddedToHand(card), x => x.OnOtherCardAddedToHand(card));
        yield break;
    }

    [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.DoCombatPhase))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnBellRung(IEnumerator result, bool playerIsAttacker)
    {
        yield return CustomTriggerFinder.TriggerAll<IOnBellRung>(false, x => x.RespondsToBellRung(playerIsAttacker), x => x.OnBellRung(playerIsAttacker));
        yield return result;
        yield break;
    }

    [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.SlotAttackSequence))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnSlotAttackSequence(IEnumerator result, CardSlot slot)
    {
        yield return CustomTriggerFinder.TriggerAll<IOnPreSlotAttackSequence>(false, x => x.RespondsToPreSlotAttackSequence(slot), x => x.OnPreSlotAttackSequence(slot));
        yield return result;
        yield return CustomTriggerFinder.TriggerAll<IOnPostSlotAttackSequence>(false, x => x.RespondsToPostSlotAttackSequence(slot), x => x.OnPostSlotAttackSequence(slot));
        yield break;
    }

    [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.SlotAttackSlot))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnPostSingularSlotAttackSlot(IEnumerator result, CardSlot attackingSlot, CardSlot opposingSlot)
    {
        yield return result;
        yield return CustomTriggerFinder.TriggerAll<IOnPostSingularSlotAttackSlot>(false, x => x.RespondsToPostSingularSlotAttackSlot(attackingSlot, opposingSlot), x => x.OnPostSingularSlotAttackSlot(attackingSlot, opposingSlot));
        yield break;
    }

    private static Type scaleChangedCoroutine;
    private static FieldInfo scaleChangedDamage;
    private static FieldInfo scaleChangedToPlayer;
    private static FieldInfo scaleChangedNumWeights;

    [HarmonyPatch(typeof(LifeManager), nameof(LifeManager.ShowDamageSequence))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnScalesChanged(IEnumerator result, int damage, int numWeights, bool toPlayer)
    {
        int initialDamage = damage;
        bool initialToPlayer = toPlayer;
        CustomTriggerFinder.CollectDataAll<IOnPreScalesChangedRef, int>(false, x => x.RespondsToPreScalesChangedRef(damage, numWeights, toPlayer), x =>
        {
            damage = x.CollectPreScalesChangedRef(damage, ref numWeights, ref toPlayer);
            if (damage < 0)
            {
                damage = -damage;
                toPlayer = !toPlayer;
            }
            return damage;
        });
        yield return CustomTriggerFinder.TriggerAll<IOnPreScalesChanged>(false, x => x.RespondsToPreScalesChanged(damage, toPlayer, initialDamage, initialToPlayer), x => x.OnPreScalesChanged(damage, toPlayer, initialDamage, initialToPlayer));
        //if (damage != 0)
        //{
        (scaleChangedDamage ??= (scaleChangedCoroutine ??= result?.GetType())?.GetField("damage"))?.SetValue(result, damage);
        (scaleChangedToPlayer ??= (scaleChangedCoroutine ??= result?.GetType())?.GetField("toPlayer"))?.SetValue(result, toPlayer);
        (scaleChangedNumWeights ??= (scaleChangedCoroutine ??= result?.GetType())?.GetField("numWeights"))?.SetValue(result, numWeights);
        yield return result;
        //}
        yield return CustomTriggerFinder.TriggerAll<IOnPostScalesChanged>(false, x => x.RespondsToPostScalesChanged(damage, toPlayer, initialDamage, initialToPlayer), x =>
            x.OnPostScalesChanged(damage, toPlayer, initialDamage, initialToPlayer));
        yield break;
    }

    [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.DoUpkeepPhase))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnUpkeepInHand(IEnumerator result, bool playerUpkeep)
    {
        yield return result;
        yield return CustomTriggerFinder.TriggerInHand<IOnUpkeepInHand>(x => x.RespondsToUpkeepInHand(playerUpkeep), x => x.OnUpkeepInHand(playerUpkeep));
        yield break;
    }

    [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.ResolveCardOnBoard))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnOtherCardResolveInHand(IEnumerator result, PlayableCard card, bool resolveTriggers = true)
    {
        yield return result;
        if (resolveTriggers)
        {
            yield return CustomTriggerFinder.TriggerInHand<IOnOtherCardResolveInHand>(x => x.RespondsToOtherCardResolveInHand(card), x => x.OnOtherCardResolveInHand(card));
        }
        yield break;
    }

    [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.PlayerTurn))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnTurnEndInHandPlayer(IEnumerator result)
    {
        yield return result;
        yield return CustomTriggerFinder.TriggerInHand<IOnTurnEndInHand>(x => x.RespondsToTurnEndInHand(true), x => x.OnTurnEndInHand(true));
        yield break;
    }

    [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.OpponentTurn))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnTurnEndInHandOpponent(IEnumerator result, TurnManager __instance)
    {
        bool turnSkipped = __instance.Opponent.SkipNextTurn;
        yield return result;
        if (!turnSkipped)
        {
            yield return CustomTriggerFinder.TriggerInHand<IOnTurnEndInHand>(x => x.RespondsToTurnEndInHand(false), x => x.OnTurnEndInHand(false));
        }
        yield break;
    }

    [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.AssignCardToSlot))]
    [HarmonyPostfix]
    private static IEnumerator TriggerOnOtherCardAssignedToSlotInHand(IEnumerator result, PlayableCard card, bool resolveTriggers)
    {
        CardSlot slot2 = card.Slot;
        yield return result;
        if (resolveTriggers && slot2 != card.Slot)
        {
            yield return CustomTriggerFinder.TriggerAll<IOnCardAssignedToSlotContext>(false, x => x.RespondsToCardAssignedToSlotContext(card, slot2, card.Slot), x =>
                x.OnCardAssignedToSlotContext(card, slot2, card.Slot));
        }
        if (resolveTriggers && slot2 != card.Slot)
        {
            yield return CustomTriggerFinder.TriggerInHand<IOnOtherCardAssignedToSlotInHand>(x => x.RespondsToOtherCardAssignedToSlotInHand(card), x => x.OnOtherCardAssignedToSlotInHand(card));
        }
        if (resolveTriggers && slot2 != card.Slot && slot2 != null)
        {
            yield return CustomTriggerFinder.TriggerAll<IOnCardAssignedToSlotNoResolve>(false, x => x.RespondsToCardAssignedToSlotNoResolve(card), x => x.OnCardAssignedToSlotNoResolve(card));
        }
        yield break;
    }

    private static FieldInfo triggerField;

    [HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.Die))]
    [HarmonyPostfix]
    private static IEnumerator TriggerDeathTriggers(IEnumerator result, PlayableCard __instance, bool wasSacrifice, PlayableCard killer = null)
    {
        CardSlot slotBeforeDeath = __instance.Slot;
        while (result.MoveNext())
        {
            yield return result.Current;
            if (result.Current.GetType() == triggerType)
            {
                Trigger t = Trigger.None;
                try
                {
                    t = (Trigger)(triggerField ??= triggerType.GetField("trigger")).GetValue(result.Current);
                }
                catch { }
                if (t == Trigger.OtherCardPreDeath)
                {
                    yield return CustomTriggerFinder.TriggerInHand<IOnOtherCardPreDeathInHand>(x => x.RespondsToOtherCardPreDeathInHand(slotBeforeDeath, !wasSacrifice, killer), x =>
                        x.OnOtherCardPreDeathInHand(slotBeforeDeath, !wasSacrifice, killer));
                }
                else if (t == Trigger.OtherCardDie)
                {
                    yield return CustomTriggerFinder.TriggerInHand<IOnOtherCardDieInHand>(x => x.RespondsToOtherCardDieInHand(__instance, slotBeforeDeath, !wasSacrifice, killer), x =>
                        x.OnOtherCardDieInHand(__instance, slotBeforeDeath, !wasSacrifice, killer));
                }
            }
        }
        yield break;
    }

    [HarmonyPatch(typeof(ConsumableItemSlot), nameof(ConsumableItemSlot.ConsumeItem))]
    [HarmonyPostfix]
    private static IEnumerator TriggerItemUse(IEnumerator result, ConsumableItemSlot __instance)
    {
        bool itemCanBeUsed = true;
        string consumableName = __instance?.Consumable?.Data?.name;
        if (!string.IsNullOrEmpty(consumableName))
        {
            CustomTriggerFinder.CollectDataAll<IItemCanBeUsed, bool>(false, x => x.RespondsToItemCanBeUsed(consumableName, itemCanBeUsed), x => itemCanBeUsed = x.CollectItemCanBeUsed(consumableName, itemCanBeUsed));
        }
        if (itemCanBeUsed)
        {
            bool successInActivation = false;
            Type activationtype = __instance.Consumable.ActivateSequence().GetType();
            if (!string.IsNullOrEmpty(consumableName))
            {
                yield return CustomTriggerFinder.TriggerAll<IOnPreItemUsed>(false, x => x.RespondsToPreItemUsed(consumableName, __instance is HammerItemSlot), x =>
                    x.OnPreItemUsed(consumableName, __instance is HammerItemSlot));
            }
            while (result.MoveNext())
            {
                yield return result.Current;
                if (result.Current.GetType() == activationtype)
                {
                    if (!string.IsNullOrEmpty(consumableName) && __instance.Consumable != null)
                    {
                        successInActivation = !__instance.Consumable.ActivationCancelled;
                    }
                }
            }
            if (!string.IsNullOrEmpty(consumableName))
            {
                yield return CustomTriggerFinder.TriggerAll<IOnPostItemUsed>(false, x => x.RespondsToPostItemUsed(consumableName, successInActivation, __instance is HammerItemSlot), x =>
                    x.OnPostItemUsed(consumableName, successInActivation, __instance is HammerItemSlot));
            }
        }
        else
        {
            __instance?.Consumable?.PlayShakeAnimation();
            if (!string.IsNullOrEmpty(consumableName))
            {
                yield return CustomTriggerFinder.TriggerAll<IOnItemPreventedFromUse>(false, x => x.RespondsToItemPreventedFromUse(consumableName), x => x.OnItemPreventedFromUse(consumableName));
            }
        }
        yield break;
    }

    [HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.GetPassiveAttackBuffs))]
    [HarmonyPostfix]
    private static void PassiveAttackBuffs(PlayableCard __instance, ref int __result)
    {
        int dummyResult = __result;
        CustomTriggerFinder.CollectDataAll<IOnCardPassiveAttackBuffs, int>(true, x => x.RespondsToCardPassiveAttackBuffs(__instance, dummyResult), x => dummyResult =
            x.CollectCardPassiveAttackBuffs(__instance, dummyResult));
        if (__instance.OnBoard)
        {
            CustomTriggerFinder.CollectDataAll<IPassiveAttackBuff, int>(true, x => true, x => dummyResult +=
                x.GetPassiveAttackBuff(__instance));
        }
        __result = dummyResult;
    }

    [HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.GetPassiveHealthBuffs))]
    [HarmonyPostfix]
    private static void PassiveHealthBuffs(PlayableCard __instance, ref int __result)
    {
        int dummyResult = __result;
        CustomTriggerFinder.CollectDataAll<IOnCardPassiveHealthBuffs, int>(true, x => x.RespondsToCardPassiveHealthBuffs(__instance, dummyResult), x => dummyResult =
            x.CollectCardPassiveHealthBuffs(__instance, dummyResult));
        if (__instance.OnBoard)
        {
            CustomTriggerFinder.CollectDataAll<IPassiveHealthBuff, int>(true, x => true, x => dummyResult +=
                x.GetPassiveHealthBuff(__instance));
        }
        __result = dummyResult;
    }

    [HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.GetOpposingSlots))]
    [HarmonyPrefix]
    private static bool OpposingSlotsPrefix(PlayableCard __instance, ref List<CardSlot> __result, ref int __state)
    {
        var all = CustomTriggerFinder.FindGlobalTriggers<ISetupAttackSequence>(true).ToList();
        all.RemoveAll(x => (x as TriggerReceiver) == null);
        all.Sort((x, x2) => x.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.ReplacesDefaultOpposingSlot, new(), new(), 0, false) -
            x2.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.ReplacesDefaultOpposingSlot, new(), new(), 0, false));
        bool didModify = false;
        bool discard = false;
        __state = 1;
        foreach (var opposing in all)
        {
            if (opposing.RespondsToModifyAttackSlots(__instance, OpposingSlotTriggerPriority.ReplacesDefaultOpposingSlot, new(), __result ?? new(), __state, false))
            {
                didModify = true;
                __result = opposing.CollectModifyAttackSlots(__instance, OpposingSlotTriggerPriority.ReplacesDefaultOpposingSlot, new List<CardSlot>(), __result ?? new(), ref __state, ref discard);
                discard = false;
            }
        }
        if (!didModify && __instance.HasAbility(Ability.AllStrike))
        {
            __state = Mathf.Max(1, (__instance.OpponentCard ? Singleton<BoardManager>.Instance.PlayerSlotsCopy : Singleton<BoardManager>.Instance.OpponentSlotsCopy).FindAll(x => x.Card != null &&
                !__instance.CanAttackDirectly(x)).Count);
        }
        if (didModify)
        {
            if (__instance.HasAbility(Ability.SplitStrike))
            {
                ProgressionData.SetAbilityLearned(Ability.SplitStrike);
                __result.Remove(__instance.Slot.opposingSlot);
                __result.AddRange(Singleton<BoardManager>.Instance.GetAdjacentSlots(__instance.Slot.opposingSlot));
            }
            if (__instance.HasTriStrike())
            {
                ProgressionData.SetAbilityLearned(Ability.TriStrike);
                __result.AddRange(Singleton<BoardManager>.Instance.GetAdjacentSlots(__instance.Slot.opposingSlot));
                if (!__result.Contains(__instance.Slot.opposingSlot))
                {
                    __result.Add(__instance.Slot.opposingSlot);
                }
            }
            if (__instance.HasAbility(Ability.DoubleStrike))
            {
                ProgressionData.SetAbilityLearned(Ability.DoubleStrike);
                __result.Add(__instance.slot.opposingSlot);
            }
        }
        if (__instance.HasAbility(Ability.SplitStrike))
        {
            __state += 1;
        }
        if (__instance.HasTriStrike())
        {
            __state += 2;
            if (__instance.HasAbility(Ability.SplitStrike))
            {
                __state += 1;
            }
        }
        if (__instance.HasAbility(Ability.DoubleStrike))
        {
            __state += 1;
        }
        __result?.Sort((CardSlot a, CardSlot b) => a.Index - b.Index);
        return !didModify;
    }

    [HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.GetOpposingSlots))]
    [HarmonyPostfix]
    private static void OpposingSlots(PlayableCard __instance, ref List<CardSlot> __result, int __state)
    {
        List<CardSlot> original = new(__result);
        bool isAttackingDefaultSlot = !__instance.HasTriStrike() && !__instance.HasAbility(Ability.SplitStrike);
        CardSlot defaultslot = __instance.Slot.opposingSlot;

        List<CardSlot> alteredOpposings = new();
        bool removeDefaultAttackSlot = false;

        foreach (IGetOpposingSlots component in CustomTriggerFinder.FindTriggersOnCard<IGetOpposingSlots>(__instance))
        {
            if ((component as TriggerReceiver) != null && component.RespondsToGetOpposingSlots())
            {
                alteredOpposings.AddRange(component.GetOpposingSlots(__result, new(alteredOpposings)));
                removeDefaultAttackSlot = removeDefaultAttackSlot || component.RemoveDefaultAttackSlot();
            }
        }

        if (alteredOpposings.Count > 0)
            __result.AddRange(alteredOpposings);

        if (isAttackingDefaultSlot && removeDefaultAttackSlot)
            __result.Remove(defaultslot);
        bool didRemoveOriginalSlot = __instance.HasAbility(Ability.SplitStrike) && (!__instance.HasTriStrike() || removeDefaultAttackSlot);
        var all = CustomTriggerFinder.FindGlobalTriggers<ISetupAttackSequence>(true).ToList();
        all.RemoveAll(x => (x as TriggerReceiver) == null);
        var dummyresult = __result; // used for sorting by trigger priority
        all.Sort((x, x2) => x.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.Normal, original, dummyresult, __state, didRemoveOriginalSlot) -
            x2.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.Normal, original, dummyresult, __state, didRemoveOriginalSlot));
        foreach (var opposing in all)
        {
            if (opposing.RespondsToModifyAttackSlots(__instance, OpposingSlotTriggerPriority.Normal, original, __result ?? new(), __state, didRemoveOriginalSlot))
            {
                __result = opposing.CollectModifyAttackSlots(__instance, OpposingSlotTriggerPriority.Normal, original, __result ?? new(), ref __state, ref didRemoveOriginalSlot);
            }
        }
        dummyresult = __result;
        all.Sort((x, x2) => x.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.BringsBackOpposingSlot, original, dummyresult, __state, didRemoveOriginalSlot) -
            x2.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.BringsBackOpposingSlot, original, dummyresult, __state, didRemoveOriginalSlot));
        foreach (var opposing in all)
        {
            if (opposing.RespondsToModifyAttackSlots(__instance, OpposingSlotTriggerPriority.BringsBackOpposingSlot, original, __result ?? new(), __state, didRemoveOriginalSlot))
            {
                __result = opposing.CollectModifyAttackSlots(__instance, OpposingSlotTriggerPriority.BringsBackOpposingSlot, original, __result ?? new(), ref __state, ref didRemoveOriginalSlot);
            }
        }
        dummyresult = __result;
        all.Sort((x, x2) => x.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.PostAdditionModification, original, dummyresult, __state, didRemoveOriginalSlot) -
            x2.GetTriggerPriority(__instance, OpposingSlotTriggerPriority.PostAdditionModification, original, dummyresult, __state, didRemoveOriginalSlot));
        foreach (var opposing in all)
        {
            if (opposing.RespondsToModifyAttackSlots(__instance, OpposingSlotTriggerPriority.PostAdditionModification, original, __result ?? new(), __state, didRemoveOriginalSlot))
            {
                __result = opposing.CollectModifyAttackSlots(__instance, OpposingSlotTriggerPriority.PostAdditionModification, original, __result ?? new(), ref __state, ref didRemoveOriginalSlot);
            }
        }
        if (didRemoveOriginalSlot && __instance.HasTriStrike())
        {
            __result.Add(__instance.Slot.opposingSlot);
        }
        __result.Sort((CardSlot a, CardSlot b) => a.Index - b.Index);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TurnManager), "PlayerTurn", MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> TriggerOnTurnEndInQueuePlayer(IEnumerable<CodeInstruction> instructions) =>
        TriggerOnTurnEndInQueue(instructions, true);

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TurnManager), "OpponentTurn", MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> TriggerOnTurnEndInQueueOpponent(IEnumerable<CodeInstruction> instructions) =>
        TriggerOnTurnEndInQueue(instructions, false);

    static IEnumerable<CodeInstruction> TriggerOnTurnEndInQueue(IEnumerable<CodeInstruction> instructions, bool playerTurn)
    {
        List<CodeInstruction> codes = instructions.ToList();

        int pointer = codes.IndexOf(codes.First(code => code.opcode == OpCodes.Callvirt && code.OperandIs(AccessTools.Method(typeof(GlobalTriggerHandler), nameof(GlobalTriggerHandler.TriggerCardsOnBoard)))));
        pointer++;

        codes.Insert(pointer++, new(playerTurn ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        codes.Insert(pointer++, new(OpCodes.Call, AccessTools.Method(typeof(CustomTriggerPatches), nameof(CustomTriggerPatches.TriggerOnTurnEndInQueueCoro))));

        return codes;
    }

    private static IEnumerator TriggerOnTurnEndInQueueCoro(IEnumerator originalTrigger, bool playerTurn)
    {
        yield return originalTrigger;

        foreach (IOnTurnEndInQueue trigger in CustomTriggerFinder.FindTriggersInQueue<IOnTurnEndInQueue>())
        {
            if ((trigger as TriggerReceiver) != null && trigger.RespondsToTurnEndInQueue(playerTurn))
                yield return trigger.OnTurnEndInQueue(playerTurn);
        }
    }

    [HarmonyPatch(typeof(GlobalTriggerHandler), nameof(GlobalTriggerHandler.TriggerNonCardReceivers)), HarmonyPostfix]
    private static IEnumerator TriggerSlotModificationHandlers(IEnumerator sequence, bool beforeCards, Trigger trigger, params object[] otherArgs)
    {
        yield return sequence;

        if (beforeCards)
        {
            // Trigger slot modifications
            List<SlotModificationBehaviour> slotModificationCache = new(SlotModificationManager.Instance?.SlotReceivers?.Select(kvp => kvp.Value.Item2) ?? Enumerable.Empty<SlotModificationBehaviour>());
            foreach (var slotMod in slotModificationCache)
            {
                if (slotMod != null && GlobalTriggerHandler.ReceiverRespondsToTrigger(trigger, slotMod, otherArgs))
                    yield return GlobalTriggerHandler.Instance.TriggerSequence(trigger, slotMod, otherArgs);
            }
        }
    }

    // IModifyAttackingSlots code can be found in DoCombatPhasePatches

    // IModifyDamageTaken and IPreTakeDamage logic can be found in TakeDamagePatches

    private static readonly Type triggerType = AccessTools.TypeByName("DiskCardGame.GlobalTriggerHandler+<TriggerCardsOnBoard>d__16");
}