using DiskCardGame;
using System.Collections;

namespace InscryptionAPI.Triggers;

/// <summary>
/// Trigger that is triggered when the card is drawn, but after it has been added to the list of cards in hand.
/// </summary>
public interface IOnAddedToHand
{
    /// <summary>
    /// Returns true if this should trigger when drawn, *after* being added to the list of cards in hand.
    /// </summary>
    /// <returns>True if this should trigger when drawn, *after* being added to the list of cards in hand.</returns>
    public bool RespondsToAddedToHand();
    /// <summary>
    /// Trigger whatever events you want to run when the card is drawn, *after* being added to the list of cards in hand.
    /// </summary>
    /// <returns>.</returns>
    public IEnumerator OnAddedToHand();
}

/// <summary>
/// Trigger that is triggered when any card is drawn, but after it has been added to the list of cards in hand.
/// </summary>
public interface IOnOtherCardAddedToHand
{
    /// <summary>
    /// Returns true if this should trigger when any card is drawn, *after* being added to the list of cards in hand.
    /// </summary>
    /// <param name="card">The card that was drawn.</param>
    /// <returns>True if this should trigger when any card is drawn, *after* being added to the list of cards in hand.</returns>
    public bool RespondsToOtherCardAddedToHand(PlayableCard card);
    /// <summary>
    /// Trigger whatever events you want to run when any card is drawn, *after* being added to the list of cards in hand.
    /// </summary>
    /// <param name="card">The card that was drawn.</param>
    /// <returns>.</returns>
    public IEnumerator OnOtherCardAddedToHand(PlayableCard card);
}

/// <summary>
/// Trigger that is triggered after any card is assigned to a slot, but only if it was assigned to a slot before.
/// </summary>
public interface IOnCardAssignedToSlotNoResolve
{
    /// <summary>
    /// Returns true if this should trigger when any card gets assigned to a new slot, with an exception of being placed on board for the first time.
    /// </summary>
    /// <param name="card">The card that was assigned to a new slot.</param>
    /// <returns>True if this should trigger when any card gets assigned to a new slot, with an exception of being placed on board for the first time.</returns>
    public bool RespondsToCardAssignedToSlotNoResolve(PlayableCard card);
    /// <summary>
    /// Trigger whatever events you want to run when any card gets assigned to a new slot, with an exception of being placed on board for the first time.
    /// </summary>
    /// <param name="card">The card that was assigned to a new slot.</param>
    /// <returns>.</returns>
    public IEnumerator OnCardAssignedToSlotNoResolve(PlayableCard card);
}

/// <summary>
/// Trigger that is triggered after any card is assigned to a slot. The difference between this and normal OnOtherCardAssignedToSlot is that this trigger also provides information about the new and old slot.
/// </summary>
public interface IOnCardAssignedToSlotContext
{
    /// <summary>
    /// Returns true if this should trigger when any card gets assigned to a new slot.
    /// </summary>
    /// <param name="card">The card that got assigned to a new slot.</param>
    /// <param name="oldSlot">The slot for the card before it moved.</param>
    /// <param name="newSlot">The slot for the card after it moved.</param>
    /// <returns>True if this should trigger when any card gets assigned to a new slot.</returns>
    public bool RespondsToCardAssignedToSlotContext(PlayableCard card, CardSlot oldSlot, CardSlot newSlot);
    /// <summary>
    /// Trigger whatever events you want to run when any card gets assigned to a new slot.
    /// </summary>
    /// <param name="card">The card that got assigned to a new slot.</param>
    /// <param name="oldSlot">The slot for the card before it moved.</param>
    /// <param name="newSlot">The slot for the card after it moved.</param>
    /// <returns>.</returns>
    public IEnumerator OnCardAssignedToSlotContext(PlayableCard card, CardSlot oldSlot, CardSlot newSlot);
}

/// <summary>
/// Trigger that is triggered after the combat phase starts.
/// </summary>
public interface IOnBellRung
{
    /// <summary>
    /// Returns true if this should trigger after the combat phase starts.
    /// </summary>
    /// <param name="playerCombatPhase">True if the player is the attacker in the attack phase.</param>
    /// <returns>True if this should trigger after the combat phase starts.</returns>
    public bool RespondsToBellRung(bool playerCombatPhase);
    /// <summary>
    /// Trigger whatever events you want to run after the combat phase starts.
    /// </summary>
    /// <param name="playerCombatPhase">True if the player is the attacker in the attack phase.</param>
    /// <returns>.</returns>
    public IEnumerator OnBellRung(bool playerCombatPhase);
}

/// <summary>
/// Trigger that is triggered before a slot does its attacks.
/// </summary>
public interface IOnPreSlotAttackSequence
{
    /// <summary>
    /// Returns true if this should trigger before a slot does its attacks.
    /// </summary>
    /// <param name="attackingSlot">The slot that is about to do its attacks.</param>
    /// <returns>True if this should trigger before a slot does its attacks.</returns>
    public bool RespondsToPreSlotAttackSequence(CardSlot attackingSlot);
    /// <summary>
    /// Trigger whatever events you want to run before a slot does its attacks.
    /// </summary>
    /// <param name="attackingSlot">The slot that is about to do its attacks.</param>
    /// <returns>.</returns>
    public IEnumerator OnPreSlotAttackSequence(CardSlot attackingSlot);
}

