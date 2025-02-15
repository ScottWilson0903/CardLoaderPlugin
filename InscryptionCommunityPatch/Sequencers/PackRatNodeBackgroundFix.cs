using DiskCardGame;
using HarmonyLib;
using System.Reflection.Emit;

namespace InscryptionCommunityPatch.Sequencers;

// 'fixes' the pack rat card not being rare
[HarmonyPatch]
internal class PackRatNodeBackgroundFix
{
    [HarmonyTranspiler, HarmonyPatch(typeof(GainConsumablesSequencer), nameof(GainConsumablesSequencer.FullConsumablesSequence), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> FixRareBackground(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        // a part of the code block we want to remove can't be removed without breaking the ienum
        // so we cut around it
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString() == "DiskCardGame.SelectableCard GetComponent[SelectableCard]()")
            {
                int startIndex = i - 3;
                for (int j = i + 1; j < codes.Count; j++)
                {
                    if (codes[j].opcode == OpCodes.Stloc_2)
                    {
                        codes.RemoveRange(startIndex, j - 3 - startIndex);
                        break;
                    }
                }
                break;
            }
        }
        return codes;
    }
}