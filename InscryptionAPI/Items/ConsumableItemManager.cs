using DiskCardGame;
using InscryptionAPI.Guid;
using InscryptionAPI.Helpers;
using InscryptionAPI.Helpers.Extensions;
using InscryptionAPI.Items.Extensions;
using InscryptionAPI.RuleBook;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;

namespace InscryptionAPI.Items;


public static class ConsumableItemManager
{
    public class FullConsumableItemData
    {
        public ConsumableItemData itemData;

        /// <summary>
        /// Tracks all rulebook redirects that this ability's description will have. Explanation of the variables is as follows:
        /// Key (string): the text that will be recoloured to indicate that it's clickable.
        /// Tuple.Item1 (PageRangeType): the type of page the redirect will go to. Use PageRangeType.Unique if you want to redirect to a custom rulebook page using its pageId.
        /// Tuple.Item2 (Color): the colour the Key text will be recoloured to.
        /// Tuple.Item3 (string): the id that the API will match against to find the redirect page. Eg, for ability redirects this will be the Ability id as a string.
        /// </summary>
        public Dictionary<string, RuleBookManager.RedirectInfo> RulebookDescriptionRedirects = new();

        public List<AbilityMetaCategory> rulebookMetaCategories = new();
        public FullConsumableItemData(ConsumableItemData data)
        {
            this.itemData = data;
        }
    }

    internal enum ConsumableState
    {
        Vanilla,
        Custom,
        All
    }

    public enum ModelType
    {
        BasicRune = 1,
        BasicRuneWithVeins = 2,
        HoveringRune = 3,
        CardInABottle = 4,
    }

    private static Sprite cardinbottleSprite;
    private static ConsumableItemResource defaultItemModel = null;
    private static ModelType defaultItemModelType = ModelType.HoveringRune;
    internal static Dictionary<string, ConsumableItemResource> prefabIDToResourceLookup = new();
    private static Dictionary<ModelType, ConsumableItemResource> typeToPrefabLookup = new();
    private static HashSet<ModelType> defaultModelTypes = new();
    internal static readonly List<ConsumableItemData> allNewItems = new();
    private static readonly List<ConsumableItemData> baseConsumableItemsDatas = new();
    public static ReadOnlyCollection<ConsumableItemData> NewConsumableItemDatas = new(allNewItems);

    internal static readonly List<FullConsumableItemData> allFullItemDatas = new();

    public static ConsumableItemData New(string pluginGUID,
        string rulebookName, string rulebookDescription,
        Texture2D rulebookSprite, Type itemType,
        GameObject prefab)
    {
        ConsumableItemResource resource = new();
        resource.FromPrefab(prefab);

        ModelType modelType = RegisterPrefab(pluginGUID, rulebookName, resource);
        UnityObject.DontDestroyOnLoad(prefab);
        prefab.SetActive(false);

        return New(pluginGUID, rulebookName, rulebookDescription, rulebookSprite, itemType, modelType);
    }

    public static ConsumableItemData New(string pluginGUID,
        string rulebookName, string rulebookDescription,
        Texture2D rulebookSprite, Type itemType,
        ConsumableItemResource resource)
    {
        ModelType registerPrefab = RegisterPrefab(pluginGUID, rulebookName, resource);
        return New(pluginGUID, rulebookName, rulebookDescription, rulebookSprite, itemType, registerPrefab);
    }

    public static ConsumableItemData New(string pluginGUID,
        string rulebookName, string rulebookDescription,
        Texture2D rulebookSprite, Type itemType,
        ModelType modelType)
    {
        ConsumableItemData data = ScriptableObject.CreateInstance<ConsumableItemData>();
        data.SetRulebookName(rulebookName);
        data.SetRulebookDescription(rulebookDescription);
        data.SetRulebookSprite(rulebookSprite.ConvertTexture());
        data.SetRegionSpecific(false);
        data.SetNotRandomlyGiven(false);
        data.SetPrefabModelType(modelType);
        data.SetPickupSoundId("stone_object_up");
        data.SetPlacedSoundId("stone_object_hit");
        data.SetExamineSoundId("stone_object_hit");
        data.SetComponentType(itemType);
        data.SetPowerLevel(1);

        return Add(pluginGUID, data);
    }

    public static ConsumableItemData NewCardInABottle(string pluginGUID, string cardName, Texture2D rulebookTexture = null)
    {
        CardInfo cardInfo = CardLoader.GetCardByName(cardName);
        if (cardInfo == null)
        {
            InscryptionAPIPlugin.Logger.LogError("[NewCardInABottle] Could not get card using name " + cardName);
            return null;
        }

        return NewCardInABottle(pluginGUID, cardInfo, rulebookTexture);
    }