/// <summary>
/// Trigger that is triggered after a slot does its attacks.
/// </summary>
public interface IOnPostSlotAttackSequence
{
    /// <summary>
    /// Returns true if this should trigger after a slot does its attacks.
    /// </summary>
    /// <param name="attackingSlot">The slot that just did its attacks.</param>
    /// <returns>True if this should trigger after a slot does its attacks.</returns>
    public bool RespondsToPostSlotAttackSequence(CardSlot attackingSlot);
    /// <summary>
    /// Trigger whatever events you want to run after a slot does its attacks.
    /// </summary>
    /// <param name="attackingSlot">The slot that just did its attacks.</param>
    /// <returns>.</returns>
    public IEnumerator OnPostSlotAttackSequence(CardSlot attackingSlot);
}

/// <summary>
/// Trigger that is triggered after a slot does an individual attack.
/// </summary>
public interface IOnPostSingularSlotAttackSlot
{
    /// <summary>
    /// Returns true if this should trigger after a slot does an individual attack.
    /// </summary>
    /// <param name="attackingSlot">The slot that has just did the attack.</param>
    /// <param name="targetSlot">The slot that the attacking slot attacked.</param>
    /// <returns>True if this should trigger after a slot does an individual attack.</returns>
    public bool RespondsToPostSingularSlotAttackSlot(CardSlot attackingSlot, CardSlot targetSlot);
    /// <summary>
    /// Trigger whatever events you want to run after a slot does an individual attack.
    /// </summary>
    /// <param name="attackingSlot">The slot that has just did the attack.</param>
    /// <param name="targetSlot">The slot that the attacking slot attacked.</param>
    /// <returns>.</returns>
    public IEnumerator OnPostSingularSlotAttackSlot(CardSlot attackingSlot, CardSlot targetSlot);
}

/// <summary>
/// Data collection trigger that is triggered before the scales are changed, can be used to change the amount of damage added and to which side it's added.
/// </summary>
public interface IOnPreScalesChangedRef
{
    /// <summary>
    /// Returns true if this should modify the damage added to the scales.
    /// </summary>
    /// <param name="damage">Number of damage currently dealt.</param>
    /// <param name="numWeights">Number of weights currently added to the scales.</param>
    /// <param name="toPlayer">True if the damage is dealt to the player.</param>
    /// <returns>.</returns>
    public bool RespondsToPreScalesChangedRef(int damage, int numWeights, bool toPlayer);
    /// <summary>
    /// Returns the new amount of damage that will be added to the scales.
    /// </summary>
    /// <param name="damage">Number of damage currently dealt.</param>
    /// <param name="numWeights">Number of weights currently added to the scales. Change this value to modify that number.</param>
    /// <param name="toPlayer">True if the damage is dealt to the player. Change this value to modify the side the damage is getting added to.</param>
    /// <returns>The new amount of damage that will be added to the scales.</returns>
    public int CollectPreScalesChangedRef(int damage, ref int numWeights, ref bool toPlayer);
}

/// <summary>
/// Trigger that is triggered before the scales are changed, after IOnPreScalesChangedRef. Also includes information about the original damage and side that the damage is added at, before those values potentially get changed by IOnPreScalesChangedRef.
/// </summary>
public interface IOnPreScalesChanged
{
    /// <summary>
    /// Returns true if this should trigger before damage is added to the scales.
    /// </summary>
    /// <param name="damage">The damage that is about to get added.</param>
    /// <param name="toPlayer">True if the damage is getting added to the player's side of the scales.</param>
    /// <param name="originalDamage">Original damage that would get added, before getting changed by IOnPreScalesChangedRef.</param>
    /// <param name="originalToPlayer">True if the damage was originally going to get added to the player's side, before getting changed by IOnPreScalesChangedRef.</param>
    /// <returns>True if this should trigger before damage is added to the scales.</returns>
    public bool RespondsToPreScalesChanged(int damage, bool toPlayer, int originalDamage, bool originalToPlayer);
    /// <summary>
    /// Run whatever events you want to trigger before damage is added to the scales.
    /// </summary>
    /// <param name="damage">The damage that is about to get added.</param>
    /// <param name="toPlayer">True if the damage is getting added to the player's side of the scales.</param>
    /// <param name="originalDamage">Original damage that would get added, before getting changed by IOnPreScalesChangedRef.</param>
    /// <param name="originalToPlayer">True if the damage was originally going to get added to the player's side, before getting changed by IOnPreScalesChangedRef.</param>
    /// <returns>.</returns>
    public IEnumerator OnPreScalesChanged(int damage, bool toPlayer, int originalDamage, bool originalToPlayer);
}

