using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.CardCosts;
using Sirenix.Serialization.Utilities;
using System.Runtime.CompilerServices;

namespace InscryptionAPI.Card;

[HarmonyPatch]
public static class CardModificationInfoManager
{
    internal const string PROPERTIES = "[Properties:";
    internal const string CUSTOM_COSTS = "[CustomCosts:";

    private class CardModExt // Needs to be defined first so the implicit static constructor works correctly
    {
        public readonly Dictionary<Type, object> TypeMap = new();
        public readonly Dictionary<string, string> StringMap = new();
    }
    private static readonly ConditionalWeakTable<CardModificationInfo, CardModExt> CardModExtensionProperties = new();

    /// <summary>
    /// Get a custom extension class that will exist on all clones of a card
    /// </summary>
    /// <param name="card">Card to access.</param>
    /// <typeparam name="T">The custom class</typeparam>
    /// <returns>The instance of T for this card.</returns>
    public static T GetExtendedClass<T>(this CardModificationInfo card) where T : class, new()
    {
        var typeMap = CardModExtensionProperties.GetOrCreateValue(card).TypeMap;
        if (typeMap.TryGetValue(typeof(T), out object tObj))
            return (T)tObj;

        else
        {
            T tInst = new();
            typeMap[typeof(T)] = tInst;
            return tInst;
        }
    }

    internal static Dictionary<string, string> GetCardModExtensionTable(this CardModificationInfo mod)
    {
        if (CardModExtensionProperties.TryGetValue(mod, out var ext))
            return ext.StringMap;

        var exp = new CardModExt();
        CardModExtensionProperties.Add(mod, exp);

        // Since this is the first time building the extension properties table,
        // let's initialize it with anything that might be in this mod already.
        // Most likely this is coming from the save file.
        if (mod.HasCustomPropertiesId())
        {
            foreach (string property in GetCustomPropertiesFromId(mod.singletonId))
            {
                string[] split = property.Split(',');
                exp.StringMap[split[0]] = split[1];
            }
        }
        if (mod.HasCustomCostsId())
        {
            foreach (string pair in GetCustomCostsFromId(mod.singletonId))
            {
                string[] splitCost = pair.Split(',');
                exp.StringMap[splitCost[0]] = splitCost[1];
            }
        }

        return exp.StringMap;
    }
    public static void SyncCardMods()
    {
        // Re-add persistent properties
        foreach (CardInfo card in CardManager.AllCardsCopy)
        {
            foreach (CardModificationInfo mod in card.Mods.Where(x => x.HasCustomPropertiesId()))
            {
                foreach (string property in GetCustomPropertiesFromId(mod.singletonId))
                {
                    string[] split = property.Split(',');
                    mod.SetExtendedProperty(split[0], split[1]);
                }
            }
            foreach (CardModificationInfo mod in card.Mods.Where(x => x.HasCustomCostsId()))
            {
                foreach (string pair in GetCustomCostsFromId(mod.singletonId))
                {
                    string[] splitCost = pair.Split(',');
                    mod.SetExtendedProperty(splitCost[0], splitCost[1]);
                }
            }
        }
    }
    public static List<string> GetCustomPropertiesFromId(string singletonId)
    {
        List<string> propertiesList = new();
        string customProperties = GetCustomPropertiesIdString(singletonId);
        if (customProperties != null)
        {
            customProperties = customProperties.Replace(PROPERTIES, "");
            if (customProperties.EndsWith("]"))
            {
                customProperties = customProperties.Remove(customProperties.Length - 1);
                customProperties.Split(';').ForEach(propertiesList.Add);
            }
        }
        return propertiesList;
    }
    public static List<string> GetCustomCostsFromId(string singletonId)
    {
        List<string> costList = new();
        string customCosts = GetCustomCostsIdString(singletonId);

        if (customCosts != null)
        {
            customCosts = customCosts.Replace(CUSTOM_COSTS, "");
            if (customCosts.EndsWith("]"))
            {
                customCosts = customCosts.Remove(customCosts.Length - 1);
                customCosts.Split(';').ForEach(costList.Add);
            }
        }
        return costList;
    }

