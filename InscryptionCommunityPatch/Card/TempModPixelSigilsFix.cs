using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace InscryptionCommunityPatch.Card;

// Lets you add and remove decals via temporary mods
[HarmonyPatch]
public class TempModPixelSigilsFix
{
    [HarmonyTranspiler, HarmonyPatch(typeof(PixelCardAbilityIcons), nameof(PixelCardAbilityIcons.DisplayAbilities),
    new Type[] { typeof(CardRenderInfo), typeof(PlayableCard) })]
    private static IEnumerable<CodeInstruction> AddTemporaryMods(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Callvirt)
            {
                MethodInfo customMethod = AccessTools.Method(typeof(TempModPixelSigilsFix), nameof(TempModPixelSigilsFix.RenderTemporarySigils),
                    new Type[] { typeof(CardInfo), typeof(PlayableCard) });
                codes[i].operand = customMethod;
                codes.Insert(i, new(OpCodes.Ldarg_2));
                break;
            }
        }
        return codes;
    }

    public static List<Ability> RenderTemporarySigils(CardInfo info, PlayableCard card)
    {
        if (card == null)
            return info.Abilities;

        List<Ability> abilities = new(info.Abilities);
        List<Ability> tempAbilities = new();

        foreach (CardModificationInfo mod in card.RenderInfo.temporaryMods)
        {
            if (mod.singletonId != "paint") // ignore abilities added by Magnificus
                tempAbilities.AddRange(mod.abilities);
        }
        abilities.AddRange(tempAbilities);
        abilities = AbilitiesUtil.RemoveNonDistinctNonStacking(abilities);
        abilities.RemoveAll((Ability x) => card.RenderInfo.temporaryMods.Exists((CardModificationInfo m) => m.negateAbilities.Contains(x)));
        if (card.Status.hiddenAbilities != null)
            abilities.RemoveAll((Ability x) => card.Status.hiddenAbilities.Contains(x));

        return abilities;
    }
}