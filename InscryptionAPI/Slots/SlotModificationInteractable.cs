using DiskCardGame;

namespace InscryptionAPI.Slots;

/// <summary>
/// Base class for all slot modification behaviors
/// </summary>
public class SlotModificationInteractable : AlternateInputInteractable
{
    private CardSlot slot;
    public override CursorType CursorType => CursorType.Inspect;
    public SlotModificationManager.ModificationType ModType { get; set; }

    public void AssignSlotModification(SlotModificationManager.ModificationType modType, CardSlot cardSlot)
    {
        ModType = modType;
        slot = cardSlot;
        base.SetEnabled(true);
    }

    public override void OnAlternateSelectStarted()
    {
        //InscryptionAPIPlugin.Logger.LogDebug($"Open to slot mod page: [{ModType}] on slot [{slot}]");
        RuleBookController.Instance.OpenToItemPage(SlotModificationManager.SLOT_PAGEID + (int)ModType, false);
    }
}