using DiskCardGame;
using GBC;
using InscryptionAPI.Helpers.Extensions;
using System.Collections;
using UnityEngine;

namespace InscryptionAPI.Card;

/// <summary>
/// A custom class implementing shield-giving behaviour.
/// With the most recent API versions, shield-giving sigils like DeathShield/Nano Armour/ Armoured can now stack and instead use a counter rather than a simple boolean.
/// </summary>
public abstract class DamageShieldBehaviour : AbilityBehaviour
{
    public int numShields; // modifiable version of NumShields
    public int NumShields => Mathf.Max(0, numShields);
    public abstract int StartingNumShields { get; } // how many shields this sigil will provide initially
    public bool HasShields() => NumShields > 0;

    public bool initialised = false;
    public virtual void Start()
    {
        if (base.Card != null)
            numShields = StartingNumShields;

        initialised = true;
    }

    public virtual void AddShields(int amount, bool updateDisplay = true)
    {
        numShields += amount;
        if (HasShields())
            base.Card.Status.lostShield = false;

        if (updateDisplay)
        {
            base.Card.OnStatsChanged();
        }
    }
    public virtual void RemoveShields(int amount, bool updateDisplay = true)
    {
        numShields -= amount;
        if (base.Card.GetTotalShields() <= 0)
            base.Card.Status.lostShield = true;

        if (!Ability.GetHideSingleStacks() && !HasShields() && !base.Card.Status.hiddenAbilities.Contains(this.Ability))
        {
            base.Card.Status.hiddenAbilities.Add(this.Ability);
            //Debug.Log("Add hidden single");
        }

        if (updateDisplay)
        {
            base.Card.OnStatsChanged();
        }
    }
    public virtual void ResetShields(bool updateDisplay)
    {
        bool depleted = !HasShields();
        numShields = StartingNumShields;
        base.Card.Status.lostShield = false;

        base.Card.Status.hiddenAbilities.RemoveAll(x => x == this.Ability);
        if (updateDisplay)
        {
            base.Card.OnStatsChanged();
        }
    }
}

/// <summary>
/// A custom class combining [DamageShieldBehaviour](InscryptionAPI.Card.DamageShieldBehaviour) and [ExtendedActivatedBehaviour](InscryptionAPI.Card.ExtendedActivatedBehaviour).
/// </summary>
public abstract class ActivatedDamageShieldBehaviour : DamageShieldBehaviour
{
    public int bloodCostMod;
    public int bonesCostMod;
    public int energyCostMod;
    public int healthCostMod;

    public virtual int StartingBloodCost { get; }
    public virtual int StartingBonesCost { get; }
    public virtual int StartingEnergyCost { get; }
    public virtual int StartingHealthCost { get; }
    public virtual int OnActivateBloodCostMod { get; set; }
    public virtual int OnActivateBonesCostMod { get; set; }
    public virtual int OnActivateEnergyCostMod { get; set; }
    public virtual int OnActivateHealthCostMod { get; set; }

    public int BloodCost => Mathf.Max(0, StartingBloodCost + bloodCostMod);
    public int BonesCost => Mathf.Max(0, StartingBonesCost + bonesCostMod);
    public int EnergyCost => Mathf.Max(0, StartingEnergyCost + energyCostMod);
    public int HealthCost => Mathf.Max(0, StartingHealthCost + healthCostMod);

    public Dictionary<CardInfo, CardSlot> currentSacrificedCardInfos = new();

