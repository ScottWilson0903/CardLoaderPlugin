using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Card;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace InscryptionCommunityPatch.Card;

// Lets you add and remove decals via temporary mods
[HarmonyPatch]
internal class TempModDecalsFix
{
    // need to do it like
    [HarmonyPrefix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.RemoveTemporaryMod))]
    private static void RemoveTemporaryModsWithDecals(PlayableCard __instance, CardModificationInfo mod)
    {
        if (mod == null || !__instance.TemporaryMods.Contains(mod) || mod.DecalIds.Count == 0)
            return;

        CardModificationInfo cardModificationInfo = __instance.Info.Mods.Find(x => x.singletonId == mod.singletonId || x.DecalIds == mod.DecalIds);
        if (cardModificationInfo != null)
            __instance.Info.Mods.Remove(cardModificationInfo);
    }
    [HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.AddTemporaryMod))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AddTemporaryDecalsWithDecals(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        object modOperand = null;

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString() == "DiskCardGame.CardModificationInfo mod")
                modOperand = codes[i].operand;

            else if (codes[i].opcode == OpCodes.Endfinally)
            {
                int endIdx = i + 1;
                MethodInfo customMethod = AccessTools.Method(typeof(TempModDecalsFix), nameof(TempModDecalsFix.AddDecalIdsToMods),
                    new Type[] { typeof(PlayableCard), typeof(CardModificationInfo) });

                codes.Insert(endIdx, new(OpCodes.Ldarg_0));
                codes.Insert(endIdx + 1, new(OpCodes.Ldloc_0));
                codes.Insert(endIdx + 2, new(OpCodes.Ldfld, modOperand));
                codes.Insert(endIdx + 3, new(OpCodes.Call, customMethod));
                break;
            }
        }

        return codes;
    }
    private static void AddDecalIdsToMods(PlayableCard card, CardModificationInfo tempMod)
    {
        // only interested in adding decals, thank you very much
        if (tempMod.DecalIds.Count == 0)
            return;

        // remove any existing mods with this id
        if (!string.IsNullOrEmpty(tempMod.singletonId))
        {
            CardModificationInfo cardModificationInfo = card.Info.Mods.Find((CardModificationInfo x) => x.singletonId == tempMod.singletonId);
            if (cardModificationInfo != null)
                card.Info.Mods.Remove(cardModificationInfo);
        }

        CardModificationInfo decalMod = new()
        {
            decalIds = tempMod.DecalIds,
            singletonId = tempMod.singletonId
        };
        decalMod.SetTemporaryDecal();
        card.Info.Mods.Add(decalMod);
    }

    [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.CleanUp))]
    [HarmonyPostfix]
    private static IEnumerator ClearTempDecals(IEnumerator enumerator)
    {
        foreach (CardInfo info in SaveManager.SaveFile.CurrentDeck.Cards)
        {
            info.Mods.RemoveAll(x => x.IsTemporaryDecal());
        }
        yield return enumerator;
    }
}