/// <summary>
/// Trigger that is triggered after the scales are changed. Also includes information about the original damage and side that the damage is added at, before those values potentially get changed by IOnPreScalesChangedRef.
/// </summary>
public interface IOnPostScalesChanged
{
    /// <summary>
    /// Returns true if this should trigger after damage is added to the scales.
    /// </summary>
    /// <param name="damage">The damage that got added to the scales.</param>
    /// <param name="toPlayer">True if the damage got added to the player's side of the scales.</param>
    /// <param name="originalDamage">Original damage that would get added, before getting changed by IOnPreScalesChangedRef.</param>
    /// <param name="originalToPlayer">True if the damage was originally going to get added to the player's side, before getting changed by IOnPreScalesChangedRef.</param>
    /// <returns>True if this should trigger after damage is added to the scales.</returns>
    public bool RespondsToPostScalesChanged(int damage, bool toPlayer, int originalDamage, bool originalToPlayer);
    /// <summary>
    /// Run whatever events you want to trigger after damage is added to the scales.
    /// </summary>
    /// <param name="damage">The damage that got added to the scales.</param>
    /// <param name="toPlayer">True if the damage got added to the player's side of the scales.</param>
    /// <param name="originalDamage">Original damage that would get added, before getting changed by IOnPreScalesChangedRef.</param>
    /// <param name="originalToPlayer">True if the damage was originally going to get added to the player's side, before getting changed by IOnPreScalesChangedRef.</param>
    /// <returns>.</returns>
    public IEnumerator OnPostScalesChanged(int damage, bool toPlayer, int originalDamage, bool originalToPlayer);
}

/// <summary>
/// Trigger that is triggered each turn, but unlike normal OnUpkeep this one only works in hand.
/// </summary>
public interface IOnUpkeepInHand
{
    /// <summary>
    /// Returns true if this should trigger at the start of the turn, but only if this card is in hand.
    /// </summary>
    /// <param name="playerUpkeep">True if it's the start of the player's turn, false otherwise.</param>
    /// <returns>True if this should trigger at the start of the turn, but only if this card is in hand.</returns>
    public bool RespondsToUpkeepInHand(bool playerUpkeep);
    /// <summary>
    /// Trigger whatever events you want to run at the start of the turn, but only if this card is in hand.
    /// </summary>
    /// <param name="playerUpkeep">True if it's the start of the player's turn, false otherwise.</param>
    /// <returns>.</returns>
    public IEnumerator OnUpkeepInHand(bool playerUpkeep);
}

/// <summary>
/// Trigger that is triggered when any card gets played, but unlike normal OnOtherCardResolveOnBoard this one only works in hand.
/// </summary>
public interface IOnOtherCardResolveInHand
{
    /// <summary>
    /// Returns true if this should trigger when any card gets played, but only if this card is in hand. 
    /// </summary>
    /// <param name="resolvingCard">The card that got played.</param>
    /// <returns>True if this should trigger when any card gets played, but only if this card is in hand. .</returns>
    public bool RespondsToOtherCardResolveInHand(PlayableCard resolvingCard);
    /// <summary>
    /// Trigger whatever events you want to run when any card gets played, but only if this card is in hand.
    /// </summary>
    /// <param name="resolvingCard">The card that got played.</param>
    /// <returns>.</returns>
    public IEnumerator OnOtherCardResolveInHand(PlayableCard resolvingCard);
}

/// <summary>
/// Trigger that is triggered when the turn ends, but unlike normal OnTurnEnd this one only works in hand.
/// </summary>
public interface IOnTurnEndInHand
{
    /// <summary>
    /// Returns true if this should trigger when the turn ends, but only if this card is in hand.
    /// </summary>
    /// <param name="playerTurn">True if it's the end of the player's turn, false otherwise.</param>
    /// <returns>True if this should trigger when the turn ends, but only if this card is in hand.</returns>
    public bool RespondsToTurnEndInHand(bool playerTurn);
    /// <summary>
    /// Trigger whatever events you want to run when the turn ends, but only if this card is in hand.
    /// </summary>
    /// <param name="playerTurn">True if it's the end of the player's turn, false otherwise.</param>
    /// <returns>.</returns>
    public IEnumerator OnTurnEndInHand(bool playerTurn);
}

/// <summary>
/// Trigger that is triggered when any card is assigned to a slot, but unlike normal OnOtherCardAssignedToSlotInHand this one only works in hand.
/// </summary>
public interface IOnOtherCardAssignedToSlotInHand
{
    /// <summary>
    /// Returns true if this should trigger when any card is assigned to a new slot, but only if this card is in hand.
    /// </summary>
    /// <param name="card">The card that got assigned to a new slot.</param>
    /// <returns>True if this should trigger when any card is assigned to a new slot, but only if this card is in hand.</returns>
    public bool RespondsToOtherCardAssignedToSlotInHand(PlayableCard card);
    /// <summary>
    /// Trigger whatever events you want to run when any card is assigned to a new slot, but only if this card is in hand.
    /// </summary>
    /// <param name="card">The card that got assigned to a new slot.</param>
    /// <returns>.</returns>
    public IEnumerator OnOtherCardAssignedToSlotInHand(PlayableCard card);
}

