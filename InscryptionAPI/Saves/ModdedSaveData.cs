namespace InscryptionAPI.Saves;

public class ModdedSaveData
{
    internal Dictionary<string, Dictionary<string, object>> SaveData = new();

    /// <summary>
    /// Get the value of a key as an object in the save data.
    /// </summary>
    /// <param name="guid">The GUID of the mod.</param>
    /// <param name="key">The key to get the value of.</param>
    /// <returns>The value of the key as an object.</returns>
    /// <typeparam name="T">The type of object you are getting.</typeparam>
    public T GetValueAsObject<T>(string guid, string key)
    {
        if (SaveData == null)
            SaveData = new();

        if (!SaveData.ContainsKey(guid))
            SaveData.Add(guid, new());

        if (!SaveData[guid].ContainsKey(key))
            SaveData[guid].Add(key, null);

        return (T)SaveData[guid][key];
    }

    public bool TryGetGuidAndKey(string prefix, string enumValue, out string guid, out string key)
    {
        foreach (KeyValuePair<string, Dictionary<string, object>> pair in SaveData)
        {
            foreach (KeyValuePair<string, object> valuePair in pair.Value)
            {
                if (valuePair.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    if (valuePair.Value.Equals(enumValue))
                    {
                        // Ability_jamesgames.inscryption.starcraftcore_Abduct
                        // to
                        // guid: jamesgames.inscryption.starcraftcore
                        // key: Abduct
                        string name = valuePair.Key.Substring(prefix.Length + 1);
                        int underscoreIndex = name.IndexOf('_');
                        guid = name.Substring(0, underscoreIndex);
                        key = name.Substring(underscoreIndex + 1);
                        return true;
                    }
                }
            }
        }

        guid = null;
        key = null;
        return false;
    }

    /// <summary>
    /// Get the value of a key as a string in the save data.
    /// </summary>
    /// <param name="guid">The GUID of the mod.</param>
    /// <param name="key">The key to get the value of.</param>
    /// <returns>The value of the key as a string.</returns>
    public string GetValue(string guid, string key)
    {
        var value = GetValueAsObject<object>(guid, key);
        return value == null ? default(string) : value.ToString();
    }

    /// <summary>
    /// Get the value of a key as an integer in the save data.
    /// </summary>
    /// <param name="guid">The GUID of the mod.</param>
    /// <param name="key">The key to get the value of.</param>
    /// <returns>The value of the key as an integer.</returns>
    public int GetValueAsInt(string guid, string key)
    {
        string value = GetValue(guid, key);
        int result;
        int.TryParse(value, out result);
        return result;
    }

    /// <summary>
    /// Get the value of a key as a float in the save data.
    /// </summary>
    /// <param name="guid">The GUID of the mod.</param>
    /// <param name="key">The key to get the value of.</param>
    /// <returns>The value of the key as a float.</returns>
    public float GetValueAsFloat(string guid, string key)
    {
        string value = GetValue(guid, key);
        float result;
        float.TryParse(value, out result);
        return result;
    }

    /// <summary>
    /// Get the value of a key as a boolean in the save data.
    /// </summary>
    /// <param name="guid">The GUID of the mod.</param>
    /// <param name="key">The key to get the value of.</param>
    /// <returns>The value of the key as a boolean.</returns>
    public bool GetValueAsBoolean(string guid, string key)
    {
        string value = GetValue(guid, key);
        bool result;
        bool.TryParse(value, out result);
        return result;
    }

    /// <summary>
    /// Set the value of a key as an object in the save data,
    /// It's recommended to not save an object that implements Unity's Object class as it can cause a infinite recursion and crash the game.
    /// </summary>
    /// <param name="guid">The GUID of the mod.</param>
    /// <param name="key">The key to set the value of.</param>
    /// <param name="value">The object value to set.</param>
    /// <typeparam name="T">The type of object you are setting.</typeparam>
    public void SetValueAsObject<T>(string guid, string key, T value)
    {
        if (SaveData == null)
            SaveData = new();

        if (!SaveData.ContainsKey(guid))
            SaveData.Add(guid, new());

        if (!SaveData[guid].ContainsKey(key))
            SaveData[guid].Add(key, value);
        else
            SaveData[guid][key] = value;
    }

    /// <summary>
    /// Set the value of a key in the save data.
    /// </summary>
    /// <param name="guid">The GUID of the mod.</param>
    /// <param name="key">The key to set the value of.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string guid, string key, object value)
    {
        SetValueAsObject(guid, key, value == null ? default(string) : value.ToString());
    }
}
