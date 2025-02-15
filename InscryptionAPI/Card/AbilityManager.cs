using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Guid;
using InscryptionAPI.Helpers;
using InscryptionAPI.Helpers.Extensions;
using InscryptionAPI.RuleBook;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace InscryptionAPI.Card;

/// <summary>
/// This manager class handles the creation and management of mod-added abilities (sigils).
/// </summary>
[HarmonyPatch]
public static class AbilityManager
{
    private class AbilityExt
    {
        public readonly Dictionary<Type, object> TypeMap = new();
        public readonly Dictionary<string, string> StringMap = new();
    }
    private static readonly ConditionalWeakTable<AbilityInfo, AbilityExt> AbilityExtensionProperties = new();

    /// <summary>
    /// A utility class that holds all of the required information about an ability in order to be able to use it in-game.
    /// </summary>
    public class FullAbility
    {
        /// <summary>
        /// The unique ID for this ability
        /// </summary>
        public readonly Ability Id;

        /// <summary>
        /// The guid of the mod that added this ability
        /// </summary>
        public readonly string ModGUID;

        /// <summary>
        /// The description object for this ability
        /// </summary>        
        public readonly AbilityInfo Info;

        /// <summary>
        /// A subclass of AbilityBehaviour that implements the logic for the ability
        /// </summary>
        public readonly Type AbilityBehavior;

        /// <summary>
        /// A 49x49 texture for the ability icon
        /// </summary>
        /// <value></value>
        public Texture Texture { get; internal set; }

        /// <summary>
        /// A 49x49 texture for the ability icon, used when the card belongs to the opponent
        /// </summary>
        /// <value></value>
        public Texture CustomFlippedTexture { get; internal set; }

        public string BaseRulebookDescription { get; internal set; }

        /// <summary>
        /// Tracks all rulebook redirects that this ability's description will have. Explanation of the variables is as follows:
        /// Key (string): the text that will be recoloured to indicate that it's clickable.
        /// Tuple.Item1 (PageRangeType): the type of page the redirect will go to. Use PageRangeType.Unique if you want to redirect to a custom rulebook page using its pageId.
        /// Tuple.Item2 (Color): the colour the Key text will be recoloured to.
        /// Tuple.Item3 (string): the id that the API will match against to find the redirect page. Eg, for ability redirects this will be the Ability id as a string.
        /// </summary>
        public Dictionary<string, RuleBookManager.RedirectInfo> RulebookDescriptionRedirects = new();

        internal static ConditionalWeakTable<AbilityInfo, FullAbility> ReverseMapper = new();

        /// <summary>
        /// Creates a new instance of FullAbility and registers its behaviour type with the [TypeManager](InscryptionAPI.Guid.TypeManager).
        /// </summary>
        /// <param name="id">The unique ID for this ability.</param>
        /// <param name="info">The description object for this ability.</param>
        /// <param name="behaviour">A subclass of AbilityBehaviour that implements the logic for the ability.</param>
        /// <param name="texture">A 49x49 texture  for the ability icon.</param>
        [Obsolete("Use the constructor that takes a modGUID parameter instead")]
        public FullAbility(Ability id, AbilityInfo info, Type behaviour, Texture texture) : this("", id, info, behaviour, texture)
        {
        }

        /// <summary>
        /// Creates a new instance of FullAbility and registers its behaviour type with the [TypeManager](InscryptionAPI.Guid.TypeManager).
        /// </summary>
        /// <param name="modGUID">The GUID of the mod that added this.</param>
        /// <param name="id">The unique ID for this ability.</param>
        /// <param name="info">The description object for this ability.</param>
        /// <param name="behaviour">A subclass of AbilityBehaviour that implements the logic for the ability.</param>
        /// <param name="texture">A 49x49 texture  for the ability icon.</param>
        public FullAbility(string modGUID, Ability id, AbilityInfo info, Type behaviour, Texture texture)
        {
            ModGUID = modGUID;
            Id = id;
            Info = info;
            AbilityBehavior = behaviour;
            Texture = texture;
            BaseRulebookDescription = info.rulebookDescription;

            ReverseMapper.Add(info, this);
            TypeManager.Add(id.ToString(), behaviour);
        }