    public static ConsumableItemData NewCardInABottle(string pluginGUID, CardInfo cardInfo, Texture2D rulebookTexture = null)
    {
        if (cardInfo == null)
        {
            InscryptionAPIPlugin.Logger.LogError("[NewCardInABottle] CardInfo is null!");
            return null;
        }

        ConsumableItemData data = ScriptableObject.Instantiate(Resources.Load<ConsumableItemData>("data/consumables/FrozenOpossumBottle"));
        data.SetRulebookName($"{cardInfo.displayedName} Bottle");
        data.SetRulebookDescription($"A {cardInfo.displayedName} is created in your hand. [define:{cardInfo.name}]");
        data.SetRegionSpecific(false);
        data.SetNotRandomlyGiven(false);
        data.SetLearnItemDescription("");
        data.SetPrefabModelType(ModelType.CardInABottle);
        data.SetComponentType(typeof(CardBottleItem));
        data.SetCardWithinBottle(cardInfo.name);

        Sprite sprite;
        if (rulebookTexture != null)
        {
            sprite = rulebookTexture.ConvertTexture();
        }
        else
        {
            cardinbottleSprite ??= TextureHelper.GetImageAsTexture("rulebookitemicon_cardinbottle.png", Assembly.GetExecutingAssembly()).ConvertTexture();
            sprite = cardinbottleSprite;
        }
        data.SetRulebookSprite(sprite);

        return Add(pluginGUID, data);
    }

    public static ConsumableItemData Add(string pluginGUID, ConsumableItemData data)
    {
        string name = pluginGUID + "_" + data.rulebookName;
        data.name = name;
        data.SetPrefabID(name);
        data.SetModPrefix(pluginGUID);

        allNewItems.Add(data);
        allFullItemDatas.Add(new(data));
        return data;
    }

    public static ModelType RegisterPrefab(string pluginGUID, string rulebookName, ConsumableItemResource resource)
    {
        ModelType type = GuidManager.GetEnumValue<ModelType>(pluginGUID, rulebookName);
        typeToPrefabLookup[type] = resource;

        return type;
    }

    private static void InitializeDefaultModels()
    {
        LoadDefaultModelFromBundle("runeroundedbottom", "RuneRoundedBottom", ModelType.BasicRune);
        LoadDefaultModelFromBundle("customitem", "RuneRoundedBottomVeins", ModelType.BasicRuneWithVeins);
        LoadDefaultModelFromBundle("customhoveringitem", "RuneHoveringItem", ModelType.HoveringRune);
        LoadDefaultModelFromResources("prefabs/items/FrozenOpossumBottleItem", ModelType.CardInABottle);
    }

    private static void LoadDefaultModelFromBundle(string assetBundlePath, string prefabName, ModelType type)
    {
        ConsumableItemResource resource = new ConsumableItemResource();
        resource.FromAssetBundleInAssembly<InscryptionAPIPlugin>(assetBundlePath, prefabName);
        typeToPrefabLookup[type] = resource;
        defaultModelTypes.Add(type);
    }

