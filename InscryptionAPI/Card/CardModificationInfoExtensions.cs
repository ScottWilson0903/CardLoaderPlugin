using DiskCardGame;
using InscryptionAPI.CardCosts;
using Sirenix.Utilities;
using static InscryptionAPI.Card.CardModificationInfoManager;
namespace InscryptionAPI.Card;

public static class CardModificationInfoExtensions
{
    public static bool HasDeathCardInfo(this CardModificationInfo mod) => mod.deathCardInfo != null;

    #region Adders
    public static CardModificationInfo AddDecalIds(this CardModificationInfo mod, params string[] decalIds)
    {
        foreach (string decalId in decalIds)
        {
            if (!mod.DecalIds.Contains(decalId))
                mod.DecalIds.Add(decalId);
        }
        return mod;
    }
    public static CardModificationInfo AddNegateAbilities(this CardModificationInfo mod, params Ability[] negateAbilities)
    {
        mod.negateAbilities ??= new();
        mod.negateAbilities.AddRange(negateAbilities);
        return mod;
    }
    #endregion

    #region Setters
    public static CardModificationInfo SetNameReplacement(this CardModificationInfo mod, string name = null)
    {
        mod.nameReplacement = name;
        return mod;
    }
    public static CardModificationInfo SetSingletonId(this CardModificationInfo mod, string id = null)
    {
        if (id != null)
            mod.singletonId = id;
        return mod;
    }
    public static CardModificationInfo SetAttackAndHealth(this CardModificationInfo mod, int attack = 0, int health = 0)
    {
        mod.attackAdjustment = attack;
        mod.healthAdjustment = health;
        return mod;
    }
    public static CardModificationInfo SetCosts(this CardModificationInfo mod, int blood = 0, int bones = 0, int energy = 0, List<GemType> gems = null)
    {
        return mod.SetBloodCost(blood).SetBonesCost(bones).SetEnergyCost(energy).SetGemsCost(gems);
    }
    public static CardModificationInfo SetBloodCost(this CardModificationInfo mod, int cost = 0)
    {
        mod.bloodCostAdjustment = cost;
        return mod;
    }
    public static CardModificationInfo SetBonesCost(this CardModificationInfo mod, int cost = 0)
    {
        mod.bonesCostAdjustment = cost;
        return mod;
    }
    public static CardModificationInfo SetEnergyCost(this CardModificationInfo mod, int cost = 0)
    {
        mod.energyCostAdjustment = cost;
        return mod;
    }
    public static CardModificationInfo SetGemsCost(this CardModificationInfo mod, List<GemType> cost = null)
    {
        if (cost != null)
            mod.addGemCost = cost;
        return mod;
    }
    public static CardModificationInfo SetGemsCost(this CardModificationInfo mod, params GemType[] cost)
    {
        if (cost.Length > 0)
            mod.addGemCost = cost.ToList();
        return mod;
    }
    public static CardModificationInfo AddAbilities(this CardModificationInfo mod, params Ability[] abilities)
    {
        mod.abilities.AddRange(abilities);
        return mod;
    }
    public static CardModificationInfo SetDeathCardPortrait(this CardModificationInfo mod,
        CompositeFigurine.FigurineType headType = CompositeFigurine.FigurineType.SettlerMan,
        int mouthIndex = 0, int eyesIndex = 0, bool lostEye = false)
    {
        mod.deathCardInfo = new(headType, mouthIndex, eyesIndex) { lostEye = lostEye };
        return mod;
    }

    #endregion

    /// <summary>
    /// Used by the API to remove temporary decal mods, since it's not done automatically.
    /// </summary>
    public static void SetTemporaryDecal(this CardModificationInfo mod) => mod.SetExtendedProperty("API:TemporaryDecal", true);
    public static bool IsTemporaryDecal(this CardModificationInfo mod) => mod.GetExtendedPropertyAsBool("API:TemporaryDecal") ?? false;

    #region Singleton ID

    /// <summary>
    /// Removes the custom properties field from a CardModificationInfo's singletonId.
    /// </summary>
    /// <param name="mod">The CardModificationInfo object whose properties we want to clear.</param>
    /// <returns>The same CardModificationInfo so a chain can continue.</returns>
    public static CardModificationInfo ClearCustomPropertiesId(this CardModificationInfo mod)
    {
        string properties = GetCustomPropertiesIdString(mod);
        if (properties != null)
            mod.singletonId = mod.singletonId.Replace(properties, "");

        return mod;
    }
    public static CardModificationInfo ClearCustomCostsId(this CardModificationInfo mod)
    {
        string costs = GetCustomCostsIdString(mod);
        if (costs != null)
            mod.singletonId = mod.singletonId.Replace(costs, "");

        return mod;
    }

