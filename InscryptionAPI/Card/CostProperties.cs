using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace InscryptionAPI.Card.CostProperties;

[HarmonyPatch]
public static class CostProperties
{
    public class RefreshCostMonoBehaviour : MonoBehaviour
    {
        public PlayableCard playableCard;
        private int cachedBloodCost = -1, cachedBoneCost = -1;
        private readonly List<GemType> cachedGemsCost = new();

        private void LateUpdate()
        {
            if (DidCostsChangeThisFrame())
                playableCard.RenderCard();
        }

        private bool DidCostsChangeThisFrame()
        {
            // update all cachedCosts before returning boolean
            bool refreshCost = false;

            int bloodCost = playableCard?.BloodCost() ?? 0;
            if (bloodCost != cachedBloodCost)
            {
                cachedBloodCost = bloodCost;
                refreshCost = true;
            }

            int boneCost = playableCard.BonesCost();
            if (boneCost != cachedBoneCost)
            {
                cachedBoneCost = boneCost;
                refreshCost = true;
            }

            List<GemType> gemsCost = playableCard.GemsCost();
            if (GemsChanged(cachedGemsCost, gemsCost))
            {
                cachedGemsCost.Clear();
                cachedGemsCost.AddRange(gemsCost);
                refreshCost = true;
            }

            return refreshCost;
        }

        public bool GemsChanged<T>(List<T> a, List<T> b)
        {
            if (a.Count != b.Count)
                return true;

            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                    return true;
            }

            return false;
        }
    }

    public static ConditionalWeakTable<CardInfo, List<WeakReference<PlayableCard>>> CardInfoToCard = new();

    /// <summary>
    /// ChangeCardCostGetter patches BloodCost so we can change the cost on the fly
    /// This reverse patch gives us access to the original method without any changes.
    /// This method has a copy of all the code that CardInfo.BloodCost had so it doesn't result in a StackOverflow and freezing the game when called.
    /// </summary>
    [HarmonyReversePatch, HarmonyPatch(typeof(CardInfo), nameof(CardInfo.BloodCost), MethodType.Getter), MethodImpl(MethodImplOptions.NoInlining)]
    public static int OriginalBloodCost(CardInfo __instance) { return 0; }

    /// <summary>
    /// ChangeCardCostGetter patches BoneCost so we can change the cost on the fly
    /// This reverse patch gives us access to the original method without any changes.
    /// This method has a copy of all the code that CardInfo.BoneCost had so it doesn't result in a StackOverflow and freezing the game when called.
    /// </summary>
    [HarmonyReversePatch, HarmonyPatch(typeof(CardInfo), nameof(CardInfo.BonesCost), MethodType.Getter), MethodImpl(MethodImplOptions.NoInlining)]
    public static int OriginalBonesCost(CardInfo __instance) { return 0; }

    /// <summary>
    /// ChangeCardCostGetter patches GemsCost so we can change the cost on the fly
    /// This reverse patch gives us access to the original method without any changes.
    /// This method has a copy of all the code that CardInfo.GemsCost had so it doesn't result in a StackOverflow and freezing the game when called.
    /// </summary>
    [HarmonyReversePatch, HarmonyPatch(typeof(CardInfo), nameof(CardInfo.GemsCost), MethodType.Getter), MethodImpl(MethodImplOptions.NoInlining)]
    public static List<GemType> OriginalGemsCost(CardInfo __instance) { return null; }

    /// <summary>
    /// Improved version of CardInfo.GemsCost that accounts for addGemCost and RemovedGemsCost().
    /// </summary>
    /// <remarks>For consistency's sake, it's recommended you use this method over OriginalGemsCost in most cases.</remarks>
    /// <param name="instance"></param>
    public static List<GemType> ImprovedGemsCost(CardInfo instance)
    {
        if (instance.Mods.Exists(x => x.nullifyGemsCost))
            return new();

        List<GemType> gems = new(instance.gemsCost);
        foreach (CardModificationInfo mod in instance.Mods)
        {
            if (mod.addGemCost != null)
            {
                gems.AddRange(mod.addGemCost);
            }
            foreach (GemType gem in mod.RemovedGemsCosts())
            {
                gems.Remove(gem);
            }
        }
        return gems;
    }

    /// <summary>
    /// ChangeCardCostGetter patches EnergyCost so we can change the cost on the fly
    /// This reverse patch gives us access to the original method without any changes.
    /// This method has a copy of all the code that CardInfo.EnergyCost had so it doesn't result in a StackOverflow and freezing the game when called.
    /// </summary>
    [HarmonyReversePatch, HarmonyPatch(typeof(CardInfo), nameof(CardInfo.EnergyCost), MethodType.Getter), MethodImpl(MethodImplOptions.NoInlining)]
    public static int OriginalEnergyCost(CardInfo __instance) { return 0; }

    public static bool ReduceGemifiedBlood(PlayableCard card, int? bloodCost = null)
    {
        return (bloodCost ?? OriginalBloodCost(card.Info)) > 0 && !ReduceGemifiedMox(card) && !ReduceGemifiedBones(card) && !ReduceGemifiedMox(card);
    }
    public static bool ReduceGemifiedMox(PlayableCard card, List<GemType> gemsCost = null)
    {
        return (gemsCost?.Count ?? ImprovedGemsCost(card.Info).Count) > 0 && !ReduceGemifiedBones(card) && !ReduceGemifiedEnergy(card);
    }
    public static bool ReduceGemifiedBones(PlayableCard card, int? bonesCost = null)
    {
        return (bonesCost ?? OriginalBonesCost(card.Info)) > 0 && !ReduceGemifiedEnergy(card);
    }
    public static bool ReduceGemifiedEnergy(PlayableCard card, int? energyCost = null)
    {
        return (energyCost ?? OriginalEnergyCost(card.Info)) > 0;
    }
}

