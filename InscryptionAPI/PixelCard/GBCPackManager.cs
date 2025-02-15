using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace InscryptionAPI.PixelCard;

[HarmonyPatch]
public static class GBCPackManager
{
    /// <summary>
    /// This event runs every time a GBC Pack is opened. By adding listeners to this event, you can modify the possible choices after non-GBCPack and singleton cards have been removed.
    /// </summary>
    public static event Func<List<CardInfo>, List<CardInfo>> ModifyGBCPacks;

    [HarmonyTranspiler, HarmonyPatch(typeof(PackOpeningUI), nameof(PackOpeningUI.AssignInfoToCards))]
    private static IEnumerable<CodeInstruction> RemoveSingletons(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("GetPixelCards"))
            {
                MethodInfo customMethod = AccessTools.Method(typeof(GBCPackManager), nameof(GBCPackManager.ModifyPixelCards));
                codes[i].operand = customMethod;
                break;
            }
        }

        return codes;
    }
    private static List<CardInfo> ModifyPixelCards()
    {
        List<CardInfo> info = CardLoader.GetPixelCards();
        info.RemoveAll(x => x.onePerDeck && SaveManager.SaveFile.gbcData.collection.cardIds.Contains(x.name));
        return ModifyGBCPacks?.Invoke(info) ?? info;
    }
}
