using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Guid;
using InscryptionAPI.Helpers;
using InscryptionAPI.RuleBook;
using Sirenix.Serialization.Utilities;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;

namespace InscryptionAPI.Slots;

/// <summary>
/// Manager for card slot modifications
/// </summary>
[HarmonyPatch]
public class SlotModificationManager : MonoBehaviour
{
    /// <summary>
    /// Unique identifiers for slot modifications.
    /// </summary>
    public enum ModificationType
    {
        NoModification = 0
    }

    /// <summary>
    /// Contains information about a slot modification.
    /// </summary>
    public class Info
    {
        /// <summary>
        /// The internal name for the slot modification.
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// The GUID of the mod that added this slot modification.
        /// </summary>
        public string ModGUID { get; internal set; }

        /// <summary>
        /// If this slot modification can appear in the rulebook, this will be the name displayed. Leave null if you don't want your slot modification to appear in the rulebook.
        /// </summary>
        public string RulebookName { get; set; }
        /// <summary>
        /// The slot's description as it will appear in the rulebook.
        /// </summary>
        public string RulebookDescription { get; set; }

        /// <summary>
        /// The sprite to use for the rulebook entry. If this is null, the API will use the first slot value in the Texture dictionary.
        /// </summary>
        public Sprite RulebookSprite { get; set; }
        /// <summary>
        /// Set using SetRulebookP03Sprite if you want your slot to have a different rulebook sprite in Act 3 than in other acts.
        /// </summary>
        public Sprite P03RulebookSprite { get; set; } = null;
        /// <summary>
        /// Set using SetRulebookGrimoraSprite if you want your slot to have a different rulebook sprite in Grimora's Act than in other acts.
        /// </summary>
        public Sprite GrimoraRulebookSprite { get; set; } = null;
        /// <summary>
        /// Set using SetRulebookMagnificusSprite if you want your slot to have a different rulebook sprite in Magnificus's Act than in other acts.
        /// </summary>
        public Sprite MagnificusRulebookSprite { get; set; } = null;

        /// <summary>
        /// The slot modification whose rulebook entry is also used by this slot modification.
        /// </summary>
        public ModificationType SharedRulebook { get; set; } = ModificationType.NoModification;
        public List<ModificationMetaCategory> MetaCategories = new();

        /// <summary>
        /// The slot's modified texture in 3D scenes (Leshy, P03, etc) (154x226)
        /// </summary>
        public Dictionary<CardTemple, Texture2D> Texture { get; internal set; }

        /// <summary>
        /// The slots modified textures in 2D scenes (Act 2) (44x58)
        /// </summary>
        /// <remarks>Each NPC has a slightly different theme, so custom slot textures need to accomodate these themes.</remarks>
        public Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> PixelBoardSlotSprites { get; internal set; }

        /// <summary>
        /// Unique identifier for the slot modification. This will be assigned by the API.
        /// </summary>
        public ModificationType ModificationType { get; internal set; }

        /// <summary>
        /// Class that contains the behavior for the slot. This must be a subclass of SlotModificationBehaviour
        /// </summary>
        public Type SlotBehaviour { get; internal set; }

        /// <summary>
        /// Tracks all rulebook redirects that this ability's description will have. Explanation of the variables is as follows:
        /// Key (string): the text that will be recoloured to indicate that it's clickable.
        /// Tuple.Item1 (PageRangeType): the type of page the redirect will go to. Use PageRangeType.Unique if you want to redirect to a custom rulebook page using its pageId.
        /// Tuple.Item2 (Color): the colour the Key text will be recoloured to.
        /// Tuple.Item3 (string): the id that the API will match against to find the redirect page. Eg, for ability redirects this will be the Ability id as a string.
        /// </summary>
        public Dictionary<string, RuleBookManager.RedirectInfo> RulebookDescriptionRedirects = new();

