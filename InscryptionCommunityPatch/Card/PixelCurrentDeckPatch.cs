using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;

[HarmonyPatch]
internal class PixelCurrentDeckPatch
{
    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.CurrentDeck), MethodType.Getter)]
    [HarmonyPostfix]
    private static void CheckForNegativeIndex(SaveFile __instance, ref DeckInfo __result)
    {
        if (__instance.IsPart2)
            __result = __instance.gbcData.deck;
    }
}
