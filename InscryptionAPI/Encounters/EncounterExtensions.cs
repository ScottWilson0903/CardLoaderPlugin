using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Card;
using System.Collections;
using System.Runtime.CompilerServices;
using static DiskCardGame.EncounterBlueprintData;

namespace InscryptionAPI.Encounters;

[HarmonyPatch]
public static class EncounterExtensions
{
    #region Opponent Extensions
    public static OpponentManager.FullOpponent OpponentById(this IEnumerable<OpponentManager.FullOpponent> opponents, Opponent.Type id)
    {
        return opponents.FirstOrDefault(o => o.Id == id);
    }

    public static OpponentManager.FullOpponent SetOpponent(this OpponentManager.FullOpponent opp, Type opponentType)
    {
        opp.Opponent = opponentType;
        return opp;
    }

    public static OpponentManager.FullOpponent SetSequencer(this OpponentManager.FullOpponent opp, string sequenceId)
    {
        opp.SpecialSequencerId = sequenceId;
        return opp;
    }

    public static OpponentManager.FullOpponent SetNewSequencer(this OpponentManager.FullOpponent opp, string pluginGuid, string sequencerName, Type sequencerType)
    {
        var newSequencer = SpecialSequenceManager.Add(pluginGuid, sequencerName, sequencerType);
        opp.SpecialSequencerId = newSequencer.Id;
        return opp;
    }
    #endregion

    #region Encounter Extensions

    #region Setters
    /// <summary>
    /// Sets the difficulty range of this encounter.<br/>
    /// Difficulty is determined by the formula (6 * <c>tier</c>) + <c>battle#</c> + <c>modifier</c>.
    /// </summary>
    /// <param name="min">The minimum difficulty.</param>
    /// <param name="max">The maximum difficulty.</param>
    public static T SetDifficulty<T>(this T blueprint, int min, int max) where T : EncounterBlueprintData
    {
        blueprint.minDifficulty = min;
        blueprint.maxDifficulty = max;
        return blueprint;
    }

    /// <summary>
    /// Adds dominant tribes to this region.<br/>
    /// The dominant tribes list determines the totems for this battle.
    /// </summary>
    /// <param name="tribes">The tribes to add.</param>
    public static T AddDominantTribes<T>(this T blueprint, params Tribe[] tribes) where T : EncounterBlueprintData
    {
        blueprint.dominantTribes ??= new();
        foreach (Tribe tribe in tribes)
        {
            blueprint.dominantTribes.Add(tribe);
        }
        return blueprint;
    }

    [Obsolete("regionSpecific is unused.")]
    public static T SetRegionSpecific<T>(this T blueprint, bool enabled) where T : EncounterBlueprintData
    {
        blueprint.regionSpecific = enabled;
        return blueprint;
    }

    /// <summary>
    /// Adds random replacement cards to this region.<br/>
    /// A card from this list is selected whenever a card is randomly replaced by <c>randomReplaceChance</c>.
    /// </summary>
    /// <param name="cards">The cards to add.</param>
    public static T AddRandomReplacementCards<T>(this T blueprint, params string[] cards) where T : EncounterBlueprintData
    {
        blueprint.randomReplacementCards ??= new();
        foreach (string card in cards)
        {
            CardInfo cardInfo = CardManager.AllCardsCopy.CardByName(card);
            if (!blueprint.randomReplacementCards.Contains(cardInfo))
            {
                blueprint.randomReplacementCards.Add(cardInfo);
            }
        }
        return blueprint;
    }

    /// <summary>
    /// Adds redundant abilities to this region.<br/>
    /// Redundant abilities will not be used on totems for this encounter.
    /// </summary>
    /// <param name="abilities">The abilities to add.</param>
    public static T SetRedundantAbilities<T>(this T blueprint, params Ability[] abilities) where T : EncounterBlueprintData
    {
        blueprint.redundantAbilities = abilities.ToList();
        return blueprint;
    }