        public Info(string name, string modGuid,
            Dictionary<CardTemple, Texture2D> texture,
            Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> pixelSprites,
            ModificationType modType, Type behaviour,
            string rulebookName, string rulebookDescription, Sprite rulebookSprite,
            List<ModificationMetaCategory> categories)
        {
            this.Name = name;
            this.ModGUID = modGuid;
            this.Texture = texture;
            this.PixelBoardSlotSprites = pixelSprites;
            this.ModificationType = modType;
            this.SlotBehaviour = behaviour;
            this.RulebookName = rulebookName;
            this.RulebookDescription = rulebookDescription;
            this.RulebookSprite = rulebookSprite;
            this.MetaCategories = categories;
        }
        public Info Clone()
        {
            return new(
                this.Name, this.ModGUID,
                this.Texture != null ? new(this.Texture) : null,
                this.PixelBoardSlotSprites != null ? new(this.PixelBoardSlotSprites) : null,
                this.ModificationType, this.SlotBehaviour,
                this.RulebookName, this.RulebookDescription, this.RulebookSprite,
                this.MetaCategories
                )
            {
                RulebookDescriptionRedirects = new(this.RulebookDescriptionRedirects),
                SharedRulebook = this.SharedRulebook,
                P03RulebookSprite = this.P03RulebookSprite,
                GrimoraRulebookSprite = this.GrimoraRulebookSprite,
                MagnificusRulebookSprite = this.MagnificusRulebookSprite
            };
        }
    }

    internal readonly Dictionary<CardSlot, Tuple<ModificationType, SlotModificationBehaviour>> SlotReceivers = new();

    internal readonly static ObservableCollection<Info> AllSlotModifications = new() {
        new("NoModification", InscryptionAPIPlugin.ModGUID, null, null, ModificationType.NoModification, null, null, null, null, new())
    };

    // old AllSlotModifications, keep in case changing it to an ObservableCollection breaks garbage
    /*internal static List<Info> AllSlotModifications = new() {
        new("NoModification", InscryptionAPIPlugin.ModGUID, null, null, ModificationType.NoModification, null, null, new())
    };*/

    public static List<Info> AllModificationInfos { get; private set; } = new();
    public static List<ModificationType> AllModificationTypes { get; private set; } = new();

    public static event Func<List<Info>, List<Info>> ModifySlotModificationList;

    static SlotModificationManager()
    {
        AllSlotModifications.CollectionChanged += static (_, _) =>
        {
            SyncSlotModificationList();
        };
    }

    public static void SyncSlotModificationList()
    {
        AllModificationInfos = AllSlotModifications.Select(x => x.Clone()).ToList();
        AllModificationInfos = ModifySlotModificationList?.Invoke(AllModificationInfos) ?? AllModificationInfos;
        AllModificationTypes = AllSlotModifications.Select(x => x.ModificationType).ToList();
    }

    private static Color ParseHtml(string html)
    {
        if (ColorUtility.TryParseHtmlString(html, out Color c))
            return c;

        return Color.white;
    }

    private static readonly Color TRANSPARENT = new(0f, 0f, 0f, 0f);

    private static readonly Dictionary<PixelBoardSpriteSetter.BoardTheme, Tuple<Color, Color>> DEFAULT_COLORS = new()
    {
        { PixelBoardSpriteSetter.BoardTheme.Tech, new(ParseHtml("#446969"), ParseHtml("#B4FFEC")) },
        { PixelBoardSpriteSetter.BoardTheme.P03, new(ParseHtml("#446969"), ParseHtml("#B4FFEC")) },
        { PixelBoardSpriteSetter.BoardTheme.Nature, new(ParseHtml("#FF9226"), ParseHtml("#F7C376")) },
        { PixelBoardSpriteSetter.BoardTheme.Wizard, new(ParseHtml("#C1D080"), ParseHtml("#EEF4C6")) },
        { PixelBoardSpriteSetter.BoardTheme.Undead, new(ParseHtml("#C1D080"), ParseHtml("#EEF4C6")) },
        { PixelBoardSpriteSetter.BoardTheme.Finale, new(ParseHtml("#E14C89"), ParseHtml("#F779AD")) },
    };

