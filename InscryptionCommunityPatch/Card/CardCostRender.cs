using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Card;
//using InscryptionAPI.CardCosts;
using InscryptionAPI.Helpers;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;

[HarmonyPatch]
public static class CardCostRender
{
    internal static readonly Dictionary<string, Texture2D> AssembledTextures = new();

    /// <summary>
    /// Grabs the texture associated with the given key if it has already been assembled.
    /// Otherwise, creates the texture using the provided string and returns it.
    /// </summary>
    /// <param name="key"></param>
    public static Texture2D GetTextureByName(string key)
    {
        if (AssembledTextures.ContainsKey(key))
        {
            if (AssembledTextures[key] != null)
                return AssembledTextures[key];

            AssembledTextures.Remove(key); // remove null textures
        }

        Texture2D texture = TextureHelper.GetImageAsTexture($"{key}.png", typeof(CardCostRender).Assembly);
        AssembledTextures.Add(key, texture);
        return texture;
    }

    /// <summary>
    /// Creates a sprite that combines all of a card's play costs.
    /// </summary>
    public static Sprite FinalCostSprite(CardInfo cardInfo, PlayableCard playableCard, TextureHelper.SpriteType spriteType, int yStep)
    {
        Texture2D baseTexture;
        List<Texture2D> masterTextures;

        int bloodCost = playableCard?.BloodCost() ?? cardInfo.BloodCost;
        int bonesCost = playableCard?.BonesCost() ?? cardInfo.BonesCost;
        int energyCost = playableCard?.EnergyCost ?? cardInfo.EnergyCost;
        List<GemType> gemsCost = playableCard?.GemsCost() ?? cardInfo.GemsCost;

        bool pixelCard = spriteType != TextureHelper.SpriteType.OversizedCostDecal;
        if (pixelCard)
        {
            baseTexture = TextureHelper.GetImageAsTexture("pixel_base.png", typeof(CardCostRender).Assembly);
            masterTextures = Part2CardCostRender.CostTextures(cardInfo, playableCard, bloodCost, bonesCost, energyCost, gemsCost);
        }
        else
        {
            baseTexture = TextureHelper.GetImageAsTexture("empty_cost.png", typeof(CardCostRender).Assembly);
            masterTextures = Part1CardCostRender.CostTextures(cardInfo, playableCard, bloodCost, bonesCost, energyCost, gemsCost);
        }

        while (masterTextures.Count < 4)
            masterTextures.Add(null);

        Texture2D combinedTexture = TextureHelper.CombineTextures(masterTextures, baseTexture, yStep: yStep);
        return TextureHelper.ConvertTexture(combinedTexture, spriteType);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardDisplayer), nameof(CardDisplayer.DisplayInfo))]
    private static void CreateFullCostSprite(CardDisplayer __instance, CardRenderInfo renderInfo, PlayableCard playableCard)
    {
        // DisplayNull will have already been called, so we just return
        // Magnificus and Grimora use their own systems so add exceptions for them
        if (renderInfo?.baseInfo == null || __instance.costRenderer == null)
            return;

        // don't just check for IsPart1/Part2 since pixel cards can appear in Act 1 (starter deck screen)
        if (__instance is CardDisplayer3D && SaveManager.SaveFile.IsPart1)
        {
            __instance.costRenderer.sprite = FinalCostSprite(renderInfo.baseInfo, playableCard, TextureHelper.SpriteType.OversizedCostDecal, 28);
        }
        else if (__instance is PixelCardDisplayer pixelDisplay && PatchPlugin.act2CostRender.Value)
        {
            if (PatchPlugin.act2VanillaStyle.Value)
            {
                __instance.costRenderer.sprite = Part2CardCostRender.FinalVanillaCostSprite(pixelDisplay, renderInfo.baseInfo, playableCard, !PatchPlugin.rightAct2Cost.Value);
            }
            else
            {
                __instance.costRenderer.sprite = FinalCostSprite(renderInfo.baseInfo, playableCard,
                    PatchPlugin.rightAct2Cost.Value ? TextureHelper.SpriteType.Act2CostDecalRight : TextureHelper.SpriteType.Act2CostDecalLeft, 8);
            }
        }
    }

    // prevents indexing error when a card has a cost greater than the vanilla limits
    [HarmonyPrefix, HarmonyPatch(typeof(CardDisplayer), nameof(CardDisplayer.GetCostSpriteForCard))]
    private static bool Part1CardCostDisplayerPatch(CardDisplayer __instance, ref Sprite __result, CardInfo card)
    {
        if (__instance is CardDisplayer3D && SceneLoader.ActiveSceneName.StartsWith("Part1")) // Make sure we are in Leshy's Cabin
        {
            return false;
        }
        if (__instance is PixelCardDisplayer && PatchPlugin.act2CostRender.Value)
        {
            return false;
        }

        return true;
    }
}