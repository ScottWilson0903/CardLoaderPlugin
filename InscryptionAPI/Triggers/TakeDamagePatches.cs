using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Card;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace InscryptionAPI.Triggers;

// Patches to TakeDamage that add some more triggers and multi-shield support
[HarmonyPatch]
public static class TakeDamagePatches
{
    /*[HarmonyPostfix, HarmonyPatch(typeof(HammerItem), nameof(HammerItem.OnValidTargetSelected))]
    public static IEnumerator AddTakeDamageHammerTrigger(IEnumerator enumerator, HammerItem __instance, CardSlot targetSlot, GameObject firstPersonItem)
    {
        var preHammer = CustomTriggerFinder.FindGlobalTriggers<IOnPreTakeDamageFromHammer>(true).ToList();
        preHammer.Sort((a, b) => b.TriggerPriority(__instance, targetSlot, firstPersonItem) - a.TriggerPriority(__instance, targetSlot, firstPersonItem));
        foreach (var pre in preHammer)
        {
            if (pre != null && pre.RespondsToPreTakeDamageFromHammer(__instance, targetSlot, firstPersonItem))
                yield return pre.OnPreTakeDamageFromHammer(__instance, targetSlot, firstPersonItem);
        }

        bool struckTarget = targetSlot.Card != null;
        yield return enumerator;

        var postHammer = CustomTriggerFinder.FindGlobalTriggers<IOnPostTakeDamageFromHammer>(true).ToList();
        postHammer.Sort((a, b) => b.TriggerPriority(__instance, targetSlot, firstPersonItem, struckTarget) - a.TriggerPriority(__instance, targetSlot, firstPersonItem, struckTarget));
        foreach (var post in postHammer)
        {
            if (post != null && post.RespondsToPostTakeDamageFromHammer(__instance, targetSlot, firstPersonItem, struckTarget))
                yield return post.OnPostTakeDamageFromHammer(__instance, targetSlot, firstPersonItem, struckTarget);
        }
    }*/
    [HarmonyPrefix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.TakeDamage))]
    private static bool AddModifyDamageTrigger(PlayableCard __instance, ref int damage, PlayableCard attacker)
    {
        int originalDamage = damage;

        List<IModifyDamageTaken> modifyTakeDamage = CustomTriggerFinder.FindGlobalTriggers<IModifyDamageTaken>(true).ToList();
        modifyTakeDamage.Sort((a, b) => b.TriggerPriority(__instance, originalDamage, attacker) - a.TriggerPriority(__instance, originalDamage, attacker));
        foreach (IModifyDamageTaken modify in modifyTakeDamage)
        {
            if (modify != null && modify.RespondsToModifyDamageTaken(__instance, damage, attacker, originalDamage))
                damage = modify.OnModifyDamageTaken(__instance, damage, attacker, originalDamage);
        }

        // no negative damage
        if (damage < 0)
            damage = 0;

        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.TakeDamage))]
    private static IEnumerator AddTakeDamageTriggers(IEnumerator enumerator, PlayableCard __instance, int damage, PlayableCard attacker)
    {
        foreach (IPreTakeDamage pre in CustomTriggerFinder.FindTriggersOnCard<IPreTakeDamage>(__instance))
        {
            if (pre.RespondsToPreTakeDamage(attacker, damage))
                yield return pre.OnPreTakeDamage(attacker, damage);
        }

        yield return enumerator;
        if (__instance == null) yield break;

        if (!__instance.HasShield() && attacker != null)
        {
            yield return CustomTriggerFinder.TriggerInHand<IOnOtherCardDealtDamageInHand>(
            x => x.RespondsToOtherCardDealtDamageInHand(attacker, attacker.Attack, __instance),
                x => x.OnOtherCardDealtDamageInHand(attacker, attacker.Attack, __instance));
        }
    }

    private const string name_Damage = "System.Int32 damage";
    private const string name_TriggerHandler = "DiskCardGame.CardTriggerHandler get_TriggerHandler()";
    private const string name_CardAttacker = "DiskCardGame.PlayableCard attacker";

    [HarmonyTranspiler, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.TakeDamage), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> TakeDamageTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        object hasShieldLabel = null;
        object damage = null;
        object damageLabel = null;
        object attacker = null;

        int shieldStart = -1, shieldEnd = -1;
        for (int i = 0; i < codes.Count; i++)
        {

            // grab the required operands, in order of appearance in the code
            if (shieldStart == -1 && codes[i].operand?.ToString() == "Boolean HasShield()")
            {
                shieldStart = i + 2;
                hasShieldLabel = codes[i + 1].operand;
                continue;
            }
            if (shieldEnd == -1 && codes[i].opcode == OpCodes.Br)
            {
                shieldEnd = i;
                continue;
            }
            if (damage == null && codes[i].operand?.ToString() == name_Damage)
            {
                damage = codes[i].operand;
                continue;
            }
            if (damageLabel == null && codes[i].operand?.ToString() == name_TriggerHandler && codes[i + 1].opcode == OpCodes.Ldc_I4_8)
            {
                // if (RespondsToTrigger(8) && damage > 0)
                //  ....

                i += 10;
                damageLabel = codes[i++].operand;
                codes.Insert(i++, new(OpCodes.Ldarg_0));
                codes.Insert(i++, new(OpCodes.Ldfld, damage));
                codes.Insert(i++, new(OpCodes.Ldc_I4_0));
                codes.Insert(i++, new(OpCodes.Cgt));
                codes.Insert(i++, new(OpCodes.Brfalse, damageLabel));
                continue;
            }
            if (codes[i].operand?.ToString() == name_CardAttacker)
            {
                attacker = codes[i].operand;
                if (shieldEnd > 0)
                {
                    CodeInstruction switch_ = codes.Find(x => x.opcode == OpCodes.Switch);
                    //switch_.WithLabels(breakShieldLabel);
                    object state = codes.Find(x => x.opcode == OpCodes.Stfld && x.operand.ToString() == "System.Int32 <>1__state").operand;
                    object current = codes.Find(x => x.opcode == OpCodes.Stfld && x.operand.ToString() == "System.Object <>2__current").operand;
                    // if (HasShield && damage > 0)
                    //   yield return TriggerBreakShield();
                    //   break;

                    // TriggerBreakShield
                    // this.current = TriggerBreakShield
                    // this.state = 7
                    // return true
                    // this.state = -1
                    // yield break (new label)

                    MethodBase breakShield = AccessTools.Method(typeof(ShieldManager), nameof(ShieldManager.TriggerBreakShield),
                        new Type[] { typeof(PlayableCard), typeof(int), typeof(PlayableCard) });

                    codes.RemoveRange(shieldStart, shieldEnd - shieldStart);

                    // && damage > 0
                    codes.Insert(shieldStart++, new(OpCodes.Ldarg_0));
                    codes.Insert(shieldStart++, new(OpCodes.Ldfld, damage));
                    codes.Insert(shieldStart++, new(OpCodes.Ldc_I4_0));
                    codes.Insert(shieldStart++, new(OpCodes.Cgt));
                    codes.Insert(shieldStart++, new(OpCodes.Brfalse, hasShieldLabel));

                    // TriggerBreakShield();
                    codes.Insert(shieldStart++, new(OpCodes.Ldarg_0));
                    codes.Insert(shieldStart++, new(OpCodes.Ldloc_1));
                    codes.Insert(shieldStart++, new(OpCodes.Ldarg_0));
                    codes.Insert(shieldStart++, new(OpCodes.Ldfld, damage));
                    codes.Insert(shieldStart++, new(OpCodes.Ldarg_0));
                    codes.Insert(shieldStart++, new(OpCodes.Ldfld, attacker));
                    codes.Insert(shieldStart++, new(OpCodes.Callvirt, breakShield));

                    // this.current = TriggerBreakShield
                    codes.Insert(shieldStart++, new(OpCodes.Stfld, current));
                    // this.state = 5
                    codes.Insert(shieldStart++, new(OpCodes.Ldarg_0));
                    codes.Insert(shieldStart++, new(OpCodes.Ldc_I4_4));
                    codes.Insert(shieldStart++, new(OpCodes.Stfld, state));
                    // return true
                    codes.Insert(shieldStart++, new(OpCodes.Ldc_I4_1));
                    codes.Insert(shieldStart++, new(OpCodes.Ret));
                    // this.state = -1
                    //generator.MarkLabel(breakShieldLabel);
                    /*CodeInstruction it = new(OpCodes.Ldarg_0);
                    it.labels.Add(breakShieldLabel);
                    codes.Insert(shieldStart++, it);
                    codes.Insert(shieldStart++, new(OpCodes.Ldc_I4_M1));
                    codes.Insert(shieldStart++, new(OpCodes.Stfld, state));*/
                    // yield break
                }
                break;
            }
        }
        return codes;
    }
}