/// <summary>
/// Trigger that is triggered before any card dies, but unlike normal OnOtherCardPreDeath this one only works in hand.
/// </summary>
public interface IOnOtherCardPreDeathInHand
{
    /// <summary>
    /// Returns true if this should trigger before any card dies, but only if this card is in hand.
    /// </summary>
    /// <param name="deathSlot">The slot that the dying card died in.</param>
    /// <param name="fromCombat">False if it was killed by a sacrifice, true otherwise.</param>
    /// <param name="killer">The card that killed the dying card. Can be null.</param>
    /// <returns>True if this should trigger before any card dies, but only if this card is in hand.</returns>
    public bool RespondsToOtherCardPreDeathInHand(CardSlot deathSlot, bool fromCombat, PlayableCard killer);
    /// <summary>
    /// Trigger whatever events you want to run before any card dies, but only if this card is in hand.
    /// </summary>
    /// <param name="deathSlot">The slot that the dying card died in.</param>
    /// <param name="fromCombat">False if it was killed by a sacrifice, true otherwise.</param>
    /// <param name="killer">The card that killed the dying card. Can be null.</param>
    /// <returns>.</returns>
    public IEnumerator OnOtherCardPreDeathInHand(CardSlot deathSlot, bool fromCombat, PlayableCard killer);
}

/// <summary>
/// Trigger that is triggered when any card deals damage to another card, but unlike normal OnOtherCardDealtDamage this one only works in hand.
/// </summary>
public interface IOnOtherCardDealtDamageInHand
{
    /// <summary>
    /// Returns true if this should trigger when any card deals damage to another card, but only if this card is in hand.
    /// </summary>
    /// <param name="attacker">The card that attacked another card.</param>
    /// <param name="amount">The damage that was dealt to the target.</param>
    /// <param name="target">The card that got attacked by the attacker.</param>
    /// <returns>True if this should trigger when any card deals damage to another card, but only if this card is in hand.</returns>
    public bool RespondsToOtherCardDealtDamageInHand(PlayableCard attacker, int amount, PlayableCard target);
    /// <summary>
    /// Trigger whatever events you want to run when any card deals damage to another card, but only if this card is in hand.
    /// </summary>
    /// <param name="attacker">The card that attacked another card.</param>
    /// <param name="amount">The damage that was dealt to the target.</param>
    /// <param name="target">The card that got attacked by the attacker.</param>
    /// <returns>.</returns>
    public IEnumerator OnOtherCardDealtDamageInHand(PlayableCard attacker, int amount, PlayableCard target);
}

/// <summary>
/// Trigger that is triggered after any card dies, but unlike normal OnOtherCardDie this one only works in hand.
/// </summary>
public interface IOnOtherCardDieInHand
{
    /// <summary>
    /// Returns true if this should trigger after any card dies, but only if this card is in hand.
    /// </summary>
    /// <param name="card">The card that is dying.</param>
    /// <param name="deathSlot">The slot that the card died in.</param>
    /// <param name="fromCombat">False if the card was killed by a sacrifice, true otherwise.</param>
    /// <param name="killer">The card that killed the dying card. Can be null.</param>
    /// <returns>True if this should trigger after any card dies, but only if this card is in hand.</returns>
    public bool RespondsToOtherCardDieInHand(PlayableCard card, CardSlot deathSlot, bool fromCombat, PlayableCard killer);
    /// <summary>
    /// Trigger whatever events you want to run after any card dies, but only if this card is in hand.
    /// </summary>
    /// <param name="card">The card that is dying.</param>
    /// <param name="deathSlot">The slot that the card died in.</param>
    /// <param name="fromCombat">False if the card was killed by a sacrifice, true otherwise.</param>
    /// <param name="killer">The card that killed the dying card. Can be null.</param>
    /// <returns>.</returns>
    public IEnumerator OnOtherCardDieInHand(PlayableCard card, CardSlot deathSlot, bool fromCombat, PlayableCard killer);
}

/// <summary>
/// Trigger that is triggered before any item is used.
/// </summary>
public interface IOnPreItemUsed
{
    /// <summary>
    /// Returns true if this should trigger before an item is used.
    /// </summary>
    /// <param name="itemName">Internal name of the item.</param>
    /// <param name="isHammer">True if the item is the act 3 hammer, false otherwise.</param>
    /// <returns>True if this should trigger before an item is used.</returns>
    public bool RespondsToPreItemUsed(string itemName, bool isHammer);
    /// <summary>
    /// Trigger whatever events you want to run before an item is used.
    /// </summary>
    /// <param name="itemName">Internal name of the item.</param>
    /// <param name="isHammer">True if the item is the act 3 hammer, false otherwise.</param>
    /// <returns>.</returns>
    public IEnumerator OnPreItemUsed(string itemName, bool isHammer);
}

/// <summary>
/// Trigger that is triggered after any item is used.
/// </summary>
public interface IOnPostItemUsed
{
    /// <summary>
    /// Returns true if this should trigger after an item is used.
    /// </summary>
    /// <param name="itemName">Internal name of the item.</param>
    /// <param name="success">Normally true, but false if a targeted use item (e.g. scissors) got the use cancelled.</param>
    /// <param name="isHammer">True if the item is the act 3 hammer, false otherwise.</param>
    /// <returns>True if this should trigger after an item is used.</returns>
    public bool RespondsToPostItemUsed(string itemName, bool success, bool isHammer);
    /// <summary>
    /// Trigger whatever events you want to run after an item is used.
    /// </summary>
    /// <param name="itemName">Internal name of the item.</param>
    /// <param name="success">Normally true, but false if a targeted use item (e.g. scissors) got the use cancelled.</param>
    /// <param name="isHammer">True if the item is the act 3 hammer, false otherwise.</param>
    /// <returns>.</returns>
    public IEnumerator OnPostItemUsed(string itemName, bool success, bool isHammer);
}

