using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionAPI.CardCosts;
using InscryptionAPI.Helpers;
using Sirenix.Serialization.Utilities;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;

[HarmonyPatch]
public static class Part3CardCostRender
{
    // This patches the way card costs are rendered in Act 1 (Leshy's cabin)
    // It allows mixed card costs to display correctly (i.e., 2 blood, 1 bone)
    // And allows gem cost and energy cost to render on the card at all.

    /// <summary>
    /// Contains rendering information for a specific custom cost
    /// </summary>
    public class CustomCostRenderInfo
    {
        /// <summary>
        /// Unique ID for this cost
        /// </summary>
        public string CostId { get; private set; }

        /// <summary>
        /// The display texture for the custom cost
        /// </summary>
        public Texture2D MainCostTexture { get; set; }

        /// <summary>
        /// The emission texture for the custom cost
        /// </summary>
        public Texture2D EmissionTexture { get; set; }

        /// <summary>
        /// The game object that holds the custom cost
        /// </summary>
        public GameObject CostContainer { get; internal set; }

        internal float StretchFactor { get; set; } = 1f;

        internal void UpdateDisplayedTextures()
        {
            if (CostContainer != null)
                CostContainer.DisplayCostOnContainer(MainCostTexture, EmissionTexture);
        }

        /// <summary>
        /// Creates a blank custom cost render information object
        /// </summary>
        /// <param name="costId">A unique ID for this cost. Use this to identify the render info at a later step</param>
        /// <remarks>You should only use this constructor if you plan to build a custom gameobject for your cost.</remarks>
        public CustomCostRenderInfo(string costId)
        {
            CostId = costId;
        }

        /// <summary>
        /// Creates a custom cost render information object with a texture displayer
        /// </summary>
        /// <param name="costId">A unique ID for this cost. Use this to identify the render info at a later step</param>
        /// <param name="mainCostTexture">The standard (albedo) texture for your custom cost</param>
        /// <param name="emissionTexture">The emissive texture for your custom cost</param>
        /// <remarks>Use this constructor if you have built your own set of textures to display custom costs</remarks>
        public CustomCostRenderInfo(string costId, Texture2D mainCostTexture, Texture2D emissionTexture) : this(costId)
        {
            MainCostTexture = mainCostTexture;
            EmissionTexture = emissionTexture;
        }

        /// <summary>
        /// Creates a custom cost render information object with a texture displayer
        /// </summary>
        /// <param name="costId">A unique ID for this cost. Use this to identify the render info at a later step</param>
        /// <param name="textures">A pair of standard and emissive textures. This is the return type of the GetIconifiedCostTexture helper method.</param>
        /// <remarks>Use this constructor if you are using the GetIconifiedCostTexture helper to generate a cost texture for you</remarks>
        public CustomCostRenderInfo(string costId, Tuple<Texture2D, Texture2D> textures) : this(costId, textures.Item1, textures.Item2)
        {
        }

        internal void Stretch(float factor)
        {
            // Reparent everything to my parent, world position stays
            List<Transform> allChildren = new();
            while (CostContainer.transform.childCount > 0)
            {
                Transform t = CostContainer.transform.GetChild(0);
                t.SetParent(CostContainer.transform.parent, true);
                allChildren.Add(t);
            }

            // Stretch me
            CostContainer.transform.localScale = new(0.475f * factor, 0.1f, 0.145f);
            CostContainer.transform.localPosition = new(.225f + (factor - 1f) * 3f * .08f,
                .01f,
                CostContainer.transform.localPosition.z
            );

            // Reparent everything back, world position stays
            foreach (Transform t in allChildren)
                t.SetParent(CostContainer.transform, true);

            this.StretchFactor = factor;
        }
    }

    private static List<GemType> BlueGemsCost(this CardInfo card) => card.GemsCost.Where(g => g == GemType.Blue).ToList();
    private static List<GemType> GreenGemsCost(this CardInfo card) => card.GemsCost.Where(g => g == GemType.Green).ToList();
    private static List<GemType> OrangeGemsCost(this CardInfo card) => card.GemsCost.Where(g => g == GemType.Orange).ToList();

    private static List<GemType> BlueGemsCost(this PlayableCard card) => card.GemsCost().Where(g => g == GemType.Blue).ToList();
    private static List<GemType> GreenGemsCost(this PlayableCard card) => card.GemsCost().Where(g => g == GemType.Green).ToList();
    private static List<GemType> OrangeGemsCost(this PlayableCard card) => card.GemsCost().Where(g => g == GemType.Orange).ToList();

    /// <summary>
    /// Hook into this event to be able to add a new custom cost to a card. In your delegate, create a new CustomCostRenderInfo object with the appropriate textures (or no textures if you will use the UpdateCardCostComplex event as well).
    /// </summary>
    public static event Action<CardInfo, List<CustomCostRenderInfo>> UpdateCardCostSimple;

    /// <summary>
    /// Hook into this event to directly modify the game object that is created to hold your custom cost. Do this ONLY if you want to do something more complex than displaying a label/texture.
    /// </summary>
    public static event Action<CardInfo, List<CustomCostRenderInfo>> UpdateCardCostComplex;

    private const float CONTAINER_Z_SPACING = 0.145f;
    private const float INITIAL_CONTAINER_Z_SPACING = 0.14f;
    private const int ICON_SPACING = 18;

    public const string ENERGY_LIGHTS = "Anim/CardBase/Top/EnergyCostLights";
    public const string UPPER_GEM_CONTAINER = "Anim/CardBase/Top/Gems";
    public const string BLUE_GEM_ORIGINAL = "Anim/CardBase/Top/Gems/Gem_Blue";
    public const string ORANGE_GEM_ORIGINAL = "Anim/CardBase/Bottom/Gems/Gem_Orange";
    public const string GREEN_GEM_ORIGINAL = "Anim/CardBase/Bottom/Gems/Gem_Green";
    public const string BLUE_GEM_COST = "Gem_Cost_Blue";
    public const string ORANGE_GEM_COST = "Gem_Cost_Orange";
    public const string GREEN_GEM_COST = "Gem_Cost_Green";
    public const string INITIAL_ALTERNATE_COST = "Anim/CardBase/Top/Gems/TextureDisplayer";