        /// <summary>
        /// Makes a deep copy of the current FullAbility object
        /// </summary>
        public FullAbility Clone()
        {
            AbilityInfo clonedInfo = ScriptableObject.CreateInstance<AbilityInfo>();
            clonedInfo.ability = Info.ability;
            clonedInfo.abilityLearnedDialogue = Info.abilityLearnedDialogue;
            clonedInfo.activated = Info.activated;
            clonedInfo.canStack = Info.canStack;
            clonedInfo.colorOverride = Info.colorOverride;
            clonedInfo.conduit = Info.conduit;
            clonedInfo.conduitCell = Info.conduitCell;
            clonedInfo.customFlippedIcon = Info.customFlippedIcon;
            clonedInfo.customFlippedPixelIcon = Info.customFlippedPixelIcon;
            clonedInfo.flipYIfOpponent = Info.flipYIfOpponent;
            clonedInfo.hasColorOverride = Info.hasColorOverride;
            clonedInfo.keywordAbility = Info.keywordAbility;
            clonedInfo.mesh3D = Info.mesh3D;
            clonedInfo.metaCategories = new(Info.metaCategories);
            clonedInfo.name = Info.name;
            clonedInfo.opponentUsable = Info.opponentUsable;
            clonedInfo.passive = Info.passive;
            clonedInfo.pixelIcon = Info.pixelIcon;
            clonedInfo.powerLevel = Info.powerLevel;
            clonedInfo.rulebookDescription = Info.rulebookDescription;
            clonedInfo.rulebookName = Info.rulebookName;
            clonedInfo.triggerText = Info.triggerText;

            AbilityExtensionProperties.Add(clonedInfo, AbilityExtensionProperties.GetOrCreateValue(Info));

            return new FullAbility(this.ModGUID, this.Id, clonedInfo, this.AbilityBehavior, this.Texture)
            {
                CustomFlippedTexture = this.CustomFlippedTexture,
                RulebookDescriptionRedirects = new(this.RulebookDescriptionRedirects)
            };
        }
    }

    /// <summary>
    /// All of the vanilla game's abilities.
    /// </summary>
    public readonly static ReadOnlyCollection<FullAbility> BaseGameAbilities = new(GenBaseGameAbilityList());
    internal readonly static ObservableCollection<FullAbility> NewAbilities = new();

    /// <summary>
    /// The current processed list of all abilities in the game, including vanilla and mod-added abilities.
    /// </summary>
    public static List<FullAbility> AllAbilities { get; private set; } = BaseGameAbilities.ToList();

    /// <summary>
    /// The current processed list of all AbilityInfos in the game, including vanilla and mod-added abilities.
    /// </summary>
    public static List<AbilityInfo> AllAbilityInfos { get; private set; } = BaseGameAbilities.Select(x => x.Info).ToList();

    /// <summary>
    /// A hook for modders to add custom code to modify the ability list dynamically.
    /// </summary>
    /// <remarks>There are two primary use cases for this hook:
    /// 
    /// - Making changes to abilities that may be added after your plugin initializes
    /// - Making context-aware changes to abilities
    /// 
    /// The way this operates is as follows:
    /// 
    /// 1. The AbilityManager makes a copy of all FullAbility objects. Note that unlike the similar code in CardManager, this is actually a *deep* copy of the AbilityInfo.
    /// 2. The code in ModifyAbiltyList is executed on the copy of those abilities.
    /// 3. The modified list becomes the game's new official list of abilities.
    /// 
    /// The reason the abilities are cloned before processing is so that you can make any change to them you wish
    /// without affecting the original ability. This means that you do not need to try to track changes or remember
    /// what the original version of the ability looked like before you started changing it; the next time 
    /// [SyncAbilityList](xref:InscryptionAPI.AbilityManager.SyncAbilityList) is called, all changes will be reverted
    /// and then re-applied.
    /// 
    /// To use this, you need to add a delegate that accepts the current list of all abilities, then returns
    /// that same list back.
    /// 
    /// ```c#
    /// AbilityManager.ModifyAbilityList += delegate(List&lt;FullAbility&gt; abilities)
    /// {
    ///     // Add the rulebook metacategories to every single ability
    ///     foreach (var ability in abilities)
    ///         ability.info.AddMetaCategories(AbilityMetaCategory.Part1Rulebook, AbilityMetaCategory.Part3Rulebook);
    ///     return abilities;
    /// }
    /// ```
    /// </remarks>
    public static event Func<List<FullAbility>, List<FullAbility>> ModifyAbilityList;

    public static AbilityMetaCategory Part2Modular => GuidManager.GetEnumValue<AbilityMetaCategory>(InscryptionAPIPlugin.ModGUID, "Part2Modular");
    /// <summary>
    /// Resynchronizes the ablity list.
    /// </summary>
    /// <remarks>Most importantly, this re-executes all custom code that was added to the [ModifyAbilityList](xref:InscryptionAPI.AbilityManager.ModifyAbilityList)
    /// event. If you are doing any sort of context-aware processing of the ability list, you may need to manually
    /// call this method in order to make sure that your code executes correctly. However, this automatically gets called
    /// every time that the game transitions from either the main menu into the base game, or whenever a new run is 
    /// started inside of Kaycee's Mod. Only in extreme edge cases should you need to manually call this.</remarks>
    public static void SyncAbilityList()
    {
        AllAbilities = BaseGameAbilities.Concat(NewAbilities).Select(a => a.Clone()).ToList();
        AllAbilities = ModifyAbilityList?.Invoke(AllAbilities) ?? AllAbilities;
        AllAbilityInfos = AllAbilities.Select(x => x.Info).ToList();

        ShieldManager.AllShieldAbilities = AllAbilities.Where(x => x.AbilityBehavior != null && x.AbilityBehavior.IsSubclassOf(typeof(DamageShieldBehaviour))).ToList();
        ShieldManager.AllShieldInfos = ShieldManager.AllShieldAbilities.Select(x => x.Info).ToList();
    }

