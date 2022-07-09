using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles the process of deserializing object properties from JSON
/// </summary>
/// <remarks>
///   <para>
///     Turns out this is actually complex as you don't want to deserialize the child tree too deep too soon so
///     this helper allows reading properties in a piecemeal fashion, which is needed for the way we set IDs.
///   </para>
/// </remarks>>
public class InProgressObjectDeserialization
{
    public Type? DynamicType;

    private readonly Type staticType;
    private readonly JsonReader reader;
    private readonly JsonSerializer serializer;

    private readonly Func<string, bool> skipMember;

    private readonly List<string> customFieldNames = new();

    private object? createdInstance;

    private List<FieldInfo>? instanceFields;
    private List<PropertyInfo>? instanceProperties;

    private List<(string Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo)?>?
        readButNotConsumedProperties;

    private List<(string Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo)?>?
        pendingCustomFields;

    private bool allowCustomFieldRead;
    private bool reachedEnd;

    public InProgressObjectDeserialization(Type staticType, JsonReader reader, JsonSerializer serializer,
        Func<string, bool> skipMember)
    {
        this.staticType = staticType;
        this.reader = reader;
        this.serializer = serializer;
        this.skipMember = skipMember;
    }

    public static void RunPrePropertyDeserializeActions(object instance)
    {
        HandleSaveLoadedTracked(instance);
    }

    public static void HandleSaveLoadedTracked(object instance)
    {
        // Any loaded object supports knowing if it was loaded from a save
        if (instance is ISaveLoadedTracked tracked)
        {
            tracked.IsLoadedFromSave = true;
        }
    }

    public (string? Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo) ReadNextProperty(
        bool ignoreNotConsumed = false)
    {
        // If there are pending reads, return those first
        if (!ignoreNotConsumed && readButNotConsumedProperties != null)
        {
            if (readButNotConsumedProperties.Count <= 0)
            {
                readButNotConsumedProperties = null;
            }
            else
            {
                var value = readButNotConsumedProperties[0] ?? throw new Exception("value may not be null");

                // There's probably not that many items that using a deque here makes that much sense
                // And see below in ReadPropertiesUntil, items can be removed from the middle anyway
                readButNotConsumedProperties.RemoveAt(0);

                if (readButNotConsumedProperties.Count < 1)
                    readButNotConsumedProperties = null;

                return value;
            }
        }

        if (reachedEnd)
            return (null, null, null, null);

        // Try to find a non-ignored property in the json data
        // This is a while true loop here as when we start reading the next thing, we need to call Read on
        // the reader first
        while (true)
        {
            reader.Read();

            if (reader.TokenType == JsonToken.EndObject)
                break;

            if (reader.TokenType != JsonToken.PropertyName)
            {
                throw new JsonException($"Unexpected token of type: {reader.TokenType} at {reader.Path}");
            }

            var name = (string?)reader.Value ?? throw new JsonException("No name in property");

            if (BaseThriveConverter.IsSpecialProperty(name))
                return (name, reader.ReadAsString(), null, null);

            // Ignore properties we don't want to read (and use for something)
            var isCustom = customFieldNames.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!IsPropertyUseful(name, out var valueType, out var field, out var property) && !isCustom)
            {
                reader.Read();

                GD.PrintErr("Ignoring save property at: ", reader.Path);

                // Seems like reader.Skip is really hard to use so we need to deserialize some stuff here which we'll
                // ignore then
                serializer.Deserialize(reader);
            }
            else
            {
                // Load this property's value
                reader.Read();
                var readValue = (name, serializer.Deserialize(reader, valueType), field, property);

                if (isCustom && !allowCustomFieldRead)
                {
                    // Too early to be reading a custom field
                    pendingCustomFields ??=
                        new List<(string Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo)?>();

                    pendingCustomFields.Add(readValue);
                }

                return readValue;
            }
        }

