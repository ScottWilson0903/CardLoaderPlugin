using DiskCardGame;
using HarmonyLib;

namespace InscryptionCommunityPatch.Card;

[HarmonyPatch]
internal class HiddenCharIndexFIx
{
    [HarmonyPatch(typeof(SequentialText), nameof(SequentialText.RemoveFirstHiddenChar))]
    [HarmonyPrefix]
    private static bool CheckForNegativeIndex(SequentialText __instance, ref int __result)
    {
        int num = __instance.GetText().IndexOf("<color=#00000000>");
        if (num != -1)
            __instance.SetText(__instance.GetText().Remove(num, 26));
        else
            num = 0;

        __result = num;
        return false;
    }
}