/// <summary>
/// Run whenever a card is asked what its opposing slots are as part of the attack sequence
/// </summary>
public interface IGetOpposingSlots
{
    /// <summary>
    /// Indicates if this card wants to respond to getting the opposing slots
    /// </summary>
    /// <returns>True to provide opposing slots, False to leave the slots as default.</returns>
    bool RespondsToGetOpposingSlots();

    /// <summary>
    /// Gets the card slots that the card wants to attack
    /// </summary>
    /// <param name="originalSlots">The set of original card slots that the card would attack (as set by abilities in the base game).</param>
    /// <param name="otherAddedSlots">Slots that have been added by other custom abilities.</param>
    /// <returns>The list of card slots you want to attack.</returns>
    /// <remarks>If your card is replacing the default attack slot (the opposing slot) see 'RemoveDefaultAttackSlot'
    /// If you are **not** replacing the default attack slot, do **not** include that here. Simple ensure 'RemoveDefaultAttackSlot' returns false.
    /// Only return the default attack slot if you want to attack it an additional time.</remarks>
    List<CardSlot> GetOpposingSlots(List<CardSlot> originalSlots, List<CardSlot> otherAddedSlots);

    /// <summary>
    /// If true, this means that the attack slots provided by GetOpposingSlots should override the default attack slot. 
    /// If false, it means they should be in addition to the default opposing slot.
    /// </summary>
    bool RemoveDefaultAttackSlot();
}

/// <summary>
/// Data collection trigger that collects data related to attacked slots.
/// </summary>
public interface ISetupAttackSequence
{
    /// <summary>
    /// If true, this trigger will collect data from CollectModifyAttackSlots.
    /// </summary>
    /// <param name="card">Card whose slots will be modified.</param>
    /// <param name="modType">Type of modification that is currently triggering.</param>
    /// <param name="originalSlots">Original slots for attack.</param>
    /// <param name="currentSlots">Current slots that are targeted.</param>
    /// <param name="attackCount">How many attacks the card does.</param>
    /// <param name="didRemoveDefaultSlot">True if the original (opposing) slot was removed by Bifurcated Strike or any other ability that does it.</param>
    /// <returns>True if this trigger will collect data from CollectModifyAttackSlots.</returns>
    public bool RespondsToModifyAttackSlots(PlayableCard card, OpposingSlotTriggerPriority modType, List<CardSlot> originalSlots, List<CardSlot> currentSlots, int attackCount, bool didRemoveDefaultSlot);
    /// <summary>
    /// Modifies the slots that will be targeted of any card.
    /// </summary>
    /// <param name="card">Card whose slots will be modified.</param>
    /// <param name="modType">Type of modification that is currently triggering.</param>
    /// <param name="originalSlots">Original slots for attack.</param>
    /// <param name="currentSlots">Current slots that are targeted.</param>
    /// <param name="attackCount">How many attacks the card does.</param>
    /// <param name="didRemoveDefaultSlot">True if the original (opposing) slot was removed by Bifurcated Strike or any other ability that does it.</param>
    /// <returns>Modified data about targeted slots for attack.</returns>
    public List<CardSlot> CollectModifyAttackSlots(PlayableCard card, OpposingSlotTriggerPriority modType, List<CardSlot> originalSlots, List<CardSlot> currentSlots, ref int attackCount, ref bool didRemoveDefaultSlot);
    /// <summary>
    /// Gets the priority for this trigger. Triggers with a higher priority will trigger first.
    /// </summary>
    /// <param name="card">Card whose slots will be modified.</param>
    /// <param name="modType">Type of modification that is currently triggering.</param>
    /// <param name="originalSlots">Original slots for attack.</param>
    /// <param name="currentSlots">Current slots that are targeted. NOTE: THIS VALUE IS *NOT* UP TO DATE, GETTING PRIORITIES HAPPENS *BEFORE* THE MODIFICATION ITSELF.</param>
    /// <param name="attackCount">How many attacks the card does. NOTE: THIS VALUE IS *NOT* UP TO DATE, GETTING PRIORITIES HAPPENS *BEFORE* THE MODIFICATION ITSELF.</param>
    /// <param name="didRemoveDefaultSlot">True if the original (opposing) slot was removed by Bifurcated Strike or any other ability that does it. NOTE: THIS VALUE IS *NOT* UP TO DATE, GETTING PRIORITIES HAPPENS *BEFORE* THE MODIFICATION ITSELF.</param>
    /// <returns>The priority for this trigger.</returns>
    public int GetTriggerPriority(PlayableCard card, OpposingSlotTriggerPriority modType, List<CardSlot> originalSlots, List<CardSlot> currentSlots, int attackCount, bool didRemoveDefaultSlot);
}

