using DiskCardGame;
using HarmonyLib;
using System.Collections;

namespace InscryptionCommunityPatch.Sequencers;

[HarmonyPatch]
internal class LeshyResetRedEyes
{
    [HarmonyPostfix, HarmonyPatch(typeof(TurnManager), nameof(TurnManager.CleanupPhase))]
    private static IEnumerator ResetLeshyEyes(IEnumerator enumerator, TurnManager __instance)
    {
        if (!PatchPlugin.configResetEyes.Value || !SaveManager.SaveFile.IsPart1 || __instance.opponent == null)
        {
            yield return enumerator;
            yield break;
        }

        bool resetEyes = false;
        if (__instance.opponent is ProspectorBossOpponent prospector)
            resetEyes = prospector.HasGrizzlyGlitchPhase(int.MinValue);

        else if (__instance.opponent is AnglerBossOpponent angler)
            resetEyes = angler.HasGrizzlyGlitchPhase(0);

        else if (__instance.opponent is TrapperTraderBossOpponent trapperTrader)
            resetEyes = trapperTrader.HasGrizzlyGlitchPhase(1);

        if (resetEyes)
            LeshyAnimationController.Instance?.ResetEyesTexture();

        yield return enumerator;
    }
}