    private static readonly Dictionary<GemType, string> COST_LOOKUP = new()
    {
        { GemType.Blue, BLUE_GEM_ORIGINAL },
        { GemType.Orange, ORANGE_GEM_ORIGINAL},
        { GemType.Green, GREEN_GEM_ORIGINAL }
    };

    private static Color BoneColor => Color.white;
    private static Color BloodColor => GameColors.Instance.glowRed;

    /// <summary>
    /// Gets a specific child GameObject instance of a particular card
    /// </summary>
    /// <param name="key">The Unity path to the object you want.</param>
    /// <remarks>The Part3CardCostRender class contains a number of constants that point to important parts of the card.</remarks>
    public static GameObject GetPiece(this DiskCardGame.Card card, string key)
    {
        Transform t = card.gameObject.transform.Find(key);
        return t?.gameObject;
    }

    private static ConditionalWeakTable<RenderStatsLayer, DiskCardGame.Card> CardRenderReverseLookup = new();

    /// <summary>
    /// Gets a reverse reference from an instance of RenderStatsLayer back to the Card it belongs to.
    /// </summary>
    public static DiskCardGame.Card GetCard(this RenderStatsLayer layer)
    {
        if (CardRenderReverseLookup.TryGetValue(layer, out DiskCardGame.Card retval))
            return retval;

        retval = layer.gameObject.GetComponentInParent<DiskCardGame.Card>();
        CardRenderReverseLookup.Add(layer, retval);
        return retval;
    }

    private static Texture2D _bgTexture = null;
    private static Texture2D BackgroundTexture
    {
        get
        {
            if (_bgTexture == null)
                _bgTexture = TextureHelper.GetImageAsTexture($"CostTextureBackground.png", typeof(Part3CardCostRender).Assembly);

            return _bgTexture;
        }
    }

    private static List<Texture2D> _sevenSegments = null;
    private static List<Texture2D> SevenSegmentDisplay
    {
        get
        {
            if (_sevenSegments == null)
            {
                _sevenSegments = new();
                for (int i = 0; i <= 9; i++)
                {
                    Texture2D tex = TextureHelper.GetImageAsTexture($"Display_{i}_small.png", typeof(Part3CardCostRender).Assembly);
                    tex.name = $"Display_{i}";
                    _sevenSegments.Add(tex);
                }

                Texture2D texx = TextureHelper.GetImageAsTexture($"Display_x_small.png", typeof(Part3CardCostRender).Assembly);
                texx.name = $"Display_x";
                _sevenSegments.Add(texx);
            }
            return _sevenSegments;
        }
    }

    private static Sprite _faceBoneSprite = null;
    private static Sprite FaceBoneSprite
    {
        get
        {
            if (_faceBoneSprite == null)
            {
                var text = TextureHelper.GetImageAsTexture("p03_face_bones_resource.png", typeof(Part3CardCostRender).Assembly);
                _faceBoneSprite = Sprite.Create(text, new Rect(0f, 0f, text.width, text.height), new Vector2(0.5f, 0.5f));
            }
            return _faceBoneSprite;
        }
    }

    private static Sprite _faceBloodSprite = null;
    private static Sprite FaceBloodSprite
    {
        get
        {
            if (_faceBloodSprite == null)
            {
                var text = TextureHelper.GetImageAsTexture("p03_face_blood_resource.png", typeof(Part3CardCostRender).Assembly);
                _faceBloodSprite = Sprite.Create(text, new Rect(0f, 0f, text.width, text.height), new Vector2(0.5f, 0.5f));
            }
            return _faceBloodSprite;
        }
    }

    private static Texture2D _costCubeBackground = null;
    private static Texture2D CostCubeBackground
    {
        get
        {
            if (_costCubeBackground == null)
            {
                _costCubeBackground = TextureHelper.GetImageAsTexture("Act3CostCubeBackground.png", typeof(Part3CardCostRender).Assembly);
                _costCubeBackground.name = "CostCubeBackground";
            }
            return _costCubeBackground;
        }
    }

    private static Texture2D _boneIcon = null;
    internal static Texture2D BoneCostIcon
    {
        get
        {
            if (_boneIcon == null)
            {
                _boneIcon = TextureHelper.GetImageAsTexture("BoneCostIcon_small.png", typeof(Part3CardCostRender).Assembly);
                _boneIcon.name = "BoneCostIcon";
            }
            return _boneIcon;
        }
    }

    private static Texture2D _bloodIconUnlit = null;
    internal static Texture2D BloodCostIconUnlit
    {
        get
        {
            if (_bloodIconUnlit == null)
            {
                _bloodIconUnlit = TextureHelper.GetImageAsTexture("BloodCostIcon_small_unlit.png", typeof(Part3CardCostRender).Assembly);
                _bloodIconUnlit.name = "BloodCostIconUnlit";
            }
            return _bloodIconUnlit;
        }
    }

    private static Texture2D _bloodIcon = null;
    internal static Texture2D BloodCostIcon
    {
        get
        {
            if (_bloodIcon == null)
            {
                _bloodIcon = TextureHelper.GetImageAsTexture("BloodCostIcon_small.png", typeof(Part3CardCostRender).Assembly);
                _bloodIcon.name = "BloodCostIcon";
            }
            return _bloodIcon;
        }
    }
    private static readonly Dictionary<string, Texture2D> AssembledTextures = new();

    private static GameObject CreateTextureDisplayer(GameObject container)
    {
        // Now a separate object for the texture displayer
        GameObject textureContainer = GameObject.CreatePrimitive(PrimitiveType.Quad);
        textureContainer.name = "TextureDisplayer";
        textureContainer.transform.SetParent(container.transform);

        GameObject.Destroy(textureContainer.GetComponent<MeshCollider>());

        Renderer textureRenderer = textureContainer.GetComponent<Renderer>();
        textureRenderer.material.EnableKeyword("_EMISSION");
        textureRenderer.material.SetTexture("_MainTex", BackgroundTexture);
        textureRenderer.material.SetTexture("_EmissionMap", BackgroundTexture);
        textureRenderer.material.SetColor("_EmissionColor", Color.white);

        textureContainer.transform.localPosition = new(0f, .501f, 0f);
        textureContainer.transform.localScale = new(0.95f, 0.95f, 0.95f);
        textureContainer.transform.localEulerAngles = new(90f, 180f, 0f);

        return textureContainer;
    }