/// <summary>
/// Flag that tells IGetOpposingSlots when to trigger.
/// </summary>
public enum OpposingSlotTriggerPriority
{
    /// <summary>
    /// Normal mode - use this for opposing slot modifications that either don't do anything to the original opposing slots or remove it.
    /// </summary>
    Normal,
    /// <summary>
    /// Bring back opposing slot - use this for opposing slot modifications that bring back the original opposing slot if it was removed like Trifurcated Strike.
    /// </summary>
    BringsBackOpposingSlot,
    /// <summary>
    /// Post slot addition modification - use this for opposing slot modifications that don't interact with any other modification and instead just statically modify the finished result like Double Strike.
    /// </summary>
    PostAdditionModification,
    /// <summary>
    /// Replaces default opposing slot - use this for opposing slot modifications that replace the original opposing slot like Omni Strike.
    /// </summary>
    ReplacesDefaultOpposingSlot
}

/// <summary>
/// Data collection trigger that collects data about the usability of an item.
/// </summary>
public interface IItemCanBeUsed
{
    /// <summary>
    /// Returns true if this should modify if an item can be used.
    /// </summary>
    /// <param name="itemname">The name of the item that is about to get used.</param>
    /// <param name="currentValue">True if it can be used currently.</param>
    /// <returns>True if this should modify if an item can be used.</returns>
    public bool RespondsToItemCanBeUsed(string itemname, bool currentValue);
    /// <summary>
    /// Returns true if an item can be used.
    /// </summary>
    /// <param name="itemname">The name of the item that is about to get used.</param>
    /// <param name="currentValue">True if it can be used currently.</param>
    /// <returns>True if the item can be used, false otherwise.</returns>
    public bool CollectItemCanBeUsed(string itemname, bool currentValue);
}

/// <summary>
/// Trigger that is triggered when an item is prevented from use using IItemCanBeUsed.
/// </summary>
public interface IOnItemPreventedFromUse
{
    /// <summary>
    /// Returns true if this should trigger when an item is prevented from use using IItemCanBeUsed.
    /// </summary>
    /// <param name="itemName">Internal name of the item.</param>
    /// <returns>True if this should trigger when an item is prevented from use using IItemCanBeUsed.</returns>
    public bool RespondsToItemPreventedFromUse(string itemName);
    /// <summary>
    /// Trigger whatever events you want to run when an item is prevented from use using IItemCanBeUsed.
    /// </summary>
    /// <param name="itemName">Internal name of the item.</param>
    /// <returns>.</returns>
    public IEnumerator OnItemPreventedFromUse(string itemName);
}

/// <summary>
/// Data collection trigger that modifies the passive attack buffs of any card.
/// </summary>
public interface IOnCardPassiveAttackBuffs
{
    /// <summary>
    /// Returns true if this should modify the passive attack buffs given to a card.
    /// </summary>
    /// <param name="card">Card the buffs will be given to.</param>
    /// <param name="currentValue">The current buff value.</param>
    /// <returns>True if this should modify the passive attack buffs given to a card.</returns>
    public bool RespondsToCardPassiveAttackBuffs(PlayableCard card, int currentValue);
    /// <summary>
    /// Returns the new attack buff value for a card.
    /// </summary>
    /// <param name="card">Card the buffs will be given to.</param>
    /// <param name="currentValue">The current buff value.</param>
    /// <returns>The new attack buff value for a card.</returns>
    public int CollectCardPassiveAttackBuffs(PlayableCard card, int currentValue);
}

/// <summary>
/// Data collection trigger that modifies the passive health buffs of any card.
/// </summary>
public interface IOnCardPassiveHealthBuffs
{
    /// <summary>
    /// Returns true if this should modify the passive health buffs given to a card.
    /// </summary>
    /// <param name="card">Card the buffs will be given to.</param>
    /// <param name="currentValue">The current buff value.</param>
    /// <returns>True if this should modify the passive health buffs given to a card.</returns>
    public bool RespondsToCardPassiveHealthBuffs(PlayableCard card, int currentValue);
    /// <summary>
    /// Returns the new health buff value for a card.
    /// </summary>
    /// <param name="card">Card the buffs will be given to.</param>
    /// <param name="currentValue">The current buff value.</param>
    /// <returns>The new health buff value for a card.</returns>
    public int CollectCardPassiveHealthBuffs(PlayableCard card, int currentValue);
}

/// <summary>
/// Used when a card wants to provide a passive buff to other cards on the board
/// </summary>
public interface IPassiveHealthBuff
{
    /// <summary>
    /// Used to provide a passive health buff to a target
    /// </summary>
    /// <returns>The amount of health you want to buff the target by.</returns>
    /// <remarks>Do not assume that the target is on your side of the board! There may be negative sigils that buff opposing cards.</remarks>
    int GetPassiveHealthBuff(PlayableCard target);
}

/// <summary>
/// Used when a card wants to provide a passive buff to other cards on the board
/// </summary>
public interface IPassiveAttackBuff
{
    /// <summary>
    /// Used to provide a passive attack buff to a target
    /// </summary>
    /// <returns>The amount of attack you want to buff the target by.</returns>
    /// <remarks>Do not assume that the target is on your side of the board! There may be negative sigils that buff opposing cards.</remarks>
    int GetPassiveAttackBuff(PlayableCard target);
}

