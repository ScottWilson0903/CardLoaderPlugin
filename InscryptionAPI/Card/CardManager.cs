using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Guid;
using InscryptionAPI.Helpers;
using InscryptionAPI.Saves;
using MonoMod.Cil;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace InscryptionAPI.Card;

[HarmonyPatch]
public static class CardManager
{
    private class CardExt // Needs to be defined first so the implicit static constructor works correctly
    {
        public readonly Dictionary<Type, object> TypeMap = new();
        public readonly Dictionary<string, string> StringMap = new();
    }
    public class CardAltPortraits
    {
        public Sprite PixelAlternatePortrait = null;
        public Sprite SteelTrapPortrait = null;
        public Sprite PixelSteelTrapPortrait = null;
        public Sprite BrokenShieldPortrait = null;
        public Sprite PixelBrokenShieldPortrait = null;
        public Sprite SacrificablePortrait = null;
        public Sprite PixelSacrificablePortrait = null;
    }
    private static readonly ConditionalWeakTable<CardInfo, CardExt> CardExtensionProperties = new();
    private static readonly ConditionalWeakTable<CardInfo, CardAltPortraits> CardAlternatePortraits = new();

    internal static readonly Dictionary<string, Func<bool, int, bool>> CustomCardUnlocks = new();

    /// <summary>
    /// The set of cards that are in the base game
    /// </summary>
    /// <returns>.</returns>
    public static readonly ReadOnlyCollection<CardInfo> BaseGameCards = new(GetBaseGameCards().ToList());
    private static readonly ObservableCollection<CardInfo> NewCards = new();

    private static bool EventActive = false;

    /// <summary>
    /// This event runs every time the card list is resynced. By adding listeners to this event, you can modify cards that have been added to the list after your mod was loaded.
    /// </summary>
    public static event Func<List<CardInfo>, List<CardInfo>> ModifyCardList;

    private static IEnumerable<CardInfo> GetBaseGameCards()
    {
        foreach (CardInfo card in Resources.LoadAll<CardInfo>("Data/Cards"))
        {
            card.SetBaseGameCard(true);
            if (card.name == "Squirrel" || card.name == "AquaSquirrel" || card.name == "Rabbit")
                card.SetAffectedByTidalLock();
            else if (card.name == "SkeletonParrot")
                card.AddTribes(Tribe.Bird);
            else if (card.name == "MudTurtle")
                card.SetBrokenShieldPortrait(card.alternatePortrait);

            yield return card;
        }
    }

    internal static void ActivateEvents() => EventActive = true;

    /// <summary>
    /// Re-executes events and rebuilds the card pool.
    /// </summary>
    public static void SyncCardList()
    {
        var cards = BaseGameCards.Concat(NewCards).Select(CardLoader.Clone).ToList();

        // Fix card copies on params
        foreach (CardInfo card in cards)
        {
            if (card.evolveParams != null && card.evolveParams.evolution != null)
            {
                List<CardModificationInfo> mods = card.evolveParams.evolution.Mods;
                card.evolveParams.evolution = cards.CardByName(card.evolveParams.evolution.name).Clone() as CardInfo;
                card.evolveParams.evolution.Mods = mods;
            }

            if (card.iceCubeParams != null && card.iceCubeParams.creatureWithin != null)
            {
                List<CardModificationInfo> mods = card.iceCubeParams.creatureWithin.Mods;
                card.iceCubeParams.creatureWithin = cards.CardByName(card.iceCubeParams.creatureWithin.name).Clone() as CardInfo;
                card.iceCubeParams.creatureWithin.Mods = mods;
            }

            if (card.tailParams != null && card.tailParams.tail != null)
            {
                List<CardModificationInfo> mods = card.tailParams.tail.Mods;
                card.tailParams.tail = cards.CardByName(card.tailParams.tail.name).Clone() as CardInfo;
                card.tailParams.tail.Mods = mods;
            }
        }

        AllCardsCopy = EventActive ? ModifyCardList?.Invoke(cards) ?? cards : cards;
    }

    private static string GetCardPrefixFromName(this CardInfo info)
    {
        string[] splitName = info.name.Split('_');
        return splitName.Length > 1 ? splitName[0] : string.Empty;
    }