    public static string GetCustomPropertiesIdString(CardModificationInfo mod) => mod.HasCustomPropertiesId() ? GetCustomPropertiesIdString(mod.singletonId) : null;
    public static string GetCustomPropertiesIdString(string singletonId)
    {
        if (!singletonId.HasCustomPropertiesId())
            return null;

        int startOf = singletonId.IndexOf(PROPERTIES);
        string propertiesString = singletonId.Substring(startOf);
        int endOf = propertiesString.IndexOf("]");

        return propertiesString.Substring(0, endOf + 1);
    }

    public static string GetCustomCostsIdString(CardModificationInfo mod) => mod.HasCustomCostsId() ? GetCustomCostsIdString(mod.singletonId) : null;
    public static string GetCustomCostsIdString(string singletonId)
    {
        if (!singletonId.HasCustomCostsId())
            return null;

        int startOf = singletonId.IndexOf(CUSTOM_COSTS);
        string costsString = singletonId.Substring(startOf);
        int endOf = costsString.IndexOf("]");

        return costsString.Substring(0, endOf + 1);
    }

    /// <summary>
    /// Retrieves the property name and associated value from the provided 'properties' string.
    /// </summary>
    /// <param name="properties">A string representing all properties.</param>
    /// <param name="key">The property name to search for.</param>
    /// <returns>A string containing the key and the value separated by a comma like so: 'key,value'</returns>
    public static string GetIdKeyPair(string properties, string key)
    {
        string retval = properties.Substring(properties.IndexOf(key + ","));
        int endIndex = retval.IndexOf(";");
        if (endIndex == -1)
            endIndex = retval.IndexOf(']');

        return retval.Substring(0, endIndex);
    }

    #region Patches
    [HarmonyPatch(typeof(CardModificationInfo), nameof(CardModificationInfo.Clone))]
    [HarmonyPostfix]
    private static void ClonePostfix(CardModificationInfo __instance, ref object __result)
    {
        // just ensures that clones of a card mod have the same extension properties
        CardModExtensionProperties.Add((CardModificationInfo)__result, CardModExtensionProperties.GetOrCreateValue(__instance));
    }

    [HarmonyPatch(typeof(DiskCardGame.Card), nameof(DiskCardGame.Card.SetInfo))]
    [HarmonyPrefix]
    private static void AddCustomCostsFromMods(CardInfo info)
    {
        List<string> customCostNames = CardCostManager.AllCustomCosts.Select(x => x.CostName).ToList();
        foreach (CardModificationInfo mod in info.Mods)
        {
            foreach (string pair in GetCustomPropertiesFromId(mod.singletonId))
            {
                string[] splitCost = pair.Split(',');
                //InscryptionAPIPlugin.Logger.LogInfo($"Property: [{splitCost[0]}] [{splitCost[1]}]");
            }
            foreach (string pair in GetCustomCostsFromId(mod.singletonId))
            {
                string[] splitCost = pair.Split(',');
                //InscryptionAPIPlugin.Logger.LogInfo($"CustomCost: [{splitCost[0]}] [{splitCost[1]}]");
            }
        }
    }
    [HarmonyPatch(typeof(CardTriggerHandler), nameof(CardTriggerHandler.RemoveAbility))]
    [HarmonyPrefix]
    private static bool FixedRemoveTemporaryMod(CardTriggerHandler __instance, Ability ability)
    {
        Tuple<Ability, AbilityBehaviour> tuple = __instance.triggeredAbilities.Find(x => x.Item1 == ability);
        if (tuple != null)
        {
            __instance.triggeredAbilities.Remove(tuple);
            if (!__instance.triggeredAbilities.Exists(x => x.Item1 == ability)) // if there are no remaining triggeredAbilities, destroy the component
                UnityObject.Destroy(tuple.Item2);
        }
        return false;
    }
    #endregion
}
