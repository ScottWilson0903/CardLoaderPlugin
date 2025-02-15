using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Encounters;
using InscryptionAPI.Helpers;
using InscryptionAPI.Helpers.Extensions;
using InscryptionAPI.Saves;
using System.Collections;
using UnityEngine;
using static InscryptionAPI.Slots.SlotModificationManager;

namespace InscryptionAPI.Slots;

[HarmonyPatch]
/// <summary>
/// Contains extension methods to simplify slot modification management
/// </summary>
public static class SlotModificationExtensions
{

    public static Info InfoByID(this IEnumerable<Info> modInfos, ModificationType mod)
    {
        return modInfos.FirstOrDefault(x => x.ModificationType == mod);
    }

    /// <summary>
    /// Sets the rulebook description for a slot modification.
    /// </summary>
    /// <param name="mod">The ModificationType to add a rulebook description for.</param>
    /// <param name="rulebookDescription">The rulebook description for the ModificationType.</param>
    /// <param name="categories">What Acts the rulebook entry should appear in.</param>
    /// <returns>The same ModificationType.</returns>
    public static ModificationType SetRulebook(this ModificationType mod, string rulebookName, string rulebookDescription, Texture2D rulebookSprite, params ModificationMetaCategory[] categories)
    {
        Info info = AllSlotModifications.InfoByID(mod);
        if (info != null)
        {
            info.RulebookName = rulebookName;
            info.RulebookDescription = rulebookDescription;
            info.RulebookSprite = rulebookSprite.ConvertTexture();
            foreach (ModificationMetaCategory category in categories)
            {
                if (!info.MetaCategories.Contains(category))
                    info.MetaCategories.Add(category);
            }
        }
        return mod;
    }
    public static ModificationType SetRulebookP03Sprite(this ModificationType mod, Texture2D spriteTexture)
    {
        Info info = AllSlotModifications.InfoByID(mod);
        if (info != null)
        {
            info.P03RulebookSprite = spriteTexture.ConvertTexture();
        }
        return mod;
    }
    public static ModificationType SetRulebookGrimoraSprite(this ModificationType mod, Texture2D spriteTexture)
    {
        Info info = AllSlotModifications.InfoByID(mod);
        if (info != null)
        {
            info.GrimoraRulebookSprite = spriteTexture.ConvertTexture();
        }
        return mod;
    }
    public static ModificationType SetRulebookMagnificusSprite(this ModificationType mod, Texture2D spriteTexture)
    {
        Info info = AllSlotModifications.InfoByID(mod);
        if (info != null)
        {
            info.MagnificusRulebookSprite = spriteTexture.ConvertTexture();
        }
        return mod;
    }
    public static ModificationType SetSharedRulebook(this ModificationType mod, ModificationType sharedRulebookType)
    {
        Info info = AllSlotModifications.InfoByID(mod);
        if (info != null)
        {
            info.SharedRulebook = sharedRulebookType;
        }
        return mod;
    }

    /// <summary>
    /// Assigns a new slot modification to a slot.
    /// </summary>
    /// <param name="slot">The slot to assign the modification to</param>
    /// <param name="modType">The modification type to assign</param>
    public static IEnumerator SetSlotModification(this CardSlot slot, ModificationType modType)
    {
        if (slot == null)
            yield break;

        Info defn = AllModificationInfos.InfoByID(modType);
        SlotModificationInteractable interactable = slot.GetComponent<SlotModificationInteractable>();
        if (defn == null || modType == ModificationType.NoModification || (defn.SharedRulebook == ModificationType.NoModification && string.IsNullOrEmpty(defn.RulebookName)))
        {
            UnityObject.Destroy(interactable);
        }
        else
        {
            interactable ??= slot.gameObject.AddComponent<SlotModificationInteractable>();
            interactable.AssignSlotModification(defn.SharedRulebook != ModificationType.NoModification ? defn.SharedRulebook : modType, slot);
        }

        // Set the ability behaviour
        SlotModificationBehaviour oldSlotModification = slot.GetComponent<SlotModificationBehaviour>();
        if (oldSlotModification != null)
        {
            yield return oldSlotModification.Cleanup(modType);
            CustomCoroutine.WaitOnConditionThenExecute(() => GlobalTriggerHandler.Instance.StackSize == 0, () => GameObject.Destroy(oldSlotModification));
            Instance.SlotReceivers.Remove(slot);
        }

        if (defn != null && defn.SlotBehaviour != null)
        {
            SlotModificationBehaviour newBehaviour = slot.gameObject.AddComponent(defn.SlotBehaviour) as SlotModificationBehaviour;
            Instance.SlotReceivers[slot] = new(modType, newBehaviour);
            yield return newBehaviour.Setup();
        }

        // Set the texture and/or sprite
        CardTemple temple = SaveManager.SaveFile.GetSceneAsCardTemple() ?? CardTemple.Nature;

        if (defn == null)
        {
            slot.ResetSlotTexture();
        }
        else if (slot is PixelCardSlot pcs)
        {
            pcs.SetSlotSprite(defn);
        }
        else
        {
            if (defn.Texture == null || !defn.Texture.ContainsKey(temple))
                slot.ResetSlotTexture();
            else
                slot.SetTexture(defn.Texture[temple]);
        }
    }