    private static void AddPrefixesToCards(IEnumerable<CardInfo> cards, string prefix)
    {
        foreach (CardInfo card in cards)
        {
            if (string.IsNullOrEmpty(card.GetModPrefix()))
            {
                card.SetModPrefix(prefix);

                if (!card.name.StartsWith($"{prefix}_"))
                    card.name = $"{prefix}_{card.name}";
            }
        }
    }

    internal static void ResolveMissingModPrefixes()
    {
        // Group all cards by the mod guid
        foreach (var group in NewCards.Where(ci => !ci.IsBaseGameCard() && ci.IsOldApiCard()).GroupBy(ci => ci.GetModTag(), ci => ci, (key, g) => new { ModId = key, Cards = g.ToList() }))
        {
            if (string.IsNullOrEmpty(group.ModId))
                continue;

            if (group.ModId.Equals("MADH.inscryption.JSONLoader", StringComparison.OrdinalIgnoreCase))
            {
                // This is a special case, but this is the most logical way to handle this

                // We will trust JSON loader to handle its own prefixes
                // This is mainly because it's completely impossible to derive prefixes from JSON loader
                // JSON loader handles loading cards from potentialy dozens of mods.
                // We will be completely unable to observe any sort of pattern from cards loaded by JSON loader
                continue;
            }

            // Get list of unique mod prefixes
            List<string> setPrefixes = group.Cards.Select(ci => ci.GetModPrefix()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            // If there is EXACTLY ONE prefix in the group, we can apply it to the rest of the group
            if (setPrefixes.Count == 1)
            {
                AddPrefixesToCards(group.Cards, setPrefixes[0]);
                continue;
            }

            // We won't try to dynamically generate prefixes unless the count of cards in the group is at least 6
            if (group.Cards.Count < 6)
                continue;

            // Okay, let's try to derive prefixes from card names
            bool appliedPrefixes = false;
            foreach (var nameGroup in group.Cards.Select(ci => ci.GetCardPrefixFromName())
                                                 .GroupBy(s => s)
                                                 .Select(g => new { Prefix = g.Key, Count = g.Count() })
                                                 .ToList())
            {
                if (nameGroup.Count >= group.Cards.Count / 2)
                {
                    AddPrefixesToCards(group.Cards, nameGroup.Prefix);
                    appliedPrefixes = true;
                }

                if (appliedPrefixes)
                    break;
            }

            if (appliedPrefixes)
                continue;

            // Okay, we can't get it from card names
            // We build it from the mod guid
            string[] guidSplit = group.ModId.Split('.');
            string prefix = guidSplit[guidSplit.Length - 1];
            AddPrefixesToCards(group.Cards, prefix);
        }
    }

    static CardManager()
    {
        InscryptionAPIPlugin.ScriptableObjectLoaderLoad += static type =>
        {
            if (type == typeof(CardInfo))
                ScriptableObjectLoader<CardInfo>.allData = AllCardsCopy;
        };
        NewCards.CollectionChanged += static (_, _) =>
        {
            SyncCardList();
        };
    }

    /// <summary>
    /// A copy of all cards in the card pool.
    /// </summary>
    /// <returns>.</returns>
    public static List<CardInfo> AllCardsCopy { get; private set; } = BaseGameCards.ToList();

    /// <summary>
    /// INTERNAL USE ONLY. Adds a new card to the card pool
    /// </summary>
    /// <param name="newCard">The card to add.</param>
    internal static void Add(CardInfo newCard)
    {
        if (string.IsNullOrEmpty(newCard.GetModTag()))
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            newCard.SetModTag(TypeManager.GetModIdFromCallstack(callingAssembly));
        }

        if (!NewCards.Contains(newCard))
            NewCards.Add(newCard);
    }

    /// <summary>
    /// Adds a new card to the card pool. If your card's name does not match your mod prefix, it will be updated to match.
    /// </summary>
    /// <param name="modPrefix">The unique prefix that identifies your card mod in the card pool.</param>
    /// <param name="newCard">The card to add.</param>
    public static void Add(string modPrefix, CardInfo newCard)
    {
        if (!newCard.name.StartsWith(modPrefix))
            newCard.name = $"{modPrefix}_{newCard.name}";

        newCard.SetModPrefix(modPrefix);

        Add(newCard);
    }

