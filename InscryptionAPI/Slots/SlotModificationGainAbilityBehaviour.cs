using DiskCardGame;
using System.Collections;
using UnityEngine;

namespace InscryptionAPI.Slots;

/// <summary>
/// Base class for all slot modification behaviors
/// </summary>
public abstract class SlotModificationGainAbilityBehaviour : SlotModificationBehaviour
{
    protected abstract Ability AbilityToGain { get; }

    private string TemporaryModId => $"SlotModification{AbilityToGain}{Slot.IsPlayerSlot}{Slot.Index}";

    private CardModificationInfo GetSlotAbilityMod(PlayableCard card, bool create = false)
    {
        card.temporaryMods ??= new();
        CardModificationInfo mod = card.TemporaryMods.FirstOrDefault(m => m != null && !string.IsNullOrEmpty(m.singletonId) && m.singletonId.Equals(TemporaryModId));
        if (mod == null && create)
            mod = new(AbilityToGain) { singletonId = TemporaryModId };

        return mod;
    }

    public override bool RespondsToOtherCardAssignedToSlot(PlayableCard otherCard) => true;

    public override IEnumerator OnOtherCardAssignedToSlot(PlayableCard otherCard)
    {
        if (otherCard == null || otherCard.Slot == null)
            yield break;

        bool shouldHaveThisTempMod = Slot.Card == otherCard;

        var mod = GetSlotAbilityMod(otherCard);

        if (shouldHaveThisTempMod && mod == null)
        {
            mod = GetSlotAbilityMod(otherCard, true);
            otherCard.AddTemporaryMod(mod);
            ResourcesManager.Instance.ForceGemsUpdate(); // just in case
        }

        if (!shouldHaveThisTempMod && mod != null)
        {
            otherCard.RemoveTemporaryMod(mod);
            ResourcesManager.Instance.ForceGemsUpdate(); // just in case
        }

        yield return new WaitForSeconds(0.1f);
    }

    public override IEnumerator Cleanup(SlotModificationManager.ModificationType replacement)
    {
        if (Slot.Card != null)
        {
            var mod = GetSlotAbilityMod(Slot.Card);
            if (mod != null)
            {
                Slot.Card.RemoveTemporaryMod(mod);
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield break;
    }

    public override IEnumerator Setup()
    {
        if (Slot.Card != null)
        {
            var mod = GetSlotAbilityMod(Slot.Card);
            if (mod == null)
            {
                Slot.Card.RemoveTemporaryMod(mod);
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield break;
    }
}