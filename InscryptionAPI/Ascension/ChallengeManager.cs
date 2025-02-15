using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Guid;
using InscryptionAPI.Helpers;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;

namespace InscryptionAPI.Ascension;

/// <summary>
/// The core class for handling and adding challenges,
/// </summary>
[HarmonyPatch]
public static class ChallengeManager
{
    /// <summary>
    /// The default activated eye sprite for challenges.
    /// </summary>
    public static readonly Texture2D DEFAULT_ACTIVATED_SPRITE = Resources.Load<Texture2D>("art/ui/ascension/ascensionicon_activated_default");
    /// <summary>
    /// The activated eye sprite for the "More Difficult" challenge. Can be used for other challenges with that form of eyes.
    /// </summary>
    public static readonly Texture2D HAPPY_ACTIVATED_SPRITE = Resources.Load<Texture2D>("art/ui/ascension/ascensionicon_activated_difficulty");

    /// <summary>
    /// Class that stores full info about challenges, including unlock level, stackability, handlers and incompatible/dependant challenges.
    /// </summary>
    public class FullChallenge
    {
        /// <summary>
        /// The challenge info of this FullChallenge.
        /// </summary>
        public AscensionChallengeInfo Challenge { get; set; }
        /// <summary>
        /// Number of times this challenge appears in the challenge screen.
        /// </summary>
        public int AppearancesInChallengeScreen { get; set; }
        /// <summary>
        /// A function that needs to return true for the challenge to be unlocked. Optional. If this isn't set, this check will be bypassed. The int argument is the current Kaycee's Mod challenge level.
        /// </summary>
        public Func<int, bool> CustomUnlockCheck { get; set; }

        /// <summary>
        /// If true, this challenge's icon(s) will occupy the entire column like the Final Boss challenge icon, and be ordered to the right of regular challenges.
        /// </summary>
        public bool Boss { get; set; }