    /// <summary>
    /// Removes a custom card from the card pool. Cannot be used to remove base game cards.
    /// </summary>
    /// <param name="card">The card to remove.</param>
    public static void Remove(CardInfo card)
    {
        if (card != null)
        {
            CardInfo c;
            while ((c = NewCards.SingleOrDefault(c => c.name == card.name)) != null)
                NewCards.Remove(c);
        }
    }

    /// <summary>
    /// Adds a new card to the card pool.
    /// </summary>
    /// <param name="modPrefix">The unique prefix that identifies your card mod in the card pool.</param>
    /// <param name="name">The internal name of your card - used to find and reference your card. If this name does not match the mod prefix, it will be changed to match [mod_prefix]_[name].</param>
    /// <param name="displayName">The displayed name of the card - what will be seen in-game on the card.</param>
    /// <param name="attack">The Power of the card.</param>
    /// <param name="health">The Health of the card.</param>
    /// <param name="description">The spoken description when the card is first encountered.</param>
    /// <returns>The newly created card's CardInfo.</returns>
    public static CardInfo New(string modPrefix, string name, string displayName, int attack, int health, string description = default)
    {
        CardInfo retval = ScriptableObject.CreateInstance<CardInfo>();
        retval.name = !name.StartsWith(modPrefix) ? $"{modPrefix}_{name}" : name;
        retval.SetBasic(displayName, attack, health, description);

        Assembly callingAssembly = Assembly.GetCallingAssembly();
        retval.SetModTag(TypeManager.GetModIdFromCallstack(callingAssembly));

        Add(modPrefix, retval);

        return retval;
    }

    /// <summary>
    /// Get a custom extension class that will exist on all clones of a card
    /// </summary>
    /// <param name="card">Card to access.</param>
    /// <typeparam name="T">The custom class</typeparam>
    /// <returns>The instance of T for this card.</returns>
    public static T GetExtendedClass<T>(this CardInfo card) where T : class, new()
    {
        var typeMap = CardExtensionProperties.GetOrCreateValue(card).TypeMap;
        if (typeMap.TryGetValue(typeof(T), out object tObj))
            return (T)tObj;

        else
        {
            T tInst = new();
            typeMap[typeof(T)] = tInst;
            return tInst;
        }
    }

    public static Dictionary<string, string> GetCardExtensionTable(this CardInfo card) => CardExtensionProperties.GetOrCreateValue(card).StringMap;
    internal static CardAltPortraits GetAltPortraits(this CardInfo card) => CardAlternatePortraits.GetOrCreateValue(card);

    public static Sprite PixelAlternatePortrait(this CardInfo card)
    {
        return card.GetAltPortraits().PixelAlternatePortrait;
    }
    public static Sprite SteelTrapPortrait(this CardInfo card)
    {
        return card.GetAltPortraits().SteelTrapPortrait;
    }
    public static Sprite EmissiveSteelTrapPortrait(this CardInfo card)
    {
        return card.GetAltPortraits().SteelTrapPortrait.GetEmissionSprite();
    }
    public static Sprite PixelSteelTrapPortrait(this CardInfo card)
    {
        return card.GetAltPortraits().PixelSteelTrapPortrait;
    }

    public static Sprite BrokenShieldPortrait(this CardInfo card)
    {
        return card.GetAltPortraits().BrokenShieldPortrait;
    }
    public static Sprite EmissiveBrokenShieldPortrait(this CardInfo card)
    {
        return card.GetAltPortraits().BrokenShieldPortrait.GetEmissionSprite();
    }
    public static Sprite PixelBrokenShieldPortrait(this CardInfo card)
    {
        return card.GetAltPortraits().PixelBrokenShieldPortrait;
    }

    public static Sprite SacrificablePortrait(this CardInfo card)
    {
        return card.GetAltPortraits().SacrificablePortrait;
    }
    public static Sprite EmissiveSacrificablePortrait(this CardInfo card)
    {
        return card.GetAltPortraits().SacrificablePortrait.GetEmissionSprite();
    }
    public static Sprite PixelSacrificablePortrait(this CardInfo card)
    {
        return card.GetAltPortraits().PixelSacrificablePortrait;
    }

    private const string ERROR = "ERROR";