    private static void LoadDefaultModelFromResources(string resourcePath, ModelType type)
    {
        ConsumableItemResource resource = new ConsumableItemResource();
        resource.FromResources(resourcePath);

        typeToPrefabLookup[type] = resource;
        defaultModelTypes.Add(type);
    }
    private static void InitializeConsumableItemDataPrefab(ConsumableItemData item)
    {
        ModelType modelType = item.GetPrefabModelType();
        if (!typeToPrefabLookup.TryGetValue(modelType, out ConsumableItemResource prefab))
        {
            // No model assigned. use default model!
            prefab = defaultItemModel;
            InscryptionAPIPlugin.Logger.LogWarning($"Could not find ModelType {modelType} for ConsumableItemData {item.rulebookName}!");
        }
        if (prefab == null)
        {
            // Model assigned but model has been deleted?
            prefab = defaultItemModel;
            InscryptionAPIPlugin.Logger.LogError($"Prefab missing for ConsumableItemData {item.rulebookName}! Using default instead.");
        }

        string prefabId = "Prefabs/Items/" + item.prefabId;
        prefabIDToResourceLookup[prefabId.ToLowerInvariant()] = prefab;
    }
    private static bool CanUseBaseModel(ConsumableItemData data, GameObject gameObject)
    {
        return data.rulebookSprite != null && gameObject.GetComponent<CardBottleItem>() == null;
    }
    private static void MoveComponent(Component sourceComp, Component targetComp)
    {
        FieldInfo[] sourceFields = sourceComp.GetType().GetFields(BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        for (int i = 0; i < sourceFields.Length; i++)
        {
            var value = sourceFields[i].GetValue(sourceComp);
            sourceFields[i].SetValue(targetComp, value);
        }
    }
    internal static void Initialize()
    {
        baseConsumableItemsDatas.Clear();
        baseConsumableItemsDatas.AddRange(ItemsUtil.AllConsumables.FindAll(a => a != null && string.IsNullOrEmpty(a.GetModPrefix())));
        foreach (ConsumableItemData itemData in baseConsumableItemsDatas)
        {
            if (!allFullItemDatas.Exists(x => x.itemData == itemData))
                allFullItemDatas.Add(new(itemData));
        }
        if (InscryptionAPIPlugin.configCustomItemTypes.Value == ConsumableState.Vanilla)
        {
            // Don't change any items!
            return;
        }

        if (defaultItemModel == null)
        {
            // NOTE: Do not add a ConsumableItem component here so we can override default models with this!
            InitializeDefaultModels();
            defaultItemModel = typeToPrefabLookup[defaultItemModelType];
        }

        // Add all totem tops to the game
        foreach (ConsumableItemData item in allNewItems)
        {
            InitializeConsumableItemDataPrefab(item);
        }

        // Override vanilla items
        if (InscryptionAPIPlugin.configCustomItemTypes.Value == ConsumableState.All)
        {
            // Change all base items to use the fallback model!
            foreach (ConsumableItemData data in baseConsumableItemsDatas)
            {
                string path = "Prefabs/Items/" + data.prefabId;
                GameObject prefab = ResourceBank.Get<GameObject>(path);
                if (prefab == null)
                {
                    InscryptionAPIPlugin.Logger.LogWarning($"Couldn't override item {data.rulebookName}. Couldn't get prefab from ResourceBank");
                    continue;
                }

                if (!CanUseBaseModel(data, prefab))
                {
                    continue;
                }

                data.SetComponentType(prefab.GetComponent<ConsumableItem>().GetType());
                data.SetPrefabModelType(defaultItemModelType);

                ConsumableItemResource resource = (ConsumableItemResource)defaultItemModel.Clone();
                resource.PreSetupCallback = static (clone, data) =>
                {
                    string path = "Prefabs/Items/" + data.prefabId;
                    GameObject defaultObject = ResourceBank.Get<GameObject>(path);

                    ConsumableItem sourceComp = defaultObject.GetComponent<ConsumableItem>();
                    ConsumableItem targetComp = clone.AddComponent<ConsumableItem>();
                    MoveComponent(sourceComp, targetComp);
                };

                prefabIDToResourceLookup[path.ToLowerInvariant()] = defaultItemModel;
            }
        }
    }
    internal static ConsumableItem SetupPrefab(ConsumableItemData data, GameObject prefab, Type itemType, ModelType modelType)
    {
        prefab.name = $"Custom Item ({data.rulebookName})";

        // Populate icon. Only for default fallback types - If anyone wants to use this then they can add to that fallback type list i guss??
        if (defaultModelTypes.Contains(modelType) && modelType != ModelType.CardInABottle)
        {
            if (data.rulebookSprite != null)
            {
                GameObject icon = prefab.gameObject.FindChild("Icon");
                if (icon != null)
                {
                    Renderer iconRenderer = icon.GetComponent<Renderer>();
                    if (iconRenderer != null)
                        iconRenderer.material.mainTexture = data.rulebookSprite.texture;
                    else
                        InscryptionAPIPlugin.Logger.LogError($"Could not find Renderer on Icon GameObject to assign tribe icon!");
                }
                else
                    InscryptionAPIPlugin.Logger.LogError($"Could not find Icon GameObject to assign tribe icon!");

            }
            else
                InscryptionAPIPlugin.Logger.LogError($"Could not change icon for {data.rulebookName}. No sprite defined!");

        }

        // Add default animation if it doesn't have one
        Animator animator = prefab.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Transform child = prefab.transform.GetChild(0);
            if (child != null)
                animator = child.gameObject.AddComponent<Animator>();

            else
                InscryptionAPIPlugin.Logger.LogError($"Could not add Animator. Missing a child game object!. Make sure you have a GameObject called Anim!");

        }
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("animation/items/ItemAnim");
            animator.Rebind();
        }

        // Add component if it doesn't exist
        ConsumableItem consumableItem = prefab.GetComponent<ConsumableItem>();
        if (consumableItem == null)
        {
            consumableItem = prefab.AddComponent(itemType) as ConsumableItem;
            if (consumableItem == null)
                InscryptionAPIPlugin.Logger.LogError($"Type given is not a ConsumableItem! You may encounter unexpected bugs");
        }

        // Mark as DontDestroyOnLoad so it doesn't get removed between levels
        // SetParent shenanigans are to avoid console warnings
        var parent = prefab.transform.parent;
        prefab.transform.SetParent(null);
        UnityObject.DontDestroyOnLoad(prefab);
        prefab.transform.SetParent(parent);

        return consumableItem;
    }
}