    /// <summary>
    /// Creates a new card slot modification.
    /// </summary>
    /// <param name="modGuid">Unique ID for the mod creating the slot modification</param>
    /// <param name="modificationName">Reference name for the slot modification</param>
    /// <param name="behaviour">The class that controls the behavior for the new slot</param>
    /// <param name="slotTexture">The 3D scene slot texture (154x226)</param>
    /// <param name="slotPixelTexture">The 2D scene slot texture</param>
    /// <returns>Unique identifier for the modification type; used to set the slot modification in the future</returns>
    public static ModificationType New(string modGuid, string modificationName, Type behaviour, Dictionary<CardTemple, Texture2D> slotTexture, Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> pixelBoardSlotSprites)
    {
        if (!behaviour.IsSubclassOf(typeof(SlotModificationBehaviour)))
            throw new InvalidOperationException($"Could not create new ModificationType {modificationName}; behaviour must be a subclass of SlotModificationBehaviour");

        ModificationType mType = GuidManager.GetEnumValue<ModificationType>(modGuid, modificationName);
        AllSlotModifications.Add(new(modificationName, modGuid, slotTexture, pixelBoardSlotSprites, mType, behaviour, null, null, null, new()));
        return mType;
    }

    /// <summary>
    /// Creates a new card slot modification.
    /// </summary>
    /// <param name="modGuid">Unique ID for the mod creating the slot modification</param>
    /// <param name="modificationName">Reference name for the slot modification</param>
    /// <param name="behaviour">The class that controls the behavior for the new slot</param>
    /// <param name="slotTexture">The 3D scene slot texture (154x226)</param>
    /// <param name="slotPixelTexture">The 2D scene slot texture</param>
    /// <returns>Unique identifier for the modification type; used to set the slot modification in the future</returns>
    public static ModificationType New(string modGuid, string modificationName, Type behaviour, Texture2D slotTexture, Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> pixelBoardSlotSprites)
    {
        Dictionary<CardTemple, Texture2D> templeMap = new()
        {
            [CardTemple.Nature] = slotTexture,
            [CardTemple.Tech] = slotTexture,
            [CardTemple.Undead] = slotTexture,
            [CardTemple.Wizard] = slotTexture
        };
        return New(modGuid, modificationName, behaviour, templeMap, pixelBoardSlotSprites);
    }

    /// <summary>
    /// Creates a new card slot modification.
    /// </summary>
    /// <param name="modGuid">Unique ID for the mod creating the slot modification</param>
    /// <param name="modificationName">Reference name for the slot modification</param>
    /// <param name="behaviour">The class that controls the behavior for the new slot</param>
    /// <param name="slotTexture">The 3D scene slot texture (154x226).</param>
    /// <param name="pixelSlotTexture">The 2D scene slot texture. If it is the size of a single slot (44x58) it will be color converted to match each theme. If it is the size of a slot sprite sheet (220x116) it will be sliced into individual sprites.</param>
    /// <returns>Unique identifier for the modification type; used to set the slot modification in the future</returns>
    public static ModificationType New(string modGuid, string modificationName, Type behaviour, Texture2D slotTexture, Texture2D pixelSlotTexture)
    {
        Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> spriteSet = null;
        if (pixelSlotTexture != null)
        {
            if (pixelSlotTexture.width == 44 && pixelSlotTexture.height == 58)
                spriteSet = BuildAct2SpriteSetFromTexture(pixelSlotTexture);
            else if (pixelSlotTexture.width == 220 && (pixelSlotTexture.height == 116 || pixelSlotTexture.height == 232))
                spriteSet = BuildAct2SpriteSetFromSpriteSheetTexture(pixelSlotTexture);
            else
                throw new InvalidOperationException($"Cannot create slot mod {modGuid}/{modificationName}. The pixel slot texture must either be a single slot (44x58) or a 5x2 sprite sheet (220x116) or a 5x4 sprite sheet (220x232)");
        }
        return New(modGuid, modificationName, behaviour, slotTexture, spriteSet);
    }