    private static string ReverseKey(string key)
    {
        foreach (var pair in ModdedSaveManager.SaveData.SaveData[InscryptionAPIPlugin.ModGUID])
            if (pair.Value.Equals(key))
                return pair.Key;

        return ERROR;
    }

    internal static void AuditCardList()
    {
        foreach (CardInfo card in AllCardsCopy)
        {
            // Audit the abilities for issues
            foreach (Ability ability in card.Abilities)
            {
                string printKey = (int)ability >= GuidManager.START_INDEX ?
                                  ReverseKey(ability.ToString()).Replace("Ability_", "") :
                                  $"Inscryption_{ability}";

                if (printKey.Equals(ERROR))
                {
                    InscryptionAPIPlugin.Logger.LogWarning($"Card {card.name} has an ability {ability} that is not a part of the base game and was not assigned by the API!");
                    continue;
                }

                AbilityInfo info = AbilitiesUtil.GetInfo(ability);

                if (info == null)
                    InscryptionAPIPlugin.Logger.LogWarning($"Card {card.name} has an ability {printKey} that has not been properly registered!");
            }

            // Audit the special abilities for issues
            foreach (SpecialTriggeredAbility specialAbility in card.SpecialAbilities)
            {
                string printKey = (int)specialAbility >= GuidManager.START_INDEX ?
                                  ReverseKey(specialAbility.ToString()).Replace("SpecialTriggeredAbility_", "") :
                                  $"Inscryption_{specialAbility}";

                if (printKey.Equals(ERROR))
                {
                    InscryptionAPIPlugin.Logger.LogWarning($"Card {card.name} has a special ability {specialAbility} that is not a part of the base game and was not assigned by the API!");
                    continue;
                }


                var fst = SpecialTriggeredAbilityManager.AllSpecialTriggers.FirstOrDefault(st => st.Id == specialAbility);

                if (fst == null)
                    InscryptionAPIPlugin.Logger.LogWarning($"Card {card.name} has a special ability {printKey} that has not been properly registered!");
            }

            // Audit the appearance behaviors for issues
            foreach (CardAppearanceBehaviour.Appearance appearance in card.appearanceBehaviour)
            {
                string printKey = (int)appearance >= GuidManager.START_INDEX ?
                                  ReverseKey(appearance.ToString()).Replace("CardAppearanceBehaviour.Appearance_", "") :
                                  $"Inscryption_{appearance}";

                if (printKey.Equals(ERROR))
                {
                    InscryptionAPIPlugin.Logger.LogWarning($"Card {card.name} has an appearance behavior {appearance} that is not a part of the base game and was not assigned by the API!");
                    continue;
                }


                var fst = CardAppearanceBehaviourManager.AllAppearances.FirstOrDefault(f => f.Id == appearance);

                if (fst == null)
                    InscryptionAPIPlugin.Logger.LogWarning($"Card {card.name} has an appearance behavior {printKey} that has not been properly registered!");
            }
        }
    }

    [HarmonyPatch(typeof(CardInfo), nameof(CardInfo.Clone))]
    [HarmonyPostfix]
    private static void ClonePostfix(CardInfo __instance, ref object __result)
    {
        // just ensures that clones of a card have the same extension properties
        CardExtensionProperties.Add((CardInfo)__result, CardExtensionProperties.GetOrCreateValue(__instance));
        CardAlternatePortraits.Add((CardInfo)__result, CardAlternatePortraits.GetOrCreateValue(__instance));
        // clone all the mods too
        // DO NOT CHANGE THIS
        // if there are errors due to this, address them where they occur, NOT HERE
        // I've spent way too much time trying to make this work and it's easier to just clone everything
        CardInfo result = (CardInfo)__result;
        result.Mods = new(__instance.Mods);
    }


    // prevent duplicate mods from being added as a result of ClonePostfix
    [HarmonyPrefix, HarmonyPatch(typeof(DeckInfo), nameof(DeckInfo.ModifyCard))]
    private static bool ModifyCardDontAddDupes(CardInfo card, CardModificationInfo mod) => !card.Mods.Contains(mod);