    public virtual bool RespondsToPostResolveOnBoard()
    {
        return false;
    }
    public virtual IEnumerator OnPostResolveOnBoard()
    {
        yield break;
    }
    public sealed override bool RespondsToResolveOnBoard() => RespondsToPostResolveOnBoard() || LearnMechanic();
    public sealed override IEnumerator OnResolveOnBoard()
    {
        if (LearnMechanic()) // Act 2 tutorial dialogue
        {
            yield return new WaitForSeconds(0.15f);
            if (StoryEventsData.EventCompleted(StoryEvent.GBCUndeadAmbition))
                yield return Singleton<DialogueHandler>.Instance.PlayDialogueEvent("ActivatedAbilityTutorial", TextBox.Style.Undead, Singleton<InBattleDialogueSpeakers>.Instance.GetSpeaker(DialogueSpeaker.Character.Grimora), null, TextBox.ScreenPosition.ForceTop);

            else if (StoryEventsData.EventCompleted(StoryEvent.GBCNatureAmbition))
                yield return Singleton<DialogueHandler>.Instance.PlayDialogueEvent("ActivatedAbilityTutorial", TextBox.Style.Nature, Singleton<InBattleDialogueSpeakers>.Instance.GetSpeaker(DialogueSpeaker.Character.Leshy), null, TextBox.ScreenPosition.ForceTop);

            else if (StoryEventsData.EventCompleted(StoryEvent.GBCTechAmbition))
                yield return Singleton<DialogueHandler>.Instance.PlayDialogueEvent("ActivatedAbilityTutorial", TextBox.Style.Tech, Singleton<InBattleDialogueSpeakers>.Instance.GetSpeaker(DialogueSpeaker.Character.P03), null, TextBox.ScreenPosition.ForceTop);

            else
                yield return Singleton<DialogueHandler>.Instance.PlayDialogueEvent("ActivatedAbilityTutorial", TextBox.Style.Magic, Singleton<InBattleDialogueSpeakers>.Instance.GetSpeaker(DialogueSpeaker.Character.Magnificus), null, TextBox.ScreenPosition.ForceTop);
        }

        if (RespondsToPostResolveOnBoard())
            yield return OnPostResolveOnBoard();
    }

    public sealed override bool RespondsToActivatedAbility(Ability ability) => this.Ability == ability;
    public sealed override IEnumerator OnActivatedAbility()
    {
        if (CanAfford() && CanActivate())
        {
            if (BloodCost > 0)
            {
                yield return ChooseSacrifices();

                if (Singleton<BoardManager>.Instance.CancelledSacrifice)
                    yield break;
            }

            if (HealthCost > 0)
            {
                base.Card.Anim.LightNegationEffect();
                base.Card.Status.damageTaken += HealthCost;
            }

            if (EnergyCost > 0)
            {
                yield return Singleton<ResourcesManager>.Instance.SpendEnergy(EnergyCost);
                if (Singleton<ConduitCircuitManager>.Instance != null)
                {
                    CardSlot cardSlot = Singleton<BoardManager>.Instance.GetSlots(getPlayerSlots: true).Find((CardSlot x) => x.Card != null && x.Card.HasAbility(Ability.ConduitEnergy));
                    if (cardSlot != null)
                    {
                        ConduitEnergy component = cardSlot.Card.GetComponent<ConduitEnergy>();
                        if (component != null && component.CompletesCircuit())
                            yield return Singleton<ResourcesManager>.Instance.AddEnergy(EnergyCost);
                    }
                }
            }
            if (BonesCost > 0)
                yield return Singleton<ResourcesManager>.Instance.SpendBones(BonesCost);

            yield return new WaitForSeconds(0.1f);
            yield return base.PreSuccessfulTriggerSequence();
            yield return Activate();
            ProgressionData.SetMechanicLearned(MechanicsConcept.GBCActivatedAbilities);

            if (HealthCost > 0) // card still exists and has 0 Health
            {
                if (base.Card && base.Card.Health <= 0 && !base.Card.Dead)
                {
                    yield return base.Card.Die(true);
                    yield return PostActivate();
                    currentSacrificedCardInfos.Clear();
                    yield break;
                }
            }
            if (OnActivateEnergyCostMod != 0)
                energyCostMod += OnActivateEnergyCostMod;
            if (OnActivateBonesCostMod != 0)
                bonesCostMod += OnActivateBonesCostMod;
            if (OnActivateHealthCostMod != 0)
                healthCostMod += OnActivateHealthCostMod;

            yield return PostActivate();
            currentSacrificedCardInfos.Clear();
        }
        else
        {
            base.Card.Anim.LightNegationEffect();
            AudioController.Instance.PlaySound2D("toneless_negate", MixerGroup.GBCSFX, 0.2f);
            yield return new WaitForSeconds(0.25f);
        }
    }
    public virtual IEnumerator PostActivate()
    {
        yield break;
    }
    public virtual bool CanActivate() => true;
    public abstract IEnumerator Activate();