    static AbilityManager()
    {
        InscryptionAPIPlugin.ScriptableObjectLoaderLoad += static type =>
        {
            if (type == typeof(AbilityInfo))
                ScriptableObjectLoader<AbilityInfo>.allData = AllAbilityInfos;
        };
        NewAbilities.CollectionChanged += static (_, _) =>
        {
            SyncAbilityList();
        };
    }

    private static List<FullAbility> GenBaseGameAbilityList()
    {
        bool useReversePatch = true;
        try
        {
            OriginalLoadAbilityIcon(Ability.AllStrike.ToString());
        }
        catch (NotImplementedException)
        {
            useReversePatch = false;
        }

        List<FullAbility> baseGame = new();
        var gameAsm = typeof(AbilityInfo).Assembly;
        foreach (var ability in Resources.LoadAll<AbilityInfo>("Data/Abilities"))
        {
            string name = ability.ability.ToString();
            if (Part2ModularAbilities.BasePart2Modular.Contains(ability.ability))
                ability.SetDefaultPart2Ability();

            if (name == "DeathShield") // add the API ability behaviour to DeathShield
            {
                ability.SetPassive(false).SetCanStack(true).SetHideSingleStacks(true);
                FullAbility ab = new(
                    null,
                    ability.ability,
                    ability,
                    typeof(APIDeathShield),
                    useReversePatch ? OriginalLoadAbilityIcon(name) : AbilitiesUtil.LoadAbilityIcon(name)
                );
                baseGame.Add(ab);
                continue;
            }
            baseGame.Add(new FullAbility(
                null,
                ability.ability,
                ability,
                gameAsm.GetType($"DiskCardGame.{name}"),
                useReversePatch ? OriginalLoadAbilityIcon(name) : AbilitiesUtil.LoadAbilityIcon(name)
            ));
        }

        return baseGame;
    }

    /// <summary>
    /// Creates a new ability and registers it to be able to be added to cards
    /// </summary>
    /// <param name="guid">The guid of the mod adding the ability.</param>
    /// <param name="info">An instance of AbilityInfo describing the ability.</param>
    /// <param name="behavior">A subclass of AbilityBehaviour that implements the logic for the ability.</param>
    /// <param name="tex">The ability icon as a 49x49 texture.</param>
    /// <returns>An instance of AbilityInfo describing the new ability.</returns>
    /// <remarks>The actual unique identifier for the new ability will be found in the 'ability' field of 
    /// the returned AbilityInfo object. There is no way for the modder to create a specific ability ID; it will
    /// always be assigned by the API.
    /// 
    /// **NOTE**: Even if you manually create an Ability identifier for your ability and attach it to the AbilityInfo 
    /// object before you pass it to the API, the API will *still* generate a unique ability ID for you, which may
    /// or may not be the same as the ID you created for yourself. As such, it is best practice to *not* set the 
    /// ablity ID yourself and leave it as its default value.</remarks>
    public static FullAbility Add(string guid, AbilityInfo info, Type behavior, Texture tex)
    {
        FullAbility full = new(guid, GuidManager.GetEnumValue<Ability>(guid, info.rulebookName), info, behavior, tex);
        full.Info.ability = full.Id;
        info.name = $"{guid}_{info.rulebookName}";
        NewAbilities.Add(full);
        return full;
    }

    /// <summary>
    /// Creates a new ability and registers it to be able to be added to cards
    /// </summary>
    /// <param name="guid">The guid of the mod adding the ability.</param>
    /// <param name="rulebookName">The name of the ability.</param>
    /// <param name="rulebookDescription">The description as it appears in the game's rulebook.</param>
    /// <param name="behavior">A subclass of AbilityBehaviour that implements the logic for the ability.</param>
    /// <param name="pathToArt">Path to the ability texture on disk.</param>
    /// <returns>An instance of AbilityInfo describing the new ability.</returns>
    /// <remarks>The actual unique identifier for the new ability will be found in the 'ability' field of 
    /// the returned AbilityInfo object. There is no way for the modder to create a specific ability ID; it will
    /// always be assigned by the API.</remarks>
    public static AbilityInfo New(string guid, string rulebookName, string rulebookDescription, Type behavior, string pathToArt)
    {
        return New(guid, rulebookName, rulebookDescription, behavior, TextureHelper.GetImageAsTexture(pathToArt));
    }