    private static GameObject GenerateSingleAdditionalCostContainer(DiskCardGame.Card card, int index)
    {
        if (card.StatsLayer is not DiskRenderStatsLayer)
            throw new InvalidOperationException("You cannot generate an additional Part 3 cost container on anything other than a Disk Card!!");

        // Start with a cube
        GameObject container = GameObject.CreatePrimitive(PrimitiveType.Cube);
        container.name = $"Card_Cost_{index}";
        container.transform.SetParent(card.GetPiece(UPPER_GEM_CONTAINER).transform);
        container.transform.localPosition = new(.24f, .01f, 0f);
        container.transform.localScale = new(0.475f, 0.1f, 0.145f);
        container.transform.localEulerAngles = new(0f, 0f, 0f);

        GameObject.Destroy(container.GetComponent<BoxCollider>());

        // This bit allows the cube primitive to be textured differently on each side
        // https://discussions.unity.com/t/change-texture-of-cube-sides/82546/3
        Mesh mesh = container.GetComponent<MeshFilter>().mesh;
        Vector2[] UVs = new Vector2[mesh.vertices.Length];

        // Front
        UVs[0] = new Vector2(0.0f, 0.0f);
        UVs[1] = new Vector2(0.333f, 0.0f);
        UVs[2] = new Vector2(0.0f, 0.333f);
        UVs[3] = new Vector2(0.333f, 0.333f);

        // Top
        UVs[4] = new Vector2(0.334f, 0.333f);
        UVs[5] = new Vector2(0.666f, 0.333f);
        UVs[8] = new Vector2(0.334f, 0.0f);
        UVs[9] = new Vector2(0.666f, 0.0f);

        // Back
        UVs[6] = new Vector2(1.0f, 0.0f);
        UVs[7] = new Vector2(0.667f, 0.0f);
        UVs[10] = new Vector2(1.0f, 0.333f);
        UVs[11] = new Vector2(0.667f, 0.333f);

        // Bottom
        UVs[12] = new Vector2(0.0f, 0.334f);
        UVs[13] = new Vector2(0.0f, 0.666f);
        UVs[14] = new Vector2(0.333f, 0.666f);
        UVs[15] = new Vector2(0.333f, 0.334f);

        // Left
        UVs[16] = new Vector2(0.334f, 0.334f);
        UVs[17] = new Vector2(0.334f, 0.666f);
        UVs[18] = new Vector2(0.666f, 0.666f);
        UVs[19] = new Vector2(0.666f, 0.334f);

        // Right        
        UVs[20] = new Vector2(0.667f, 0.334f);
        UVs[21] = new Vector2(0.667f, 0.666f);
        UVs[22] = new Vector2(1.0f, 0.666f);
        UVs[23] = new Vector2(1.0f, 0.334f);

        mesh.uv = UVs;

        // Now we go ahead and set the texture
        Renderer renderer = container.GetComponent<Renderer>();
        renderer.material.SetTexture("_MainTex", CostCubeBackground);

        // And after doing all that work, make it invisible if it's the first one ;)
        if (index == 0)
            renderer.enabled = false;

        // Now a separate object for the texture displayer
        CreateTextureDisplayer(container);

        return container;
    }

    private static readonly ConditionalWeakTable<RenderStatsLayer, List<GameObject>> ExtraCostContainers = new();

    /// <summary>
    /// Gets the cached set of additional cost containers that have been built to accomodate costs beyond 1 type
    /// </summary>
    public static List<GameObject> GetCostContainers(this RenderStatsLayer layer) => ExtraCostContainers.GetOrCreateValue(layer);

    /// <summary>
    /// Gets the set of additional cost containers to hold costs beyond 1. If additional containers need to be built, this will buidl them.
    /// </summary>
    public static List<GameObject> GetCostContainers(this DiskCardGame.Card card, int numberOfContainers)
    {
        List<GameObject> retval = card.StatsLayer.GetCostContainers();
        if (card.StatsLayer is not DiskRenderStatsLayer)
            return retval;

        // Cool - we need to make more cost containers
        while (retval.Count < numberOfContainers)
            retval.Add(GenerateSingleAdditionalCostContainer(card, retval.Count));

        // Delete every child object that's not the texture displayer
        List<Transform> children = new();
        foreach (var container in retval)
        {
            children.Clear();
            for (int i = 0; i < container.transform.childCount; i++)
            {
                Transform child = container.transform.GetChild(i);
                if (!child.gameObject.name.Equals("TextureDisplayer"))
                    children.Add(child);
            }
            for (int i = 0; i < children.Count; i++)
                GameObject.Destroy(children[i].gameObject);
        }

        return retval;
    }

    private static GameObject MakeCostContainer(DiskCardGame.Card card, string path, Color lightColor)
    {
        GameObject container = GameObject.CreatePrimitive(PrimitiveType.Quad);
        container.name = path.Split('/').Last();
        container.transform.SetParent(card.GetPiece(UPPER_GEM_CONTAINER).transform);

        GameObject.Destroy(container.GetComponent<MeshCollider>());

        Renderer renderer = container.GetComponent<Renderer>();
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetTexture("_MainTex", BackgroundTexture);
        renderer.material.SetTexture("_EmissionMap", BackgroundTexture);

        container.transform.localPosition = new(.25f, .0565f, 0f);
        container.transform.localScale = new(0.43f, 0.12f, 0.08f);
        container.transform.localEulerAngles = new(90f, 245f, 65f);

        return container;
    }

    private static readonly Dictionary<string, Dictionary<int, Texture2D>> TextureCache = new();
    private static readonly Dictionary<string, Dictionary<int, Texture2D>> EmissionTextureCache = new();