[HarmonyPatch]
internal static class ChangeCardCostGetter
{
    [HarmonyPatch(typeof(CardInfo), nameof(CardInfo.BloodCost), MethodType.Getter), HarmonyPrefix]
    public static bool BloodCost(CardInfo __instance, ref int __result)
    {
        PlayableCard card = __instance.GetPlayableCard();
        __result = Mathf.Max(0, card?.BloodCost() ?? CostProperties.OriginalBloodCost(__instance));
        return false;
    }

    [HarmonyPatch(typeof(CardInfo), nameof(CardInfo.BonesCost), MethodType.Getter), HarmonyPrefix]
    public static bool BoneCost(CardInfo __instance, ref int __result)
    {
        PlayableCard card = __instance.GetPlayableCard();
        if (card == null)
            return true;

        __result = Mathf.Max(0, card.BonesCost());
        return false;
    }

    [HarmonyPatch(typeof(CardInfo), nameof(CardInfo.GemsCost), MethodType.Getter), HarmonyPrefix]
    public static bool GemsCost(CardInfo __instance, ref List<GemType> __result)
    {
        PlayableCard card = __instance.GetPlayableCard();
        __result = card?.GemsCost() ?? CostProperties.ImprovedGemsCost(__instance);
        return false;
    }

    [HarmonyPatch(typeof(CardInfo), nameof(CardInfo.EnergyCost), MethodType.Getter), HarmonyPrefix]
    public static bool EnergyCost(CardInfo __instance, ref int __result)
    {
        PlayableCard card = __instance.GetPlayableCard();
        __result = card?.EnergyCost ?? CostProperties.OriginalEnergyCost(__instance);
        return false;
    }