    /// <summary>
    /// Creates a new ability and registers it to be able to be added to cards
    /// </summary>
    /// <param name="guid">The guid of the mod adding the ability.</param>
    /// <param name="rulebookName">The name of the ability.</param>
    /// <param name="rulebookDescription">The description as it appears in the game's rulebook.</param>
    /// <param name="behavior">A subclass of AbilityBehaviour that implements the logic for the ability.</param>
    /// <param name="tex">The ability icon as a 49x49 texture.</param>
    /// <returns>An instance of AbilityInfo describing the new ability.</returns>
    /// <remarks>The actual unique identifier for the new ability will be found in the 'ability' field of 
    /// the returned AbilityInfo object. There is no way for the modder to create a specific ability ID; it will
    /// always be assigned by the API.</remarks>
    public static AbilityInfo New(string guid, string rulebookName, string rulebookDescription, Type behavior, Texture tex)
    {
        AbilityInfo info = ScriptableObject.CreateInstance<AbilityInfo>();
        info.rulebookName = rulebookName;
        info.rulebookDescription = rulebookDescription;
        Add(guid, info, behavior, tex);
        return info;
    }

    /// <summary>
    /// Removes an ability from the game based on ability ID. Can only remove mod-added abilities, not vanilla abilities.
    /// </summary>
    /// <param name="id">The unique ID of the ability to remove.</param>
    public static void Remove(Ability id) => NewAbilities.Remove(NewAbilities.FirstOrDefault(x => x.Id == id));

    /// <summary>
    /// Removes an ability from the game based on ability ID. Can only remove mod-added abilities, not vanilla abilities.
    /// </summary>
    /// <param name="ability">The instance of the ability to remove.</param>
    public static void Remove(FullAbility ability) => NewAbilities.Remove(ability);

    internal static Dictionary<string, string> GetAbilityExtensionTable(this AbilityInfo info)
    {
        return AbilityExtensionProperties.GetOrCreateValue(info).StringMap;
    }

