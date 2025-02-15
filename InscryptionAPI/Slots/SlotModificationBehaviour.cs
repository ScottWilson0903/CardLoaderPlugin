using DiskCardGame;
using System.Collections;

namespace InscryptionAPI.Slots;

/// <summary>
/// Base class for all slot modification behaviors
/// </summary>
public abstract class SlotModificationBehaviour : TriggerReceiver
{
    /// <summary>
    /// The slot that the behaviour is applied to.
    /// </summary>
    public CardSlot Slot => gameObject.GetComponent<CardSlot>();

    /// <summary>
    /// Use to setup any additional custom slot visualizations when created.
    /// </summary>
    public virtual IEnumerator Setup()
    {
        yield break;
    }

    /// <summary>
    /// Use to clean up any additional custom slot visualizations before being removed
    /// </summary>
    public virtual IEnumerator Cleanup(SlotModificationManager.ModificationType replacement)
    {
        yield break;
    }
}