        /// <summary>
        /// True if this challenge appears more than 1 time in the challenge screen, false otherwise.
        /// </summary>
        public bool Stackable
        {
            get
            {
                return AppearancesInChallengeScreen > 1;
            }
            set
            {
                if (Stackable != value)
                {
                    AppearancesInChallengeScreen = value ? 2 : 1;
                }
            }
        }
        /// <summary>
        /// True if this challenge appears in the challenge screen (AppearancesInChallengeScreen > 0), false otherwise.
        /// </summary>
        public bool AppearsInChallengeScreen
        {
            get
            {
                return AppearancesInChallengeScreen > 0;
            }
            set
            {
                if (AppearsInChallengeScreen != value)
                {
                    AppearancesInChallengeScreen = value ? 1 : 0;
                }
            }
        }
        /// <summary>
        /// The minimum unlock level required for the challenge to be playable.
        /// </summary>
        public int UnlockLevel { get; set; }
        /// <summary>
        /// The challenge handler type for this challenge. For this to be valid, it should inherit ChallengeBehaviour, not be abstract and not be null. This is basically a trigger receiver that will be instantiated every time the player enters a card battle for easy challenge effects.
        /// </summary>
        public Type ChallengeHandler { get; set; }
        /// <summary>
        /// A func that will return the list of the challenges required for this challenge to be activated. Defaults to null, or no dependencies.
        /// </summary>
        public Func<FullChallenge[], IEnumerable<AscensionChallenge>> DependantChallengeGetter { get; set; }
        /// <summary>
        /// A func that will return the list of the challenges incompatible with this challenge, that will get disabled when this challenge is activated. Defaults to null, or no incompatibilities.
        /// </summary>
        public Func<FullChallenge[], IEnumerable<AscensionChallenge>> IncompatibleChallengeGetter { get; set; }
        /// <summary>
        /// The object flags for the challenge. Doesn't do anything by itself, but can be used for building a list of incompatible and dependant challenges. Defaults to null.
        /// </summary>
        public List<object> Flags { get; set; }
        /// <summary>
        /// Returns true if t is a valid challenge handler type (inherits ChallengeBehaviour, not abstract and not null), false otherwise.
        /// </summary>
        /// <param name="t">The type to check.</param>
        /// <returns>True if t is a valid challenge handler type (inherits ChallengeBehaviour, not abstract and not null), false otherwise.</returns>
        public static bool ValidHandlerType(Type t)
        {
            return t != null && !t.IsAbstract && t.IsSubclassOf(typeof(ChallengeBehaviour));
        }
        /// <summary>
        /// The sorting value of this challenge based on its pointValue.
        ///  1 - Positive
        ///  0 - Zero
        /// -1 - Negative
        /// </summary>
        public int SortValue => Math.Sign(Challenge.pointValue);
        /// <summary>
        /// This challenge's SortValue, accounting for if Boss is true.
        ///  -2 - Positive Boss
        ///  -3 - Zero Boss
        ///  -4 - Negative Boss
        /// </summary>
        public int BossSortValue
        {
            get
            {
                if (!Boss)
                    return SortValue;

                return SortValue - 3;
            }
        }
        /// <summary>
        /// Returns true if this challenge has a valid handler type (inherits ChallengeBehaviour, not abstract and not null), false otherwise.
        /// </summary>
        /// <returns>True if this challenge has a valid handler type (inherits ChallengeBehaviour, not abstract and not null), false otherwise.</returns>
        public bool HasValidHandler()
        {
            return ValidHandlerType(ChallengeHandler);
        }
        /// <summary>
        /// Converts this full challenge to an AscensionChallengeInfo, returning this.Challenge.
        /// </summary>
        /// <param name="fc">this.Challenge.</param>
        public static implicit operator AscensionChallengeInfo(FullChallenge fc)
        {
            return fc.Challenge;
        }
        /// <summary>
        /// Sets the AscensionChallengeInfo of this full challenge.
        /// </summary>
        /// <param name="challenge">The new AscensionChallengeInfo for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetChallenge(AscensionChallengeInfo challenge)
        {
            Challenge = challenge;
            return this;
        }
        /// <summary>
        /// Sets the number of appearances in challenge screen of this full challenge.
        /// </summary>
        /// <param name="appearancesInChallengeScreen">The new number of appearances in challenge screen for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetAppearancesInChallengeScreen(int appearancesInChallengeScreen)
        {
            AppearancesInChallengeScreen = appearancesInChallengeScreen;
            return this;
        }
        /// <summary>
        /// Sets the unlock level of this full challenge.
        /// </summary>
        /// <param name="unlockLevel">The new unlock level for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetUnlockLevel(int unlockLevel)
        {
            UnlockLevel = unlockLevel;
            return this;
        }
        /// <summary>
        /// Sets the challenge handler type of this full challenge.
        /// </summary>
        /// <param name="challengeHandler">The new challenge handler for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetChallengeHandler(Type challengeHandler)
        {
            ChallengeHandler = challengeHandler;
            return this;
        }
        /// <summary>
        /// Sets the dependant challenge getter of this full challenge.
        /// </summary>
        /// <param name="dependantChallengeGetter">The new dependant challenge getter for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetDependantChallengeGetter(Func<FullChallenge[], IEnumerable<AscensionChallenge>> dependantChallengeGetter)
        {
            DependantChallengeGetter = dependantChallengeGetter;
            return this;
        }
        /// <summary>
        /// Sets the dependency challenges of this full challenge.
        /// </summary>
        /// <param name="dependantChallenges">The new dependency challenges for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetDependantChallengeGetterStatic(params AscensionChallenge[] dependantChallenges)
        {
            DependantChallengeGetter = (x) => dependantChallenges;
            return this;
        }
        /// <summary>
        /// Sets the dependant whitelist challenge flags (flags that challenges should have to be counted dependencies for this challenge) of this full challenge.
        /// </summary>
        /// <param name="whitelistFlags">The new dependant whitelist challenge flags for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetDependantChallengeGetterFlagWhitelist(params string[] whitelistFlags)
        {
            DependantChallengeGetter = (x) => x.Where(x2 => x2.Flags != null && x2.Flags.Exists(x3 => whitelistFlags.Contains(x3))).Select(x => x.Challenge.challengeType);
            return this;
        }
        /// <summary>
        /// Sets the dependant blacklist challenge flags (flags that challenges should *NOT* have to be counted dependencies for this challenge) of this full challenge.
        /// </summary>
        /// <param name="blacklistFlags">The new dependant blacklist challenge flags for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetDependantChallengeGetterFlagBlacklist(params string[] blacklistFlags)
        {
            DependantChallengeGetter = (x) => x.Where(x2 => x2.Flags == null || !x2.Flags.Exists(x3 => blacklistFlags.Contains(x3))).Select(x => x.Challenge.challengeType);
            return this;
        }
        /// <summary>
        /// Sets the incompatible challenge getter of this full challenge.
        /// </summary>
        /// <param name="incompatibleChallengeGetter">The new incompatible challenge getter for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetIncompatibleChallengeGetter(Func<FullChallenge[], IEnumerable<AscensionChallenge>> incompatibleChallengeGetter)
        {
            IncompatibleChallengeGetter = incompatibleChallengeGetter;
            return this;
        }
        /// <summary>
        /// Sets the incompatible challenges of this full challenge.
        /// </summary>
        /// <param name="incompatibleChallenges">The new incompatible challenges for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetIncompatibleChallengeGetterStatic(params AscensionChallenge[] incompatibleChallenges)
        {
            IncompatibleChallengeGetter = (x) => incompatibleChallenges;
            return this;
        }
        /// <summary>
        /// Sets the incompatible whitelist challenge flags (flags that challenges should have to be counted incompatible with this challenge) of this full challenge.
        /// </summary>
        /// <param name="whitelistFlags">The new incompatible whitelist challenge flags for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetIncompatibleChallengeGetterFlagWhitelist(params string[] whitelistFlags)
        {
            IncompatibleChallengeGetter = (x) => x.Where(x2 => x2.Flags.Exists(x3 => whitelistFlags.Contains(x3))).Select(x => x.Challenge.challengeType);
            return this;
        }
        /// <summary>
        /// Sets the incompatible blacklist challenge flags (flags that challenges should *NOT* have to be counted incompatible with this challenge) of this full challenge.
        /// </summary>
        /// <param name="blacklistFlags">The new incompatible blacklist challenge flags for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetIncompatibleChallengeGetterFlagBlacklist(params string[] blacklistFlags)
        {
            IncompatibleChallengeGetter = (x) => x.Where(x2 => !x2.Flags.Exists(x3 => blacklistFlags.Contains(x3))).Select(x => x.Challenge.challengeType);
            return this;
        }
        /// <summary>
        /// Sets the flags of this full challenge
        /// </summary>
        /// <param name="flags">The new flags for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetFlags(params object[] flags)
        {
            Flags = flags.ToList();
            return this;
        }
        /// <summary>
        /// Sets the custom unlock check of this full challenge
        /// </summary>
        /// <param name="check">The new custom unlock check for this challenge.</param>
        /// <returns>This full challenge, for chaining purposes.</returns>
        public FullChallenge SetCustomUnlock(Func<int, bool> check)
        {
            CustomUnlockCheck = check;
            return this;
        }
        public FullChallenge SetBoss(bool boss)
        {
            Boss = boss;
            return this;
        }
    }
    /// <summary>
    /// The list of all base game challenges in the form of ChallengeManager.FullChallenge.
    /// </summary>
    public static readonly ReadOnlyCollection<FullChallenge> BaseGameChallenges = new(GenBaseGameChallenges());
    internal static readonly ObservableCollection<FullChallenge> NewInfos = new();
    /// <summary>
    /// The list of all new challenges added with the API in the form of ChallengeManager.FullChallenge.
    /// </summary>
    public static readonly ReadOnlyCollection<FullChallenge> NewChallenges = new(NewInfos);

