using DiskCardGame;
using InscryptionAPI;
using InscryptionAPI.CardCosts;
using InscryptionAPI.Helpers;
using InscryptionCommunityPatch.Card;
using System.Collections;
using UnityEngine;

namespace InscryptionCommunityPatch.Tests;

public class TestCost : CustomCardCost
{
    public override string CostName => "TestCost";
    public static void Init()
    {
        PatchPlugin.Logger.LogDebug("Adding TestCost");

        CardCostManager.Register(InscryptionAPIPlugin.ModGUID, "TestCost", typeof(TestCost), Texture3D, TexturePixel)
            .SetCostTier(CostTier)
            .SetCanBePlayedByTurn2WithHand(CanBePlayed)
            .SetFoundAtChoiceNodes(true, TextureHelper.GetImageAsTexture("testCost.png", typeof(TestCost).Assembly));
    }

    public static int CostTier(int amount)
    {
        return Mathf.FloorToInt(amount / 2f);
    }
    public static bool CanBePlayed(int amount, CardInfo card, List<CardInfo> hand)
    {
        return amount <= 2;
    }
    public static Texture2D Texture3D(int cardCost, CardInfo cardInfo, PlayableCard playableCard)
    {
        Texture2D tex = TextureHelper.GetImageAsTexture($"energy_cost_{Mathf.Min(7, cardCost)}.png", typeof(PatchPlugin).Assembly);
        return ReverseColours(tex);
    }
    public static Texture2D TexturePixel(int cardCost, CardInfo info, PlayableCard playableCard)
    {
        Texture2D tex = Part2CardCostRender.CombineIconAndCount(cardCost, TextureHelper.GetImageAsTexture($"pixel_energy.png", typeof(PatchPlugin).Assembly));
        return ReverseColours(tex);
    }
    private static Texture2D ReverseColours(Texture2D texToReverse)
    {
        for (int x = 0; x < texToReverse.width; x++)
        {
            for (int y = 0; y < texToReverse.height; y++)
            {
                Color pixel = texToReverse.GetPixel(x, y);
                Color.RGBToHSV(pixel, out float H, out float S, out float V);
                Color final_color = Color.HSVToRGB((H + 0.5f) % 1f, (S + 0.5f) % 1f, (V + 0.5f) % 1f);
                final_color.a = pixel.a;

                texToReverse.SetPixel(x, y, final_color);
            }
        }
        texToReverse.Apply();
        return texToReverse;
    }

    public override bool CostSatisfied(int cardCost, PlayableCard card)
    {
        return cardCost <= (Singleton<ResourcesManager>.Instance.PlayerEnergy - card.EnergyCost);
    }

    public override string CostUnsatisfiedHint(int cardCost, PlayableCard card)
    {
        return "Eat your greens aby. " + card.Info.DisplayedNameLocalized;
    }

    public override IEnumerator OnPlayed(int cardCost, PlayableCard card)
    {
        if (cardCost > 0)
            yield return Singleton<ResourcesManager>.Instance.SpendEnergy(cardCost);
    }
}