using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Helpers;
using InscryptionAPI.Helpers.Extensions;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace InscryptionAPI.Triggers;

// Inserts a null check for opposingSlot.Card after CardGettingAttacked is triggered
// Tweaks the direct damage logic to subtract from DamageDealtThisPhase if the opposing and attacking are on the same side
[HarmonyPatch]
public static class SlotAttackSlotPatches
{
    private const string name_GlobalTrigger = "DiskCardGame.GlobalTriggerHandler get_Instance()";
    private const string name_GetCard = "DiskCardGame.PlayableCard get_Card()";
    private const string name_GetAttack = "Int32 get_Attack()";
    private const string name_OpposingSlot = "DiskCardGame.CardSlot opposingSlot";
    private const string name_CombatPhase = "DiskCardGame.CombatPhaseManager+<>c__DisplayClass5_0 <>8__1";
    private const string name_AttackingSlot = "DiskCardGame.CardSlot attackingSlot";
    private const string name_CanAttackDirectly = "Boolean CanAttackDirectly(DiskCardGame.CardSlot)";
    private const string name_VisualizeCardAttackingDirectly = "System.Collections.IEnumerator VisualizeCardAttackingDirectly(DiskCardGame.CardSlot, DiskCardGame.CardSlot, Int32)";

    private const string modifiedAttackCustomField = "modifiedAttack";

    private static readonly MethodInfo method_GetCard = AccessTools.PropertyGetter(typeof(CardSlot), nameof(CardSlot.Card));

    private static readonly MethodInfo method_NewDamage = AccessTools.Method(typeof(SlotAttackSlotPatches), nameof(SlotAttackSlotPatches.DamageToDealThisPhase),
        new Type[] { typeof(CardSlot), typeof(CardSlot) });

    private static readonly MethodInfo method_NewTriggers = AccessTools.Method(typeof(SlotAttackSlotPatches), nameof(SlotAttackSlotPatches.TriggerOnDirectDamageTriggers),
        new Type[] { typeof(PlayableCard), typeof(CardSlot) });

    private static readonly MethodInfo method_SetCustomField = AccessTools.Method(typeof(CustomFields), nameof(CustomFields.Set));

    private static readonly MethodInfo method_GetCustomField = AccessTools.Method(typeof(CustomFields), nameof(CustomFields.Get),
        new Type[] { typeof(object), typeof(string) }, new Type[] { typeof(int) });

    // make this public so people can alter it themselves
    public static int DamageToDealThisPhase(CardSlot attackingSlot, CardSlot opposingSlot)
    {
        int originalDamage = attackingSlot.Card.Attack;
        int damage = originalDamage;

        // Trigger IModifyDirectDamage first and treat the new damage as the attacking card's attack
        var modifyDirectDamage = CustomTriggerFinder.FindGlobalTriggers<IModifyDirectDamage>(true).ToList();
        modifyDirectDamage.Sort((a, b) =>
            b.TriggerPriority(opposingSlot, damage, attackingSlot.Card)
            - a.TriggerPriority(opposingSlot, damage, attackingSlot.Card)
        );

        foreach (var modify in modifyDirectDamage)
        {
            if (modify.RespondsToModifyDirectDamage(opposingSlot, damage, attackingSlot.Card, originalDamage))
                damage = modify.OnModifyDirectDamage(opposingSlot, damage, attackingSlot.Card, originalDamage);
        }

        // first thing we check for is self-damage; if the attacking slot is on the same side as the opposing slot, deal self-damage
        if (attackingSlot.IsPlayerSlot == opposingSlot.IsPlayerSlot)
            return -damage;

        // this is some new stuff to account for out-of-turn damage
        else if (TurnManager.Instance.IsPlayerTurn)
        {
            // if an opponent is attacking during the player's turn, deal positive/negative damage depending on what slot is being attacked
            if (attackingSlot.IsOpponentSlot())
                return opposingSlot.IsPlayerSlot ? -damage : damage;
        }
        else if (attackingSlot.IsPlayerSlot)
        {
            // if a player is attacking during the opponent's turn, deal positive/negative damage depending on what slot is being attacked
            return opposingSlot.IsOpponentSlot() ? -damage : damage;
        }

        return damage;
    }