    #region Properties
    public static bool HasCustomPropertyId(this CardModificationInfo mod, string propertyName) => HasCustomPropertiesId(mod.singletonId) && mod.singletonId.Contains(propertyName);
    public static bool HasCustomPropertyId(this string singletonId, string propertyName) => HasCustomPropertiesId(singletonId) && singletonId.Contains(propertyName);
    public static bool HasCustomPropertiesId(this CardModificationInfo mod) => !mod.singletonId.IsNullOrWhitespace() && mod.singletonId.Contains(PROPERTIES);
    public static bool HasCustomPropertiesId(this string singletonId) => !singletonId.IsNullOrWhitespace() && singletonId.Contains(PROPERTIES);
    public static CardModificationInfo SetPropertiesId(this CardModificationInfo mod, string propertyName, object value)
    {
        string valuePair = $"{propertyName},{value}";
        if (mod.HasCustomPropertiesId())
        {
            string newProperties;
            string oldProperties = GetCustomPropertiesIdString(mod); // get the current string of properties

            // if the property is already set
            if (oldProperties.Contains(propertyName))
            {
                string oldValue = mod.GetCustomPropertyId(propertyName);
                string currentPair = GetIdKeyPair(oldProperties, propertyName);

                newProperties = oldProperties.Replace(currentPair, currentPair.Replace(oldValue, value.ToString()));
            }
            else
            {
                newProperties = oldProperties.Remove(oldProperties.Length - 1) + ";" + valuePair + "]"; // add the new value pair to the end
            }

            mod.singletonId = mod.singletonId.Replace(oldProperties, newProperties);
        }
        else if (mod.singletonId.IsNullOrWhitespace())
        {
            mod.singletonId = PROPERTIES + valuePair + "]";
        }
        else
        {
            mod.singletonId += PROPERTIES + valuePair + "]";
        }
        return mod;
    }

    public static bool HasCustomProperty(this CardModificationInfo mod, string propertyName) => mod.GetCustomProperty(propertyName) != null;
    public static string GetCustomProperty(this CardModificationInfo mod, string propertyName)
    {
        return mod.GetExtendedProperty(propertyName) ?? mod.GetCustomPropertyId(propertyName);
    }
    public static string GetCustomPropertyId(this CardModificationInfo mod, string propertyName)
    {
        if (!mod.HasCustomPropertyId(propertyName))
            return null;

        string currentProperties = GetCustomPropertiesIdString(mod);
        int costIdx = currentProperties.IndexOf(propertyName + ",");
        if (costIdx == -1)
            return null;

        string pairString = currentProperties.Substring(costIdx);
        int valueIdx = pairString.IndexOf(';');
        if (valueIdx == -1) // if this is the last valuePair in the string
            valueIdx = pairString.IndexOf(']');

        return pairString.Substring(valueIdx).Replace(propertyName + ",", "");
    }

    #endregion

    #region CustomCosts
    public static bool HasCustomCostId(this CardModificationInfo mod, string costName) => HasCustomCostsId(mod.singletonId) && mod.singletonId.Contains(costName);
    public static bool HasCustomCostId(this string singletonId, string costName) => HasCustomCostsId(singletonId) && singletonId.Contains(costName);
    public static bool HasCustomCostsId(this CardModificationInfo mod) => !mod.singletonId.IsNullOrWhitespace() && mod.singletonId.Contains(CUSTOM_COSTS);
    public static bool HasCustomCostsId(this string singletonId) => !singletonId.IsNullOrWhitespace() && singletonId.Contains(CUSTOM_COSTS);

    public static CardModificationInfo SetCustomCostId(this CardModificationInfo mod, string costName, object value)
    {
        string valuePair = $"{costName},{value}";
        if (mod.HasCustomCostsId())
        {
            string newCosts;
            string oldCosts = GetCustomCostsIdString(mod); // get the current string of properties
            if (oldCosts.Contains(costName))
            {
                string oldValue = mod.GetCustomCostId(costName);
                string currentPair = GetIdKeyPair(oldCosts, costName);

                newCosts = oldCosts.Replace(currentPair, currentPair.Replace(oldValue, value.ToString()));
            }
            else
            {
                newCosts = oldCosts.Remove(oldCosts.Length - 1) + ";" + valuePair + "]"; // add the new value pair to the end
            }

            mod.singletonId = mod.singletonId.Replace(oldCosts, newCosts);
        }
        else if (mod.singletonId.IsNullOrWhitespace())
        {
            mod.singletonId = CUSTOM_COSTS + valuePair + "]";
        }
        else
        {
            mod.singletonId += CUSTOM_COSTS + valuePair + "]";
        }
        return mod;
    }

