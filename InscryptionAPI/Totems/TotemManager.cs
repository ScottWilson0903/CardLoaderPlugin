using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionAPI.Helpers;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace InscryptionAPI.Totems;

public static class TotemManager
{
    internal enum TotemTopState
    {
        Vanilla,
        CustomTribes,
        AllTribes
    }

    #region Patches
    [HarmonyPatch(typeof(BuildTotemSequencer), "GenerateTotemChoices", new Type[] { typeof(BuildTotemNodeData), typeof(int) })]
    private class ItemsUtil_AllConsumables
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemTopState.Vanilla)
                return instructions;

            // === We want to turn this

            // List<Tribe> list = new()
            // {
            //     Tribe.Bird,
            //     Tribe.Canine,
            //     Tribe.Hooved,
            //     Tribe.Insect,
            //     Tribe.Reptile
            // };

            // === Into this

            // List<Tribe> list = new()
            // {
            //     Tribe.Bird,
            //     Tribe.Canine,
            //     Tribe.Hooved,
            //     Tribe.Insect,
            //     Tribe.Reptile
            // };
            // ItemsUtil_AllConsumables.AddCustomTribesToList(list);

            // ===


            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Newobj)
                {
                    // Make a new list
                    for (int j = i + 1; i < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Stloc_0)
                        {
                            MethodInfo customMethod = AccessTools.Method(typeof(ItemsUtil_AllConsumables), nameof(AddCustomTribesToList), new Type[] { typeof(List<Tribe>) });

                            // Stored the list
                            codes.Insert(j + 1, new CodeInstruction(OpCodes.Ldloc_0));
                            codes.Insert(j + 2, new CodeInstruction(OpCodes.Call, customMethod));
                            return codes;
                        }
                    }
                }
            }

            return codes;
        }


        public static void AddCustomTribesToList(List<Tribe> list)
        {
            list.AddRange(TribeManager.NewTribes.Where(x => x.tribeChoice).Select(x => x.tribe));
            InscryptionAPIPlugin.Logger.LogDebug($"Total Tribes: {list.Count}");

            List<CardInfo> cardsWithTribes = CardManager.AllCardsCopy.FindAll(x => x.tribes.Count > 0);
            list.RemoveAll(x => !cardsWithTribes.Exists(ci => ci.IsOfTribe(x)));

            InscryptionAPIPlugin.Logger.LogDebug($"Tribes with 1 Card: {list.Count}");
        }
    }

    [HarmonyPatch(typeof(ResourceBank), "Awake", new Type[] { })]
    private class ResourceBank_Awake
    {
        private static void Postfix(ResourceBank __instance)
        {
            // The resource bank has been cleared. refill it
            if (ResourceBank.Get<GameObject>(CustomTotemTopResourcePath) == null)
                Initialize();
        }
    }

    [HarmonyPatch(typeof(Totem), "GetTopPiecePrefab", new Type[] { typeof(TotemTopData) })]
    private class Totem_GetTopPiecePrefab
    {
        private static bool Prefix(TotemTopData data, ref GameObject __result)
        {
            if (TribeManager.IsCustomTribe(data.prerequisites.tribe))
            {
                CustomTotemTop customTribeTotem = totemTops.Find((a) => a.Tribe == data.prerequisites.tribe);

                // use the custom Totem top if it exists; otherwise use the default
                __result = customTribeTotem?.Prefab ?? defaultTotemTop.Prefab;
                return false;
            }
            else if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemTopState.AllTribes)
            {
                __result = defaultTotemTop.Prefab;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Totem), "SetData", new Type[] { typeof(ItemData) })]
    private class Totem_SetData
    {
        private static void Postfix(Totem __instance, ItemData data)
        {
            __instance.topPieceParent.GetComponentInChildren<CompositeTotemPiece>().SetData(__instance.TotemItemData.top);
        }
    }

    [HarmonyPatch(typeof(TotemTopData), "PrefabId", MethodType.Getter)]
    private class TotemTopData_PrefabId
    {
        private static bool Prefix(TotemTopData __instance, ref string __result)
        {
            // Custom totem tops will always use the fallback UNLESS there is an override
            if (TribeManager.IsCustomTribe(__instance.prerequisites.tribe))
            {
                CustomTotemTop customTribeTotem = totemTops.Find((a) => a.Tribe == __instance.prerequisites.tribe);
                if (customTribeTotem == null)
                {
                    __result = CustomTotemTopID;
                    return false;
                }
            }
            else if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemTopState.AllTribes)
            {
                // All non-custom tribes will use the fallback model 
                __result = CustomTotemTopID;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// If someone added a mod with sigils then removed the mod. They have learned the ability but it no longer exists.
    /// This breaks the new totem sequence because it's not checking if the ability info exists.
    /// So check that the ability info exists
    /// </summary>
    [HarmonyPatch]
    private class FixMissingAbilityInfo
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var innerClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<>c, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            yield return AccessTools.Method(innerClass, "<GenerateTotemChoices>b__26_0");
        }

        public static bool Prefix(Ability x, ref bool __result)
        {
            if (!RunState.Run.totemBottoms.Contains(x))
            {
                AbilityInfo abilityInfo = AbilitiesUtil.GetInfo(x);
                __result = abilityInfo != null && !abilityInfo.metaCategories.Contains(AbilityMetaCategory.Part1Modular);

            }
            else
            {
                __result = true;
            }

            return false;
        }
    }

    /// <summary>
    /// If someone added a mod with sigils then removed the mod. They have learned the ability but it no longer exists.
    /// This breaks the new totem sequence because it's not checking if the ability info exists.
    /// So check that the ability info exists
    /// </summary>
    [HarmonyPatch]
    private class FixMissingAbilityInfo2
    {
        // think this could be simplified but I ain't touching it
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var innerClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<>c__DisplayClass26_0, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            yield return AccessTools.Method(innerClass, "<GenerateTotemChoices>b__1");
        }

        private static bool Prefix(Ability x)
        {
            return AbilitiesUtil.GetInfo(x) != null;
        }
    }
    #endregion

    [Obsolete("Deprecated. Use NewTopPiece<T> instead.")]
    public static CustomTotemTop NewTopPiece(string name, string guid, Tribe tribe, GameObject prefab = null)
    {
        if (prefab == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Cannot load NewTopPiece for {guid}.{name}. Prefab is null!");
            return null;
        }

        return Add(new CustomTotemTop()
        {
            Name = name,
            GUID = guid,
            Prefab = prefab,
            Tribe = tribe
        });
    }

    public static CustomTotemTop NewTopPiece<T>(string name, string guid, Tribe tribe, GameObject prefab) where T : CompositeTotemPiece
    {
        if (prefab == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Cannot load NewTopPiece for {guid}.{name}; prefab is null!");
            return null;
        }

        return Add(new CustomTotemTop()
        {
            Name = name,
            GUID = guid,
            Type = typeof(T),
            Prefab = prefab,
            Tribe = tribe
        });
    }

    private static CustomTotemTop Add(CustomTotemTop totem)
    {
        totemTops.Add(totem);
        return totem;
    }

    private const string CustomTotemTopID = "TotemPieces/TotemTop_Custom";
    private const string CustomTotemTopResourcePath = "Prefabs/Items/" + CustomTotemTopID;

    private static CustomTotemTop defaultTotemTop = null;
    private readonly static List<CustomTotemTop> totemTops = new();

    /// <summary>
    /// A collection of all new totem tops added using the API.
    /// </summary>
    public readonly static ReadOnlyCollection<CustomTotemTop> NewTotemTops = new(totemTops);

    /// <summary>
    /// Totem top that is used for custom tribes if no custom model is provided
    /// </summary>
    public static CustomTotemTop DefaultTotemTop => defaultTotemTop;

    [Obsolete("Obsolete. Use SetDefaultTotemTop<T> instead to ensure the totem top is set up correctly.")]
    public static void SetDefaultTotemTop(GameObject gameObject)
    {
        if (defaultTotemTop == null)
            InitializeDefaultTotemTop();

        defaultTotemTop.Prefab = gameObject;
        GameObject.DontDestroyOnLoad(gameObject);
    }

    public static void SetDefaultTotemTop<T>(GameObject gameObject) where T : CompositeTotemPiece
    {
        if (defaultTotemTop == null)
            InitializeDefaultTotemTop();

        // Attach missing components
        SetupTotemTopPrefab(gameObject, typeof(T));

        defaultTotemTop.Prefab = gameObject;
    }

    private static void InitializeDefaultTotemTop()
    {
        byte[] resourceBytes = TextureHelper.GetResourceBytes("customtotemtop", typeof(InscryptionAPIPlugin).Assembly);
        if (AssetBundleHelper.TryGet(resourceBytes, "CustomTotemTop", out GameObject go))
        {
            defaultTotemTop = NewTopPiece<CustomIconTotemTopPiece>("DefaultTotemTop",
                InscryptionAPIPlugin.ModGUID,
                Tribe.None,
                go
            );
            GameObject.DontDestroyOnLoad(go);
        }
    }

    private static void Initialize()
    {
        // Don't change any totems!
        if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemTopState.Vanilla)
            return;

        if (defaultTotemTop == null)
            InitializeDefaultTotemTop();

        // Add all totem tops to the game
        foreach (CustomTotemTop totem in totemTops)
        {
            string path = "Prefabs/Items/TotemPieces/TotemTop_" + totem.Tribe;
            if (totem == defaultTotemTop)
                path = CustomTotemTopResourcePath;

            GameObject prefab = totem.Prefab;
            if (prefab == null)
            {
                InscryptionAPIPlugin.Logger.LogError($"Cannot load NewTopPiece for {totem.GUID}.{totem.Name}. Prefab is null!");
                continue;
            }

            // Attach missing components
            SetupTotemTopPrefab(prefab, totem.Type);

            // Add to resources so it can be part of the pool
            ResourceBank.instance.resources.Add(new ResourceBank.Resource()
            {
                path = path,
                asset = prefab
            });
        }

    }
    private static void SetupTotemTopPrefab(GameObject prefab, Type scriptType)
    {
        // Add require components in case the prefab doesn't have them
        if (prefab.GetComponent<CompositeTotemPiece>() == null)
            prefab.AddComponent(scriptType);

        if (prefab.GetComponent<Animator>() == null)
        {
            Animator addComponent = prefab.AddComponent<Animator>();
            addComponent.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("animation/items/ItemAnim");
            addComponent.Rebind();
        }

        // Mark as don't destroy on load so it doesn't get removed between levels
        UnityObject.DontDestroyOnLoad(prefab);
    }

    public class CustomTotemTop
    {
        public string Name;
        public string GUID;
        public Type Type = typeof(CustomIconTotemTopPiece);
        public GameObject Prefab;
        public Tribe Tribe;
    }
}
