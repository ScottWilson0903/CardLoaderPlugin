using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Dialogue;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;


[HarmonyPatch]
internal static class Act2ShapeshifterPatches
{

    [HarmonyPostfix, HarmonyPatch(typeof(Shapeshifter), nameof(Shapeshifter.RevealInBattle))]
    private static IEnumerator Act2RevealInBattle(IEnumerator result, Shapeshifter __instance)
    {
        if (SaveManager.SaveFile.IsPart2)
        {
            yield return new WaitForSeconds(0.25f);
            AudioController.Instance.PlaySound2D("trial_cave_outro#1", MixerGroup.TableObjectsSFX);
            yield return __instance.PlayableCard.TransformIntoCard(CardLoader.GetCardByName("Ijiraq"), null, __instance.PlayableCard.ClearAppearanceBehaviours);
            if (!DialogueEventsData.EventIsPlayed("IjiraqRevealed"))
            {
                yield return new WaitForSeconds(0.5f);
                yield return DialogueManager.PlayDialogueEventSafe("IjiraqRevealed", TextDisplayer.MessageAdvanceMode.Input);
            }
        }
        else
        {
            //PatchPlugin.Logger.LogInfo($"RevealInBattle1: Shapeshifter:{__instance != null} Card:{__instance.PlayableCard != null} Audio:{AudioController.Instance != null} Text:{TextDisplayer.Instance != null}");
            yield return result;
            //PatchPlugin.Logger.LogInfo($"RevealInBattle2: Shapeshifter:{__instance != null} Card:{__instance.PlayableCard != null} Audio:{AudioController.Instance != null} Text:{TextDisplayer.Instance != null}");
        }
    }

    [HarmonyPatch(typeof(Shapeshifter), nameof(Shapeshifter.DisguiseOutOfBattle))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PixelOutsideFix(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        MethodBase customMethod = AccessTools.Method(typeof(Act2ShapeshifterPatches), nameof(Act2ShapeshifterPatches.GetIjiraqDisguises));

        int endIndex = codes.FindIndex(x => x.opcode == OpCodes.Stloc_1);
        codes.RemoveRange(0, endIndex);
        codes.Insert(0, new(OpCodes.Callvirt, customMethod));
        //PatchPlugin.Logger.LogInfo($"Inserted GetIjiraqDisguises");
        return codes;
    }

    public static List<CardInfo> GetIjiraqDisguises()
    {
        if (SaveManager.SaveFile.IsPart2)
        {
            PatchPlugin.Logger.LogInfo($"GetIjiraqDisguises: Act2:{SaveData.Data.collection.CardInfos.Count}");
            return new(SaveData.Data.collection.CardInfos);
        }
        PatchPlugin.Logger.LogInfo($"GetIjiraqDisguises: DeckCount:{RunState.Run.playerDeck.Cards.Count}");
        return new(RunState.Run.playerDeck.Cards);
    }
}