    public static bool HasCustomCost(this CardModificationInfo mod, string costName) => (mod.GetExtendedProperty(costName) ?? mod.GetCustomCostId(costName)) != null;
    public static int GetCustomCost(this CardModificationInfo mod, string costName)
    {
        return mod.GetExtendedPropertyAsInt(costName) ?? mod.GetCustomCostIdValue(costName);
    }
    public static string GetCustomCostId(this CardModificationInfo mod, string costName)
    {
        if (!mod.HasCustomCostId(costName))
            return null;

        string currentCosts = GetCustomCostsIdString(mod);
        int costIdx = currentCosts.IndexOf(costName + ",");
        if (costIdx == -1)
            return null;

        string pairString = currentCosts.Substring(costIdx);

        int valueIdx = pairString.IndexOf(';');
        if (valueIdx == -1) // if this is the last valuePair in the string
            valueIdx = pairString.IndexOf(']');

        return pairString.Substring(0, valueIdx).Replace(costName + ",", "");
    }
    public static int GetCustomCostIdValue(this CardModificationInfo mod, string costName)
    {
        string valueStr = mod.GetCustomCostId(costName);
        if (valueStr == null)
            return 0;

        return int.TryParse(valueStr, out int result) ? result : 0;
    }
    public static List<CardCostManager.FullCardCost> GetCustomCostsFromMod(this CardModificationInfo mod)
    {
        List<CardCostManager.FullCardCost> costs = new();
        foreach (string key in mod.GetCardModExtensionTable().Keys)
        {
            if (CardCostManager.AllCustomCosts.Exists(x => x.CostName == key))
                costs.Add(CardCostManager.AllCustomCosts.Find(x => x.CostName == key));
        }
        if (mod.HasCustomPropertiesId())
        {
            List<string> modCosts = new();
            CardModificationInfoManager.GetCustomCostsFromId(mod.singletonId).ForEach(x => modCosts.Add(x.Split(',')[0]));
            costs.AddRange(CardCostManager.AllCustomCosts.Where(x => !costs.Contains(x) && modCosts.Contains(x.CostName)));
        }
        return costs;
    }

    #endregion

    #region ID Getters
    public static string CleanId(this CardModificationInfo mod) => mod.IdWithoutCosts().IdWithoutProperties();
    public static string IdWithoutProperties(this CardModificationInfo mod) => mod.singletonId.IdWithoutProperties();
    public static string IdWithoutProperties(this string singletonId)
    {
        if (!singletonId.HasCustomPropertiesId())
            return singletonId;

        string properties = GetCustomPropertiesIdString(singletonId);
        return singletonId.Replace(properties, "");
    }
    public static string IdWithoutCosts(this CardModificationInfo mod) => mod.singletonId.IdWithoutCosts();
    public static string IdWithoutCosts(this string singletonId)
    {
        if (!singletonId.HasCustomCostsId())
            return singletonId;

        string costs = GetCustomCostsIdString(singletonId);
        return singletonId.Replace(costs, "");
    }

    #endregion

    #endregion

    #region Extended Properties

    /// <summary>
    /// Adds a custom property value to the CardModificationInfo.
    /// </summary>
    /// <param name="mod">CardModificationInfo to access.</param>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value of the property.</param>
    /// <param name="addToSingletonId">Appends a string containing the custom property name and value to the mod's singletonId in the forme of "[ExtProperty:{propertyName},{value}:ExtProperty]".</param>
    /// <returns>The same CardModificationInfo so a chain can continue.</returns>
    public static CardModificationInfo SetExtendedProperty(this CardModificationInfo mod, string propertyName, object value, bool addToSingletonId = false)
    {
        mod.GetCardModExtensionTable()[propertyName] = value?.ToString();
        if (addToSingletonId)
            mod.SetPropertiesId(propertyName, value);

        return mod;
    }

    /// <summary>
    /// Gets a custom property value from the card.
    /// </summary>
    /// <param name="mod">CardModificationInfo to access.</param>
    /// <param name="propertyName">The name of the property to get the value of.</param>
    /// <returns>The custom property value as a string. If it doesn't exist, returns null.</returns>
    public static string GetExtendedProperty(this CardModificationInfo mod, string propertyName)
    {
        mod.GetCardModExtensionTable().TryGetValue(propertyName, out var ret);
        return ret;
    }

    /// <summary>
    /// Gets a custom property as an int (can be null).
    /// </summary>
    /// <param name="mod">CardModificationInfo to access.</param>
    /// <param name="propertyName">Property name to get value of.</param>
    /// <returns>Returns the value of the property as an int or null if it didn't exist or couldn't be parsed as int.</returns>
    public static int? GetExtendedPropertyAsInt(this CardModificationInfo mod, string propertyName)
    {
        mod.GetCardModExtensionTable().TryGetValue(propertyName, out var str);
        return int.TryParse(str, out var ret) ? ret : null;
    }