/// <summary>
/// Data collection trigger that modifies the damage taken by any card.
/// </summary>
[Obsolete("Use IModifyDamageTaken instead.")]
public interface ICardTakenDamageModifier
{
    /// <summary>
    /// Returns true if this should modify the amount of damage taken by a card.
    /// </summary>
    /// <param name="card">Card that took damage.</param>
    /// <param name="currentValue">The current amount of damage dealt.</param>
    /// <returns>True if this should modify the amount of damage taken by a card.</returns>
    public bool RespondsToCardTakenDamageModifier(PlayableCard card, int currentValue);
    /// <summary>
    /// Returns the new amount of damage that will be taken by a card.
    /// </summary>
    /// <param name="card">Card that took damage.</param>
    /// <param name="currentValue">The current amount of damage dealt.</param>
    /// <returns>The new amount of damage that will be taken by a card.</returns>
    public int CollectCardTakenDamageModifier(PlayableCard card, int currentValue);
}

/// <summary>
/// Used when changing what CardSlots are queued for attacking.
/// Triggers before null and Powerless cards are removed, so no making empty slots attack and no making Attack-less cards attack.
/// </summary>
public interface IGetAttackingSlots
{
    /// <summary>
    /// Returns true if this should modify the list of attacking CardSlots.
    /// </summary>
    /// <param name="playerIsAttacker">Whether the player is current attacking.</param>
    /// <param name="originalSlots">The vanilla list of attacking CardSlots; a copy of the current attacker's side of the board.</param>
    /// <param name="currentSlots">The current list of attacking CardSlots, after modifications and the like.</param>
    /// <returns>True if this should modify the amount of damage taken by a card.</returns>
    public bool RespondsToGetAttackingSlots(bool playerIsAttacker, List<CardSlot> originalSlots, List<CardSlot> currentSlots);

    /// <summary>
    /// Returns the new list of attacking CardSlots.
    /// </summary>
    /// <param name="playerIsAttacker">Whether the player is current attacking.</param>
    /// <param name="originalSlots">The vanilla list of attacking CardSlots.</param>
    /// <param name="currentSlots">The current list of attacking CardSlots.</param>
    /// <returns>The new list of attacking CardSlots.</returns>
    public List<CardSlot> GetAttackingSlots(bool playerIsAttacker, List<CardSlot> originalSlots, List<CardSlot> currentSlots);

    /// <summary>
    /// Trigger priority. Higher numbers trigger first.
    /// </summary>
    /// <param name="playerIsAttacker">Whether the player is current attacking.</param>
    /// <param name="originalSlots">The vanilla list of attacking CardSlots.</param>
    /// <returns>The trigger priority int.</returns>
    public int TriggerPriority(bool playerIsAttacker, List<CardSlot> originalSlots);
}

/// <summary>
/// Data collection trigger that modifies the damage taken by any card.
/// Should be used instead of ICardTakenDamageModifier.
/// </summary>
public interface IModifyDamageTaken
{
    /// <summary>
    /// Returns true if this should modify the amount of damage taken by a card.
    /// </summary>
    /// <param name="target">The card that took damage.</param>
    /// <param name="damage">The current amount of damage to be delt.</param>
    /// <param name="attacker">The attacking card.</param>
    /// <param name="originalDamage">The original amount of damage to be delt.</param>
    /// <returns>True if this should modify the amount of damage taken by a card.</returns>
    public bool RespondsToModifyDamageTaken(PlayableCard target, int damage, PlayableCard attacker, int originalDamage);

    /// <summary>
    /// Returns the new amount of damage taken by a card.
    /// </summary>
    /// <param name="target">The card that took damage.</param>
    /// <param name="damage">The current amount of damage to be delt.</param>
    /// <param name="attacker">The attacking card.</param>
    /// <param name="originalDamage">The original amount of damage to be delt.</param>
    /// <returns>The new amount of damage taken by a card.</returns>
    public int OnModifyDamageTaken(PlayableCard target, int damage, PlayableCard attacker, int originalDamage);

    /// <summary>
    /// Trigger priority. Higher numbers trigger first.
    /// </summary>
    /// <param name="target">The card that took damage.</param>
    /// <param name="damage">The amount of damage to be delt. NOTE: THIS VALUE IS *NOT* UP TO DATE, GETTING PRIORITIES HAPPENS *BEFORE* THE MODIFICATION ITSELF.</param>
    /// <param name="attacker">The attacking card.</param>
    /// <returns>The trigger priority int.</returns>
    public int TriggerPriority(PlayableCard target, int damage, PlayableCard attacker);
}

/// <summary>
/// Trigger that is triggered right before a card takes damage.
/// </summary>
public interface IPreTakeDamage
{
    /// <summary>
    /// Returns true if this should trigger before a card takes damage.
    /// </summary>
    /// <param name="source">The attacking card; The source of the damage.</param>
    /// <param name="damage">The amount of damage to be delt.</param>
    /// <returns>True if this should trigger before a card takes damage.</returns>
    public bool RespondsToPreTakeDamage(PlayableCard source, int damage);

    /// <summary>
    /// Trigger whatever events you want to run before a card takes damage.
    /// </summary>
    /// <param name="source">The attacking card; The source of the damage.</param>
    /// <param name="damage">The amount of damage to be delt.</param>
    /// <returns>.</returns>
    public IEnumerator OnPreTakeDamage(PlayableCard source, int damage);
}