    /// <summary>
    /// Creates a new card slot modification
    /// </summary>
    /// <param name="modGuid">Unique ID for the mod creating the slot modification</param>
    /// <param name="modificationName">Reference name for the slot modification</param>
    /// <param name="behaviour">The class that controls the behavior for the new slot</param>
    /// <param name="slotTexture">The 3D scene slot texture (154x226)</param>
    /// <returns>Unique identifier for the modification type; used to set the slot modification in the future</returns>
    public static ModificationType New(string modGuid, string modificationName, Type behaviour, Texture2D slotTexture) => New(modGuid, modificationName, behaviour, slotTexture, (Texture2D)null);

    /// <summary>
    /// Creates a new card slot modification
    /// </summary>
    /// <param name="modGuid">Unique ID for the mod creating the slot modification</param>
    /// <param name="modificationName">Reference name for the slot modification</param>
    /// <param name="behaviour">The class that controls the behavior for the new slot</param>
    /// <returns>Unique identifier for the modification type; used to set the slot modification in the future</returns>
    public static ModificationType New(string modGuid, string modificationName, Type behaviour) => New(modGuid, modificationName, behaviour, null, (Texture2D)null);

    internal static readonly Dictionary<CardTemple, Texture> DefaultSlotTextures = new()
    {
        { CardTemple.Nature, ResourceBank.Get<Texture>("Art/Cards/card_slot") },
        { CardTemple.Tech, ResourceBank.Get<Texture>("Art/Cards/card_slot_tech") },
        { CardTemple.Wizard, ResourceBank.Get<Texture>("Art/Cards/card_slot_undead") },
        { CardTemple.Undead, ResourceBank.Get<Texture>("Art/Cards/card_slot_wizard") }
    };

    internal static readonly Dictionary<CardTemple, List<Texture>> PlayerOverrideSlots = new();
    internal static readonly Dictionary<CardTemple, List<Texture>> OpponentOverrideSlots = new();

    private static void ConditionallyResetAllSlotTextures()
    {
        if (BoardManager.m_Instance != null)
        {
            foreach (var slot in BoardManager.Instance.AllSlotsCopy)
            {
                if (slot.GetSlotModification() == ModificationType.NoModification)
                    slot.ResetSlotTexture();
            }
        }
    }

    private static Texture2D ConvertAct2TextureColor(Texture2D tex, Color targetColor)
    {
        Texture2D newTex = TextureHelper.DuplicateTexture(tex);
        newTex.filterMode = FilterMode.Point;
        for (int x = 0; x < newTex.width; x++)
        {
            for (int y = 0; y < newTex.height; y++)
            {
                var pix = tex.GetPixel(x, y);
                if (pix.a < 1)
                    newTex.SetPixel(x, y, TRANSPARENT);
                else if (pix == Color.black)
                    newTex.SetPixel(x, y, Color.black);
                else
                    newTex.SetPixel(x, y, targetColor);
            }
        }
        newTex.Apply();
        return newTex;
    }

    private static PixelBoardSpriteSetter.BoardThemeSpriteSet GetSpriteSetFromTexture(Texture2D tex, PixelBoardSpriteSetter.BoardTheme theme)
    {
        int offset = theme == PixelBoardSpriteSetter.BoardTheme.Finale ? 4 : (int)theme;
        bool hasOpponentSlots = tex.height == 232;

        Sprite playerSprite = Sprite.Create(tex, new Rect(0f + (float)offset * 44f, tex.height - 58f, 44f, 58f), new Vector2(0.5f, 0.5f));
        Sprite playerHoverSprite = Sprite.Create(tex, new Rect(0f + (float)offset * 44f, tex.height - 116f, 44f, 58f), new Vector2(0.5f, 0.5f));

        Sprite opponentSprite = !hasOpponentSlots ? playerSprite : Sprite.Create(tex, new Rect(0f + (float)offset * 44f, tex.height - 174f, 44f, 58f), new Vector2(0.5f, 0.5f));
        Sprite opponentHoverSprite = !hasOpponentSlots ? playerHoverSprite : Sprite.Create(tex, new Rect(0f + (float)offset * 44f, 0f, 44f, 58f), new Vector2(0.5f, 0.5f));

        List<PixelBoardSpriteSetter.BoardThemeSpriteSet.SpecificSlotSprites> opponentSprites = new();
        if (hasOpponentSlots)
        {
            for (int i = 0; i < 4; i++)
            {
                opponentSprites.Add(new()
                {
                    playerSlot = false,
                    index = i,
                    slotDefault = opponentSprite,
                    slotHighlight = opponentHoverSprite
                });
            }
        }

        return new()
        {
            id = theme,
            slotDefault = playerSprite,
            slotHighlight = playerHoverSprite,
            specificSlotSprites = opponentSprites,
            flipPlayerSlotSpriteX = false,
            flipPlayerSlotSpriteY = false
        };
    }

