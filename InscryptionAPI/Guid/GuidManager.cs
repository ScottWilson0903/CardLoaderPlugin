using InscryptionAPI.Saves;

namespace InscryptionAPI.Guid;

public static class GuidManager
{
    public static string GetFullyQualifiedName(string guid, string value)
    {
        return $"{guid}_{value}";
    }

    private static readonly Dictionary<int, Type> reverseMapper = new();

    private static readonly object lockObject = new object();

    public const int START_INDEX = 1000;

    public const string MAX_DATA = "maximumStoredValueForEnum";

    public static Type GetEnumType(int number)
    {
        reverseMapper.TryGetValue(number, out var res);
        return res;
    }

    /// <summary>
    /// Gets a list of created Enum values of the provided Enum type.
    /// </summary>
    /// <typeparam name="T">The Enum type to search for.</typeparam>
    /// <returns>The list of Enum values created by the API.</returns>
    unsafe public static List<T> GetValues<T>() where T : unmanaged, System.Enum
    {
        List<T> itemList = new();
        foreach (T item in Enum.GetValues(typeof(T)))
            itemList.Add(item);

        string startKey = typeof(T).Name + "_";
        foreach (var item in ModdedSaveManager.SaveData.SaveData[InscryptionAPIPlugin.ModGUID])
        {
            if (item.Key.StartsWith(startKey))
            {
                int enumVal = int.Parse((string)item.Value);
                T convertedEnumVal = *(T*)&enumVal;
                itemList.Add(convertedEnumVal);
            }
        }

        return itemList;
    }

    unsafe public static T GetEnumValue<T>(string guid, string value) where T : unmanaged, System.Enum
    {
        if (sizeof(T) != sizeof(int))
            throw new NotSupportedException($"Cannot manage values of type {typeof(T).Name} in GuidManager.GetEnumValue");

        string saveKey = $"{typeof(T).Name}_{guid}_{value}";

        int enumValue = ModdedSaveManager.SaveData.GetValueAsInt(InscryptionAPIPlugin.ModGUID, saveKey);
        if (enumValue == default)
        {
            lock (lockObject)
            {
                enumValue = ModdedSaveManager.SaveData.GetValueAsInt(InscryptionAPIPlugin.ModGUID, MAX_DATA);
                if (enumValue < START_INDEX)
                    enumValue = START_INDEX;

                ModdedSaveManager.SaveData.SetValue(InscryptionAPIPlugin.ModGUID, MAX_DATA, enumValue + 1);
                ModdedSaveManager.SaveData.SetValue(InscryptionAPIPlugin.ModGUID, saveKey, enumValue);

                ModdedSaveManager.isSystemDirty = true;
            }
        }

        reverseMapper[enumValue] = typeof(T);
        return *(T*)&enumValue;
    }

    public static bool TryGetGuidAndKeyEnumValue<T>(T value, out string guid, out string key) where T : System.Enum
    {
        string prefix = $"{typeof(T).Name}";
        return ModdedSaveManager.SaveData.TryGetGuidAndKey(prefix, value.ToString(), out guid, out key);
    }
}