    private static List<FullChallenge> GenBaseGameChallenges()
    {
        List<AscensionChallengeInfo> infos = new(Resources.LoadAll<AscensionChallengeInfo>("Data/Ascension/Challenges"));

        GameObject screenobj = Resources.Load<GameObject>("prefabs/ui/ascension/AscensionChallengesScreen");

        AscensionUnlockSchedule.Initialize();
        AscensionUnlockSchedule schedule = AscensionUnlockSchedule.instance;
        List<FullChallenge> retval = new();
        AscensionChallengeScreen screen = screenobj.GetComponentInChildren<AscensionChallengeScreen>();
        List<AscensionIconInteractable> sortedicons = new(screen.icons);
        sortedicons.Sort((x, x2) => Mathf.RoundToInt((Mathf.Abs(x.transform.position.x - x2.transform.position.x) < 0.1f ? x2.transform.position.y - x.transform.position.y : x.transform.position.x - x2.transform.position.x) * 100));
        foreach (AscensionIconInteractable icon in sortedicons)
        {
            FullChallenge existing = retval.Find(x => x != null && x.Challenge == icon.Info);
            if (existing != null)
            {
                existing.AppearancesInChallengeScreen++;
            }
            else
            {
                retval.Add(new()
                {
                    Challenge = icon.Info,
                    AppearancesInChallengeScreen = 1,
                    UnlockLevel = schedule.unlockTiers.FindIndex(x => x.challengesUnlocked.Contains(icon.Info.challengeType)),
                    ChallengeHandler = null,
                    Boss = icon.Info.challengeType == AscensionChallenge.FinalBoss
                });
            }
        }

        return retval;
    }

    internal static FullChallenge[] GetChallengeIcons()
    {
        AscensionChallengeScreen screen = Singleton<AscensionChallengeScreen>.Instance;
        if (screen == null)
        {
            return new FullChallenge[0];
        }
        return screen.icons?.FindAll(x => x != null && x.challengeInfo != null && x.challengeInfo.challengeType != AscensionChallenge.None && x.challengeInfo.GetFullChallenge() != null)?
            .ConvertAll(x => x.challengeInfo.challengeType.GetFullChallenge())?.ToArray() ?? new FullChallenge[0];
    }

    public static event Func<List<FullChallenge>, List<FullChallenge>> ModifyChallenges;

    private static AscensionChallengeInfo CloneChallengeInfo(AscensionChallengeInfo info)
    {
        AscensionChallengeInfo retval = new()
        {
            activatedSprite = info.activatedSprite,
            challengeType = info.challengeType,
            description = info.description,
            iconSprite = info.iconSprite,
            name = info.name,
            pointValue = info.pointValue,
            title = info.title
        };
        return retval;
    }

    public static void SyncChallengeList()
    {
        List<FullChallenge> asci = new(NewInfos);
        var challenges = BaseGameChallenges.Concat(asci).ToList();
        AllChallenges = ModifyChallenges?.Invoke(challenges) ?? challenges;
    }