/// <summary>
/// Data collection trigger that modifies the damage taken when directly attacked.
/// </summary>
public interface IModifyDirectDamage
{
    /// <summary>
    /// Returns true if this should modify the amount of damage taken when directly attacked.
    /// </summary>
    /// <param name="target">The card slot targeted for the attack.</param>
    /// <param name="damage">The current amount of damage to be delt.</param>
    /// <param name="attacker">The attacking card.</param>
    /// <param name="originalDamage">The original amount of damage to be delt.</param>
    /// <returns>True if this should modify the amount of damage taken when directly attacked.</returns>
    public bool RespondsToModifyDirectDamage(CardSlot target, int damage, PlayableCard attacker, int originalDamage);

    public int OnModifyDirectDamage(CardSlot target, int damage, PlayableCard attacker, int originalDamage);

    public int TriggerPriority(CardSlot target, int damage, PlayableCard attacker);
}

/// <summary>
/// Trigger that is triggered when the turn ends, but unlike normal OnTurnEnd this one only works on cards in the opponent's queue.
/// </summary>
public interface IOnTurnEndInQueue
{
    /// <summary>
    /// Returns true if this should trigger when the turn ends, but only if this card is in the opponent's queue.
    /// </summary>
    /// <param name="playerTurn">True if it's the end of the player's turn, false otherwise.</param>
    /// <returns>True if this should trigger when the turn ends, but only if this card is in the opponent's queue.</returns>
    public bool RespondsToTurnEndInQueue(bool playerTurn);

    /// <summary>
    /// Trigger whatever events you want to run when the turn ends, but only if this card is in the opponent's queue.
    /// </summary>
    /// <param name="playerTurn">True if it's the end of the player's turn, false otherwise.</param>
    /// <returns>.</returns>
    public IEnumerator OnTurnEndInQueue(bool playerTurn);
}

/// <summary>
/// Trigger that is triggered when a card deals direct damage.
/// This is a replacement for the vanilla DealDamageDirectly trigger, which unlike the base-game trigger, triggers for all cards on the board and supplies more information.
/// </summary>
public interface IOnCardDealtDamageDirectly
{
    /// <summary>
    /// Returns true if this should trigger when a card deals direct damage.
    /// </summary>
    /// <param name="attacker">The card that dealt the direct damage.</param>
    /// <param name="opposingSlot">The card slot that was targeted for the direct attack.</param>
    /// <param name="damage">The amount of direct damage delt.</param>
    /// <returns>True if this should trigger when a card deals direct damage.</returns>
    public bool RespondsToCardDealtDamageDirectly(PlayableCard attacker, CardSlot opposingSlot, int damage);

    /// <summary>
    /// Trigger whatever events you want to run when a card deals direct damage.
    /// </summary>
    /// <param name="attacker">The card that dealt the direct damage.</param>
    /// <param name="opposingSlot">The card slot that was targeted for the direct attack.</param>
    /// <param name="damage">The amount of direct damage delt.</param>
    /// <returns>.</returns>
    public IEnumerator OnCardDealtDamageDirectly(PlayableCard attacker, CardSlot opposingSlot, int damage);
}

/// <summary>
/// Trigger that is called after a shielded card was attacked and lost a shield.
/// </summary>
public interface IShieldPreventedDamage
{
    public bool RespondsToShieldPreventedDamage(PlayableCard target, int damage, PlayableCard attacker);
    public IEnumerator OnShieldPreventedDamage(PlayableCard target, int damage, PlayableCard attacker);
    public int ShieldPreventedDamagePriority(PlayableCard target, int damage, PlayableCard attacker);
}
/// <summary>
/// Variant of IShieldPreventDamage that triggers for cards in the hand.
/// </summary>
public interface IShieldPreventedDamageInHand
{
    public bool RespondsToShieldPreventedDamageInHand(PlayableCard target, int damage, PlayableCard attacker);
    public IEnumerator OnShieldPreventedDamageInHand(PlayableCard target, int damage, PlayableCard attacker);
    public int ShieldPreventedDamageInHandPriority(PlayableCard target, int damage, PlayableCard attacker);
}
/*public interface IOnPreTakeDamageFromHammer
{
    public bool RespondsToPreTakeDamageFromHammer(HammerItem hammer, CardSlot targetSlot, GameObject firstPersonItem);

    public IEnumerator OnPreTakeDamageFromHammer(HammerItem hammer, CardSlot targetSlot, GameObject firstPersonItem);

    public int TriggerPriority(HammerItem hammer, CardSlot targetSlot, GameObject firstPersonItem);
}

public interface IOnPostTakeDamageFromHammer
{
    public bool RespondsToPostTakeDamageFromHammer(HammerItem hammer, CardSlot targetSlot, GameObject firstPersonItem, bool struckTarget);

    public IEnumerator OnPostTakeDamageFromHammer(HammerItem hammer, CardSlot targetSlot, GameObject firstPersonItem, bool struckTarget);

    public int TriggerPriority(HammerItem hammer, CardSlot targetSlot, GameObject firstPersonItem, bool struckTarget);
}*/