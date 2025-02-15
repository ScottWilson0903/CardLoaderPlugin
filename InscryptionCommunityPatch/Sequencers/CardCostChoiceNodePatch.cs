using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Helpers;
using UnityEngine;

namespace InscryptionCommunityPatch.Sequencers;

[HarmonyPatch]
internal static class ChoiceNodePatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(CardSingleChoicesSequencer), nameof(CardSingleChoicesSequencer.GetCardbackTexture))]
    private static void CardSingleChoicesSequencer_GetCardbackTexture(ref Texture __result, CardChoice choice)
    {
        switch (choice.resourceType)
        {
            case ResourceType.Energy:
                __result = TextureHelper.GetImageAsTexture("energyCost.png", typeof(ChoiceNodePatch).Assembly);
                break;
            case ResourceType.Gems:
                __result = TextureHelper.GetImageAsTexture(MoxTextureName(choice.resourceAmount), typeof(ChoiceNodePatch).Assembly);
                break;
        }
    }
    public static string MoxTextureName(int index) => "moxCost" + (index switch { 1 => "Green", 2 => "Orange", 3 => "Blue", _ => "" }) + ".png";
}