    static ChallengeManager()
    {
        InscryptionAPIPlugin.ScriptableObjectLoaderLoad += static type =>
        {
            if (type == typeof(AscensionChallengeInfo))
            {
                ScriptableObjectLoader<AscensionChallengeInfo>.allData = AllInfo;
            }
        };
        NewInfos.CollectionChanged += static (_, _) =>
        {
            SyncChallengeList();
        };
    }

    /// <summary>
    /// Gets the AscensionChallengeInfo with the challengeType equal to info.
    /// </summary>
    /// <param name="info">The challenge type to search for.</param>
    /// <returns>The challenge that was found.</returns>
    public static AscensionChallengeInfo GetInfo(this AscensionChallenge info)
    {
        return AllChallenges.Find(x => x != null && x.Challenge != null && x.Challenge.challengeType == info)?.Challenge;
    }

    /// <summary>
    /// Gets the ChallengeManager.FullChallenge with the challengeType equal to info.
    /// </summary>
    /// <param name="info">The challenge type to search for.</param>
    /// <returns>The full challenge that was found.</returns>
    public static FullChallenge GetFullChallenge(this AscensionChallenge info)
    {
        return AllChallenges.Find(x => x != null && x.Challenge != null && x.Challenge.challengeType == info);
    }

    /// <summary>
    /// Gets the ChallengeManager.FullChallenge with the challengeType equal to info's challengeType.
    /// </summary>
    /// <param name="info">The AscensionChallengeInfo with the challenge type to search for.</param>
    /// <returns>The full challenge that was found.</returns>
    public static FullChallenge GetFullChallenge(this AscensionChallengeInfo info)
    {
        if (info == null)
        {
            return null;
        }
        return info.challengeType.GetFullChallenge();
    }

    /// <summary>
    /// All challenges in the form of ChallengeManager.FullChallenge, including both basegame and new challenges.
    /// </summary>
    public static List<FullChallenge> AllChallenges = BaseGameChallenges.ToList();
    /// <summary>
    /// All challenges in the form of AscensionChallengeInfo, including both basegame and new challenges.
    /// </summary>
    public static List<AscensionChallengeInfo> AllInfo => AllChallenges.ConvertAll(x => x.Challenge);

    /// <summary>
    /// Returns true if the challenge with the challengeType of id is stackable, false otherwise.
    /// </summary>
    /// <param name="id">The challenge type to search for.</param>
    /// <returns>True if the challenge with the challengeType of id is stackable, false otherwise.</returns>
    public static bool IsStackable(AscensionChallenge id)
    {
        return (AllChallenges.Find(x => x != null && x.Challenge != null && x.Challenge.challengeType == id)?.Stackable).GetValueOrDefault();
    }

    /// <summary>
    /// Adds a new challenge with the info of AscensionChallengeInfo, adds the additional info to it and returns it.
    /// </summary>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="info">The challenge info to add.</param>
    /// <param name="unlockLevel">The minimum unlock level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="stackable">True if the challenge should appear twice in the challenge screen, false otherwise.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the challenge info and other additional info.</returns>
    public static FullChallenge Add(string pluginGuid, AscensionChallengeInfo info, int unlockLevel = 0, bool stackable = false)
    {
        return AddSpecific(pluginGuid, info, unlockLevel, stackable ? 2 : 1);
    }

    /// <summary>
    /// Adds a new challenge with the info of AscensionChallengeInfo, adds the additional info to it and returns it.
    /// </summary>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="info">The challenge info to add.</param>
    /// <param name="unlockLevel">The minimum unlock level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="numAppearancesInChallengeScreen">The number of times this challenge should appear in the challenge screen. Defaults to 1.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the challenge info and other additional info.</returns>
    public static FullChallenge AddSpecific(string pluginGuid, AscensionChallengeInfo info, int unlockLevel = 0, int numAppearancesInChallengeScreen = 1)
    {
        return AddSpecific(pluginGuid, info, null, unlockLevel, null, null, null, numAppearancesInChallengeScreen);
    }

    /// <summary>
    /// Creates and registers a new challenge.
    /// </summary>
    /// <typeparam name="T">The challenge handler type for the challenge.</typeparam>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="title">The name of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="description">The description of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="pointValue">The points given for activating the challenge.</param>
    /// <param name="iconTexture">The icon of the challenge that will appear in the challenge screen.</param>
    /// <param name="activatedTexture">The glowy part of the icon that will be shown when the challenge is activated.</param>
    /// <param name="unlockLevel">The minimum level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="flags">The object flags for the challenge. Doesn't do anything by itself, but can be used for building a list of incompatible and dependant challenges. Defaults to null.</param>
    /// <param name="dependantChallengeGetter">A func that will return the list of the challenges required for this challenge to be activated. Defaults to null, or no dependencies.</param>
    /// <param name="incompatibleChallengeGetter">A func that will return the list of the challenges incompatible with this challenge, that will get disabled when this challenge is activated. Defaults to null, or no incompatibilities.</param>
    /// <param name="numAppearancesInChallengeScreen">The number of times this challenge should appear in the challenge screen. Defaults to 1.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the built challenge info and other additional info.</returns>
    public static FullChallenge AddSpecific<T>(
        string pluginGuid,
        string title,
        string description,
        int pointValue,
        Texture2D iconTexture,
        Texture2D activatedTexture,
        int unlockLevel = 0,
        List<object> flags = null,
        Func<FullChallenge[], IEnumerable<AscensionChallenge>> dependantChallengeGetter = null,
        Func<FullChallenge[], IEnumerable<AscensionChallenge>> incompatibleChallengeGetter = null,
        int numAppearancesInChallengeScreen = 1
    ) where T : ChallengeBehaviour
    {
        return AddSpecific(pluginGuid, title, description, pointValue, iconTexture, activatedTexture, typeof(T), unlockLevel, flags, dependantChallengeGetter, incompatibleChallengeGetter, numAppearancesInChallengeScreen);
    }