    public static T SetUnlockedCardPrerequisites<T>(this T blueprint, params string[] cards) where T : EncounterBlueprintData
    {
        blueprint.unlockedCardPrerequisites ??= new();
        foreach (string card in cards)
        {
            CardInfo cardInfo = CardManager.AllCardsCopy.CardByName(card);
            if (!blueprint.unlockedCardPrerequisites.Contains(cardInfo))
                blueprint.unlockedCardPrerequisites.Add(cardInfo);
        }
        return blueprint;
    }

    public static T AddTurnMods<T>(this T blueprint, params TurnModBlueprint[] turnMods) where T : EncounterBlueprintData
    {
        blueprint.turnMods = turnMods.ToList();
        return blueprint;
    }
    #endregion

    #region Turns

    #region Setters

    /// <summary>
    /// Sets the minimum and maximum difficulty values for this CardBlueprint.
    /// </summary>
    /// <param name="card">The CardBlueprint to access.</param>
    /// <param name="min">The minimum difficulty for this card to appear.</param>
    /// <param name="max">The maximum difficulty for this card to appear at.</param>
    /// <returns>The same CardBlueprint so a chain can continue.</returns>
    public static CardBlueprint SetDifficulty(this CardBlueprint card, int min, int max)
    {
        card.minDifficulty = min;
        card.maxDifficulty = max;
        return card;
    }

    /// <summary>
    /// Sets whether what card the CardBlueprint will be replaced with if difficultyReplace == true, and what difficulty threshold it will be replaced.
    /// </summary>
    /// <param name="blueprint">The CardBlueprint to access.</param>
    /// <param name="replaceWithDifficulty">Whether this card will be replaced at certain difficulties.</param>
    /// <param name="replacementName">The name of the card that will replace this card.</param>
    /// <param name="requiredDifficulty">The minimum difficulty for the card to be replaced.</param>
    /// <returns>The same CardBlueprint so a chain can continue.</returns>
    public static CardBlueprint SetReplacement(this CardBlueprint blueprint, string replacementName, int requiredDifficulty = 0, bool replaceWithDifficulty = true)
    {
        blueprint.difficultyReplace = replaceWithDifficulty;
        blueprint.difficultyReq = requiredDifficulty;
        blueprint.replacement = CardManager.AllCardsCopy.CardByName(replacementName);
        return blueprint;
    }

    /// <summary>
    /// Sets the difficulties of each CardBlueprint in the list to the specified values.
    /// </summary>
    /// <param name="list">The list to access.</param>
    /// <param name="min">The minimum difficulty.</param>
    /// <param name="max">The maximum difficulty.</param>
    /// <returns>The same list so a chain can continue.</returns>
    public static List<CardBlueprint> SetTurnDifficulty(this List<CardBlueprint> list, int min, int max)
    {
        foreach (CardBlueprint blueprint in list)
        {
            blueprint.SetDifficulty(min, max);
        }
        return list;
    }

    /// <summary>
    /// Duplicates a list representing a turn the specified number of times.
    /// </summary>
    /// <param name="list">The list to access.</param>
    /// <param name="amount">How many times the list should be duplicated.</param>
    /// <returns>An array containing the newly duplicated lists. Use with EncounterBlueprintData.AddTurns().</returns>
    public static List<CardBlueprint>[] DuplicateTurn(this List<CardBlueprint> list, int amount)
    {
        List<CardBlueprint>[] array = new List<CardBlueprint>[1 + amount];
        for (int i = 0; i < 1 + amount; i++)
            array.AddToArray(list);

        return array;
    }

    #endregion

    #region Creators/Adders
    /// <summary>
    /// Creates a new turn for this encounter and returns the builder.
    /// </summary>
    public static TurnBuilder<T> CreateTurn<T>(this T blueprint) where T : EncounterBlueprintData
    {
        TurnBuilder<T> turnBuilder = new();
        turnBuilder.SetBlueprint(blueprint);
        return turnBuilder;
    }