    /// <summary>
    /// Converts a sheet of act 2 slot textures into a full collection of sprite sets. READ THE DOCUMENTATION.
    /// </summary>
    /// <param name="texture"></param>
    /// <remarks>This expects a texture with 8 slot textures in it, arranged in either 2 or 4 rows of 5. Each slot texture is 44x58.
    /// The first/bottom-most row contains all of the the normal slot sprites and the second row contains all of the hover sprites.
    /// If you want the opponent sprites to be different, you have to provide 4 rows; the third row contains normal opponent slot sprites and the fourth row contains hovered opponent slot sprites.
    /// Each row must go in this order from left to right: Nature, Undead, Tech, Wizard, Finale.
    public static Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> BuildAct2SpriteSetFromSpriteSheetTexture(Texture2D texture)
    {
        Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> retval = new();

        List<PixelBoardSpriteSetter.BoardTheme> themes = Enum.GetValues(typeof(PixelBoardSpriteSetter.BoardTheme)).Cast<PixelBoardSpriteSetter.BoardTheme>().ToList();
        themes.Remove(PixelBoardSpriteSetter.BoardTheme.P03);
        themes.Remove(PixelBoardSpriteSetter.BoardTheme.NUM_THEMES);

        foreach (var theme in themes)
            retval[theme] = GetSpriteSetFromTexture(texture, theme);

        return retval;
    }

    /// <summary>
    /// Converts a single act 2 slot texture into a full collection of sprite sets by repeatedly recoloring the texture
    /// </summary>
    /// <param name="texture"></param>
    public static Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> BuildAct2SpriteSetFromTexture(Texture2D texture)
    {
        Dictionary<PixelBoardSpriteSetter.BoardTheme, PixelBoardSpriteSetter.BoardThemeSpriteSet> retval = new();
        foreach (PixelBoardSpriteSetter.BoardTheme theme in Enum.GetValues(typeof(PixelBoardSpriteSetter.BoardTheme)))
        {
            if (theme == PixelBoardSpriteSetter.BoardTheme.NUM_THEMES)
                continue;

            Texture2D slotTexture = ConvertAct2TextureColor(texture, DEFAULT_COLORS[theme].Item1);
            Texture2D highlightedTexture = ConvertAct2TextureColor(texture, DEFAULT_COLORS[theme].Item2);

            PixelBoardSpriteSetter.BoardThemeSpriteSet spriteSet = new();
            spriteSet.id = theme;
            spriteSet.slotDefault = Sprite.Create(slotTexture, new Rect(0f, 0f, 44f, 58f), new Vector2(0.5f, 0.5f));
            spriteSet.slotHighlight = Sprite.Create(highlightedTexture, new Rect(0f, 0f, 44f, 58f), new Vector2(0.5f, 0.5f));
            spriteSet.specificSlotSprites = new();

            retval[theme] = spriteSet;
        }
        return retval;
    }

