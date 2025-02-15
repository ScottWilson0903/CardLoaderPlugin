using DiskCardGame;
using System.Collections;
using UnityEngine;

namespace InscryptionAPI.Card;

public static partial class CardExtensions
{
    #region Ability
    /// <summary>
    /// Checks if the CardModificationInfo does not have a specific Ability.
    /// </summary>
    /// <param name="mod">CardModificationInfo to access.</param>
    /// <param name="ability">The ability to check for.</param>
    /// <returns>Returns true if the ability does not exist.</returns>
    public static bool LacksAbility(this CardModificationInfo mod, Ability ability) => !mod.HasAbility(ability);

    /// <summary>
    /// Checks if the CardInfo does not have a specific Ability.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="ability">The ability to check for.</param>
    /// <returns>Returns true if the ability does not exist.</returns>
    public static bool LacksAbility(this CardInfo cardInfo, Ability ability)
    {
        return !cardInfo.HasAbility(ability);
    }

    /// <summary>
    /// Checks if the CardInfo has all of the given Abilities.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="abilities">The Abilities to check for.</param>
    /// <returns>Returns true if cardInfo has all of the given Abilities.</returns>
    public static bool HasAllAbilities(this CardInfo cardInfo, params Ability[] abilities)
    {
        foreach (Ability ability in abilities)
        {
            if (cardInfo.LacksAbility(ability))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the CardInfo has none of the specified Abilities.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="abilities">The Abilities to check for.</param>
    /// <returns>Returns true if cardInfo has none of the given Abilities.</returns>
    public static bool LacksAllAbilities(this CardInfo cardInfo, params Ability[] abilities)
    {
        foreach (Ability ability in abilities)
        {
            if (cardInfo.HasAbility(ability))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the CardInfo has any of the specified Abilities.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="abilities">The Abilities to check for.</param>
    /// <returns>Returns true if cardInfo has at least one of the given Abilities.</returns>
    public static bool HasAnyOfAbilities(this CardInfo cardInfo, params Ability[] abilities)
    {
        foreach (Ability ability in abilities)
        {
            if (cardInfo.HasAbility(ability))
                return true;
        }
        return false;
    }
    #endregion

    #region SpecialAbility
    /// <summary>
    /// Checks if the CardInfo has a specific SpecialTriggeredAbility.
    ///
    /// A condensed version of `cardInfo.SpecialAbilities.Contains(ability)`.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="ability">The specialTriggeredAbility to check for.</param>
    /// <returns>Returns true if the specialTriggeredAbility does exist.</returns>
    public static bool HasSpecialAbility(this CardInfo cardInfo, SpecialTriggeredAbility ability)
    {
        return cardInfo.SpecialAbilities.Contains(ability);
    }

    /// <summary>
    /// Checks if the CardInfo does not have a specific SpecialTriggeredAbility.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="ability">The specialTriggeredAbility to check for.</param>
    /// <returns>Returns true if the specialTriggeredAbility does not exist.</returns>
    public static bool LacksSpecialAbility(this CardInfo cardInfo, SpecialTriggeredAbility ability)
    {
        return !cardInfo.HasSpecialAbility(ability);
    }

    /// <summary>
    /// Checks if the CardInfo has all of the specified SpecialTriggeredAbilities.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="specialAbilities">The SpecialTriggeredAbilities to check for.</param>
    /// <returns>Returns true if cardInfo has all of the specified SpecialTriggeredAbilities.</returns>
    public static bool HasAllSpecialAbilities(this CardInfo cardInfo, params SpecialTriggeredAbility[] specialAbilities)
    {
        foreach (SpecialTriggeredAbility special in specialAbilities)
        {
            if (cardInfo.LacksSpecialAbility(special))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the CardInfo has none of the specified SpecialTriggeredAbilities.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="specialAbilities">The SpecialTriggeredAbilitiess to check for.</param>
    /// <returns>Returns true if cardInfo has none of the specified SpecialTriggeredAbilities.</returns>
    public static bool LacksAllSpecialAbilities(this CardInfo cardInfo, params SpecialTriggeredAbility[] specialAbilities)
    {
        foreach (SpecialTriggeredAbility special in specialAbilities)
        {
            if (cardInfo.HasSpecialAbility(special))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the CardInfo has any of the specified SpecialTriggeredAbilities.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="specialAbilities">The SpecialTriggeredAbilities to check for.</param>
    /// <returns>Returns true if cardInfo has at least one of the specified SpecialTriggeredAbilities.</returns>
    public static bool HasAnyOfSpecialAbilities(this CardInfo cardInfo, params SpecialTriggeredAbility[] specialAbilities)
    {
        foreach (SpecialTriggeredAbility special in specialAbilities)
        {
            if (cardInfo.HasSpecialAbility(special))
                return true;
        }
        return false;
    }
    #endregion

    #region Trait
    /// <summary>
    /// Checks if the CardInfo does not belong to a specific Tribe.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="tribe">The tribe to check for.</param>
    /// <returns>Returns true if the card is not of the specified tribe.</returns>
    public static bool IsNotOfTribe(this CardInfo cardInfo, Tribe tribe) => !cardInfo.IsOfTribe(tribe);

    /// <summary>
    /// Checks if the CardInfo does not have a specific Trait.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="trait">The Trait to check for.</param>
    /// <returns>Returns true if the card is does not have the specified Trait.</returns>
    public static bool LacksTrait(this CardInfo cardInfo, Trait trait) => !cardInfo.HasTrait(trait);

    /// <summary>
    /// Checks if the CardInfo has all of the specified Traits.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="traits">The Traits to check for.</param>
    /// <returns>Returns true if cardInfo has all of the specified Traits.</returns>
    public static bool HasAllTraits(this CardInfo cardInfo, params Trait[] traits)
    {
        foreach (Trait trait in traits)
        {
            if (cardInfo.LacksTrait(trait))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the CardInfo has none of the specified Traits.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="traits">The Traits to check for.</param>
    /// <returns>Returns true if cardInfo has none of the specified Traits.</returns>
    public static bool LacksAllTraits(this CardInfo cardInfo, params Trait[] traits)
    {
        foreach (Trait trait in traits)
        {
            if (cardInfo.HasTrait(trait))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the CardInfo has any of the specified Traits.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="traits">The Traits to check for.</param>
    /// <returns>Returns true if cardInfo has at least one of the specified Traits.</returns>
    public static bool HasAnyOfTraits(this CardInfo cardInfo, params Trait[] traits)
    {
        foreach (Trait trait in traits)
        {
            if (cardInfo.HasTrait(trait))
                return true;
        }
        return false;
    }

    public static bool IsPelt(this CardInfo cardInfo) => cardInfo.HasTrait(Trait.Pelt);
    public static bool IsTerrain(this CardInfo cardInfo) => cardInfo.HasTrait(Trait.Terrain);
    #endregion

    #region CardMetaCategory
    /// <summary>
    /// Checks if the CardInfo has a specific CardMetaCategory.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="trait">The CardMetaCategory to check for.</param>
    /// <returns>Returns true if the card is does not have the specified CardMetaCategory.</returns>
    public static bool HasCardMetaCategory(this CardInfo cardInfo, CardMetaCategory metaCategory) => cardInfo.metaCategories.Contains(metaCategory);

    /// <summary>
    /// Checks if the CardInfo does not have a specific CardMetaCategory.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="metaCategory">The CardMetaCategory to check for.</param>
    /// <returns>Returns true if the card is does not have the specified CardMetaCategory.</returns>
    public static bool LacksCardMetaCategory(this CardInfo cardInfo, CardMetaCategory metaCategory) => !cardInfo.HasCardMetaCategory(metaCategory);

    /// <summary>
    /// Checks if the CardInfo has any of the specified CardMetaCategories.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="metaCategories">The CardMetaCategories to check for.</param>
    /// <returns>Returns true if the card has at least one of the specified CardMetaCategories.</returns>
    public static bool HasAnyOfCardMetaCategories(this CardInfo cardInfo, params CardMetaCategory[] metaCategories)
    {
        foreach (CardMetaCategory meta in metaCategories)
        {
            if (cardInfo.HasCardMetaCategory(meta))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the CardInfo has none of the specified CardMetaCategories.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="metaCategories">The CardMetaCategories to check for.</param>
    /// <returns>Returns true if the card has none of the specified CardMetaCategories.</returns>
    public static bool LacksAllCardMetaCategories(this CardInfo cardInfo, params CardMetaCategory[] metaCategories)
    {
        foreach (CardMetaCategory meta in metaCategories)
        {
            if (cardInfo.HasCardMetaCategory(meta))
                return false;
        }
        return true;
    }
    #endregion

    #region PlayableCard
    /// <summary>
    /// Gets the health adjustment given by a PlayableCard's Stat Icon.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <returns>How much Health the Stat Icon gives.</returns>
    public static int GetStatIconHealthBuffs(this PlayableCard card)
    {
        int num = 0;
        foreach (CardModificationInfo temporaryMod in card.TemporaryMods)
        {
            if (temporaryMod.singletonId == "VARIABLE_STAT")
                num += temporaryMod.healthAdjustment;
        }
        return num;
    }
    /// <summary>
    /// Gets the number of Ability stacks a card has.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <param name="ability">The Ability to check for.</param>
    /// <returns>The number of Ability stacks the card has.</returns>
    public static int GetAbilityStacks(this PlayableCard card, Ability ability)
    {
        int count = 0;
        count += card.Info.Abilities.Count(a => a == ability);
        count += AbilitiesUtil.GetAbilitiesFromMods(card.TemporaryMods).Count(a => a == ability);
        count -= card.TemporaryMods.SelectMany(m => m.negateAbilities).Count(a => a == ability);

        if (count <= 0)
            return 0;

        // If it's not stackable, you get at most one
        return AbilitiesUtil.GetInfo(ability).canStack ? count : 1;
    }

    /// <summary>
    /// Check if the other PlayableCard is on the same side of the board as this PlayableCard.
    /// </summary>
    /// <param name="playableCard">The PlayableCard to access.</param>
    /// <param name="otherCard">The other PlayableCard.</param>
    /// <returns>Returns true if both cards are on the board and both are on the opponent cards or both are player cards.</returns>
    public static bool OtherCardIsOnSameSide(this PlayableCard playableCard, PlayableCard otherCard)
    {
        return playableCard.OnBoard && otherCard.OnBoard && playableCard.OpponentCard == otherCard.OpponentCard;
    }

    /// <summary>
    /// Retrieve a list of all abilities that exist on the PlayableCard.
    ///
    /// This will retrieve all Abilities from both TemporaryMods and from the underlying CardInfo object.
    /// </summary>
    /// <param name="playableCard">The PlayableCard to access.</param>
    /// <returns>A list of Abilities from the PlayableCard and underlying CardInfo object.</returns>
    public static List<Ability> AllAbilities(this PlayableCard playableCard)
    {
        return playableCard.GetAbilitiesFromAllMods().Concat(playableCard.Info.Abilities).ToList();
    }
    /// <summary>
    /// A variant of PlayableCard.AllAbilities that can account for negated abilities in the PlayableCard's TemporaryMods.
    /// </summary>
    /// <param name="playableCard">The PlayableCard to access.</param>
    /// <param name="accountForNegation">Whether or not to check TemporaryMods for negated abilities.</param>
    /// <returns>A list of Abilities from the PlayableCard and underlying CardInfo object</returns>
    public static List<Ability> AllAbilities(this PlayableCard playableCard, bool accountForNegation)
    {
        List<Ability> retval = playableCard.AllAbilities();
        if (!accountForNegation)
            return retval;

        playableCard.TemporaryMods.ForEach(delegate (CardModificationInfo m)
        {
            m.negateAbilities?.ForEach(delegate (Ability a)
            {
                retval.Remove(a);
            });
        });
        return retval;
    }

    /// <summary>
    /// Retrieve a list of all CardModificationInfos that exist on the PlayableCard.
    ///
    /// This will retrieve all mods from TemporaryMods and the underlying CardInfo.Mods.
    /// </summary>
    /// <param name="playableCard">The PlayableCard to access.</param>
    /// <returns>A list of CardModificationInfos from the PlayableCard and underlying CardInfo object.</returns>
    public static List<CardModificationInfo> AllCardModificationInfos(this PlayableCard playableCard)
    {
        return playableCard.TemporaryMods.Concat(playableCard.Info.Mods).ToList();
    }

    /// <summary>
    /// Removes the provided CardModificationInfo from the PlayableCard.
    ///
    /// Searches both its TemporaryMods list and its CardInfo.Mods list.
    /// </summary>
    /// <param name="playableCard">The PlayableCard to access.</param>
    /// <param name="modToRemove">The CardModificationInfo object to remove.</param>
    /// <param name="updateDisplay">Whether or not to call OnStatsChanged after removing the card mod.</param>
    /// <returns>True if the mod was successfully removed or false if the mod was not removed or was null.</returns>
    public static bool RemoveCardModificationInfo(this PlayableCard playableCard, CardModificationInfo modToRemove, bool updateDisplay = true)
    {
        if (modToRemove == null)
            return false;

        if (playableCard.TemporaryMods.Contains(modToRemove))
        {
            playableCard.RemoveTemporaryMod(modToRemove, updateDisplay);
            return true;
        }
        if (playableCard.Info.Mods.Contains(modToRemove))
        {
            playableCard.Info.Mods.Remove(modToRemove);
            foreach (Ability ability in modToRemove.abilities)
            {
                playableCard.TriggerHandler.RemoveAbility(ability);
            }
            if (updateDisplay)
                playableCard.OnStatsChanged();

            return true;
        }
        return false;
    }
    /// <summary>
    /// Retrieve a list of all special triggered abilities that exist on the PlayableCard.
    ///
    /// This will retrieve all SpecialTriggeredAbility from both TemporaryMods and from the underlying CardInfo object.
    /// </summary>
    /// <param name="playableCard">The PlayableCard to access.</param>
    /// <returns>A list of SpecialTriggeredAbility from the PlayableCard and underlying CardInfo object.</returns>
    public static List<SpecialTriggeredAbility> AllSpecialAbilities(this PlayableCard playableCard)
    {
        return new List<SpecialTriggeredAbility>(playableCard.TemporaryMods.Concat(playableCard.Info.Mods).SelectMany(mod => mod.specialAbilities));
    }

    /// <summary>
    /// Retrieve a list of Ability that exist in TemporaryMods and the underlying CardInfo.Mods lists.
    /// </summary>
    /// <param name="playableCard">The PlayableCard to access.</param>
    /// <returns>A list of Ability from the PlayableCard and underlying CardInfo object.</returns>
    public static List<Ability> GetAbilitiesFromAllMods(this PlayableCard playableCard)
    {
        return AbilitiesUtil.GetAbilitiesFromMods(playableCard.TemporaryMods.Concat(playableCard.Info.Mods).ToList());
    }

    /// <summary>
    /// Checks if the card has a specific Trait.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="trait">The trait to check for.</param>
    /// <returns>Returns true if the card has the specified trait.</returns>
    public static bool HasTrait(this PlayableCard playableCard, Trait trait) => playableCard.Info.HasTrait(trait);

    /// <summary>
    /// Checks if the card does not have a specific Trait.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="trait">The trait to check for.</param>
    /// <returns>Returns true if the card does not have the specified trait.</returns>
    public static bool LacksTrait(this PlayableCard playableCard, Trait trait) => playableCard.Info.LacksTrait(trait);

    /// <summary>
    /// Checks if the PlayableCard has all of the specified Traits.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="traits">The Traits to check for.</param>
    /// <returns>Returns true if playableCard has all of the specified Traits.</returns>
    public static bool HasAllTraits(this PlayableCard playableCard, params Trait[] traits) => playableCard.Info.HasAllTraits(traits);

    /// <summary>
    /// Checks if the PlayableCard has none of the specified Traits.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="traits">The Traits to check for.</param>
    /// <returns>Returns true if playableCard has none of the specified Traits.</returns>
    public static bool LacksAllTraits(this PlayableCard playableCard, params Trait[] traits) => playableCard.Info.LacksAllTraits(traits);

    /// <summary>
    /// Checks if the PlayableCard has any of the specified Traits.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="traits">The Traits to check for.</param>
    /// <returns>Returns true if playableCard has at least one of the specified Traits.</returns>
    public static bool HasAnyOfTraits(this PlayableCard playableCard, params Trait[] traits) => playableCard.Info.HasAnyOfTraits(traits);

    /// <summary>
    /// Checks if the PlayableCard is of a specified Tribe.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="tribe">The tribe to check for.</param>
    /// <returns>Returns true if the card is of the specified tribe.</returns>
    public static bool IsOfTribe(this PlayableCard playableCard, Tribe tribe) => playableCard.Info.IsOfTribe(tribe);

    /// <summary>
    /// Checks if the PlayableCard is not of a specified Tribe.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="tribe">The tribe to check for.</param>
    /// <returns>Returns true if the card is not of the specified tribe.</returns>
    public static bool IsNotOfTribe(this PlayableCard playableCard, Tribe tribe) => playableCard.Info.IsNotOfTribe(tribe);

    /// <summary>
    /// Checks if the card is not null and not Dead.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <returns>Returns true if the card is not null or not Dead.</returns>
    public static bool NotDead(this PlayableCard playableCard) => playableCard && !playableCard.Dead;

    /// <summary>
    /// Checks if the card is not the opponent's card.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <returns>Returns true if card is not the opponent's card.</returns>
    public static bool IsPlayerCard(this PlayableCard playableCard) => !playableCard.OpponentCard;

    /// <summary>
    /// Check the PlayableCard not having a specific Ability.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="ability">The ability to check for.</param>
    /// <returns>Returns true if the ability does not exist.</returns>
    public static bool LacksAbility(this PlayableCard playableCard, Ability ability) => !playableCard.HasAbility(ability);

    /// <summary>
    /// Check if the PlayableCard has all of the given abilities.
    ///
    /// A condensed version of `CardInfo.HasAllAbilities`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="abilities">The Abilities to check for.</param>
    /// <returns>Returns true if playableCard has all of the given Abilities.</returns>
    public static bool HasAllAbilities(this PlayableCard playableCard, params Ability[] abilities)
    {
        foreach (Ability ability in abilities)
        {
            if (playableCard.LacksAbility(ability))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the PlayableCard has none of the specified Abilities.
    ///
    /// A condensed version of `CardInfo.LacksAllSpecialAbilities`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="abilities">The Abilities to check for.</param>
    /// <returns>Returns true if playableCard has none of the given Abilities.</returns>
    public static bool LacksAllAbilities(this PlayableCard playableCard, params Ability[] abilities)
    {
        foreach (Ability ability in abilities)
        {
            if (playableCard.HasAbility(ability))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check if the PlayableCard has any of the specified Abilities.
    ///
    /// A condensed version of `CardInfo.HasAnyOfAbilities`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="abilities">The Abilities to check for.</param>
    /// <returns>Returns true if playableCard has at least one of the given Abilities.</returns>
    public static bool HasAnyOfAbilities(this PlayableCard playableCard, params Ability[] abilities)
    {
        foreach (Ability ability in abilities)
        {
            if (playableCard.HasAbility(ability))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check the PlayableCard not having a specific SpecialTriggeredAbility.
    ///
    /// A condensed version of `!playableCard.Info.SpecialAbilities.Contains(ability)`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="ability">The specialTriggeredAbility to check for.</param>
    /// <returns>Returns true if the specialTriggeredAbility does not exist.</returns>
    public static bool LacksSpecialAbility(this PlayableCard playableCard, SpecialTriggeredAbility ability) => !playableCard.HasSpecialAbility(ability);

    /// <summary>
    /// Check the PlayableCard having a specific SpecialTriggeredAbility.
    ///
    /// A condensed version of `playableCard.Info.SpecialAbilities.Contains(ability)`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="ability">The specialTriggeredAbility to check for.</param>
    /// <returns>Returns true if the specialTriggeredAbility does exist.</returns>
    public static bool HasSpecialAbility(this PlayableCard playableCard, SpecialTriggeredAbility ability)
    {
        return playableCard.TemporaryMods.Exists(mod => mod.specialAbilities.Contains(ability))
            || playableCard.Info.HasSpecialAbility(ability);
    }

    /// <summary>
    /// Checks if the PlayableCard has all of the specified SpecialTriggeredAbilities.
    ///
    /// A condensed version of `CardInfo.HasAllSpecialAbilities`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="specialAbilities">The SpecialTriggeredAbilities to check for.</param>
    /// <returns>Returns true if playableCard has all of the specified SpecialTriggeredAbilities.</returns>
    public static bool HasAllSpecialAbilities(this PlayableCard playableCard, params SpecialTriggeredAbility[] specialAbilities)
    {
        foreach (SpecialTriggeredAbility special in specialAbilities)
        {
            if (playableCard.LacksSpecialAbility(special))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the PlayableCard has none of the specified SpecialTriggeredAbilities.
    ///
    /// A condensed version of `CardInfo.LacksAllSpecialAbilities`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="specialAbilities">The SpecialTriggeredAbilitiess to check for.</param>
    /// <returns>Returns true if playableCard has none of the specified SpecialTriggeredAbilities.</returns>
    public static bool LacksAllSpecialAbilities(this PlayableCard playableCard, params SpecialTriggeredAbility[] specialAbilities)
    {
        foreach (SpecialTriggeredAbility special in specialAbilities)
        {
            if (playableCard.HasSpecialAbility(special))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the PlayableCard has any of the specified SpecialTriggeredAbilities.
    ///
    /// A condensed version of `CardInfo.HasAnyOfSpecialAbilities`.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <param name="specialAbilities">The SpecialTriggeredAbilities to check for.</param>
    /// <returns>Returns true if playableCard has at least one of the specified SpecialTriggeredAbilities.</returns>
    public static bool HasAnyOfSpecialAbilities(this PlayableCard playableCard, params SpecialTriggeredAbility[] specialAbilities)
    {
        foreach (SpecialTriggeredAbility special in specialAbilities)
        {
            if (playableCard.HasSpecialAbility(special))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if the PlayableCard has a card opposing it in the opposite slot.
    ///
    /// Also acts as a null check if this PlayableCard is in a slot.
    /// </summary>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <returns>Returns true if a card exists in the opposing slot.</returns>
    public static bool HasOpposingCard(this PlayableCard playableCard) => playableCard.Slot && playableCard.Slot.opposingSlot.Card;

    /// <summary>
    /// Retrieve the CardSlot object that is opposing this PlayableCard.
    /// </summary>
    /// <remarks>It is on the implementer to check if the returned value is not null</remarks>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <returns>The card slot opposite of this playableCard, otherwise return null.</returns>
    public static CardSlot OpposingSlot(this PlayableCard playableCard) => playableCard.Slot ? playableCard.Slot.opposingSlot : null;

    /// <summary>
    /// Retrieve the PlayableCard that is opposing this PlayableCard in the opposite slot.
    /// </summary>
    /// <remarks>It is on the implementer to check if the returned value is not null</remarks>
    /// <param name="playableCard">PlayableCard to access.</param>
    /// <returns>The card in the opposing slot, otherwise return null.</returns>
    public static PlayableCard OpposingCard(this PlayableCard playableCard) => playableCard.OpposingSlot()?.Card;

    public static void AddTemporaryMods(this PlayableCard card, params CardModificationInfo[] mods)
    {
        foreach (CardModificationInfo mod in mods)
            card.AddTemporaryMod(mod);
    }
    public static void RemoveTemporaryMods(this PlayableCard card, params CardModificationInfo[] mods)
    {
        foreach (CardModificationInfo mod in mods)
            card.RemoveTemporaryMod(mod);
    }

    /// <summary>
    /// A version of TransformIntoCard tailored to visually work on cards in the hand.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <param name="evolvedInfo">The CardInfo to change the card into.</param>
    /// <param name="preTransformCallback">An Action to invoke before the evolvedInfo is set.</param>
    /// <param name="onTransformedCallback">An Action to invoke after the evolvedInfo is set.</param>
    public static IEnumerator TransformIntoCardInHand(this PlayableCard card, CardInfo evolvedInfo, Action onTransformedCallback = null, Action preTransformCallback = null)
    {
        Singleton<ViewManager>.Instance.SwitchToView(View.Hand);
        yield return new WaitForSeconds(0.15f);
        yield return card.Anim.FlipInAir();
        yield return new WaitForSeconds(0.1f);
        preTransformCallback?.Invoke();
        card.SetInfo(evolvedInfo);
        onTransformedCallback?.Invoke();
        AbilityManager.FixStackTriggers(card);
    }
    /// <summary>
    /// A version of TransformIntoCardInHand that incorporates MoveCardAboveHand.
    /// </summary>
    public static IEnumerator TransformIntoCardAboveHand(this PlayableCard card, CardInfo evolvedInfo, Action onTransformedCallback = null, Action preTransformCallback = null)
    {
        Singleton<ViewManager>.Instance.SwitchToView(View.Default);
        (Singleton<PlayerHand>.Instance as PlayerHand3D)?.MoveCardAboveHand(card);

        yield return new WaitForSeconds(0.15f);
        yield return card.Anim.FlipInAir();
        yield return new WaitForSeconds(0.1f);
        preTransformCallback?.Invoke();
        card.SetInfo(evolvedInfo);
        onTransformedCallback?.Invoke();
        AbilityManager.FixStackTriggers(card);
    }
    #endregion

    #region Shields
    /// <summary>
    /// Increases the amount of shields the card has. Affects the internal counter; does not add or remove shield sigil stacks.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <param name="amount">How many shields to add.</param>
    /// <param name="updateDisplay">Whether to update the card's display, meaning sigil stack numbers and any shield effects.</param>
    public static void AddShieldCount(this PlayableCard card, int amount, bool updateDisplay = true)
    {
        card.AddShieldCount<DamageShieldBehaviour>(amount, updateDisplay);
    }
    /// <summary>
    /// Increases the amount of shields a specific ability is currently giving.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <param name="amount">How many shields to add.</param>
    /// <param name="ability">The shield-giving ability to add more shields to.</param>
    /// <param name="updateDisplay">Whether to update the card's display, meaning sigil stack numbers and any shield effects.</param>
    public static void AddShieldCount(this PlayableCard card, int amount, Ability ability, bool updateDisplay = true)
    {
        DamageShieldBehaviour component = card.GetShieldBehaviour(ability);
        if (component == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"[AddShieldCount] DamageShieldBehaviour of Ability [{ability}] not found!");
            return;
        }
        component.AddShields(amount, updateDisplay);
    }
    /// <summary>
    /// Increases the amount of shields a specific ability's AbilityBehaviour is currently giving.
    /// </summary>
    public static void AddShieldCount<T>(this PlayableCard card, int amount, bool updateDisplay = true) where T : DamageShieldBehaviour
    {
        card.GetComponent<T>()?.AddShields(amount, updateDisplay);
    }

    /// <summary>
    /// Reduces the amount of shields a specific ability is currently giving.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <param name="amount">How many shields to add.</param>
    /// <param name="ability">The shield-giving ability to add more shields to.</param>
    /// <param name="updateDisplay">Whether to update the card's display, meaning sigil stack numbers and any shield effects.</param>
    public static void RemoveShieldCount(this PlayableCard card, int amount, bool updateDisplay = true)
    {
        card.RemoveShieldCount<DamageShieldBehaviour>(amount, updateDisplay);
    }
    /// <summary>
    /// Reduces the amount of shields a specific ability is currently giving.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <param name="amount">How many shields to add.</param>
    /// <param name="ability">The shield-giving ability to remove shields from.</param>
    /// <param name="updateDisplay">Whether to update the card's display, meaning sigil stack numbers and any shield effects.</param>
    public static void RemoveShieldCount(this PlayableCard card, int amount, Ability ability, bool updateDisplay = true)
    {
        DamageShieldBehaviour component = card.GetShieldBehaviour(ability);
        if (component == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"[RemoveShieldCount] DamageShieldBehaviour of Ability [{ability}] not found!");
            return;
        }
        component.RemoveShields(amount, updateDisplay);
    }
    public static void RemoveShieldCount<T>(this PlayableCard card, int amount, bool updateDisplay = true) where T : DamageShieldBehaviour
    {
        card.GetComponent<T>()?.RemoveShields(amount, updateDisplay);
    }

    /// <summary>
    /// Gets the number of shields the target card has. Each shield negates one damaging hit.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <returns>The number of shields the card has.</returns>
    public static int GetTotalShields(this PlayableCard card)
    {
        // covers for a situation I discovered where you use a SpecialBattleSequencer's triggers to advance a boss fight
        // somehow you can end up with a null playablecard which breaks this bit here
        if (card == null)
            return 0;

        int totalShields = 0;
        List<Ability> distinct = new(); // keep track of non-stacking shield abilities so we don't add them again
        foreach (DamageShieldBehaviour component in card.GetComponents<DamageShieldBehaviour>())
        {
            if (AbilitiesUtil.GetInfo(component.Ability).canStack)
                totalShields += component.NumShields;

            else if (!distinct.Contains(component.Ability))
            {
                totalShields += component.NumShields;
                distinct.Add(component.Ability);
            }
        }

        return totalShields;
    }

    /// <summary>
    /// Retrieves the current value of a DamageShieldBehaviour's NumShield.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="card"></param>
    /// <returns>The current value of the DamageShieldBehaviour's NumShield.</returns>
    public static int GetShieldCount<T>(this PlayableCard card) where T : DamageShieldBehaviour
    {
        T comp = card.GetComponent<T>();
        return comp?.NumShields ?? 0;
    }

    /// <summary>
    /// Retrieves the DamageShieldBehaviour component corresponding to the given Ability, or null if it does not exist.
    /// </summary>
    /// <param name="card"></param>
    /// <param name="ability"></param>
    /// <returns>The DamageShieldBehaviour component of the given Ability, or null if it does not exist.</returns>
    public static DamageShieldBehaviour GetShieldBehaviour(this PlayableCard card, Ability ability)
    {
        Type type = ShieldManager.AllShieldAbilities.AbilityByID(ability)?.AbilityBehavior;
        if (type == null)
            return null;

        return card.GetComponent(type) as DamageShieldBehaviour;
    }
    /// <summary>
    /// A variant of ResetShield that only resets shields belonging is a certain ability.
    /// </summary>
    /// <param name="card">The PlayableCard to access.</param>
    /// <param name="ability">The shield ability to look for.</param>
    public static void ResetShield(this PlayableCard card, Ability ability)
    {
        foreach (DamageShieldBehaviour com in card.GetComponents<DamageShieldBehaviour>())
        {
            if (com.Ability == ability)
                com.ResetShields(false);
        }
        card.Status.lostShield = false;
    }

    public static bool IsShieldAbility(this Ability ability)
    {
        return ShieldManager.AllShieldInfos.AbilityByID(ability) != null;
    }
    public static bool IsShieldAbility(this AbilityInfo abilityInfo)
    {
        return ShieldManager.AllShieldInfos.Contains(abilityInfo);
    }
    /// <summary>
    /// Returns whether or not the PlayableCard has an Ability that overrides DamageShieldBehaviour.
    /// </summary>
    /// <param name="card">The PlayableCard object to check.</param>
    public static bool HasShieldAbility(this PlayableCard card)
    {
        return card.AllAbilities().Exists(x => x.IsShieldAbility());
    }
    /// <summary>
    /// Returns whether or not the CardInfo has an Ability that overrides DamageShieldBehaviour.
    /// </summary>
    /// <param name="cardInfo">The CardInfo object to check.</param>
    public static bool HasShieldAbility(this CardInfo cardInfo)
    {
        return cardInfo.Abilities.Exists(x => x.IsShieldAbility());
    }

    #endregion

    /// <summary>
    /// Spawns the CardInfo object to the player's hand.
    /// </summary>
    /// <param name="cardInfo">CardInfo to access.</param>
    /// <param name="temporaryMods">The mods that will be added to the PlayableCard object.</param>
    /// <param name="spawnOffset">The position of where the card will appear from. Default is a Vector3 of (0, 6, 1.5).</param>
    /// <param name="onDrawnTriggerDelay">The amount of time to wait before being added to the hand.</param>
    /// <param name="cardSpawnedCallback">
    /// The action to invoke after the card has spawned but before being added to the hand.
    /// 1. One of two uses in the vanilla game is if the player has completed the event 'ImprovedSmokeCardDiscovered'.
    ///     If this event is complete, the 'Improved Smoke' PlayableCard has the emissive portrait forced on and is then re-rendered.
    /// 2. The other use is during Grimora's fight in Act 2. During the reanimation sequence, the background sprite is replaced with a rare card background.
    /// .</param>
    /// <returns>The enumeration of the card being placed in the player's hand.</returns>
    public static IEnumerator SpawnInHand(this CardInfo cardInfo, List<CardModificationInfo> temporaryMods = null, Vector3 spawnOffset = default, float onDrawnTriggerDelay = 0f, Action<PlayableCard> cardSpawnedCallback = null)
    {
        if (spawnOffset == default)
            spawnOffset = CardSpawner.Instance.spawnedPositionOffset;

        yield return CardSpawner.Instance.SpawnCardToHand(cardInfo, temporaryMods, spawnOffset, onDrawnTriggerDelay, cardSpawnedCallback);
    }

    /// <summary>
    /// Creates a basic EncounterBlueprintData.CardBlueprint based off the CardInfo object.
    /// </summary>
    /// <param name="cardInfo">CardInfo to create the blueprint with.</param>
    /// <returns>The CardBlueprint object that can be used when creating EncounterData.</returns>
    public static EncounterBlueprintData.CardBlueprint CreateBlueprint(this CardInfo cardInfo)
    {
        return new EncounterBlueprintData.CardBlueprint
        {
            card = cardInfo
        };
    }
}