    /// <summary>
    /// Gets the current modification of a slot
    /// </summary>
    public static ModificationType GetSlotModification(this CardSlot slot)
    {
        return slot == null
            ? ModificationType.NoModification
            : Instance.SlotReceivers.ContainsKey(slot)
            ? Instance.SlotReceivers[slot].Item1
            : ModificationType.NoModification;
    }

    private static void SetSlotSprite(this PixelCardSlot slot, Info defn)
    {
        if (defn == null)
        {
            InscryptionAPIPlugin.Logger.LogDebug($"Resetting slot {slot.Index} to default because mod info was null");
            slot.ResetSlotSprite();
            return;
        }

        if (defn.PixelBoardSlotSprites == null)
        {
            InscryptionAPIPlugin.Logger.LogDebug($"Resetting slot {slot.Index} to default because mod info did not contain pixel slot info");
            slot.ResetSlotSprite();
            return;
        }

        var triggeringNPC = GBCEncounterManager.Instance?.GetTriggeringNPC();
        if (triggeringNPC == null)
        {
            InscryptionAPIPlugin.Logger.LogDebug($"Doing nothing to slot {slot.Index} because the triggering NPC was null");
            return;
        }

        if (!defn.PixelBoardSlotSprites.ContainsKey(triggeringNPC.BattleBackgroundTheme))
        {
            InscryptionAPIPlugin.Logger.LogDebug($"Resetting slot {slot.Index} to default because pixel slot info did not contain a definition for {triggeringNPC.BattleBackgroundTheme}");
            slot.ResetSlotSprite();
            return;
        }

        var spriteSet = defn.PixelBoardSlotSprites[triggeringNPC.BattleBackgroundTheme];
        if (spriteSet == null)
        {
            InscryptionAPIPlugin.Logger.LogDebug($"Resetting slot {slot.Index} to default because pixel slot info had a null definition for {triggeringNPC.BattleBackgroundTheme}");
            slot.ResetSlotSprite();
            return;
        }

        var specificSprites = spriteSet.specificSlotSprites.Find(s => s.playerSlot == slot.IsPlayerSlot && s.index == slot.Index);

        if (specificSprites == null)
            slot.SetSprites(spriteSet.slotDefault, spriteSet.slotHighlight, slot.IsPlayerSlot && spriteSet.flipPlayerSlotSpriteY, false);
        else
            slot.SetSprites(specificSprites.slotDefault, specificSprites.slotHighlight, slot.IsPlayerSlot && spriteSet.flipPlayerSlotSpriteY, false);
    }

    private static void ResetSlotSprite(this PixelCardSlot slot)
    {
        var triggeringNPC = GBCEncounterManager.Instance?.GetTriggeringNPC();
        if (triggeringNPC == null)
            return;

        var spriteSet = PixelBoardSpriteSetter.Instance.themeSpriteSets.Find(s => s.id == triggeringNPC.BattleBackgroundTheme);
        if (spriteSet == null)
            return;

        var specificSprites = spriteSet.specificSlotSprites.Find(s => s.playerSlot == slot.IsPlayerSlot && s.index == slot.Index);

        if (specificSprites != null)
            slot.SetSprites(specificSprites.slotDefault, specificSprites.slotHighlight, slot.IsPlayerSlot && spriteSet.flipPlayerSlotSpriteY, false);
        else
            slot.SetSprites(spriteSet.slotDefault, spriteSet.slotHighlight, slot.IsPlayerSlot && spriteSet.flipPlayerSlotSpriteY, false);
    }

    /// <summary>
    /// Resets a slot's texture back to the default texture for that slot based on the current act.
    /// </summary>
    public static void ResetSlotTexture(this CardSlot slot)
    {
        if (slot is PixelCardSlot pcs)
        {
            pcs.ResetSlotSprite();
            return;
        }

        CardTemple temple = SaveManager.SaveFile.GetSceneAsCardTemple() ?? CardTemple.Nature;

        Dictionary<CardTemple, List<Texture>> lookup = slot.IsOpponentSlot() ? OpponentOverrideSlots : PlayerOverrideSlots;
        var newTexture = DefaultSlotTextures[temple];
        if (lookup.ContainsKey(temple))
        {
            // Get the texture overrides
            var textureChoices = lookup[temple];
            int idx = slot.Index;
            if (idx >= textureChoices.Count)
            {
                // Try to guess what the best index would be
                int slotCount = BoardManager.Instance.PlayerSlotsCopy.Count;
                if (slot.Index == slotCount - 1) // the last slot
                    idx = textureChoices.Count - 1;
                else // Use the next to last slot
                    idx = textureChoices.Count - 2;
            }
            if (idx < 0)
                idx = 0;

            if (textureChoices[idx] != null)
                newTexture = textureChoices[idx];
        }

        slot.SetTexture(newTexture);
    }
}