    /// <summary>
    /// Adds a new turn to the EncounterBlueprintData using the specified CardBlueprints.
    /// </summary>
    /// <param name="cards">The CardBlurprints to add. If none are specified, creates an empty turn.</param>
    /// <returns>The same EncounterBlueprintData so a chain can continue.</returns>
    public static T AddTurn<T>(this T blueprint, params CardBlueprint[] cards) where T : EncounterBlueprintData
    {
        blueprint.AddTurn(cards?.ToList() ?? new());
        return blueprint;
    }

    /// <summary>
    /// Adds a new turn to the EncounterBlueprintData.
    /// </summary>
    /// <param name="turn">The turn to add. If null, creates an empty turn.</param>
    /// <returns>The same EncounterBlueprintData so a chain can continue.</returns>
    public static T AddTurn<T>(this T blueprint, List<CardBlueprint> turn = null) where T : EncounterBlueprintData
    {
        blueprint.turns ??= new();
        blueprint.turns.Add(turn ?? new());
        return blueprint;
    }

    /// <summary>
    /// Adds new turns to the EncounterBlueprintData using the specified CardBlueprint Lists.
    /// </summary>
    /// <param name="turns">The Lists of CardBlueprints to add to the EncounterBlueprintData.</param>
    /// <returns>The same EncounterBlueprintData so a chain can continue.</returns>
    public static T AddTurns<T>(this T blueprint, params List<CardBlueprint>[] turns) where T : EncounterBlueprintData
    {
        foreach (List<CardBlueprint> turn in turns)
            blueprint.AddTurn(turn);

        return blueprint;
    }

    public static T AddTurns<T>(this T blueprint, List<List<CardBlueprint>> turns) where T : EncounterBlueprintData
    {
        blueprint.turns ??= new();
        foreach (List<CardBlueprint> item in turns)
            blueprint.AddTurn(item);

        return blueprint;
    }

    /// <summary>
    /// Duplicates the contents of the turn plan into itself the specified number of times.
    /// </summary>
    public static T DuplicateTurns<T>(this T blueprint, int amount) where T : EncounterBlueprintData
    {
        List<List<CardBlueprint>> plan = new();
        for (int i = 0; i < 1 + amount; i++)
        {
            foreach (List<CardBlueprint> turn in blueprint.turns)
                plan.Add(turn);
        }
        blueprint.turns = plan;

        return blueprint;
    }

    /// <summary>
    /// Sets the difficulty values of all CardBlueprints in the turn plan to the same values.
    /// </summary>
    public static T SyncTurnDifficulties<T>(this T blueprint, int minDifficulty, int maxDifficulty) where T : EncounterBlueprintData
    {
        foreach (List<CardBlueprint> turn in blueprint.turns)
            turn.SetTurnDifficulty(minDifficulty, maxDifficulty);

        return blueprint;
    }

    #endregion

    #endregion

    #endregion

    #region GBC NPC Information

    private static ConditionalWeakTable<GBCEncounterManager, CachedGCBNPCDescriptor> LAST_KNOWN_NPC = new();

    [HarmonyPatch(typeof(GBCEncounterManager), nameof(GBCEncounterManager.EncounterSequence)), HarmonyPostfix]
    private static IEnumerator CaptureLastKnownTriggeringNPC(IEnumerator sequence, CardBattleNPC triggeringNPC)
    {
        LAST_KNOWN_NPC.Remove(GBCEncounterManager.Instance);
        LAST_KNOWN_NPC.Add(GBCEncounterManager.Instance, new CachedGCBNPCDescriptor(triggeringNPC));

        yield return sequence;

        LAST_KNOWN_NPC.Remove(GBCEncounterManager.Instance);
    }

    /// <summary>
    /// Gets information about the NPC that triggered the current battle
    /// </summary>
    public static CachedGCBNPCDescriptor GetTriggeringNPC(this GBCEncounterManager mgr)
    {
        if (LAST_KNOWN_NPC.TryGetValue(mgr, out CachedGCBNPCDescriptor value))
            return value;
        return null;
    }

    #endregion
}