    /// <summary>
    /// Allows you to change the default slot texture for a given 3D scene
    /// </summary>
    /// <param name="temple">Indicator for which scene/scrybe this should apply to</param>
    /// <param name="playerSlot">Texture for each player slot. Null textures will be replaced with the game's default.</param>
    /// <param name="opponentSlot">Texture for each opponent slot. Null textures will be replaced with the game's default.</param>
    /// <remarks>This does not work for Act 2. The base game uses the <c>CardBattleNPC</c> to
    /// set default textures. It's possible that a future API update would allow for custom Act 2 
    /// NPCs, so this will deliberately not touch Act 2</remarks>
    public static void OverrideDefaultSlotTexture(CardTemple temple, Texture playerSlot, Texture opponentSlot)
    {
        OverrideDefaultSlotTexture(
            temple,
            playerSlot == null ? null : new List<Texture>() { playerSlot, playerSlot, playerSlot, playerSlot },
            opponentSlot == null ? null : new List<Texture>() { opponentSlot, opponentSlot, opponentSlot, opponentSlot }
        );
    }

    /// <summary>
    /// Allows you to change the default slot texture for a given 3D scene
    /// </summary>
    /// <param name="temple">Indicator for which scene/scrybe this should apply to</param>
    /// <param name="playerSlots">Textures for each player slot. Null textures will be replaced with the game's default.</param>
    /// <param name="opponentSlots">Textures for each opponent slot. Null textures will be replaced with the game's default.</param>
    /// <remarks>This does not work for Act 2. The base game uses the <c>CardBattleNPC</c> to
    /// set default textures. It's possible that a future API update would allow for custom Act 2 
    /// NPCs, so this will deliberately not touch Act 2</remarks>
    public static void OverrideDefaultSlotTexture(CardTemple temple, List<Texture> playerSlots, List<Texture> opponentSlots)
    {
        if (playerSlots != null && playerSlots.Any(t => t != null))
            PlayerOverrideSlots[temple] = new(playerSlots);

        if (opponentSlots != null && opponentSlots.Any(t => t != null))
            OpponentOverrideSlots[temple] = new(opponentSlots);

        ConditionallyResetAllSlotTextures();
    }

    /// <summary>
    /// Resets the default slot texture back to the base game's default texture.
    /// </summary>
    /// <param name="temple">The scene/scrybe to reset</param>
    public static void ResetDefaultSlotTexture(CardTemple temple)
    {
        if (PlayerOverrideSlots.ContainsKey(temple))
            PlayerOverrideSlots.Remove(temple);
        if (OpponentOverrideSlots.ContainsKey(temple))
            OpponentOverrideSlots.Remove(temple);
        ConditionallyResetAllSlotTextures();
    }

    /// <summary>
    /// Used to determine what Acts a modification's rulebook entry should appear in.
    /// Values correspond to specific AbilityMetaCategory values.
    /// </summary>
    public enum ModificationMetaCategory
    {
        Part1Rulebook = 0,
        Part3Rulebook = 2,
        GrimoraRulebook = 5,
        MagnificusRulebook = 6
    }

    private static SlotModificationManager m_instance;

    /// <summary>
    /// Singleton instance for the SlotModificationManager.
    /// </summary>
    public static SlotModificationManager Instance
    {
        get
        {
            if (m_instance != null)
                return m_instance;

            Instantiate();
            return m_instance;
        }
        set => m_instance = value;
    }

