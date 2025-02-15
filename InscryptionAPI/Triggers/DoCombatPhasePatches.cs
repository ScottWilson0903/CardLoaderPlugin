using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Helpers.Extensions;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace InscryptionAPI.Triggers;

// Patches to DoCombatPhase that add negative damage support and modifying attack slots support
[HarmonyPatch]
public static class DoCombatPhasePatches
{
    private const string name_GetDamageDealt = "Int32 get_DamageDealtThisPhase()";
    private const string name_PlayerAttacker = "System.Boolean playerIsAttacker";
    private const string name_SpecialSequencer = "DiskCardGame.SpecialBattleSequencer specialSequencer";
    private const string name_SquirrelAttacker = "System.Boolean <attackedWithSquirrel>5__4";
    private const string name_CombatCurrent = "System.Object <>2__current";

    // make this public for modders wanting to modify it
    public static IEnumerator NegativeDamageSupport(CombatPhaseManager __instance, bool playerIsAttacker, SpecialBattleSequencer specialSequencer, bool attackedWithSquirrel)
    {
        // we want to implement logic for if damage is less than 0 (damage dealt to self)
        if (__instance.DamageDealtThisPhase != 0)
        {
            bool targetOfDamage = __instance.DamageDealtThisPhase < 0 ? !playerIsAttacker : playerIsAttacker;

            yield return new WaitForSeconds(0.4f);
            yield return __instance.VisualizeDamageMovingToScales(targetOfDamage);

            // calculate overkillDamage only if we've actually dealt damage to the opponent
            int overkillDamage = 0;
            if (playerIsAttacker && __instance.DamageDealtThisPhase > 0)
            {
                overkillDamage = Singleton<LifeManager>.Instance.Balance + __instance.DamageDealtThisPhase - 5;
                if (attackedWithSquirrel && overkillDamage >= 0)
                    AchievementManager.Unlock(Achievement.PART1_SPECIAL1);

                overkillDamage = Mathf.Max(0, overkillDamage);
            }

            // DamageDealtThisPhase has to be a positive number when calculating damage and other stats
            int absDamageDealt = Math.Abs(__instance.DamageDealtThisPhase);
            int damage = absDamageDealt - overkillDamage;
            AscensionStatsData.TryIncreaseStat(AscensionStat.Type.MostDamageDealt, absDamageDealt);
            if (absDamageDealt >= 666)
                AchievementManager.Unlock(Achievement.PART2_SPECIAL2);

            bool canDealDamage = specialSequencer == null || !specialSequencer.PreventDamageAddedToScales;
            if (canDealDamage)
                yield return Singleton<LifeManager>.Instance.ShowDamageSequence(damage, damage, !targetOfDamage, 0f);

            if (specialSequencer != null)
                yield return specialSequencer.DamageAddedToScale(damage + overkillDamage, targetOfDamage);

            if (canDealDamage && overkillDamage > 0 && Singleton<TurnManager>.Instance.Opponent.NumLives == 1 && Singleton<TurnManager>.Instance.Opponent.GiveCurrencyOnDefeat)
            {
                yield return Singleton<TurnManager>.Instance.Opponent.TryRevokeSurrender();
                RunState.Run.currency += overkillDamage;
                yield return __instance.VisualizeExcessLethalDamage(overkillDamage, specialSequencer);
            }
        }
    }

    public static List<CardSlot> ModifyAttackingSlots(bool playerIsAttacker)
    {
        List<CardSlot> originalSlots = BoardManager.Instance.GetSlotsCopy(playerIsAttacker);
        List<CardSlot> currentSlots = new(originalSlots);

        var triggers = CustomTriggerFinder.FindTriggersOnBoard<IGetAttackingSlots>(false).ToList();
        triggers.Sort((IGetAttackingSlots a, IGetAttackingSlots b) => b.TriggerPriority(playerIsAttacker, originalSlots) - a.TriggerPriority(playerIsAttacker, originalSlots));

        foreach (IGetAttackingSlots t in triggers)
        {
            if ((t as TriggerReceiver) != null && t.RespondsToGetAttackingSlots(playerIsAttacker, originalSlots, currentSlots))
            {
                List<CardSlot> addedSlots = t.GetAttackingSlots(playerIsAttacker, originalSlots, currentSlots);
                if (addedSlots != null && addedSlots.Count > 0)
                    currentSlots.AddRange(addedSlots);
            }
        }
        return currentSlots;
    }