    /// <summary>
    /// Creates and registers a new challenge.
    /// </summary>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="title">The name of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="description">The description of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="pointValue">The points given for activating the challenge.</param>
    /// <param name="iconTexture">The icon of the challenge that will appear in the challenge screen.</param>
    /// <param name="activatedTexture">The glowy part of the icon that will be shown when the challenge is activated.</param>
    /// <param name="handlerType">The challenge handler type for the challenge.</param>
    /// <param name="unlockLevel">The minimum level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="flags">The object flags for the challenge. Doesn't do anything by itself, but can be used for building a list of incompatible and dependant challenges. Defaults to null.</param>
    /// <param name="dependantChallengeGetter">A func that will return the list of the challenges required for this challenge to be activated. Defaults to null, or no dependencies.</param>
    /// <param name="incompatibleChallengeGetter">A func that will return the list of the challenges incompatible with this challenge, that will get disabled when this challenge is activated. Defaults to null, or no incompatibilities.</param>
    /// <param name="numAppearancesInChallengeScreen">The number of times this challenge should appear in the challenge screen. Defaults to 1.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the built challenge info and other additional info.</returns>
    public static FullChallenge AddSpecific(
        string pluginGuid,
        string title,
        string description,
        int pointValue,
        Texture2D iconTexture,
        Texture2D activatedTexture,
        Type handlerType = null,
        int unlockLevel = 0,
        List<object> flags = null,
        Func<FullChallenge[], IEnumerable<AscensionChallenge>> dependantChallengeGetter = null,
        Func<FullChallenge[], IEnumerable<AscensionChallenge>> incompatibleChallengeGetter = null,
        int numAppearancesInChallengeScreen = 1
    )
    {
        AscensionChallengeInfo info = ScriptableObject.CreateInstance<AscensionChallengeInfo>();
        info.title = title;
        info.challengeType = AscensionChallenge.None;
        info.description = description;
        info.pointValue = pointValue;
        info.iconSprite = TextureHelper.ConvertTexture(iconTexture, TextureHelper.SpriteType.ChallengeIcon);
        info.activatedSprite = TextureHelper.ConvertTexture(activatedTexture, TextureHelper.SpriteType.ChallengeIcon);

        return AddSpecific(pluginGuid, info, handlerType, unlockLevel, flags, dependantChallengeGetter, incompatibleChallengeGetter, numAppearancesInChallengeScreen);
    }

    /// <summary>
    /// Adds a new challenge with the info of AscensionChallengeInfo, adds the additional info to it and returns it.
    /// </summary>
    /// <typeparam name="T">The challenge handler type for the challenge.</typeparam>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="info">The challenge info to add.</param>
    /// <param name="unlockLevel">The minimum level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="flags">The object flags for the challenge. Doesn't do anything by itself, but can be used for building a list of incompatible and dependant challenges. Defaults to null.</param>
    /// <param name="dependantChallengeGetter">A func that will return the list of the challenges required for this challenge to be activated. Defaults to null, or no dependencies.</param>
    /// <param name="incompatibleChallengeGetter">A func that will return the list of the challenges incompatible with this challenge, that will get disabled when this challenge is activated. Defaults to null, or no incompatibilities.</param>
    /// <param name="numAppearancesInChallengeScreen">The number of times this challenge should appear in the challenge screen. Defaults to 1.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the challenge info and other additional info.</returns>
    public static FullChallenge AddSpecific<T>(string pluginGuid, AscensionChallengeInfo info, int unlockLevel = 0, List<object> flags = null,
        Func<FullChallenge[], IEnumerable<AscensionChallenge>> dependantChallengeGetter = null, Func<FullChallenge[], IEnumerable<AscensionChallenge>> incompatibleChallengeGetter = null, int numAppearancesInChallengeScreen = 1)
        where T : ChallengeBehaviour
    {
        return AddSpecific(pluginGuid, info, typeof(T), unlockLevel, flags, dependantChallengeGetter, incompatibleChallengeGetter, numAppearancesInChallengeScreen);
    }