    /// <summary>
    /// Gets a custom property as a float (can be null).
    /// </summary>
    /// <param name="mod">CardModificationInfo to access.</param>
    /// <param name="propertyName">Property name to get value of.</param>
    /// <returns>Returns the value of the property as a float or null if it didn't exist or couldn't be parsed as float.</returns>
    public static float? GetExtendedPropertyAsFloat(this CardModificationInfo mod, string propertyName)
    {
        mod.GetCardModExtensionTable().TryGetValue(propertyName, out var str);
        return float.TryParse(str, out var ret) ? ret : null;
    }

    /// <summary>
    /// Gets a custom property as a boolean (can be null).
    /// </summary>
    /// <param name="mod">CardModificationInfo to access.</param>
    /// <param name="propertyName">Property name to get value of.</param>
    /// <returns>Returns the value of the property as a boolean or null if it didn't exist or couldn't be parsed as boolean.</returns>
    public static bool? GetExtendedPropertyAsBool(this CardModificationInfo mod, string propertyName)
    {
        mod.GetCardModExtensionTable().TryGetValue(propertyName, out var str);
        return bool.TryParse(str, out var ret) ? ret : null;
    }
    #endregion

    #region RemoveGems
    public static void ClearAllRemovedGemsCosts(this CardModificationInfo mod)
    {
        if (mod.HasRemovedGreenGemCost())
            mod.RemoveGreenGemCost(false);

        if (mod.HasRemovedOrangeGemCost())
            mod.RemoveOrangeGemCost(false);

        if (mod.HasRemoveBlueGemCost())
            mod.RemoveBlueGemCost(false);
    }
    public static List<GemType> RemovedGemsCosts(this CardModificationInfo mod)
    {
        List<GemType> gems = new();
        if (mod.HasRemovedGreenGemCost())
            gems.Add(GemType.Green);

        if (mod.HasRemovedOrangeGemCost())
            gems.Add(GemType.Orange);

        if (mod.HasRemoveBlueGemCost())
            gems.Add(GemType.Blue);

        return gems;
    }
    public static CardModificationInfo RemoveGemsCost(this CardModificationInfo mod, params GemType[] gemTypes)
    {
        if (gemTypes.Contains(GemType.Green))
            mod.RemoveGreenGemCost(true);

        if (gemTypes.Contains(GemType.Orange))
            mod.RemoveOrangeGemCost(true);

        if (gemTypes.Contains(GemType.Blue))
            mod.RemoveBlueGemCost(true);

        return mod;
    }
    public static CardModificationInfo ClearRemovedGemsCost(this CardModificationInfo mod, params GemType[] gemTypes)
    {
        if (gemTypes.Contains(GemType.Green))
            mod.RemoveGreenGemCost(false);

        if (gemTypes.Contains(GemType.Orange))
            mod.RemoveOrangeGemCost(false);

        if (gemTypes.Contains(GemType.Blue))
            mod.RemoveBlueGemCost(false);

        return mod;
    }
    public static CardModificationInfo RemoveGreenGemCost(this CardModificationInfo mod, bool remove) => mod.SetExtendedProperty("RemoveGreenGem", remove, true);
    public static CardModificationInfo RemoveOrangeGemCost(this CardModificationInfo mod, bool remove) => mod.SetExtendedProperty("RemoveOrangeGem", remove, true);
    public static CardModificationInfo RemoveBlueGemCost(this CardModificationInfo mod, bool remove) => mod.SetExtendedProperty("RemoveBlueGem", remove, true);
    public static bool HasRemovedGreenGemCost(this CardModificationInfo mod) => mod.GetExtendedPropertyAsBool("RemoveGreenGem") ?? false;
    public static bool HasRemovedOrangeGemCost(this CardModificationInfo mod) => mod.GetExtendedPropertyAsBool("RemoveOrangeGem") ?? false;
    public static bool HasRemoveBlueGemCost(this CardModificationInfo mod) => mod.GetExtendedPropertyAsBool("RemoveBlueGem") ?? false;

    public static bool HasRemovedAnyGemCost(this CardModificationInfo mod) => mod.HasRemovedGreenGemCost() || mod.HasRemovedOrangeGemCost() || mod.HasRemoveBlueGemCost();
    public static bool HasRemovedGemCost(this CardModificationInfo mod, GemType gemType)
    {
        return gemType switch
        {
            GemType.Green => mod.HasRemovedGreenGemCost(),
            GemType.Orange => mod.HasRemovedOrangeGemCost(),
            GemType.Blue => mod.HasRemoveBlueGemCost(),
            _ => false,
        };
    }
    #endregion
}
