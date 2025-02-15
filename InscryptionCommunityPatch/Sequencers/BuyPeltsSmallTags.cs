using DiskCardGame;
using HarmonyLib;
using UnityEngine;

namespace InscryptionCommunityPatch.Sequencers;

[HarmonyPatch]
internal class BuyPeltsSmallTags
{
    [HarmonyPostfix, HarmonyPatch(typeof(BuyPeltsSequencer), nameof(BuyPeltsSequencer.AddPricetagToCard))]
    private static void ReducePricetagSize(SelectableCard card)
    {
        if (PatchPlugin.configSmallPricetags.Value || PatchPlugin.configMovePricetags.Value)
        {
            Transform t = card.transform.Find("pricetag");
            if (t == null)
                return;

            if (PatchPlugin.configSmallPricetags.Value)
            {
                t.localScale = new(0.75f, 1f, 0.75f);
            }
            if (PatchPlugin.configMovePricetags.Value)
            {
                t.localPosition = new(t.localPosition.x * -1, t.localPosition.y, t.localPosition.z);
            }
        }
    }
}