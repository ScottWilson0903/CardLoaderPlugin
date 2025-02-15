using System.Runtime.CompilerServices;

namespace InscryptionAPI.Helpers;

/// <summary>
/// Allows for an easy way to add custom fields to an object, or to an entire class.
/// </summary>
public static class CustomFields
{
    static ConditionalWeakTable<object, Dictionary<string, object>> objectFields = new ConditionalWeakTable<object, Dictionary<string, object>>();

    /// <summary>
    /// Returns a custom field.
    /// </summary>
    /// <typeparam name="T">The type of the custom field.</typeparam>
    /// <param name="obj">The object which has the custom field.</param>
    /// <param name="field">The name of the custom field.</param>
    /// <returns>A custom field of type T. If the field is not found, default(T) is returned.</returns>
    public static T Get<T>(object obj, string field)
    {
        if (!HasField(obj, field)) Set(obj, field, default(T));

        objectFields.TryGetValue(obj, out Dictionary<string, object> fields);
        return (T)fields[field];
    }

    /// <summary>
    /// Returns a static custom field.
    /// </summary>
    /// <typeparam name="T">The type of the custom field.</typeparam>
    /// <typeparam name="C">The class the static field is stored on.</typeparam>
    /// <param name="field">The name of the custom field.</param>
    /// <returns>A custom field of type T. If the field is not found, default(T) is returned.</returns>
    public static T GetStatic<T, C>(string field)
        => Get<T>(typeof(C), field);

    /// <summary>
    /// Returns a static custom field.
    /// </summary>
    /// <typeparam name="T">The type of the custom field.</typeparam>
    /// <param name="field">The name of the custom field.</param>
    /// <param name="classType">The type of the class the static field is stored on.</param>
    /// <returns>A custom field of type T. If the field is not found, default(T) is returned.</returns>
    public static T GetStatic<T>(string field, Type classType)
        => Get<T>(classType, field);

    /// <summary>
    /// Set a custom field.
    /// </summary>
    /// <param name="obj">The object which you want to store the custom field on.</param>
    /// <param name="field">The name of the custom field.</param>
    /// <param name="value">The value of the custom field.</param>
    public static void Set(object obj, string field, object value)
    {
        if (!objectFields.TryGetValue(obj, out Dictionary<string, object> fields))
        {
            fields = new Dictionary<string, object>();
            objectFields.Add(obj, fields);
        }

        fields[field] = value;
    }

    /// <summary>
    /// Set a static custom field.
    /// </summary>
    /// <typeparam name="C">The class which you want to store the static field is stored on.</typeparam>
    /// <param name="field">The name of the custom field.</param>
    /// <param name="value">The value of the custom field.</param>
    public static void SetStatic<C>(string field, object value)
        => Set(typeof(C), field, value);

    /// <summary>
    /// Set a static custom field.
    /// </summary>
    /// <param name="field">The name of the custom field.</param>
    /// <param name="value">The value of the custom field.</param>
    /// <param name="classType">The type of the class which you want to store the static field is stored on.</param>
    public static void SetStatic(string field, object value, Type classType)
        => Set(classType, field, value);

    /// <summary>
    /// Check if an object currently stores a custom field.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="field">The name of the custom field to check for.</param>
    /// <returns>True if the object is storing the custom field.</returns>
    public static bool HasField(object obj, string field)
    {
        if (!objectFields.TryGetValue(obj, out Dictionary<string, object> fields)) return false;
        return fields.ContainsKey(field);
    }

    /// <summary>
    /// Check if a class currently stores a static custom field.
    /// </summary>
    /// <typeparam name="C">The class which you want to check.</typeparam>
    /// <param name="field">The name of the static custom field to check for.</param>
    /// <returns>True if the class is storing the static custom field.</returns>
    public static bool HasStaticField<C>(string field)
        => HasField(typeof(C), field);

    /// <summary>
    /// Check if a class currently stores a static custom field.
    /// </summary>
    /// <param name="field">The name of the static custom field to check for.</param>
    /// <param name="classType">The type of the class which you want to check.</param>
    /// <returns>True if the class is storing the static custom field.</returns>
    public static bool HasStaticField(string field, Type classType)
        => HasField(classType, field);
}