    /// <summary>
    /// Adds a new challenge with the info of AscensionChallengeInfo, adds the additional info to it and returns it.
    /// </summary>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="info">The challenge info to add.</param>
    /// <param name="handlerType">The challenge handler type for the challenge.</param>
    /// <param name="unlockLevel">The minimum level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="flags">The object flags for the challenge. Doesn't do anything by itself, but can be used for building a list of incompatible and dependant challenges. Defaults to null.</param>
    /// <param name="dependantChallengeGetter">A func that will return the list of the challenges required for this challenge to be activated. Defaults to null, or no dependencies.</param>
    /// <param name="incompatibleChallengeGetter">A func that will return the list of the challenges incompatible with this challenge, that will get disabled when this challenge is activated. Defaults to null, or no incompatibilities.</param>
    /// <param name="numAppearancesInChallengeScreen">The number of times this challenge should appear in the challenge screen. Defaults to 1.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the challenge info and other additional info.</returns>
    public static FullChallenge AddSpecific(string pluginGuid, AscensionChallengeInfo info, Type handlerType = null, int unlockLevel = 0, List<object> flags = null,
        Func<FullChallenge[], IEnumerable<AscensionChallenge>> dependantChallengeGetter = null, Func<FullChallenge[], IEnumerable<AscensionChallenge>> incompatibleChallengeGetter = null, int numAppearancesInChallengeScreen = 1)
    {
        info.challengeType = GuidManager.GetEnumValue<AscensionChallenge>(pluginGuid, info.title);

        FullChallenge fc = new()
        {
            Challenge = info,
            AppearancesInChallengeScreen = numAppearancesInChallengeScreen,
            UnlockLevel = unlockLevel,
            ChallengeHandler = handlerType,
            DependantChallengeGetter = dependantChallengeGetter,
            IncompatibleChallengeGetter = incompatibleChallengeGetter,
            Flags = flags != null ? new(flags) : new()
        };

        NewInfos.Add(fc);

        return fc;
    }

    /// <summary>
    /// Creates and registers a new challenge.
    /// </summary>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="title">The name of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="description">The description of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="pointValue">The points given for activating the challenge.</param>
    /// <param name="iconTexture">The icon of the challenge that will appear in the challenge screen.</param>
    /// <param name="activatedTexture">The glowy part of the icon that will be shown when the challenge is activated.</param>
    /// <param name="unlockLevel">The minimum level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="numAppearancesInChallengeScreen">The number of times this challenge should appear in the challenge screen. Defaults to 1.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the built challenge info and other additional info.</returns>
    public static FullChallenge AddSpecific(
        string pluginGuid,
        string title,
        string description,
        int pointValue,
        Texture2D iconTexture,
        Texture2D activatedTexture,
        int unlockLevel = 0,
        int numAppearancesInChallengeScreen = 1
    )
    {
        AscensionChallengeInfo info = ScriptableObject.CreateInstance<AscensionChallengeInfo>();
        info.title = title;
        info.challengeType = AscensionChallenge.None;
        info.description = description;
        info.pointValue = pointValue;
        info.iconSprite = TextureHelper.ConvertTexture(iconTexture, TextureHelper.SpriteType.ChallengeIcon);
        info.activatedSprite = TextureHelper.ConvertTexture(activatedTexture, TextureHelper.SpriteType.ChallengeIcon);

        return AddSpecific(pluginGuid, info, unlockLevel, numAppearancesInChallengeScreen);
    }

    /// <summary>
    /// Adds a new challenge with the info of AscensionChallengeInfo, adds the additional info to it and returns it.
    /// </summary>
    /// <param name="pluginGuid">The GUID of the plugin adding the challenge.</param>
    /// <param name="title">The name of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="description">The description of the challenge that will be displayed in the challenge screen.</param>
    /// <param name="pointValue">The points given for activating the challenge.</param>
    /// <param name="iconTexture">The icon of the challenge that will appear in the challenge screen.</param>
    /// <param name="activatedTexture">The glowy part of the icon that will be shown when the challenge is activated.</param>
    /// <param name="unlockLevel">The minimum level required for the challenge to be playable. Defaults to 0.</param>
    /// <param name="stackable">True if the challenge should appear twice in the challenge screen, false otherwise.</param>
    /// <returns>The built ChallengeManager.FullChallenge, with the built challenge info and other additional info.</returns>
    public static FullChallenge Add(
        string pluginGuid,
        string title,
        string description,
        int pointValue,
        Texture2D iconTexture,
        Texture2D activatedTexture,
        int unlockLevel = 0,
        bool stackable = false
    )
    {
        return AddSpecific(pluginGuid, title, description, pointValue, iconTexture, activatedTexture, unlockLevel, stackable ? 2 : 1);
    }

    [HarmonyPatch(typeof(AscensionUnlockSchedule), "ChallengeIsUnlockedForLevel")]
    [HarmonyPostfix]
    private static void IsCustomChallengeUnlocked(ref bool __result, int level, AscensionChallenge challenge)
    {
        if (!__result)
        {
            FullChallenge fc = NewInfos.ToList().Find(x => x != null && x.Challenge != null && x.Challenge.challengeType == challenge);
            __result = fc != null && level >= fc.UnlockLevel;
        }
        __result &= challenge.GetFullChallenge()?.CustomUnlockCheck == null || challenge.GetFullChallenge().CustomUnlockCheck(level);
    }