    private static void Instantiate()
    {
        if (m_instance != null)
            return;

        GameObject slotModManager = new("SlotModificationManager");
        slotModManager.transform.SetParent(BoardManager.Instance.gameObject.transform);
        m_instance = slotModManager.AddComponent<SlotModificationManager>();
    }
    #region Patches
    [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.CleanupPhase))]
    [HarmonyPostfix]
    private static IEnumerator CleanupSlots(IEnumerator sequence)
    {
        foreach (CardSlot slot in BoardManager.Instance.AllSlots)
        {
            if (slot.GetSlotModification() != ModificationType.NoModification)
                yield return slot.SetSlotModification(ModificationType.NoModification);

            SlotModificationInteractable interactable = slot.GetComponent<SlotModificationInteractable>();
            if (!interactable.SafeIsUnityNull())
                UnityObject.Destroy(interactable);
        }

        // At this point, all of the receivers should be clear, but regardless, we double check
        if (GlobalTriggerHandler.Instance.StackSize > 0)
            yield return new WaitUntil(() => GlobalTriggerHandler.Instance.StackSize == 0);
        foreach (var kvp in Instance.SlotReceivers)
            GameObject.Destroy(kvp.Value.Item2);

        Instance.SlotReceivers.Clear();

        yield return sequence;
    }

    [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.CleanUp))]
    [HarmonyPostfix]
    private static IEnumerator CleanUpModifiedSlots(IEnumerator sequence)
    {
        foreach (Info defn in AllModificationInfos.Where(m => m.SlotBehaviour != null))
        {
            Component comp = BoardManager.Instance.gameObject.GetComponent(defn.SlotBehaviour);
            if (!comp.SafeIsUnityNull())
                UnityObject.Destroy(comp);
        }

        yield return sequence;
    }

    [HarmonyPatch(typeof(RuleBookInfo), "ConstructPageData", new Type[] { typeof(AbilityMetaCategory) })]
    [HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)] // make sure custom item pages have been added first
    private static void AddSlotModificationsToRuleBook(AbilityMetaCategory metaCategory, RuleBookInfo __instance, ref List<RuleBookPageInfo> __result)
    {
        if (AllModificationInfos.Count == 0)
            return;

        List<Info> infos = AllModificationInfos.Where(x => RuleBookManager.SlotModShouldBeAdded(x, (ModificationMetaCategory)metaCategory)).ToList();
        infos.RemoveAll(x => x.SharedRulebook != ModificationType.NoModification);
        if (infos.Count == 0)
            return;

        int curPageNum = 1;
        PageRangeInfo itemPageRange = __instance.pageRanges.Find(x => x.type == PageRangeType.Items);
        PageRangeInfo abilityRange = __instance.pageRanges.Find(x => x.type == PageRangeType.Abilities);
        GameObject pagePrefab = (itemPageRange ?? abilityRange).rangePrefab;
        int insertPosition = __result.FindLastIndex(rbi => pagePrefab) + 1;
        foreach (Info slot in infos)
        {
            RuleBookPageInfo info = new();
            info.pagePrefab = (SaveManager.SaveFile.IsPart1 ? itemPageRange : abilityRange).rangePrefab;
            info.headerText = string.Format(Localization.Translate("APPENDIX XII, SUBSECTION I - SLOT EFFECTS {0}"), curPageNum);
            info.pageId = SLOT_PAGEID + (int)slot.ModificationType;
            __result.Insert(insertPosition, info);
            curPageNum++;
            insertPosition++;
        }
    }

    public const string SLOT_PAGEID = "SlotModification_";
    private static Vector3 PART_3_SCALE = new(0.7f, 0.7f, 1f);

    [HarmonyPrefix, HarmonyPatch(typeof(ItemPage), nameof(ItemPage.FillPage))]
    private static bool OverrideWithSlotInfo(ItemPage __instance, string headerText, params object[] otherArgs)
    {
        if (!SaveManager.SaveFile.IsPart1)
            return true;

        // Slot modification pages use ItemPage format in Act 1
        __instance.iconRenderer.transform.localScale = SaveManager.SaveFile.IsPart3 ? PART_3_SCALE : Vector3.one;
        if (otherArgs[0] is string pageId && pageId.StartsWith(SLOT_PAGEID))
        {
            string modString = pageId.Replace(SLOT_PAGEID, "");
            if (int.TryParse(modString, out int modType))
            {
                if (__instance.headerTextMesh != null)
                {
                    __instance.headerTextMesh.text = headerText;
                }
                Info info = AllModificationInfos.InfoByID((ModificationType)modType);
                __instance.nameTextMesh.text = Localization.Translate(info.RulebookName);
                __instance.descriptionTextMesh.text = Localization.Translate(info.RulebookDescription);
                __instance.iconRenderer.sprite = info.RulebookSprite;
                __instance.iconRenderer.transform.localScale = new(0.8f, 0.8f, 1f);
                InscryptionAPIPlugin.Logger.LogDebug($"Create rulebook page for slot modification [{info.ModificationType}] ({info.RulebookName}).");
                return false;
            }
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AbilityPage), nameof(AbilityPage.FillPage))]
    private static bool OverrideWithSlotInfo(AbilityPage __instance, string headerText, params object[] otherArgs)
    {
        if (SaveManager.SaveFile.IsPart1)
            return true;

        __instance.mainAbilityGroup.iconRenderer.transform.localScale = SaveManager.SaveFile.IsPart3 ? PART_3_SCALE : Vector3.one;
        Transform slotRendererObj = __instance.mainAbilityGroup.transform.Find("SlotRenderer");
        slotRendererObj?.gameObject.SetActive(false);
        __instance.mainAbilityGroup.iconRenderer.transform.parent.gameObject.SetActive(true);

        // if this page should be filled with info from a slot modification
        if (otherArgs.Length > 0 && otherArgs.Last() is string pageId && pageId.StartsWith(SLOT_PAGEID))
        {
            string modString = pageId.Replace(SLOT_PAGEID, "");
            if (int.TryParse(modString, out int modType))
            {
                if (__instance.headerTextMesh != null)
                {
                    __instance.headerTextMesh.text = headerText;
                }
                Info info = AllModificationInfos.InfoByID((ModificationType)modType);
                __instance.mainAbilityGroup.nameTextMesh.text = Localization.Translate(info.RulebookName);
                __instance.mainAbilityGroup.descriptionTextMesh.text = Localization.Translate(info.RulebookDescription);
                __instance.mainAbilityGroup.iconRenderer.transform.parent.gameObject.SetActive(false);
                if (SaveManager.SaveFile.IsPart3)
                {
                    if (slotRendererObj == null)
                    {
                        slotRendererObj = Instantiate(__instance.mainAbilityGroup.iconRenderer.transform.parent, __instance.mainAbilityGroup.transform);
                        slotRendererObj.name = "SlotRenderer";
                        slotRendererObj.localPosition += new Vector3(0.1f, -0.1f, 0f);
                        slotRendererObj.localScale = new(0.6f, 0.6f, 0.6f);
                        Destroy(slotRendererObj.Find("Icon").gameObject);
                    }
                    slotRendererObj.gameObject.SetActive(true);
                    __instance.mainAbilityGroup.iconRenderer.transform.parent.gameObject.SetActive(false);
                    slotRendererObj.GetComponent<SpriteRenderer>().sprite = info.P03RulebookSprite ?? info.RulebookSprite;
                }
                else if (SaveManager.SaveFile.IsMagnificus)
                {
                    if (slotRendererObj == null)
                    {
                        slotRendererObj = Instantiate(__instance.mainAbilityGroup.iconRenderer.transform.parent, __instance.mainAbilityGroup.transform);
                        slotRendererObj.name = "SlotRenderer";
                        slotRendererObj.localPosition += new Vector3(0.1f, -0.1f, 0f);
                    }
                    slotRendererObj.gameObject.SetActive(true);
                    __instance.mainAbilityGroup.iconRenderer.transform.parent.gameObject.SetActive(false);
                    slotRendererObj.GetChild(0).GetComponent<MeshRenderer>().material.mainTexture = (info.MagnificusRulebookSprite ?? info.RulebookSprite).texture;
                }
                else
                {
                    InscryptionAPIPlugin.Logger.LogDebug("Slot modification page: WIP");
                }

                __instance.mainAbilityGroup.iconRenderer.transform.localScale = new(0.8f, 0.8f, 1f);
                //InscryptionAPIPlugin.Logger.LogDebug($"Create rulebook page for slot modification [{info.ModificationType}] ({info.RulebookName}).");
                return false;
            }
        }
        return true;
    }
    #endregion
}