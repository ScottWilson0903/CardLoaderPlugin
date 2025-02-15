using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Card;
using System.Collections;
using UnityEngine;

namespace InscryptionAPI.Helpers;

public static class ResourcesManagerHelpers
{
    /// <summary>
    /// Removes a given amount of energy cells, which determines how much energy a player has available at the start of a turn.
    /// Affected by 'ResourcesManager.preventNextEnergyLoss'.
    /// </summary>
    /// <param name="instance">The ResourcesManager Instance.</param>
    /// <param name="amount">How many energy cells to close. Gets capped to the current number of open energy cells.</param>
    public static IEnumerator RemoveMaxEnergy(this ResourcesManager instance, int amount)
    {
        yield return instance.RemoveMaxEnergy(amount, true);
    }

    /// <summary>
    /// A variant of RemoveMaxEnergy that can bypass ResourcesManager.PreventNextEnergyLoss.
    /// Affected by 'ResourcesManager.preventNextEnergyLoss'.
    /// </summary>
    /// <param name="instance">The ResourcesManager Instance.</param>
    /// <param name="amount">How many energy cells to close. Gets capped to the current number of open energy cells.</param>
    public static IEnumerator RemoveMaxEnergy(this ResourcesManager instance, int amount, bool preventable)
    {
        if (preventable && instance.preventNextEnergyLoss)
        {
            instance.preventNextEnergyLoss = false;
            yield break;
        }
        int numToClose = Mathf.Min(instance.PlayerMaxEnergy, amount);

        instance.PlayerMaxEnergy -= numToClose;
        if (instance.PlayerEnergy > instance.PlayerMaxEnergy)
        {
            instance.PlayerEnergy -= numToClose;
            yield return instance.ShowSpendEnergy(numToClose);
        }

        yield return instance.ShowRemoveMaxEnergy(numToClose);
    }
    public static IEnumerator ShowRemoveMaxEnergy(this ResourcesManager instance, int amount)
    {
        PixelResourcesManager pixelManager = instance as PixelResourcesManager;
        for (int i = instance.PlayerMaxEnergy + amount - 1; i >= instance.PlayerMaxEnergy; i--)
        {
            if (pixelManager != null)
            {
                AudioController.Instance.PlaySound2D("crushBlip3", MixerGroup.None, 0.4f, 0f, new AudioParams.Pitch(0.9f + (instance.PlayerMaxEnergy + i) * 0.05f));
                pixelManager.energyRenderers[i].sprite = GetEmptyBatterySprite.emptyBatterySprite;
                pixelManager.BounceRenderer((instance as PixelResourcesManager).energyRenderers[i].transform);
            }
            else if (ResourceDrone.m_Instance != null)
            {
                AudioController.Instance.PlaySound3D("crushBlip3", MixerGroup.TableObjectsSFX, instance.transform.position, 0.4f, 0f, new AudioParams.Pitch(0.9f + (instance.PlayerMaxEnergy + i) * 0.05f));
                ResourceDrone.Instance.cellRenderers[i].material.DisableKeyword("_EMISSION");
                ResourceDrone.Instance.CloseCell(i, false);
            }
            yield return new WaitForSeconds(0.05f);
        }
    }
    public static int GemsOfType(this ResourcesManager instance, GemType gem)
    {
        return instance.gems.Count(x => x == gem);
    }
    public static int GemCount(bool playerGems, GemType gemToCheck)
    {
        if (playerGems)
            return ResourcesManager.Instance.GemsOfType(gemToCheck);
        else
            return OpponentGemsManager.Instance.GemsOfType(gemToCheck);
    }
    public static bool OwnerHasGems(bool playerGems, params GemType[] gems)
    {
        if (playerGems)
            return PlayerHasGems(gems);
        else
            return OpponentHasGems(gems);
    }
    public static bool OpponentHasGems(params GemType[] gems)
    {
        if (OpponentGemsManager.Instance == null)
            return false;

        foreach (GemType gem in gems)
        {
            if (!OpponentGemsManager.Instance.HasGem(gem))
                return false;
        }
        return true;
    }
    public static bool PlayerHasGems(params GemType[] gems)
    {
        foreach (GemType gem in gems)
        {
            if (!ResourcesManager.Instance.HasGem(gem))
                return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PixelResourcesManager), nameof(PixelResourcesManager.Start))]
public static class GetEmptyBatterySprite
{
    public static Sprite emptyBatterySprite = null;

    [HarmonyPostfix]
    private static void GetSprite(PixelResourcesManager __instance)
    {
        if (emptyBatterySprite == null)
            emptyBatterySprite = __instance.energyRenderers[0].sprite;
    }
}