    [HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.EnergyCost), MethodType.Getter), HarmonyPrefix]
    public static bool DisableVanillaEnergyCost(PlayableCard __instance, ref int __result)
    {
        // patch this to follow the same pattern as the other cost methods
        int energyCost = CostProperties.OriginalEnergyCost(__instance.Info);
        if (__instance.IsUsingBlueGem() && CostProperties.ReduceGemifiedEnergy(__instance, energyCost))
            energyCost--;

        foreach (CardModificationInfo mod in __instance.TemporaryMods)
            energyCost += mod.energyCostAdjustment;

        __result = Mathf.Max(0, energyCost);
        return false;
    }
}

/*[HarmonyPatch(typeof(DiskCardGame.Card), nameof(DiskCardGame.Card.Info), MethodType.Setter)]
internal static class Card_SetInfo
{
    public static void Postfix(DiskCardGame.Card __instance)
    {
        //return;
        if (__instance is not PlayableCard playableCard)
            return;
        
        CardInfo info = playableCard.Info;
        
        if (CostProperties.CardInfoToCard.TryGetValue(info, out List<WeakReference<PlayableCard>> cardList))
        {
            PlayableCard card = null;
            for (int i = cardList.Count - 1; i >= 0; i--)
            {
                if (!cardList[i].TryGetTarget(out PlayableCard innerCard) || innerCard == null)
                {
                    // NOTE: We store a list of cards so if we don't clear this list then it will fill up forever
                    cardList.RemoveAt(i);
                }
                else if(innerCard == playableCard)
                {
                    card = innerCard;
                }
            }
            
            if (card == null)
            {
                cardList.Add(new WeakReference<PlayableCard>(playableCard));
                if (cardList.Count > 1)
                {
                    InscryptionAPIPlugin.Logger.LogWarning($"More than 1 card are using the same card info. This can cause unexpected problems with dynamic costs! {info.displayedName}");
                }
            }
        }
        else
        {
            Debug.Log($"New-un");
            CostProperties.CardInfoToCard.Add(info, new List<WeakReference<PlayableCard>>()
            {
                new WeakReference<PlayableCard>(playableCard)
            });
        }
    }
}*/

[HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.SetInfo))]
internal static class AddRefreshBehaviourToCard
{
    private static void Postfix(PlayableCard __instance)
    {
        // add the refresh component if it doesn't exist, then set the Card to the calling instance
        if (__instance.GetComponent<CostProperties.RefreshCostMonoBehaviour>() == null)
        {
            __instance.gameObject.AddComponent<CostProperties.RefreshCostMonoBehaviour>().playableCard = __instance;
            if (CostProperties.CardInfoToCard.TryGetValue(__instance.Info, out List<WeakReference<PlayableCard>> cardList))
            {
                PlayableCard card = null;
                for (int i = cardList.Count - 1; i >= 0; i--)
                {
                    // NOTE: We store a list of cards so if we don't clear this list then it will fill up forever
                    if (!cardList[i].TryGetTarget(out PlayableCard innerCard) || innerCard == null)
                        cardList.RemoveAt(i);

                    else if (innerCard == __instance)
                        card = innerCard;
                }

                if (card == null)
                {
                    cardList.Add(new WeakReference<PlayableCard>(__instance));
                    if (cardList.Count > 1)
                    {
                        InscryptionAPIPlugin.Logger.LogWarning($"More than 1 card are using the same card info. This can cause unexpected problems with dynamic costs! {__instance.Info.displayedName}");
                    }
                }
            }
            else
            {
                CostProperties.CardInfoToCard.Add(__instance.Info, new List<WeakReference<PlayableCard>>()
                {
                    new WeakReference<PlayableCard>(__instance)
                });
            }

        }
    }
}

[HarmonyPatch]
internal static class TurnManager_CleanupPhase
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(TurnManager), nameof(TurnManager.CleanupPhase));
        yield return AccessTools.Method(typeof(GBCEncounterManager), nameof(GBCEncounterManager.LoadOverworldScene));
    }

    private static void Postfix()
    {
        // NOTE: This is a hack to clear the table
        CostProperties.CardInfoToCard = new ConditionalWeakTable<CardInfo, List<WeakReference<PlayableCard>>>();
    }
}