        return (null, null, null, null);
    }

    /// <summary>
    ///   Gets an already parsed custom field. This should be the way to access custom fields as the default handling
    ///   always reads all properties of objects.
    /// </summary>
    /// <param name="name">The name of the field to look for</param>
    /// <returns>The field if found, nulls otherwise</returns>
    public (string? Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo)
        GetCustomProperty(string name)
    {
        if (!allowCustomFieldRead)
            throw new InvalidOperationException("Custom field read is not valid yet");

        // Search in not consumed reads first
        var value = pendingCustomFields?.FirstOrDefault(t =>
            t!.Value.Name!.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (value != null)
        {
            pendingCustomFields!.Remove(value);
            return value.Value;
        }

        return (null, null, null, null);
    }

    /// <summary>
    ///   Tries to find a property by name
    /// </summary>
    /// <param name="lookForName">The name to look for</param>
    /// <returns>The found field, or nulls if it cannot be found</returns>
    public (string? Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo) ReadPropertiesUntil(
        string lookForName)
    {
        (string Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo)? alreadyReadValue;

        // Search in not consumed reads first
        if (allowCustomFieldRead && pendingCustomFields != null)
        {
            alreadyReadValue = pendingCustomFields.FirstOrDefault(t =>
                t!.Value.Name!.Equals(lookForName, StringComparison.OrdinalIgnoreCase));

            if (alreadyReadValue != null)
            {
                pendingCustomFields!.Remove(alreadyReadValue);
                return alreadyReadValue.Value;
            }
        }

        alreadyReadValue = readButNotConsumedProperties?.FirstOrDefault(t =>
            t!.Value.Name!.Equals(lookForName, StringComparison.OrdinalIgnoreCase));

        if (alreadyReadValue != null)
        {
            readButNotConsumedProperties!.Remove(alreadyReadValue);
            return alreadyReadValue.Value;
        }

        while (true)
        {
            var property = ReadNextProperty(true);

            // Assume reading only fails if there is nothing more to read
            if (property.Name == null)
                break;

            if (BaseThriveConverter.IsSpecialProperty(property.Name))
            {
                throw new JsonException(
                    "Seeing a special field name while looking for specific fields is not valid data");
            }

            if (property.Name.Equals(lookForName, StringComparison.OrdinalIgnoreCase))
            {
                // Found what we were looking for
                return property;
            }

            // Not what we wanted, so we need to save it for later
            readButNotConsumedProperties ??=
                new List<(string Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo)?>();

            readButNotConsumedProperties.Add(property!);
        }

        return (null, null, null, null);
    }

    /// <summary>
    ///   Puts a value to the list of next values to read
    /// </summary>
    public void OfferPotentiallyConstructorParameter(
        (string Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo) property)
    {
        readButNotConsumedProperties ??=
            new List<(string Name, object? Value, FieldInfo? FieldInfo, PropertyInfo? PropertyInfo)?>();

        readButNotConsumedProperties.Add(property);
    }

    public void LoadProperties()
    {
        var type = DynamicType ?? staticType;

        instanceFields ??= BaseThriveConverter.FieldsOf(type).ToList();

        instanceProperties ??= BaseThriveConverter.PropertiesOf(type).ToList();
    }

    public bool PropertiesAlreadyLoaded()
    {
        return instanceFields != null || instanceProperties != null;
    }

    public object GetInstance()
    {
        if (createdInstance == null)
            CreateInstance();

        return createdInstance!;
    }

    public void ReceiveInstance(object instance)
    {
        if (createdInstance != null)
            throw new InvalidOperationException("Already has an instance");

        createdInstance = instance;
        HandleSaveLoadedTracked(instance);
        RunPrePropertyDeserializeActions(createdInstance);
    }

    public void MarkStartCustomFields()
    {
        allowCustomFieldRead = true;
    }

    public void DisallowFurtherReading()
    {
        reachedEnd = true;
    }

    /// <summary>
    ///   Must be used to inform this of all custom used fields (that are written by custom serializers) otherwise
    ///   this will just skip reading them.
    /// </summary>
    /// <param name="propertyName">The name of the field</param>
    public void RegisterExtraField(string propertyName)
    {
        customFieldNames.Add(propertyName);
    }

    private bool Skip(string name)
    {
        return skipMember(name);
    }

    /// <summary>
    ///   Finds the actual key for a thing ignoring different cases
    /// </summary>
    /// <param name="candidateKey">Key to test with if it can be found</param>
    /// <param name="type">The type of the found field</param>
    /// <param name="fieldInfo">If the name matched a field, returns that field's info</param>
    /// <param name="propertyInfo">If the name matched a property, returns that property's info</param>
    /// <returns>The best found key. Or null if it doesn't match anything</returns>
    private string? DetermineKey(string candidateKey, out Type? type, out FieldInfo? fieldInfo,
        out PropertyInfo? propertyInfo)
    {
        LoadProperties();

        foreach (var field in instanceFields!)
        {
            if (field.Name.Equals(candidateKey, StringComparison.OrdinalIgnoreCase))
            {
                type = field.FieldType;
                fieldInfo = field;
                propertyInfo = null;
                return field.Name;
            }
        }

        foreach (var property in instanceProperties!)
        {
            if (property.Name.Equals(candidateKey, StringComparison.OrdinalIgnoreCase))
            {
                type = property.PropertyType;
                propertyInfo = property;
                fieldInfo = null;
                return property.Name;
            }
        }

        // No matches
        type = null;
        fieldInfo = null;
        propertyInfo = null;
        return null;
    }

    private bool IsPropertyUseful(string name, out Type? type, out FieldInfo? fieldInfo,
        out PropertyInfo? propertyInfo)
    {
        LoadProperties();

        var finalName = DetermineKey(name, out type, out fieldInfo, out propertyInfo);

        if (finalName == null)
            return false;

        if (Skip(finalName))
            return false;

        return true;
    }

    private void CreateInstance()
    {
        var type = DynamicType ?? staticType;

        if (type == typeof(DynamicDeserializeObjectConverter))
            throw new JsonException("Dynamic dummy deserialize used object didn't specify type");

        // Detect scene loaded type
        bool sceneLoad = type.CustomAttributes.Any(
            attr => attr.AttributeType == typeof(SceneLoadedClassAttribute));

        createdInstance = !sceneLoad ?
            CreateDeserializedInstance(type) :
            BaseThriveConverter.CreateDeserializedFromScene(type, this);

        if (createdInstance == null)
            throw new JsonException("instance of deserialized object should have been created");

        RunPrePropertyDeserializeActions(createdInstance);
    }

    /// <summary>
    ///   Figures out what constructor to call for an object that is to be loaded from JSON
    /// </summary>
    /// <returns>The created object</returns>
    /// <exception cref="JsonException">If can't be created</exception>
    private object CreateDeserializedInstance(Type objectType)
    {
        var constructorAttribute = typeof(JsonConstructorAttribute);

        // Consider private constructors but ignore those that do not have the [JsonConstructor] attribute.
        var privateJsonConstructors = objectType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(
                c => c.CustomAttributes.Any(a => a.AttributeType == constructorAttribute));

        var potentialConstructors = objectType.GetConstructors().Concat(
            privateJsonConstructors).Where(c => !c.ContainsGenericParameters);

        // For simplicity regarding the way we read properties now (and to avoid reading *all* properties),
        // we do it similarly as the default Newtonsoft JSON by requiring marking which constructor to use
        ConstructorInfo? attributedConstructor = null;
        ConstructorInfo? secondBestConstructor = null;

        foreach (var candidate in potentialConstructors)
        {
            if (candidate.ContainsGenericParameters)
                continue;

            bool hasCustomAttribute = candidate.CustomAttributes.Any(a => a.AttributeType == constructorAttribute);

            if (hasCustomAttribute)
            {
                if (attributedConstructor == null)
                {
                    attributedConstructor = candidate;
                    continue;
                }

                throw new JsonException(
                    $"can't have multiple constructors with: {constructorAttribute.Name}, class: {objectType.Name}");
            }

            if (secondBestConstructor == null)
            {
                secondBestConstructor = candidate;
            }
        }

        var best = attributedConstructor != null ? attributedConstructor : secondBestConstructor;

        if (best == null)
        {
            throw new JsonException($"no suitable constructor found for current type: {objectType.Name}");
        }

        var constructorParameterInfo = best.GetParameters();
        object?[] constructorArgs = new object?[constructorParameterInfo.Length];

        int index = 0;

        // We need to read enough attributes to be able to call the constructor
        foreach (var param in constructorParameterInfo)
        {
            var fieldName = DetermineKey(param.Name, out _, out var fieldInfo, out var propertyInfo);

            if (fieldName == null)
            {
                throw new JsonException(
                    "No matching field found for constructor's name (needs to be same except for case): " +
                    $"{param.Name}, class: {objectType.Name}");
            }

            if (fieldInfo != null)
            {
                if (fieldInfo.FieldType != param.ParameterType)
                {
                    throw new JsonException(
                        $"Mismatching field and constructor parameter type for: {param.Name}, " +
                        $"class: {objectType.Name}");
                }
            }
            else if (propertyInfo != null)
            {
                if (propertyInfo.PropertyType != param.ParameterType)
                {
                    throw new JsonException(
                        $"Mismatching property and constructor parameter type for: {param.Name}, " +
                        $"class: {objectType.Name}");
                }
            }
            else
            {
                throw new Exception("logic error in key determination");
            }

            var (jsonName, value, _, _) = ReadPropertiesUntil(fieldName);

            if (jsonName == null)
            {
                throw new JsonException(
                    $"Could not find field in JSON for constructor parameter: {param.Name}, " +
                    $"class: {objectType.Name}");
            }

            constructorArgs[index++] = value;
        }

        var instance = best.Invoke(constructorArgs) ?? throw new Exception("constructor invoke returned null");

        // Early load is also supported for non-scene loaded objects
        if (instance is IGodotEarlyNodeResolve early)
        {
            early.ResolveNodeReferences();
        }

        return instance;
    }
}