    private IEnumerator ChooseSacrifices()
    {
        BoardManager manager = Singleton<BoardManager>.Instance;
        List<CardSlot> validSlots = manager.GetCardSlots(
            !base.Card.OpponentCard, x => x.Card != null & x.Card != base.Card);

        Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
        Singleton<ViewManager>.Instance.SwitchToView(manager.BoardView);
        Singleton<InteractionCursor>.Instance.ForceCursorType(CursorType.Sacrifice);
        manager.cancelledPlacementWithInput = false;
        manager.currentValidSlots = validSlots;
        manager.currentSacrificeDemandingCard = base.Card;
        manager.CancelledSacrifice = false;
        manager.LastSacrificesInfo.Clear();
        manager.SetQueueSlotsEnabled(false);
        foreach (CardSlot allSlot in manager.AllSlots.Where(x => x.Card != base.Card))
        {
            if (!allSlot.IsPlayerSlot || allSlot.Card == null)
            {
                allSlot.SetEnabled(enabled: false);
                allSlot.ShowState(HighlightedInteractable.State.NonInteractable);
            }
            if (allSlot.IsPlayerSlot && allSlot.Card != null && allSlot.Card.CanBeSacrificed)
                allSlot.Card.Anim.SetShaking(true);
        }
        yield return manager.SetSacrificeMarkersShown(BloodCost);
        while (manager.GetValueOfSacrifices(manager.currentSacrifices) < BloodCost && !manager.cancelledPlacementWithInput)
        {
            manager.SetSacrificeMarkersValue(manager.currentSacrifices.Count);
            yield return new WaitForEndOfFrame();
        }

        foreach (CardSlot allSlot2 in manager.AllSlots)
        {
            allSlot2.SetEnabled(false);
            if (allSlot2.IsPlayerSlot && allSlot2.Card != null)
                allSlot2.Card.Anim.SetShaking(shaking: false);
        }

        foreach (CardSlot currentSacrifice in manager.currentSacrifices)
            manager.LastSacrificesInfo.Add(currentSacrifice.Card.Info);

        if (manager.cancelledPlacementWithInput)
        {
            manager.HideSacrificeMarkers();

            foreach (CardSlot slot in manager.GetSlots(getPlayerSlots: true))
            {
                if (slot.Card != null)
                {
                    slot.Card.Anim.SetSacrificeHoverMarkerShown(false);
                    if (manager.currentSacrifices.Contains(slot))
                        slot.Card.Anim.SetMarkedForSacrifice(false);
                }
            }
            Singleton<ViewManager>.Instance.SwitchToView(manager.defaultView);
            Singleton<InteractionCursor>.Instance.ClearForcedCursorType();
            manager.CancelledSacrifice = true;
        }
        else
        {
            manager.SetSacrificeMarkersValue(manager.GetValueOfSacrifices(manager.currentSacrifices));
            yield return new WaitForSeconds(0.2f);
            manager.HideSacrificeMarkers();
            foreach (CardSlot currentSacrifice2 in manager.currentSacrifices)
            {
                if (currentSacrifice2.Card != null && !currentSacrifice2.Card.Dead)
                {
                    manager.SacrificesMadeThisTurn++;
                    currentSacrificedCardInfos.Add(currentSacrifice2.Card.Info, currentSacrifice2);
                    yield return currentSacrifice2.Card.Sacrifice();
                    Singleton<ViewManager>.Instance.SwitchToView(manager.BoardView);
                }
            }
        }
        manager.SetQueueSlotsEnabled(slotsEnabled: true);
        foreach (CardSlot allSlot3 in manager.AllSlots)
        {
            allSlot3.SetEnabled(enabled: true);
            allSlot3.ShowState(HighlightedInteractable.State.Interactable);
        }
        manager.currentSacrificeDemandingCard = null;
        manager.currentSacrifices.Clear();
    }

    private bool CanAfford()
    {
        if (BloodCost <= 0 || SacrificeValue() >= BloodCost)
        {
            if (base.Card.Health >= HealthCost)
            {
                if (Singleton<ResourcesManager>.Instance.PlayerEnergy >= EnergyCost)
                    return Singleton<ResourcesManager>.Instance.PlayerBones >= BonesCost;
            }
        }
        return false;
    }
    private bool LearnMechanic() => SaveManager.SaveFile.IsPart2 && !ProgressionData.LearnedMechanic(MechanicsConcept.GBCActivatedAbilities);
    private int SacrificeValue()
    {
        return Singleton<BoardManager>.Instance.GetValueOfSacrifices(
            Singleton<BoardManager>.Instance.GetCardSlots(!base.Card.OpponentCard, x => x.Card != null && x.Card != base.Card));
    }
}

/// <summary>
/// A custom AbilityBehaviour that's attached to the DeathShield ability so it functions with the API's shield logic changes.
/// </summary>
public class APIDeathShield : DamageShieldBehaviour
{
    public override Ability Ability => Ability.DeathShield;
    public override int StartingNumShields => base.Card.GetAbilityStacks(Ability); // look ma, I stack!
}