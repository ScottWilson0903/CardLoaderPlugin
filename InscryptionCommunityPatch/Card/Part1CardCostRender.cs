using DiskCardGame;
using InscryptionAPI.Card;
using InscryptionAPI.CardCosts;
//using InscryptionAPI.CardCosts;
using InscryptionAPI.Helpers;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;

//[HarmonyPatch]
/// <summary>
/// Modifies how card costs are rendered in Act to add support for mixed card costs, custom costs, and energy and Mox costs.
/// </summary>
public static class Part1CardCostRender
{
    public static event Action<CardInfo, List<Texture2D>> UpdateCardCost;

    public static List<Texture2D> CostTextures(CardInfo cardInfo, PlayableCard playableCard, int bloodCost, int bonesCost, int energyCost, List<GemType> gemsCost)
    {
        List<Texture2D> costTextures = new();
        if (gemsCost.Count > 0)
        {
            List<Texture2D> gemCost = GetMoxTextures(gemsCost);
            costTextures.Add(TextureHelper.CombineTextures(gemCost, TextureHelper.GetImageAsTexture("mox_cost_empty.png", typeof(Part1CardCostRender).Assembly), xStep: 21));
        }

        // there's a 6+ texture but since Energy can't go above 6 normally I have excluded it from consideration
        if (energyCost > 0)
            costTextures.Add(CardCostRender.GetTextureByName($"energy_cost_{Mathf.Min(7, energyCost)}"));

        if (bonesCost > 0)
            costTextures.Add(CardCostRender.GetTextureByName($"bone_cost_{Mathf.Min(16, bonesCost)}"));

        if (bloodCost > 0)
            costTextures.Add(CardCostRender.GetTextureByName($"blood_cost_{Mathf.Min(14, bloodCost)}"));

        // get a list of the custom costs we need textures for
        // check for PlayableCard to account for possible dynamic costs (no API support but who knows what modders do)
        List<CardCostManager.FullCardCost> customCosts;
        if (playableCard != null)
            customCosts = playableCard.GetCustomCardCosts().Select(x => CardCostManager.AllCustomCosts.Find(c => c.CostName == x.CostName)).ToList();
        else
            customCosts = cardInfo.GetCustomCosts();

        foreach (CardCostManager.FullCardCost fullCost in customCosts)
        {
            int amount = playableCard?.GetCustomCost(fullCost) ?? cardInfo.GetCustomCost(fullCost);
            string key = $"{fullCost.ModGUID}_{fullCost.CostName}_{amount}_part1";
            if (CardCostRender.AssembledTextures.ContainsKey(key))
            {
                if (CardCostRender.AssembledTextures[key] != null)
                    costTextures.Add(CardCostRender.AssembledTextures[key]);
                else
                    CardCostRender.AssembledTextures.Remove(key);
            }
            else
            {
                Texture2D costTex = fullCost.CostTexture(amount, cardInfo, playableCard);
                if (costTex != null)
                {
                    costTextures.Add(costTex);
                    CardCostRender.AssembledTextures.Add(key, costTex);
                }
            }
        }

        // Call the event and allow others to modify the list of textures
        UpdateCardCost?.Invoke(cardInfo, costTextures);
        return costTextures;
    }

    public static List<Texture2D> GetMoxTextures(List<GemType> gemsCost)
    {
        List<Texture2D> gemCost = new();
        if (gemsCost.Count <= 3)
        {
            foreach (GemType type in gemsCost.Where(x => x == GemType.Orange))
                gemCost.Add(CardCostRender.GetTextureByName("mox_cost_o"));

            foreach (GemType type in gemsCost.Where(x => x == GemType.Green))
                gemCost.Add(CardCostRender.GetTextureByName("mox_cost_g"));

            foreach (GemType type in gemsCost.Where(x => x == GemType.Blue))
                gemCost.Add(CardCostRender.GetTextureByName("mox_cost_b"));
        }
        else
        {
            int orangeCount = Mathf.Min(4, gemsCost.Count(x => x == GemType.Orange));
            int greenCount = Mathf.Min(4, gemsCost.Count(x => x == GemType.Green));
            int blueCount = Mathf.Min(4, gemsCost.Count(y => y == GemType.Blue));
            if (orangeCount > 0)
            {
                if (orangeCount == 1)
                {
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_o"));
                }
                else if (orangeCount > 2 || (greenCount + blueCount > 1))
                {
                    gemCost.Add(CardCostRender.GetTextureByName($"mox_cost_o_{orangeCount}"));
                    orangeCount = 1;
                }
                else
                {
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_o"));
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_o"));
                }
            }

            if (greenCount > 0)
            {
                if (greenCount == 1)
                {
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_g"));
                }
                else if (greenCount > 2 || (blueCount + orangeCount > 1))
                {
                    gemCost.Add(CardCostRender.GetTextureByName($"mox_cost_g_{greenCount}"));
                    greenCount = 1; // green is only using 1 sprite 'slot'
                }
                else
                {
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_g"));
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_g"));
                }
            }

            if (blueCount > 0)
            {
                if (blueCount == 1)
                {
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_b"));
                }
                else if (blueCount > 2 || (greenCount + orangeCount > 1))
                {
                    gemCost.Add(CardCostRender.GetTextureByName($"mox_cost_b_{blueCount}"));
                }
                else
                {
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_b"));
                    gemCost.Add(CardCostRender.GetTextureByName("mox_cost_b"));
                }
            }
        }

        while (gemCost.Count < 3)
            gemCost.Insert(0, null);

        return gemCost;
    }
}