    // We want to add support for negative values of DamageDealtThisPhase so modders can add self damage behaviours more easily
    [HarmonyTranspiler, HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.DoCombatPhase), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> SelfDamage(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        // we want to slowly narrow our search until we find exactly where we want to insert our code
        // we also want to grab the operands for important things we'll need
        object op_PlayerAttacker = null;
        object op_SpecialSequencer = null;
        object op_SquirrelAttacker = null;

        for (int a = 0; a < codes.Count; a++)
        {
            // change this:
            // attackingSlots = (playerIsAttacker ? PlayerSlotsCopy : OpponentSlotsCopy);

            // to this:
            // attackingSlots = ModifyAttackingSlots(playerIsAttacker);
            if (codes[a].opcode == OpCodes.Brtrue)
            {
                MethodBase customMethod = AccessTools.Method(typeof(DoCombatPhasePatches), nameof(DoCombatPhasePatches.ModifyAttackingSlots),
                    new Type[] { typeof(bool) });

                for (int b = a; b < codes.Count; b++)
                {
                    if (codes[b].opcode == OpCodes.Stfld)
                    {
                        codes.RemoveRange(a, b - a);
                        break;
                    }
                }
                codes.Insert(a, new(OpCodes.Call, customMethod));
                break;
            }
        }
        for (int i = 0; i < codes.Count; i++)
        {
            if (op_PlayerAttacker == null && codes[i].operand?.ToString() == name_PlayerAttacker)
                op_PlayerAttacker = codes[i].operand;

            else if (op_SpecialSequencer == null && codes[i].operand?.ToString() == name_SpecialSequencer)
                op_SpecialSequencer = codes[i].operand;

            else if (op_SquirrelAttacker == null && codes[i].operand?.ToString() == name_SquirrelAttacker)
                op_SquirrelAttacker = codes[i].operand;

            else if (codes[i].operand?.ToString() == name_GetDamageDealt)
            {
                int startIndex = i - 1;

                for (int j = i; j < codes.Count; j++)
                {
                    // we need to preserve state changes or the whole thing will break
                    // the code we want to replace ends after the state changes to 11
                    // so we look ahead for an 11
                    if (codes[j + 2].opcode == OpCodes.Ldc_I4_S && codes[j + 2].operand.ToString() == "11")
                    {
                        MethodBase customMethod = AccessTools.Method(typeof(DoCombatPhasePatches), nameof(DoCombatPhasePatches.NegativeDamageSupport),
                            new Type[] { typeof(CombatPhaseManager), typeof(bool), typeof(SpecialBattleSequencer), typeof(bool) });

                        // remove codes marked with Nop
                        codes.RemoveAll(x => x.opcode == OpCodes.Nop);

                        // change startIndex.opcode to Ldarg_0 for the stfld that will immediately follow the callvirt, then add a new ldloc_2
                        codes[startIndex].opcode = OpCodes.Ldarg_0;
                        codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldloc_2));
                        // this.playerIsAttacker
                        codes.Insert(startIndex + 2, new CodeInstruction(OpCodes.Ldarg_0));
                        codes.Insert(startIndex + 3, new CodeInstruction(OpCodes.Ldfld, op_PlayerAttacker));
                        // this.specialSequencer
                        codes.Insert(startIndex + 4, new CodeInstruction(OpCodes.Ldarg_0));
                        codes.Insert(startIndex + 5, new CodeInstruction(OpCodes.Ldfld, op_SpecialSequencer));
                        // this.attackedWithSquirrel
                        codes.Insert(startIndex + 6, new CodeInstruction(OpCodes.Ldarg_0));
                        codes.Insert(startIndex + 7, new CodeInstruction(OpCodes.Ldfld, op_SquirrelAttacker));
                        // NegativeDamageSupport(ldloc.2, this.playerIsAttacker, this.specialSequencer, this.attackedWithSquirrel)
                        codes.Insert(startIndex + 8, new CodeInstruction(OpCodes.Callvirt, customMethod));
                        return codes;
                    }
                    else
                    {
                        // we want to preserve all labels so none of the if-elses break
                        // then mark the code as Nop so we can remove it later
                        codes[j].MoveLabelsTo(codes[startIndex]);
                        codes[j].opcode = OpCodes.Nop;
                    }
                }
            }
        }

        return codes;
    }
}