    #region LoadAbilityIcon
    [HarmonyReversePatch(HarmonyReversePatchType.Original)]
    [HarmonyPatch(typeof(AbilitiesUtil), nameof(AbilitiesUtil.LoadAbilityIcon))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Texture OriginalLoadAbilityIcon(string abilityName, bool fillerAbility = false, bool something = false) { throw new NotImplementedException(); }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbilitiesUtil), nameof(AbilitiesUtil.LoadAbilityIcon))]
    private static bool LoadAbilityIconReplacement(string abilityName, ref Texture __result)
    {
        if (string.IsNullOrEmpty(abilityName))
            return true;

        bool normalTexture = true;
        if (abilityName.EndsWith("_opponent"))
        {
            normalTexture = false;
            abilityName = abilityName.Replace("_opponent", "");
        }

        if (int.TryParse(abilityName, out int abilityId))
        {
            FullAbility ability = AllAbilities.FirstOrDefault(x => x.Id == (Ability)abilityId);
            __result = (normalTexture || ability.CustomFlippedTexture == null) ? ability.Texture : ability.CustomFlippedTexture;
            return false;
        }

        if (Enum.TryParse(abilityName, out Ability abilityEnum))
        {
            FullAbility ability = AllAbilities.FirstOrDefault(x => x.Id == abilityEnum);
            __result = (normalTexture || ability.CustomFlippedTexture == null) ? ability.Texture : ability.CustomFlippedTexture;
            return false;
        }

        return true;
    }
    #endregion

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbilitiesUtil), nameof(AbilitiesUtil.GetLearnedAbilities))]
    private static bool GetLearnedAbilitesReplacement(bool opponentUsable, int minPower, int maxPower, AbilityMetaCategory categoryCriteria, ref List<Ability> __result)
    {
        __result = new();
        foreach (var ability in AllAbilityInfos)
        {
            bool canUse = true;
            canUse &= !opponentUsable || ability.opponentUsable;
            canUse &= minPower <= ability.powerLevel && maxPower >= ability.powerLevel;
            canUse &= ability.metaCategories.Contains(categoryCriteria);
            canUse &= ProgressionData.LearnedAbility(ability.ability);

            if (canUse)
                __result.Add(ability.ability);
        }

        return false;
    }

    #region Rulebook Description
    public const string SIGILCOST = "[sigilcost:";
    [HarmonyPostfix, HarmonyPatch(typeof(AbilityInfo), nameof(AbilityInfo.ParseAndTranslateDescription))]
    private static void CleanUpParsedDescription(ref string __result)
    {
        while (__result.Contains(SIGILCOST))
        {
            string textToCheck = __result.Substring(__result.IndexOf(SIGILCOST));
            if (!textToCheck.Contains("]"))
                break;

            textToCheck = textToCheck.Substring(0, textToCheck.IndexOf("]") + 1);
            __result = __result.Replace(textToCheck, textToCheck.Replace(SIGILCOST, "").Replace("]", ""));
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RuleBookController), nameof(RuleBookController.OpenToAbilityPage))]
    private static bool UpdateRulebookDescription(PlayableCard card)
    {
        ExtendedActivatedAbilityBehaviour component = card?.GetComponent<ExtendedActivatedAbilityBehaviour>();
        if (component != null)
        {
            foreach (FullAbility ab in AllAbilities.Where(ai => ai.Info.activated && card.HasAbility(ai.Id)))
            {
                if (ab.AbilityBehavior.IsAssignableFrom(component.GetType()))
                    ab.Info.rulebookDescription = ParseAndUpdateDescription(ab.Info.rulebookDescription, component);
            }
        }

        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RuleBookController), nameof(RuleBookController.SetShown))]
    private static bool ResetAlteredDescriptions(bool shown)
    {
        if (shown)
            return true;

        foreach (FullAbility ab in AllAbilities.Where(a => !BaseGameAbilities.Contains(a) && a.Info.activated))
        {
            AbilityInfo info = AbilitiesUtil.GetInfo(ab.Id);
            if (info.rulebookDescription != ab.BaseRulebookDescription)
                info.ResetDescription();
        }
        return true;
    }

    internal static string ParseAndUpdateDescription(string description, ExtendedActivatedAbilityBehaviour ability)
    {
        while (description.Contains(SIGILCOST))
        {
            int startIndex = description.IndexOf(SIGILCOST);
            string textToChange = description.Substring(startIndex);
            if (!textToChange.Contains("]"))
                break;

            int endIndex = textToChange.IndexOf("]");
            textToChange = textToChange.Substring(0, endIndex + 1);

            StringBuilder allCosts = new();
            if (ability.BonesCost > 0)
            {
                allCosts.Append(ability.BonesCost.ToString() + " bone");
                if (ability.BonesCost != 1)
                    allCosts.Append("s");
            }
            if (ability.EnergyCost > 0)
            {
                if (allCosts.ToString() != "")
                    allCosts.Append(", ");
                allCosts.Append(ability.EnergyCost.ToString() + " energy");
            }
            if (ability.HealthCost > 0)
            {
                if (allCosts.ToString() != "")
                    allCosts.Append(", ");
                allCosts.Append(ability.HealthCost.ToString() + " health");
            }

            return description.Replace(textToChange, allCosts.ToString() == "" ? "nothing" : allCosts.ToString());
        }

        string[] blocks = description.Split(' ');
        bool energy = ability.energyCostMod == 0;
        bool bones = ability.bonesCostMod == 0;
        bool health = ability.healthCostMod == 0;
        for (int i = 0; i < blocks.Length; i++)
        {
            if (energy && bones && health)
                break;

            if (blocks[i].Any(c => char.IsDigit(c)))
            {
                string nextBlock = blocks[i + 1].ToLowerInvariant();
                if (nextBlock.Contains("energy") || nextBlock.Contains("bone") || nextBlock.Contains("health"))
                {
                    string num = "";
                    foreach (char c in blocks[i])
                    {
                        if (char.IsDigit(c))
                            num += c;
                    }

                    if (!energy && nextBlock.Contains("energy"))
                    {
                        energy = true;
                        blocks[i] = blocks[i].Replace(num, ability.EnergyCost.ToString());
                    }
                    else if (!bones && nextBlock.Contains("bone"))
                    {
                        bones = true;
                        blocks[i] = blocks[i].Replace(num, ability.BonesCost.ToString());

                        if (nextBlock.Contains("bones"))
                        {
                            if (ability.BonesCost == 1)
                                blocks[i + 1] = nextBlock.Replace("bones", "bone");
                        }
                        else
                        {
                            if (ability.BonesCost != 1)
                                blocks[i + 1] = nextBlock.Replace("bone", "bones");
                        }
                    }
                    else if (!health && nextBlock.Contains("health") && blocks[i - 1].ToLowerInvariant() != "power")
                    {
                        health = true;
                        blocks[i] = blocks[i].Replace(num, ability.HealthCost.ToString());
                    }
                }
            }
        }

        return blocks.Join(delimiter: " ");
    }

    [HarmonyPatch(typeof(RuleBookInfo), "ConstructPageData", new Type[] { typeof(AbilityMetaCategory) })]
    [HarmonyPostfix]
    private static void FixRulebook(AbilityMetaCategory metaCategory, RuleBookInfo __instance, ref List<RuleBookPageInfo> __result)
    {
        //InscryptionAPIPlugin.Logger.LogInfo($"In rulebook patch: I see {NewAbilities.Count}");
        if (NewAbilities.Count > 0)
        {
            foreach (PageRangeInfo pageRangeInfo in __instance.pageRanges)
            {
                if (pageRangeInfo.type == PageRangeType.Abilities) // regular abilities
                {
                    int curPageNum = (int)Ability.NUM_ABILITIES;
                    int insertPosition = __result.FindLastIndex(rbi => rbi.pagePrefab == pageRangeInfo.rangePrefab) + 1;
                    List<FullAbility> abilitiesToAdd = NewAbilities.Where(x => __instance.AbilityShouldBeAdded((int)x.Id, metaCategory)).ToList();
                    foreach (FullAbility fab in abilitiesToAdd)
                    {
                        RuleBookPageInfo info = new();
                        info.pagePrefab = pageRangeInfo.rangePrefab;
                        info.headerText = string.Format(Localization.Translate("APPENDIX XII, SUBSECTION I - MOD ABILITIES {0}"), curPageNum);
                        __instance.FillAbilityPage(info, pageRangeInfo, (int)fab.Id);
                        __result.Insert(insertPosition, info);
                        curPageNum++;
                        insertPosition++;
                    }
                }
            }
        }
    }

    #endregion

    [HarmonyPostfix, HarmonyPatch(typeof(PlayableCard), nameof(PlayableCard.TransformIntoCard))]
    private static IEnumerator TriggerStacksOnceAfterEvolve(IEnumerator enumerator, PlayableCard __instance)
    {
        yield return enumerator;
        FixStackTriggers(__instance);
    }
    internal static void FixStackTriggers(PlayableCard __instance)
    {
        // get all triggered abilities
        List<Ability> abilities = __instance.TriggerHandler.triggeredAbilities.ConvertAll(x => x.Item1);

        // loop through the distinct abilities and fix the dumb doubling bug
        foreach (var ab in abilities.Distinct())
        {
            AbilityInfo info = AllAbilityInfos.AbilityByID(ab);
            if (info.passive || !info.canStack || !info.GetTriggersOncePerStack())
                continue;

            // since evolving doubles the triggers, remove half of them
            for (int i = 0; i < abilities.Count(x => x == ab) / 2; i++)
            {
                var tuple = __instance.TriggerHandler.triggeredAbilities.Find(x => x.Item1 == ab);
                __instance.TriggerHandler.triggeredAbilities.Remove(tuple);
            }
        }
    }

    [HarmonyPatch(typeof(GlobalTriggerHandler), nameof(GlobalTriggerHandler.TriggerCardsOnBoard))]
    [HarmonyPostfix]
    private static IEnumerator WaterborneFix(IEnumerator result, Trigger trigger, bool triggerFacedown, params object[] otherArgs)
    {
        yield return result;
        if (triggerFacedown)
            yield break;

        bool RespondsToTrigger(CardTriggerHandler r, Trigger trigger, params object[] otherArgs)
        {
            foreach (TriggerReceiver receiver in r.GetAllReceivers())
            {
                if (GlobalTriggerHandler.ReceiverRespondsToTrigger(trigger, receiver, otherArgs) && (receiver is IActivateWhenFacedown && (receiver as IActivateWhenFacedown).ShouldTriggerWhenFaceDown(trigger, otherArgs)))
                    return true;

            }
            return false;
        }
        IEnumerator OnTrigger(CardTriggerHandler r, Trigger trigger, params object[] otherArgs)
        {
            foreach (TriggerReceiver receiver in r.GetAllReceivers())
            {
                if (GlobalTriggerHandler.ReceiverRespondsToTrigger(trigger, receiver, otherArgs) && ((receiver is IActivateWhenFacedown && (receiver as IActivateWhenFacedown).ShouldTriggerWhenFaceDown(trigger, otherArgs))))
                    yield return Singleton<GlobalTriggerHandler>.Instance.TriggerSequence(trigger, receiver, otherArgs);

            }
            yield break;
        }
        List<PlayableCard> list = new(Singleton<BoardManager>.Instance.CardsOnBoard);
        foreach (PlayableCard playableCard in list)
        {
            if (playableCard != null && playableCard.FaceDown && RespondsToTrigger(playableCard.TriggerHandler, trigger, otherArgs))
                yield return OnTrigger(playableCard.TriggerHandler, trigger, otherArgs);

        }
        yield break;
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(AbilityIconInteractable), nameof(AbilityIconInteractable.AssignAbility))]
    public static IEnumerable<CodeInstruction> AbilityIconInteractable_AssignAbility(IEnumerable<CodeInstruction> instructions)
    {
        // === We want to turn this:
        // AbilityInfo info2 = AbilitiesUtil.GetInfo(ability);

        // === Into this:
        // AbilityInfo info2 = AbilitiesUtil.GetInfo(ability);
        // Log(info2);
        List<CodeInstruction> codes = new(instructions);

        MethodInfo LogAbilityMethodInfo = SymbolExtensions.GetMethodInfo(() => LogAbilityInfo(Ability.Apparition, null, null));
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Stloc_1)
            {
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldarg_1)); // ability
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldloc_1)); // abilityInfo
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldarg_2)); // info
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, LogAbilityMethodInfo));
                break;
            }
        }

        return codes;
    }

    private static void LogAbilityInfo(Ability ability, AbilityInfo abilityInfo, CardInfo info)
    {
        if (abilityInfo == null)
            InscryptionAPIPlugin.Logger.LogError("Cannot find ability " + ability + " for " + info.displayedName);
    }

    #region Evolve Changes
    [HarmonyPatch(typeof(Evolve), nameof(Evolve.OnUpkeep), MethodType.Enumerator)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> EvolveOnUpkeepPatches(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldc_I4_5)
            {
                // this probably belongs in the community patches but this transpiler was already here, so eh
                // overrides the transformer icon so it can display numbers
                MethodInfo customMethod = AccessTools.Method(typeof(AbilityManager), nameof(AbilityManager.OverrideEvolveDerivedIcon),
                    new Type[] { typeof(Evolve), typeof(int) });

                i -= 2;
                int end = codes.FindIndex(i, x => x.opcode == OpCodes.Ldloc_1);
                codes.RemoveRange(i, end - i);
                codes.Insert(i++, new(OpCodes.Ldloc_3));
                codes.Insert(i++, new(OpCodes.Call, customMethod));
            }
            else if (codes[i].opcode == OpCodes.Stfld && codes[i].operand.ToString() == "DiskCardGame.CardInfo <evolution>5__2")
            {
                // evolve doesn't copy mods
                object operand = codes[i].operand;
                MethodInfo customMethod = AccessTools.Method(typeof(AbilityManager), nameof(AbilityManager.RemoveDuplicateMods),
                    new Type[] { typeof(Evolve), typeof(CardInfo) });

                i += 2;
                codes.Insert(i++, new(OpCodes.Ldloc_1));
                codes.Insert(i++, new(OpCodes.Ldarg_0));
                codes.Insert(i++, new(OpCodes.Ldfld, operand));
                codes.Insert(i++, new(OpCodes.Call, customMethod));
                break;
            }
        }

        return codes;
    }
    private static void OverrideEvolveDerivedIcon(Evolve evolve, int turnsLeftToEvolve)
    {
        if (evolve.Ability == Ability.Evolve)
        {
            evolve.Card.RenderInfo.OverrideAbilityIcon(
                Ability.Evolve, ResourceBank.Get<Texture>("Art/Cards/AbilityIcons/ability_evolve_" + turnsLeftToEvolve)
                );
        }
        else if (evolve.Ability == Ability.Transformer && (evolve.Card.Info.evolveParams?.turnsToEvolve ?? 1) != 1)
        {
            evolve.Card.RenderInfo.OverrideAbilityIcon(
                Ability.Transformer, TextureHelper.GetImageAsTexture($"ability_transformer_{turnsLeftToEvolve}.png", typeof(AbilityManager).Assembly)
                );
        }
    }
    private static void RemoveDuplicateMods(Evolve instance, CardInfo evolution) => evolution.Mods.RemoveAll(instance.Card.Info.Mods.Contains);

    [HarmonyPostfix, HarmonyPatch(typeof(Evolve), nameof(Evolve.RemoveTemporaryModsWithEvolve))]
    private static void ResetOverrideAndTurnsInPlay(Evolve __instance)
    {
        // this stuff is needed to fix problems relating to (intentional) chain evolutions that have different evolution delays
        __instance.numTurnsInPlay = 0;
        if (__instance.Card.RenderInfo.overriddenAbilityIcons.ContainsKey(__instance.Ability))
        {
            __instance.Card.RenderInfo.overriddenAbilityIcons.Remove(__instance.Ability);
            __instance.Card.StatsLayer.RenderCard(__instance.Card.RenderInfo);
        }
    }
    #endregion

    #region Better Transformer
    [HarmonyPostfix, HarmonyPatch(typeof(Transformer), nameof(Transformer.GetBeastModeStatsMod))]
    private static void ModifyOtherTransformerCosts(ref CardModificationInfo __result, CardInfo beastModeCard, CardInfo botModeCard)
    {
        __result.SetBloodCost(botModeCard.BloodCost - beastModeCard.BloodCost)
            .SetBonesCost(botModeCard.BonesCost - beastModeCard.BonesCost);
        // currently no way of nullifying specific gem costs
    }
    [HarmonyPostfix, HarmonyPatch(typeof(Transformer), nameof(Transformer.GetTransformCardInfo))]
    private static void ChangeTransformerInfoMethod(Transformer __instance, ref CardInfo __result)
    {
        __result = NewGetTransformCardInfo(__instance.Card);
    }
    private static CardInfo NewGetTransformCardInfo(PlayableCard card)
    {
        // mods carry over when transforming now, so make sure that we aren't transforming into ourselves endlessly
        // if a modder wants that logic for whatever reason, they can use the evolveParams
        string transformCardId = card.Info.Mods.Find(x => !string.IsNullOrEmpty(x.transformerBeastCardId) && x.transformerBeastCardId != card.Info.name)?.transformerBeastCardId;

        // use the API-defined TransformerCardId, evolveParams evolution name, or CSformerAdder as a fallback
        transformCardId ??= ((card.Info.GetTransformerCardId() ?? card.Info.evolveParams?.evolution?.name) ?? "CXformerAdder");

        CardInfo cardByName = CardLoader.GetCardByName(transformCardId);
        CardModificationInfo beastModeStatsMod = Transformer.GetBeastModeStatsMod(cardByName, card.Info);
        beastModeStatsMod.nameReplacement = card.Info.DisplayedNameEnglish;
        beastModeStatsMod.nonCopyable = true;
        cardByName.Mods.Add(beastModeStatsMod);

        // if the evolution already has transformer, assume the modder's set up their own logic we don't want to override
        if (cardByName.LacksAbility(Ability.Transformer))
        {
            cardByName.Mods.Add(new(Ability.Transformer)
            {
                nonCopyable = true
            });
            cardByName.SetEvolve(card.Info, 1);
        }

        if (card.Info.HasSpecialAbility(SpecialTriggeredAbility.TalkingCardChooser))
        {
            card.RenderInfo.prefabPortrait = null;
            card.RenderInfo.hidePortrait = false;
            card.ClearAppearanceBehaviours();
            Singleton<CardRenderCamera>.Instance.StopLiveRenderCard(card.StatsLayer);
        }
        return cardByName;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(AbilityIconInteractable), nameof(AbilityIconInteractable.LoadIcon))]
    private static void LoadTransformerIcon(ref Texture __result, CardInfo info, AbilityInfo ability)
    {
        if (ability.ability != Ability.Transformer)
            return;

        // if the num of turns to evolve is 1, use the default, numberless icon so we don't mess with too much of the vanilla visuals
        int turnsToEvolve = info?.evolveParams?.turnsToEvolve ?? 1;
        if (turnsToEvolve <= 1)
            return;

        __result = TextureHelper.GetImageAsTexture($"ability_transformer_{turnsToEvolve}.png", typeof(AbilityManager).Assembly);
    }
    #endregion

    #region Better Squirrel Orbit
    [HarmonyPatch(typeof(SquirrelOrbit), nameof(SquirrelOrbit.OnUpkeep))]
    [HarmonyPrefix]
    private static bool FixSquirrelOrbit(SquirrelOrbit __instance, ref IEnumerator __result)
    {
        __result = BetterSquirrelOrbit(__instance);
        return false;
    }
    private static IEnumerator BetterSquirrelOrbit(SquirrelOrbit instance)
    {
        List<CardSlot> affectedSlots = new();

        if (instance.Card.HasTrait(Trait.Giant))
            affectedSlots = Singleton<BoardManager>.Instance.GetSlotsCopy(instance.Card.OpponentCard);
        else
            affectedSlots = Singleton<BoardManager>.Instance.AllSlotsCopy;

        affectedSlots.RemoveAll(x => !x.Card || !x.Card.IsAffectedByTidalLock());
        if (affectedSlots.Count == 0)
            yield break;

        instance.Card.Anim.LightNegationEffect();
        yield return new WaitForSeconds(0.2f);

        foreach (CardSlot slot in affectedSlots)
        {
            PlayableCard item = slot.Card;
            Singleton<ViewManager>.Instance.SwitchToView(View.Board);
            yield return new WaitForSeconds(0.25f);
            yield return item.Die(false);
            yield return new WaitForSeconds(0.1f);

            if (instance.Card.HasTrait(Trait.Giant))
                Singleton<ViewManager>.Instance.SwitchToView(View.OpponentQueue);

            yield return new WaitForSeconds(0.1f);

            if (instance.Card.HasSpecialAbility(SpecialTriggeredAbility.GiantMoon))
            {
                instance.FindMoonPortrait();
                instance.moonPortrait.InstantiateOrbitingObject(item.Info);
            }

            if (instance.HasLearned)
                yield return new WaitForSeconds(0.5f);
            else
            {
                yield return new WaitForSeconds(1f);
                yield return instance.LearnAbility();
            }
        }
    }
    #endregion
}