    // We want to add a null check after CardGettingAttacked is triggered, so we'll look for triggers
    [HarmonyTranspiler, HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.SlotAttackSlot), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        // we want to slowly narrow our search until we find exactly where we want to insert our code
        for (int a = 0; a < codes.Count; a++)
        {
            // separated into their own methods so I can save on eye strain and brain fog
            if (ModifyDirectDamageCheck(codes, ref a))
            {
                for (int b = a; b < codes.Count; b++)
                {
                    if (CallTriggerOnDirectDamage(codes, ref b))
                    {
                        for (int c = b; c < codes.Count; c++)
                        {
                            if (OpposingCardNullCheck(codes, c))
                                break;
                        }
                        break;
                    }
                }
                break;
            }
        }

        return codes;
    }

    private static bool OpposingCardNullCheck(List<CodeInstruction> codes, int i)
    {
        // Looking for where GlobalTriggerHandler is called for CardGettingAttacked (enum 7)
        if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString() == name_GlobalTrigger && codes[i + 1].opcode == OpCodes.Ldc_I4_7)
        {
            MethodInfo op_Inequality = null;
            object op_OpposingSlot = null;
            object op_GetCard = null;
            object op_BreakLabel = null;

            for (int j = i + 1; j < codes.Count; j++)
            {
                // we need to get the operand for opposingSlot so we can insert it later
                if (codes[j].opcode == OpCodes.Ldfld && codes[j].operand.ToString() == name_OpposingSlot && op_OpposingSlot == null)
                    op_OpposingSlot = codes[j].operand;

                // also get the operand for get_card
                if (codes[j].opcode == OpCodes.Callvirt && codes[j].operand.ToString() == name_GetCard && op_GetCard == null)
                    op_GetCard = codes[j].operand;

                // if true, we've found the start of the if statement we'll be nesting in
                if (codes[j].opcode == OpCodes.Stfld && codes[j - 1].opcode == OpCodes.Ldc_I4_M1)
                {
                    for (int k = j + 1; k < codes.Count; k++)
                    {
                        // we want to grab the operand for !=
                        if (codes[k].opcode == OpCodes.Call && op_Inequality == null)
                            op_Inequality = (MethodInfo)codes[k].operand;

                        // we also want to grab the break label so we know where to jump
                        if (op_BreakLabel == null && codes[k].opcode == OpCodes.Brfalse)
                            op_BreakLabel = codes[k].operand;

                        // if true, we've found where we want to insert our junk
                        if (codes[k].opcode == OpCodes.Stfld)
                        {
                            // if (this.opposingSlot.Card != null)
                            codes.Insert(k + 1, new CodeInstruction(OpCodes.Ldarg_0));
                            codes.Insert(k + 2, new CodeInstruction(OpCodes.Ldfld, op_OpposingSlot));
                            codes.Insert(k + 3, new CodeInstruction(OpCodes.Callvirt, op_GetCard));
                            codes.Insert(k + 4, new CodeInstruction(OpCodes.Ldnull));
                            codes.Insert(k + 5, new CodeInstruction(OpCodes.Call, op_Inequality));
                            codes.Insert(k + 6, new CodeInstruction(OpCodes.Brfalse, op_BreakLabel));
                            return true;
                        }
                    }
                    break;
                }
            }
        }

        return false;
    }

    // Modifies direct attack damage and stores the new damage in a custom field
    private static bool ModifyDirectDamageCheck(List<CodeInstruction> codes, ref int index)
    {
        if (codes[index].opcode == OpCodes.Callvirt && codes[index].operand.ToString() == name_CanAttackDirectly)
        {
            int startIndex = index + 2;

            object op_OpposingSlot = null;
            object op_DisplayClass = null;
            object op_AttackingSlot = null;

            // look backwards and retrieve opposingSlot
            for (int i = index - 1; i > 0; i--)
            {
                if (op_OpposingSlot == null && codes[i].operand?.ToString() == name_OpposingSlot)
                {
                    op_OpposingSlot = codes[i].operand;
                    break;
                }
            }

            // look forward and retrieve displayClass and attackingSlot
            for (int i = startIndex; i < codes.Count; i++)
            {
                if (op_DisplayClass == null && codes[i].operand?.ToString() == name_CombatPhase)
                {
                    op_DisplayClass = codes[i].operand;
                    op_AttackingSlot = codes[i + 1].operand;
                    break;
                }
            }

            int j = startIndex;

            // CustomFields.Set(this.CombatPhase.AttackingSlot.Card, "modifiedAttack", DamageToDealThisPhase(this.CombatPhase.AttackingSlot, this.OpposingSlot));
            codes.Insert(j++, new(OpCodes.Ldarg_0));
            codes.Insert(j++, new(OpCodes.Ldfld, op_DisplayClass));
            codes.Insert(j++, new(OpCodes.Ldfld, op_AttackingSlot));
            codes.Insert(j++, new(OpCodes.Callvirt, method_GetCard));
            codes.Insert(j++, new(OpCodes.Ldstr, modifiedAttackCustomField));
            codes.Insert(j++, new(OpCodes.Ldarg_0));
            codes.Insert(j++, new(OpCodes.Ldfld, op_DisplayClass));
            codes.Insert(j++, new(OpCodes.Ldfld, op_AttackingSlot));
            codes.Insert(j++, new(OpCodes.Ldarg_0));
            codes.Insert(j++, new(OpCodes.Ldfld, op_OpposingSlot));
            codes.Insert(j++, new(OpCodes.Callvirt, method_NewDamage));
            codes.Insert(j++, new(OpCodes.Box, typeof(int)));
            codes.Insert(j++, new(OpCodes.Call, method_SetCustomField));

            index = j;

            // replace the next 2 occurances of get_Attack() with the custom field call
            for (int c = 0; c < 2; c++)
            {
                for (; j < codes.Count; j++)
                {
                    if (codes[j].opcode != OpCodes.Callvirt || codes[j].operand.ToString() != name_GetAttack) continue;

                    codes[j++] = new(OpCodes.Ldstr, modifiedAttackCustomField);
                    codes.Insert(j++, new(OpCodes.Callvirt, method_GetCustomField));

                    index = j;
                    break;
                }
            }

            return true;
        }
        return false;
    }

    // Repalces the DealDamageDirectly trigger call with a call to TriggerOnDirectDamageTriggers
    private static bool CallTriggerOnDirectDamage(List<CodeInstruction> codes, ref int index)
    {
        if (codes[index].opcode != OpCodes.Callvirt || codes[index].operand.ToString() != name_GetCard) return false;
        int startIndex = ++index;

        object op_OpposingSlot = null;

        // look backwards and retrieve opposingSlot
        for (int i = startIndex - 1; i > 0; i--)
        {
            if (op_OpposingSlot == null && codes[i].operand?.ToString() == name_OpposingSlot)
            {
                op_OpposingSlot = codes[i].operand;
                break;
            }
        }

        // look backwards for ldarg0 and duplicate it
        for (int i = startIndex - 1; i > 0; i--)
        {
            if (codes[i].opcode == OpCodes.Ldarg_0)
            {
                codes.Insert(i, new(OpCodes.Ldarg_0));

                index++;
                startIndex++;

                break;
            }
        }

        for (int i = startIndex; i < codes.Count; i++)
        {
            if (codes[i].opcode != OpCodes.Ldc_I4_7) continue;

            // remove the existing trigger call
            codes.RemoveRange(startIndex, i - startIndex - 2);

            // insert the call to our new trigger
            codes.Insert(index++, new(OpCodes.Ldarg_0));
            codes.Insert(index++, new(OpCodes.Ldfld, op_OpposingSlot));
            codes.Insert(index++, new(OpCodes.Callvirt, method_NewTriggers));

            break;
        }

        return true;
    }

    // Trigger both the vanilla trigger and the new trigger
    private static IEnumerator TriggerOnDirectDamageTriggers(PlayableCard attacker, CardSlot opposingSlot)
    {
        int damage = CustomFields.Get<int>(attacker, modifiedAttackCustomField);

        // trigger the vanilla trigger
        if (attacker.TriggerHandler.RespondsToTrigger(Trigger.DealDamageDirectly, new object[] { damage }))
        {
            yield return attacker.TriggerHandler.OnTrigger(Trigger.DealDamageDirectly, new object[] { damage });
        }

        // trigger the new modded trigger
        yield return CustomTriggerFinder.TriggerAll<IOnCardDealtDamageDirectly>(false, x => x.RespondsToCardDealtDamageDirectly(attacker, opposingSlot, damage), x => x.OnCardDealtDamageDirectly(attacker, opposingSlot, damage));
    }
}