using GBC;
using HarmonyLib;

namespace InscryptionCommunityPatch.Card;

[HarmonyPatch]
internal class Act2HideStatsPatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(PixelCardDisplayer), nameof(PixelCardDisplayer.DisplayInfo))]
    private static void HideAttackAndHealth(PixelCardDisplayer __instance)
    {
        if (__instance?.info?.hideAttackAndHealth ?? false)
        {
            __instance.SetAttackHidden(true);
            __instance.SetHealthHidden(true);
        }
    }
}