    /// <summary>
    /// Generates a texture that displays the cost of a specific resource, either by duplicating the icon or using a seven-segment display if there isn't enough space
    /// </summary>
    /// <param name="iconTexture">The icon for your resource</param>
    /// <param name="cost">The amount of that resource to display</param>
    /// <param name="forceDigitDisplay">Force the texture to render using the 7-segment display</param>
    /// <returns>A tuple of two textures. The first has the default background, the second does not. The first is best used as a default texture and the second as an emission texture.</returns>
    /// <remarks>If there is enough space in the texture to render multiple instances of the icon, this will do that.
    /// Otherwise, it will render a texture that shows [ICON] x Cost.
    /// In order to take advantage of texture caching, your icon texture must have a unique name.</remarks>
    public static Tuple<Texture2D, Texture2D> GetIconifiedCostTexture(Texture2D iconTexture, int cost, bool forceDigitDisplay = false)
    {
        return GetIconifiedCostTexture(iconTexture, null, cost, forceDigitDisplay);
    }

    /// <summary>
    /// Generates a texture that displays the cost of a specific resource, either by duplicating the icon or using a seven-segment display if there isn't enough space
    /// </summary>
    /// <param name="iconTexture">The icon for your resource</param>
    /// <param name="unlitIconTexture">The "unlit" (off) icon for your resource</param>
    /// <param name="cost">The amount of that resource to display</param>
    /// <param name="forceDigitDisplay">Force the texture to render using the 7-segment display</param>
    /// <returns>A tuple of two textures. The first has the default background, the second does not. The first is best used as a default texture and the second as an emission texture.</returns>
    /// <remarks>If there is enough space in the texture to render multiple instances of the icon, this will do that.
    /// Otherwise, it will render a texture that shows [ICON] x Cost.
    /// In order to take advantage of texture caching, your icon texture must have a unique name.</remarks>
    public static Tuple<Texture2D, Texture2D> GetIconifiedCostTexture(Texture2D iconTexture, Texture2D unlitIconTexture, int cost, bool forceDigitDisplay = false)
    {
        if (!string.IsNullOrEmpty(iconTexture.name) && !iconTexture.name.Equals("untitled", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!TextureCache.Keys.Contains(iconTexture.name))
            {
                TextureCache.Add(iconTexture.name, new());
                EmissionTextureCache.Add(iconTexture.name, new());
            }

            if (TextureCache[iconTexture.name].ContainsKey(cost))
                return new(TextureCache[iconTexture.name][cost], EmissionTextureCache[iconTexture.name][cost]);
        }

        Texture2D newTexture = TextureHelper.DuplicateTexture(BackgroundTexture);
        newTexture.name = $"{iconTexture.name}_{cost}";

        Texture2D newEmission = new(newTexture.width, newTexture.height, newTexture.format, false);
        newEmission.name = $"{iconTexture.name}_{cost}_emission";

        Color[] emptyFill = new Color[newEmission.width * newEmission.height];
        Color empty = new(0f, 0f, 0f, 0f);
        for (int i = 0; i < emptyFill.Length; i++)
            emptyFill[i] = empty;

        newEmission.SetPixels(emptyFill);

        // Figure out the maximum number of icons we can fit
        // There is minimum 13 pixel border on either side and a 20 pixel gap between each icon.
        int maxCostIcons = Mathf.FloorToInt((BackgroundTexture.width - (13 * 2) + ICON_SPACING) / (ICON_SPACING + iconTexture.width));

        int rightPad = 13;

        List<Texture2D> texturesRightToLeft = new();
        if (cost <= maxCostIcons && !forceDigitDisplay)
        {
            for (int i = 0; i < cost; i++)
                texturesRightToLeft.Add(iconTexture);
            while (texturesRightToLeft.Count < maxCostIcons)
                texturesRightToLeft.Add(null); // This is a real hack - see below why i do this

            // Recalculate the right pad to center the things
            int width = (ICON_SPACING + iconTexture.width) * maxCostIcons - ICON_SPACING;
            rightPad = Mathf.FloorToInt((BackgroundTexture.width - width) / 2);
        }
        else
        {
            texturesRightToLeft.Add(SevenSegmentDisplay[cost % 10]);

            int tensDigit = Mathf.FloorToInt(cost / 10f) % 10;
            texturesRightToLeft.Add(SevenSegmentDisplay[tensDigit]);

            texturesRightToLeft.Add(SevenSegmentDisplay[10]);
            texturesRightToLeft.Add(iconTexture);
        }

        int leftWidthPad = 0;
        for (int d = 0; d < texturesRightToLeft.Count; d++)
        {
            // Here, 'null' is being used as an indicator that we need to show an "unlit" version of the icon
            float multiplier = 1.0f;
            if (texturesRightToLeft[d] == null)
            {
                texturesRightToLeft[d] = unlitIconTexture ?? iconTexture;
                multiplier = unlitIconTexture == null ? 0.4f : 1.0f;
            }

            int ty = Mathf.FloorToInt((newTexture.height - texturesRightToLeft[d].height) / 2f);
            leftWidthPad += texturesRightToLeft[d].width;
            int tx = newTexture.width - rightPad - leftWidthPad - d * ICON_SPACING;
            for (int x = 0; x < texturesRightToLeft[d].width; x++)
            {
                for (int y = 0; y < texturesRightToLeft[d].height; y++)
                {
                    Color bc = texturesRightToLeft[d].GetPixel(x, y);
                    if (bc.a > 0f)
                    {
                        newTexture.SetPixel(tx + x, ty + y, bc * multiplier);
                        newEmission.SetPixel(tx + x, ty + y, bc * multiplier);
                    }
                }
            }
        }

        newTexture.Apply();
        newEmission.Apply();

        newTexture.filterMode = FilterMode.Point;
        newEmission.filterMode = FilterMode.Point;


        if (!String.IsNullOrEmpty(iconTexture.name) && !iconTexture.name.Equals("untitled", StringComparison.InvariantCultureIgnoreCase))
        {
            TextureCache[iconTexture.name].Add(cost, newTexture);
            EmissionTextureCache[iconTexture.name].Add(cost, newEmission);
        }

        return new(newTexture, newEmission);
    }