    [HarmonyILManipulator]
    [HarmonyPatch(typeof(ConceptProgressionTree), nameof(ConceptProgressionTree.CardUnlocked))]
    private static void FixCardUnlocked(ILContext il)
    {
        ILCursor c = new(il);

        c.GotoNext(MoveType.Before,
            x => x.MatchCallOrCallvirt(AccessTools.Method(typeof(UnityObject), "op_Equality", new Type[] { typeof(UnityObject), typeof(UnityObject) }))
        );

        c.Remove();
        c.EmitDelegate<Func<CardInfo, CardInfo, bool>>(static (c1, c2) => c1.name == c2.name);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ConceptProgressionTree), nameof(ConceptProgressionTree.CardUnlocked))]
    private static void AddCustomCardUnlock(ref bool __result, CardInfo card)
    {
        __result &= !CustomCardUnlocks.ContainsKey(card.name) || CustomCardUnlocks[card.name](SaveFile.IsAscension, (AscensionSaveData.Data?.challengeLevel).GetValueOrDefault());
    }

    [HarmonyPatch(typeof(CardLoader), "GetCardByName", new Type[] { typeof(string) })]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // === We want to turn this

        // return CardLoader.Clone(ScriptableObjectLoader<CardInfo>.AllData.Find((CardInfo x) => x.name == name));

        // === Into this

        // return CardLoader.Clone(LogCardInfo(ScriptableObjectLoader<CardInfo>.AllData.Find((CardInfo x) => x.name == name)));

        // ===
        List<CodeInstruction> codes = new(instructions);

        MethodInfo CloneMethodInfo = SymbolExtensions.GetMethodInfo(() => CardLoader.Clone(null));
        MethodInfo LogCardInfoMethodInfo = SymbolExtensions.GetMethodInfo(() => LogCardInfo(null, null));
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && codes[i].operand == CloneMethodInfo)
            {
                codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0)); // name
                codes.Insert(i++, new CodeInstruction(OpCodes.Call, LogCardInfoMethodInfo)); // LogCardInfo
                break;
            }
        }

        return codes;
    }

    public static CardInfo LogCardInfo(CardInfo info, string cardInfoName)
    {
        if (info == null)
            InscryptionAPIPlugin.Logger.LogError("[CardLoader] Could not find CardInfo with name '" + cardInfoName + "'");

        return info;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.SwitchToAlternatePortrait))]
    private static void SwitchToAlternatePortraitPixel(PlayableCard __instance)
    {
        if (SaveManager.SaveFile.IsPart2 && __instance.Info.HasPixelAlternatePortrait())
        {
            __instance.GetComponentInChildren<PixelCardDisplayer>().portraitRenderer.sprite = __instance.Info.PixelAlternatePortrait();
            __instance.GetComponentInChildren<PixelCardDisplayer>().portraitRenderer.enabled = true;
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.SwitchToDefaultPortrait))]
    private static void SwitchToDefaultPortraitPixel(PlayableCard __instance)
    {
        if (SaveManager.SaveFile.IsPart2)
        {
            __instance.GetComponentInChildren<PixelCardDisplayer>().portraitRenderer.sprite = __instance.Info.pixelPortrait;
            __instance.GetComponentInChildren<PixelCardDisplayer>().portraitRenderer.enabled = __instance.Info.pixelPortrait != null;
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PaperCardAnimationController), nameof(PaperCardAnimationController.SetShaking))]
    [HarmonyPatch(typeof(CardAnimationController), nameof(CardAnimationController.SetShaking))]
    private static void SwitchToSacrificablePortrait(CardAnimationController __instance)
    {
        if (__instance.PlayableCard == null)
            return;

        if (SaveManager.SaveFile.IsPart2)
        {
            if (__instance.PlayableCard.Info.HasPixelSacrificablePortrait())
            {
                __instance.PlayableCard.GetComponentInChildren<PixelCardDisplayer>().portraitRenderer.sprite = __instance.PlayableCard.Info.PixelSacrificablePortrait();
                __instance.PlayableCard.GetComponentInChildren<PixelCardDisplayer>().portraitRenderer.enabled = true;
            }
        }
        else if (__instance.PlayableCard.Info.HasSacrificablePortrait())
        {
            __instance.PlayableCard.SwitchToPortrait(__instance.PlayableCard.Info.SacrificablePortrait());
        }
    }
}