    [HarmonyPatch(typeof(AscensionMenuScreens), nameof(AscensionMenuScreens.TransitionToGame))]
    [HarmonyPrefix]
    private static void ResyncWhenTransitioningToGame()
    {
        SyncChallengeList();
    }

    [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.SetupPhase))]
    [HarmonyPostfix]
    private static IEnumerator ActivateChallenges(IEnumerator result, TurnManager __instance)
    {
        ChallengeBehaviour.DestroyAllInstances();
        if (SaveFile.IsAscension)
        {
            AscensionChallenge[] challenges = AscensionSaveData.Data.activeChallenges.ToArray();
            foreach (AscensionChallenge chall in challenges)
            {
                FullChallenge fc = chall.GetFullChallenge();
                if (fc != null && fc.HasValidHandler())
                {
                    int instances = ChallengeBehaviour.CountInstancesOfType(fc.Challenge.challengeType);
                    if (fc.Stackable || instances < 1)
                    {
                        GameObject handler = new(fc.Challenge.name + " Challenge Handler");
                        ChallengeBehaviour behav = handler.AddComponent(fc.ChallengeHandler) as ChallengeBehaviour;
                        if (behav != null)
                        {
                            GlobalTriggerHandler.Instance?.RegisterNonCardReceiver(behav);
                            behav.challenge = fc;
                            behav.instanceNumber = instances + 1;
                            ChallengeBehaviour.Instances.Add(behav);
                            if (behav.RespondsToPreBattleSetup())
                            {
                                yield return behav.OnPreBattleSetup();
                            }
                        }
                    }
                }
            }
        }
        yield return result;
        ChallengeBehaviour[] cbs = ChallengeBehaviour.Instances.ToArray();
        foreach (ChallengeBehaviour bb in cbs)
        {
            if (bb != null && bb.RespondsToPostBattleSetup())
                yield return bb.OnPostBattleSetup();
        }
        yield break;
    }

    [HarmonyPatch(typeof(ChallengeActivationUI), nameof(ChallengeActivationUI.ShowActivation))]
    [HarmonyPrefix]
    private static void FixIconColor(ChallengeActivationUI __instance, AscensionChallenge challenge)
    {
        Color iconColor = GameColors.Instance.gold;
        var info = AscensionChallengesUtil.GetInfo(challenge);
        if (info == null || info.pointValue > 0 || challenge == AscensionChallenge.None)
        {
            iconColor = GameColors.Instance.red;
        }
        else if (info.pointValue < 0)
        {
            iconColor = GameColors.Instance.darkLimeGreen;
        }
        __instance.icon.color = iconColor;
        __instance.blinkEffect.blinkOffColor = iconColor;
    }

    [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.CleanupPhase))]
    [HarmonyPostfix]
    private static IEnumerator Postfix(IEnumerator result)
    {
        ChallengeBehaviour[] cbs = ChallengeBehaviour.Instances.ToArray();
        foreach (ChallengeBehaviour bb in cbs)
        {
            if (bb != null && bb.RespondsToPreBattleCleanup())
                yield return bb.OnPreBattleCleanup();
        }
        yield return result;
        cbs = ChallengeBehaviour.Instances.ToArray();
        foreach (ChallengeBehaviour bb in cbs)
        {
            if (bb != null && bb.RespondsToPostBattleCleanup())
                yield return bb.OnPostBattleCleanup();
        }
        ChallengeBehaviour.DestroyAllInstances();
        yield break;
    }

    [HarmonyPatch(typeof(AscensionIconInteractable), nameof(AscensionIconInteractable.OnCursorSelectStart))]
    [HarmonyPostfix]
    private static void DoDependenciesAndIncompatibilities(AscensionIconInteractable __instance)
    {
        if (__instance.Unlocked && __instance.clickable)
        {
            bool setCurrentIcon = false;
            if (currentIcon == null)
            {
                currentIcon = __instance;
                setCurrentIcon = true;
            }
            bool activated = __instance.activatedRenderer.enabled;
            AscensionChallengeScreen screen = Singleton<AscensionChallengeScreen>.Instance;
            var challengeInfo = __instance.challengeInfo;
            if (activated)
            {
                bool shouldDisable = false;
                List<AscensionChallenge> dependencies = challengeInfo?.GetFullChallenge()?.DependantChallengeGetter?.Invoke(GetChallengeIcons())?.ToList();
                List<AscensionChallenge> incompatibilities = challengeInfo?.GetFullChallenge()?.IncompatibleChallengeGetter?.Invoke(GetChallengeIcons())?.ToList();
                if (dependencies != null && incompatibilities != null)
                {
                    incompatibilities.RemoveAll(x => dependencies.Contains(x));
                }
                if (dependencies != null)
                {
                    dependencies.RemoveAll(x => x == challengeInfo.challengeType);
                    List<AscensionChallenge> dependenciesClone = new(dependencies);
                    foreach (var icon in screen.icons.Where(x => x != null && x.Unlocked && x.clickable && x.activatedRenderer != null && x.activatedRenderer.enabled && x.Info != null &&
                        dependencies.Contains(x.Info.challengeType)))
                    {
                        if (dependenciesClone.Contains(icon.Info.challengeType))
                        {
                            dependenciesClone.Remove(icon.Info.challengeType);
                        }
                    }
                    if (dependenciesClone.Count > 0)
                    {
                        List<AscensionIconInteractable> challengeIcons = new();
                        foreach (var icon in screen.icons.Where(x => x != null && x.Unlocked && x.clickable && x.activatedRenderer != null && !x.activatedRenderer.enabled && x.Info != null &&
                            dependencies.Contains(x.Info.challengeType)))
                        {
                            if (dependenciesClone.Contains(icon.Info.challengeType))
                            {
                                dependenciesClone.Remove(icon.Info.challengeType);
                                challengeIcons.Add(icon);
                            }
                        }
                        if (dependenciesClone.Count > 0)
                        {
                            shouldDisable = true;
                        }
                        else
                        {
                            foreach (var icon in challengeIcons)
                            {
                                icon?.OnCursorSelectStart();
                            }
                            Singleton<AscensionChallengeDisplayer>.Instance?.DisplayChallenge(challengeInfo, false);
                        }
                    }
                }
                if (shouldDisable)
                {
                    __instance.ShowActivated(false);
                    screen.SetChallengeActivated(__instance.challengeInfo, false);
                }
                else
                {
                    foreach (var icon in screen.icons)
                    {
                        if (icon.activatedRenderer != null && icon.activatedRenderer.enabled && icon != currentIcon && icon.challengeInfo != null)
                        {
                            List<AscensionChallenge> dependencies2 = icon?.challengeInfo?.GetFullChallenge()?.DependantChallengeGetter?.Invoke(GetChallengeIcons())?.ToList();
                            List<AscensionChallenge> incompatibilities2 = icon?.challengeInfo?.GetFullChallenge()?.IncompatibleChallengeGetter?.Invoke(GetChallengeIcons())?.ToList();
                            if (incompatibilities2 != null)
                            {
                                if (dependencies2 != null)
                                    incompatibilities.RemoveAll(dependencies2.Contains);

                                incompatibilities2.RemoveAll(x => x == icon.challengeInfo.challengeType);
                                if (screen.icons.Exists(x => x.Unlocked && x.clickable && x.Info != null && x.activatedRenderer != null && x.activatedRenderer.enabled && incompatibilities2.Contains(x.Info.challengeType)))
                                    icon?.OnCursorSelectStart();
                            }
                        }
                    }
                    if (incompatibilities != null)
                    {
                        incompatibilities.RemoveAll(x => x == challengeInfo.challengeType);
                        screen.icons.FindAll(x => x.Unlocked && x.clickable && x.activatedRenderer.enabled && incompatibilities.Contains(x.Info.challengeType)).ForEach(x => x.OnCursorSelectStart());
                        screen.icons.FindAll(x => x.Unlocked && x.clickable && x.activatedRenderer.enabled && incompatibilities.Contains(x.Info.challengeType)).ForEach(x => x.OnCursorSelectStart());
                        AscensionSaveData.Data.activeChallenges?.RemoveAll(incompatibilities.Contains);
                        screen.challengeLevelText?.UpdateText();
                        Singleton<AscensionChallengeDisplayer>.Instance?.DisplayChallenge(challengeInfo, false);
                    }
                }
            }
            else
            {
                foreach (var icon in screen.icons)
                {
                    if (icon.activatedRenderer.enabled && icon != currentIcon)
                    {
                        List<AscensionChallenge> dependencies = icon?.challengeInfo?.GetFullChallenge()?.DependantChallengeGetter?.Invoke(GetChallengeIcons())?.ToList();
                        List<AscensionChallenge> incompatibilities = icon?.challengeInfo?.GetFullChallenge()?.IncompatibleChallengeGetter?.Invoke(GetChallengeIcons())?.ToList();
                        if (dependencies != null)
                        {
                            incompatibilities?.RemoveAll(dependencies.Contains);

                            dependencies.RemoveAll(x => x == icon.challengeInfo.challengeType);
                            List<AscensionChallenge> dependenciesClone = new(dependencies);
                            foreach (var icon2 in screen.icons.Where(x => x.Unlocked && x.clickable && x.Info != null && x.activatedRenderer != null && x.activatedRenderer.enabled && dependencies.Contains(x.Info.challengeType)))
                            {
                                if (dependenciesClone.Contains(icon2.Info.challengeType))
                                    dependenciesClone.Remove(icon2.Info.challengeType);
                            }
                            if (dependenciesClone.Count > 0)
                                icon?.OnCursorSelectStart();
                        }
                    }
                }
            }
            if (setCurrentIcon)
                currentIcon = null;
        }
    }

    private static AscensionIconInteractable currentIcon;
}