    /// <summary>
    /// Displays the given set of cost textures on the container object
    /// </summary>
    /// <param name="texture">The default texture</param>
    /// <param name="emission">The emission texture</param>
    /// <remarks>Does nothing if the game object is not the appropriate game object (i.e., it wasn't created in this helper)</remarks>
    public static void DisplayCostOnContainer(this GameObject gameObject, Texture2D texture, Texture2D emission)
    {
        Transform displayerTransform = gameObject.transform.Find("TextureDisplayer");
        if (displayerTransform == null)
            return;

        if (texture == null && emission == null)
        {
            displayerTransform.gameObject.SetActive(false);
            return;
        }

        displayerTransform.gameObject.SetActive(true);

        Renderer renderer = displayerTransform.gameObject.GetComponent<Renderer>();
        renderer.material.SetTexture("_MainTex", texture);
        renderer.material.SetTexture("_EmissionMap", emission);
    }

    /// <summary>
    /// Displays the given set of cost textures on the container object
    /// </summary>
    /// <param name="costTextures">The cost textures (as generated by the GetIconifiedCostTexture helper)</param>
    /// <remarks>Does nothing if the game object is not the appropriate game object (i.e., it wasn't created in this helper)</remarks>
    public static void DisplayCostOnContainer(this GameObject gameObject, Tuple<Texture2D, Texture2D> costTextures)
    {
        gameObject.DisplayCostOnContainer(costTextures.Item1, costTextures.Item2);
    }

    private static readonly ConditionalWeakTable<RenderStatsLayer, List<Tuple<GemType, int, Renderer>>> GemContainerLookup = new();

    /// <summary>
    /// Gets a reference to the renderers for the three possible gem costs for the card.
    /// </summary>
    /// <returns>A dictionary mapping GemType to Renderer. The renderers will be null if the container was never created.</returns>
    public static List<Tuple<GemType, int, Renderer>> GetGemCostContainer(this RenderStatsLayer layer) => GemContainerLookup.GetOrCreateValue(layer);

    private static bool ValidateContainer(List<Tuple<GemType, int, Renderer>> container, DiskCardGame.Card card)
    {
        var gemsCost = (card as PlayableCard)?.GemsCost() ?? card.Info.GemsCost;
        // Quick validation
        if (gemsCost.Count != container.Count)
            return false;

        var contCt = container.GroupBy(g => g.Item1).ToDictionary(g => g.Key, g => g.Count());
        var costCt = gemsCost.GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());
        foreach (var gem in contCt.Keys.Concat(costCt.Keys))
            if (!costCt.ContainsKey(gem) || !contCt.ContainsKey(gem) || costCt[gem] != contCt[gem])
                return false;

        return true;
    }

    private static bool MatchesGem(this Tuple<GemType, int, Renderer> item, GemType? gemType) => (gemType ?? item.Item1) == item.Item1;

    /// <summary>
    /// Gets a reference to the renderers for the three possible gem costs for the card.
    /// </summary>
    /// <param name="force">If true, this will create a gem cost container if it doesn't already exist</param>
    /// <param name="verify">If true, this will validate that the existing container matches the card's current cost. This is off by default for performance reasons</param> 
    /// <returns>A dictionary mapping GemType to Renderer. The renderers will be null if the container was never created.</returns>
    public static List<Tuple<GemType, int, Renderer>> GetGemCostContainer(this DiskCardGame.Card card, bool force = false, GameObject container = null, bool verify = false, GemType? gemType = null, float stretchFactor = 1f)
    {
        var retval = card.StatsLayer.GetGemCostContainer();

        if (retval != null)
        {
            if (force)
            {
                foreach (var item in retval)
                {
                    if (item.MatchesGem(gemType) && !item.Item3.SafeIsUnityNull())
                        GameObject.Destroy(item.Item3);
                }
                retval.RemoveAll(item => item.MatchesGem(gemType));
            }
            else
            {
                if (verify && ValidateContainer(retval, card))
                    return retval;
                else if (retval.Any(item => item.MatchesGem(gemType)))
                    return retval;
            }
        }

        // First, see if it needs to be created
        GameObject gemContainer = container ?? card.GetPiece(UPPER_GEM_CONTAINER);

        // Remove any existing gems
        List<Transform> toDelete = new();
        for (int i = 0; i < gemContainer.transform.childCount; i++)
        {
            Transform child = gemContainer.transform.GetChild(i);
            if (!child.gameObject.name.Equals("TextureDisplayer"))
                toDelete.Add(gemContainer.transform.GetChild(i));
        }
        foreach (var c in toDelete)
            GameObject.Destroy(c.gameObject);

        if (!CardRenderReverseLookup.TryGetValue(card.StatsLayer, out var test))
            CardRenderReverseLookup.Add(card.StatsLayer, card);

        // Get the card full cost and sort it in order
        var gemsCost = (card as PlayableCard)?.GemsCost() ?? card.Info.GemsCost;
        gemsCost = gemsCost.Where(g => (gemType ?? g) == g).OrderBy(g => -(int)g).ToList();

        int idx = 0;
        Dictionary<GemType, int> counts = new()
        {
            { GemType.Blue, 0 },
            { GemType.Green, 0 },
            { GemType.Orange, 0 }
        };
        float anchor = gemsCost.Count <= 3 ? 0.3f : 0.1f * stretchFactor + .2f;
        float gap = gemsCost.Count <= 3 ? 0.3f : (2f * anchor) / (gemsCost.Count - 1);
        foreach (var gem in gemsCost)
        {
            GameObject costGem = GameObject.Instantiate(card.GetPiece(COST_LOOKUP[gem]), gemContainer.transform);
            costGem.name = $"Gem_Cost_{idx + 1}";
            costGem.transform.localScale = new(650f, 200f / stretchFactor, 450f);
            costGem.transform.localEulerAngles = new(270f, 90f, 0f);
            costGem.transform.localPosition = new(
                -anchor + (gap * idx),
                gem == GemType.Blue ? 0.36f
                : gem == GemType.Green ? 0.39f
                : 0.41f,
                0f
            );
            counts[gem] += 1;
            retval.Add(new(gem, counts[gem], costGem.GetComponent<Renderer>()));

            idx++;
        }

        return retval;
    }

    private static void ResetBlueGemifyGem(DiskCardGame.Card card, bool originalPosition, bool active)
    {
        GameObject gem = card.GetPiece(BLUE_GEM_ORIGINAL);
        if (gem == null)
            return;

        gem.SetActive(active);

        if (originalPosition)
        {
            gem.transform.localPosition = new(0.0935f, 0f, 0.162f);
            gem.transform.localScale = new(122.6777f, 122.6777f, 100f);
        }
        else
        {
            gem.transform.localPosition = new(0.54f, 0f, 0f);
            gem.transform.localScale = new(80f, 80f, 100f);
        }
    }

    [HarmonyPatch(typeof(DiskCardGame.Card), nameof(DiskCardGame.Card.RenderCard))]
    [HarmonyPostfix]
    private static void DisplayAlternateCostSingle(DiskCardGame.Card __instance)
    {
        // This will render alternate costs to the cost window.
        // Note that right now this only support single alternate costs, not combined costs
        if (__instance.StatsLayer is not DiskRenderStatsLayer drsl)
            return;

        // Okay, first we need to figure out how many costs we actually have
        List<CustomCostRenderInfo> costDisplays = new();
        PlayableCard playableCard = __instance as PlayableCard;

        int energyCost = playableCard?.EnergyCost ?? __instance.Info.EnergyCost;
        if (energyCost > 0)
            costDisplays.Add(new("Energy"));

        List<GemType> gemsCost = playableCard?.GemsCost() ?? __instance.Info.GemsCost;
        if (gemsCost.Count > 0)
        {
            if (gemsCost.Count <= 3)
            {
                costDisplays.Add(new("Gems_All"));
            }
            else
            {
                if ((playableCard?.OrangeGemsCost() ?? __instance.Info.OrangeGemsCost()).Count > 0)
                    costDisplays.Add(new("Gems_Orange"));
                if ((playableCard?.BlueGemsCost() ?? __instance.Info.BlueGemsCost()).Count > 0)
                    costDisplays.Add(new("Gems_Blue"));
                if ((playableCard?.GreenGemsCost() ?? __instance.Info.GreenGemsCost()).Count > 0)
                    costDisplays.Add(new("Gems_Green"));
            }
        }

        int bonesCost = playableCard?.BonesCost() ?? __instance.Info.BonesCost;
        if (bonesCost > 0)
            costDisplays.Add(new("Bones", GetIconifiedCostTexture(BoneCostIcon, bonesCost, forceDigitDisplay: true)));

        int bloodCost = playableCard?.BloodCost() ?? __instance.Info.BloodCost;
        if (bloodCost > 0)
            costDisplays.Add(new("Blood", GetIconifiedCostTexture(BloodCostIcon, BloodCostIconUnlit, bloodCost)));

        // get a list of the custom costs we need textures for
        // check for PlayableCard to account for possible dynamic costs (no API support but who knows what modders do)
        List<CardCostManager.FullCardCost> customCosts;
        if (playableCard != null)
            customCosts = playableCard.GetCustomCardCosts().Select(x => CardCostManager.AllCustomCosts.Find(c => c.CostName == x.CostName)).ToList();
        else
            customCosts = __instance.Info.GetCustomCosts();

        // take the 'one' texture and use it as a base for creating the rest of the textures
        foreach (CardCostManager.FullCardCost fullCost in customCosts)
        {
            Texture2D costTex = null;
            string key = $"{fullCost.ModGUID}_{fullCost.CostName}_1_part3";
            if (CardCostRender.AssembledTextures.ContainsKey(key))
            {
                if (CardCostRender.AssembledTextures[key] != null)
                    costTex = CardCostRender.AssembledTextures[key];
                else
                    CardCostRender.AssembledTextures.Remove(key);
            }
            else
            {
                Texture2D oneTex = fullCost.CostTexture(1, __instance.Info, playableCard);
                if (oneTex != null)
                {
                    costTex = oneTex;
                    CardCostRender.AssembledTextures.Add(key, costTex);
                }
            }
            if (costTex != null)
            {
                int amount = playableCard?.GetCustomCost(fullCost) ?? __instance.Info.GetCustomCost(fullCost);
                costDisplays.Add(new(
                    fullCost.CostName,
                    GetIconifiedCostTexture(AssembledTextures[key], amount)
                    ));
            }
        }

        // Get the other mod costs
        UpdateCardCostSimple?.Invoke(__instance.Info, costDisplays);

        // Quick bailout if possible (for cards that only have energy cost)
        if (costDisplays.Count == 0 || (costDisplays.Count == 1 && energyCost > 0))
        {
            __instance.GetPiece(ENERGY_LIGHTS).SetActive(true);
            ResetBlueGemifyGem(__instance, true, __instance.Info.Gemified);
            return;
        }

        // Get all the cost containers
        List<GameObject> costContainers = __instance.GetCostContainers(costDisplays.Count);
        for (int i = 0; i < costContainers.Count; i++)
        {
            if (i < costDisplays.Count)
            {
                costContainers[i].SetActive(true);

                if (i > 0)
                {
                    costContainers[i].transform.localPosition = new(
                        costContainers[i].transform.localPosition.x,
                        costContainers[i].transform.localPosition.y,
                        INITIAL_CONTAINER_Z_SPACING + CONTAINER_Z_SPACING * (i - 1)
                    );
                }
                costDisplays[i].CostContainer = costContainers[i];
                costDisplays[i].UpdateDisplayedTextures();
            }
            else
            {
                costContainers[i].SetActive(false);
            }
        }

        // Run the complex update
        UpdateCardCostComplex?.Invoke(__instance.Info, costDisplays);

        // Handle our special case (the gems)
        if (gemsCost.Count > 0)
        {
            if (gemsCost.Count <= 3)
            {
                CustomCostRenderInfo gemRenderInfo = costDisplays.First(c => c.CostId.Equals("Gems_All", StringComparison.InvariantCultureIgnoreCase));
                var gemContainer = __instance.GetGemCostContainer(force: true, container: gemRenderInfo.CostContainer);
                foreach (Renderer renderer in gemContainer.Where(v => v != null && v.Item3 != null).Select(v => v.Item3))
                    renderer.gameObject.SetActive(true);
            }
            else
            {
                int orangeGemsCost = (playableCard?.OrangeGemsCost() ?? __instance.Info.OrangeGemsCost()).Count;
                int blueGemsCost = (playableCard?.BlueGemsCost() ?? __instance.Info.BlueGemsCost()).Count;
                int greenGemsCost = (playableCard?.GreenGemsCost() ?? __instance.Info.GreenGemsCost()).Count;
                if (orangeGemsCost > 0)
                {
                    CustomCostRenderInfo gemRenderInfo = costDisplays.First(c => c.CostId.Equals("Gems_Orange", StringComparison.InvariantCultureIgnoreCase));
                    if (orangeGemsCost > 3)
                        gemRenderInfo.Stretch((float)orangeGemsCost / 3f);

                    var gemContainer = __instance.GetGemCostContainer(force: true, container: gemRenderInfo.CostContainer, gemType: GemType.Orange, stretchFactor: (float)orangeGemsCost / 3f);
                    foreach (var renderer in gemContainer.Where(v => v != null && v.Item3 != null).Select(v => v.Item3))
                        renderer.gameObject.SetActive(true);
                }
                if (blueGemsCost > 0)
                {
                    CustomCostRenderInfo gemRenderInfo = costDisplays.First(c => c.CostId.Equals("Gems_Blue", StringComparison.InvariantCultureIgnoreCase));
                    if (blueGemsCost > 3)
                        gemRenderInfo.Stretch((float)blueGemsCost / 3f);

                    var gemContainer = __instance.GetGemCostContainer(force: true, container: gemRenderInfo.CostContainer, gemType: GemType.Blue, stretchFactor: (float)blueGemsCost / 3f);
                    foreach (var renderer in gemContainer.Where(v => v != null && v.Item3 != null).Select(v => v.Item3))
                        renderer.gameObject.SetActive(true);
                }
                if (greenGemsCost > 0)
                {
                    CustomCostRenderInfo gemRenderInfo = costDisplays.First(c => c.CostId.Equals("Gems_Green", StringComparison.InvariantCultureIgnoreCase));
                    if (greenGemsCost > 3)
                        gemRenderInfo.Stretch((float)greenGemsCost / 3f);

                    var gemContainer = __instance.GetGemCostContainer(force: true, container: gemRenderInfo.CostContainer, gemType: GemType.Green, stretchFactor: (float)greenGemsCost / 3f);
                    foreach (var renderer in gemContainer.Where(v => v != null && v.Item3 != null).Select(v => v.Item3))
                        renderer.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            // Just in case, destroy the gem renderer set
            if (GemContainerLookup.TryGetValue(__instance.StatsLayer, out var item))
                item.Clear();
        }

        // Handle the energy bars
        bool energyLightsActive = energyCost > 0 || costDisplays.Count == 0;
        __instance.GetPiece(ENERGY_LIGHTS).SetActive(energyLightsActive);
        ResetBlueGemifyGem(__instance, true, __instance.Info.Gemified && energyLightsActive);
    }

    [HarmonyPatch(typeof(DiskRenderStatsLayer), nameof(DiskRenderStatsLayer.ManagedUpdate))]
    [HarmonyPostfix]
    private static void UpdateGemsCost(DiskRenderStatsLayer __instance)
    {
        var gemContainer = __instance.GetGemCostContainer();
        DiskCardGame.Card card = __instance.GetCard();

        // Here, all we can do is check each item in the gemContainer and see
        // if we have the gem or not
        foreach (var obj in gemContainer)
        {
            Color emissionColor = !GameFlowManager.IsCardBattle || ResourcesManager.Instance.gems.Where(g => g == obj.Item1).Count() >= obj.Item2 ? Color.white : Color.gray;
            obj.Item3.GetPropertyBlock(__instance.gemsPropertyBlock);
            __instance.gemsPropertyBlock.SetColor("_EmissionColor", emissionColor);
            obj.Item3.SetPropertyBlock(__instance.gemsPropertyBlock);
        }
    }

    [HarmonyPatch(typeof(P03FaceCardDisplayer), nameof(P03FaceCardDisplayer.DisplayCard))]
    [HarmonyPostfix]
    private static void SetCostsOnP03Face(P03FaceCardDisplayer __instance, CardInfo info, CardModificationInfo mod)
    {
        // The starting point for each additional cost is based on which energy bars are enabled
        var rend = __instance.energyBars.FirstOrDefault(r => !r.enabled);
        float curPos = rend == null ? __instance.energyBars[5].transform.localPosition.x : rend.transform.localPosition.x;
        curPos += 0.02f;

        // Blink indicators
        bool blinkGems = false;
        bool blinkBones = false;
        bool blinkBlood = false;

        // Handle gems cost. They already exist, which is fantastic. We just need to update them.
        int blue = 0;
        int orange = 0;
        int green = 0;
        foreach (var g in info.GemsCost)
        {
            if (g == GemType.Green)
                green += 1;
            if (g == GemType.Orange)
                orange += 1;
            if (g == GemType.Blue)
                blue += 1;
        }
        if (mod != null)
        {
            if (mod.addGemCost != null)
            {
                foreach (var g in mod.addGemCost)
                {
                    if (g == GemType.Green)
                        green += 1;
                    if (g == GemType.Orange)
                        orange += 1;
                    if (g == GemType.Blue)
                        blue += 1;
                }
                blinkGems = true;
            }
            if (mod.HasRemovedAnyGemCost())
                blinkGems = true;
            if (mod.HasRemoveBlueGemCost() && blue > 0)
                blue -= 1;
            if (mod.HasRemovedGreenGemCost() && green > 0)
                green -= 1;
            if (mod.HasRemovedOrangeGemCost() && orange > 0)
                orange -= 1;
            if (mod.nullifyGemsCost)
            {
                green = orange = blue = 0;
                blinkGems = true;
            }
        }

        // Turn off all gems
        var gems = __instance.transform.Find("Resources/Gems");
        gems.transform.localPosition = Vector3.zero;
        for (int i = 0; i < gems.childCount; i++)
        {
            var obj = gems.GetChild(i)?.gameObject;
            if (obj == null)
                continue;

            obj.SetActive(false);
            if (obj.GetComponent<BlinkColor>() == null)
            {
                var blink = obj.AddComponent<BlinkColor>();
                blink.color1 = obj.GetComponent<SpriteRenderer>().color;
                blink.color2 = new(0.0078f, 0.0392f, 0.0667f);
                blink.renderers = new() { obj.GetComponent<SpriteRenderer>() };
                blink.colorId = "_Color";
                blink.frequency = 0.2f;
                blink.originalFrequency = 0.2f;
                blink.timer = 0.0681f;
            }
        }

        // Order goes blue, orange, green (from right to left); -0.15 for each
        for (int i = 0; i < blue; i++)
        {
            string name = "Gem_Blue";
            if (i > 0)
                name += $"_{i}";
            var obj = __instance.transform.Find($"Resources/Gems/{name}") ?? GameObject.Instantiate(__instance.transform.Find("Resources/Gems/Gem_Blue").gameObject, gems).transform;
            obj.name = name;
            curPos -= 0.1f;
            obj.localPosition = new(curPos, 0f, 0f);
            obj.gameObject.SetActive(true);

            Color blueGemColor = new Color(0f, 0.8507f, 1f) * 0.8f;
            obj.GetComponent<SpriteRenderer>().enabled = true;
            obj.GetComponent<SpriteRenderer>().color = blueGemColor;
            obj.GetComponent<BlinkColor>().enabled = blinkGems;
            obj.GetComponent<BlinkColor>().color1 = blueGemColor;
            curPos -= 0.1f;
        }
        for (int i = 0; i < orange; i++)
        {
            string name = "Gem_Orange";
            if (i > 0)
                name += $"_{i}";
            var obj = __instance.transform.Find($"Resources/Gems/{name}") ?? GameObject.Instantiate(__instance.transform.Find("Resources/Gems/Gem_Orange").gameObject, gems).transform;
            obj.name = name;
            curPos -= 0.1f;
            obj.localPosition = new(curPos, 0f, 0f);
            obj.gameObject.SetActive(true);
            obj.GetComponent<SpriteRenderer>().enabled = true;
            obj.GetComponent<BlinkColor>().enabled = blinkGems;
            curPos -= 0.1f;
        }
        for (int i = 0; i < green; i++)
        {
            string name = "Gem_Green";
            if (i > 0)
                name += $"_{i}";
            var obj = __instance.transform.Find($"Resources/Gems/{name}") ?? GameObject.Instantiate(__instance.transform.Find("Resources/Gems/Gem_Green").gameObject, gems).transform;
            obj.name = name;
            curPos -= 0.095f;
            obj.localPosition = new(curPos, 0f, 0f);
            obj.gameObject.SetActive(true);
            obj.GetComponent<SpriteRenderer>().enabled = true;
            obj.GetComponent<BlinkColor>().enabled = blinkGems;
            curPos -= 0.095f;
        }

        // Turn off all of the blood and bones items
        var resources = __instance.transform.Find("Resources");
        for (int i = 0; i < resources.childCount; i++)
        {
            var t = resources.GetChild(i);
            if (t.gameObject.name.Contains("Bone") || t.gameObject.name.Contains("Blood"))
                t.gameObject.SetActive(false);
        }

        // Gems are super complicated. Bones are also kinda complicated.
        // For now I ignore it and we'll just let the bones overflow
        int bones = info.BonesCost + (mod?.bonesCostAdjustment).GetValueOrDefault(0);
        blinkBones = (mod?.bonesCostAdjustment).GetValueOrDefault(0) != 0;
        for (int i = 0; i < bones; i++)
        {
            string k = $"Resources/Bones_{i}";
            var obj = __instance.transform.Find(k);
            if (obj == null)
            {
                obj = GameObject.Instantiate(__instance.transform.Find("Resources/Gems/Gem_Green").gameObject, resources).transform;
                obj.name = $"Bones_{i}";
                var renderer = obj.GetComponent<SpriteRenderer>();
                renderer.sprite = FaceBoneSprite;
                renderer.color = BoneColor * 0.9f;
                obj.GetComponent<BlinkColor>().color1 = BoneColor * 0.9f;
            }
            curPos -= 0.069f;
            obj.localPosition = new(curPos, 0f, 0f);
            obj.gameObject.SetActive(true);
            obj.GetComponent<SpriteRenderer>().enabled = true;
            obj.GetComponent<BlinkColor>().enabled = blinkBones;
            curPos -= 0.069f;
        }

        int blood = info.BloodCost + (mod?.bloodCostAdjustment).GetValueOrDefault(0);
        blinkBlood = (mod?.bloodCostAdjustment).GetValueOrDefault(0) != 0;
        for (int i = 0; i < blood; i++)
        {
            string k = $"Resources/Blood_{i}";
            var obj = __instance.transform.Find(k);
            if (obj == null)
            {
                obj = GameObject.Instantiate(__instance.transform.Find("Resources/Gems/Gem_Green").gameObject, resources).transform;
                obj.name = $"Blood_{i}";
                var renderer = obj.GetComponent<SpriteRenderer>();
                renderer.sprite = FaceBloodSprite;
                renderer.color = BloodColor * 0.9f;
                obj.GetComponent<BlinkColor>().color1 = BloodColor * 0.9f;
            }
            curPos -= 0.057f;
            obj.localPosition = new(curPos, 0f, 0f);
            obj.gameObject.SetActive(true);
            obj.GetComponent<SpriteRenderer>().enabled = true;
            obj.GetComponent<BlinkColor>().enabled = blinkBlood;
            curPos -= 0.057f;
        }

        foreach (var gb in __instance.GetComponentsInChildren<BlinkColor>())
        {
            gb.blinkOn = false;
            gb.Reset();
        }
    }
}