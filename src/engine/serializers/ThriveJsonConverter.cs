using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Main JSON conversion class for Thrive handling all our custom stuff
/// </summary>
public class ThriveJsonConverter
{
    private static readonly ThriveJsonConverter InstanceValue = new ThriveJsonConverter(SimulationParameters.Instance);

    private readonly SimulationParameters simulation;

    /// <summary>
    ///   This is populated with all the thrive json converter types
    /// </summary>
    private readonly JsonConverter[] thriveConverters;

    private ThriveJsonConverter(SimulationParameters simulation)
    {
        this.simulation = simulation;

        thriveConverters = new JsonConverter[]
        {
            new DefaultThriveJSONConverter(simulation),
            new RegistryTypeConverter(simulation),
            new GodotColorConverter(),

            // Probably less likely used converters defined last
            new PatchConverter(simulation),
        };
    }

    public static ThriveJsonConverter Instance => InstanceValue;

    public string SerializeObject(object o)
    {
        return JsonConvert.SerializeObject(o, Constants.SAVE_FORMATTING, thriveConverters);
    }

    public T DeserializeObject<T>(string genes)
    {
        return JsonConvert.DeserializeObject<T>(genes, thriveConverters);
    }
}

/// <summary>
///   Base for all the thrive json converter types.
///   this is used to allow access to the global information that shouldn't be saved.
/// </summary>
public abstract class BaseThriveConverter : JsonConverter
{
    public readonly SimulationParameters Simulation;

    protected BaseThriveConverter(SimulationParameters simulation)
    {
        Simulation = simulation;
    }

    /// <summary>
    ///   These need to always be able to read as we use json for saving so it makes no sense to
    ///   have a one-way converter
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    ///   Finds the actual key for a thing ignoring different cases
    /// </summary>
    /// <param name="items">Items to check keys for</param>
    /// <param name="candidateKey">Key to test with if it can be found</param>
    /// <returns>The best found key</returns>
    public static string DetermineKey(JObject items, string candidateKey)
    {
        if (items.ContainsKey(candidateKey))
            return candidateKey;

        foreach (var item in items)
        {
            if (item.Key.Equals(candidateKey, StringComparison.OrdinalIgnoreCase))
            {
                return item.Key;
            }
        }

        // No matches
        return candidateKey;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        var customRead = ReadCustomJson(reader, objectType, existingValue, serializer);

        if (customRead.performed)
            return customRead.read;

        if (reader.TokenType != JsonToken.StartObject)
        {
            return null;
        }

        var item = JObject.Load(reader);

        // Find a constructor we can call
        ConstructorInfo constructor = null;

        foreach (var candidate in objectType.GetConstructors())
        {
            if (candidate.ContainsGenericParameters)
                continue;

            bool canUseThis = true;

            // Check do we have all the needed parameters
            foreach (var param in candidate.GetParameters())
            {
                if (!item.ContainsKey(DetermineKey(item, param.Name)))
                {
                    canUseThis = false;
                    break;
                }
            }

            if (!canUseThis)
                continue;

            if (constructor == null || constructor.GetParameters().Length < candidate.GetParameters().Length)
                constructor = candidate;
        }

        if (constructor == null)
        {
            throw new JsonException($"no suitable constructor found for current type: {objectType}");
        }

        HashSet<string> alreadyConsumedItems = new HashSet<string>();

        foreach (var param in constructor.GetParameters())
        {
            alreadyConsumedItems.Add(DetermineKey(item, param.Name));
        }

        // Load constructor params
        object[] constructorArgs = constructor.GetParameters()
            .Select((p) => ReadMember(DetermineKey(item, p.Name),
                p.ParameterType, item, reader, serializer)).ToArray();

        var instance = constructor.Invoke(constructorArgs);

        var properties = PropertiesOf(instance);

        var fields = FieldsOf(instance);

        bool Skip(string name, string key)
        {
            return SkipMember(name) || alreadyConsumedItems.Contains(key);
        }

        foreach (var property in properties)
        {
            var name = DetermineKey(item, property.Name);
            if (Skip(property.Name, name))
                continue;

            property.SetValue(instance, ReadMember(name, property.PropertyType, item, reader,
                serializer));
        }

        foreach (var field in fields)
        {
            var name = DetermineKey(item, field.Name);
            if (Skip(field.Name, name))
                continue;

            field.SetValue(instance, ReadMember(name, field.FieldType, item, reader, serializer));
        }

        return instance;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        if (WriteCustomJson(writer, value, serializer))
            return;

        var properties = PropertiesOf(value);

        var fields = FieldsOf(value);

        writer.WriteStartObject();

        foreach (var property in properties)
        {
            WriteMember(property.Name, property.GetValue(value, null), property.PropertyType, writer, serializer);
        }

        foreach (var field in fields)
        {
            WriteMember(field.Name, field.GetValue(value), field.FieldType, writer, serializer);
        }

        writer.WriteEndObject();
    }

    protected virtual (object read, bool performed) ReadCustomJson(JsonReader reader, Type objectType,
        object existingValue, JsonSerializer serializer)
    {
        return (null, false);
    }

    protected virtual bool WriteCustomJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        return false;
    }

    /// <summary>
    ///   Default member writer for thrive types. Has special handling for some Thrive types,
    ///   others use default serializers
    /// </summary>
    protected virtual void WriteMember(string name, object memberValue, Type memberType, JsonWriter writer,
        JsonSerializer serializer)
    {
        if (SkipMember(name))
            return;

        writer.WritePropertyName(name);

        // Special handle types (none currently)

        // Use default serializer on everything else
        serializer.Serialize(writer, memberValue);
    }

    protected virtual object ReadMember(string name, Type memberType, JObject item, JsonReader reader,
        JsonSerializer serializer)
    {
        var value = item[name];

        // Special handle types (none currently)

        // Use default get on everything else
        return value?.ToObject(memberType, serializer);
    }

    protected virtual bool SkipMember(string name)
    {
        return false;
    }

    private static IEnumerable<FieldInfo> FieldsOf(object value)
    {
        var fields = value.GetType().GetFields().Where((p) => p.CustomAttributes.All(
            a => a.AttributeType != typeof(JsonIgnoreAttribute)));

        // Ignore fields that aren't public unless it has JsonPropertyAttribute
        return fields.Where((p) =>
            (p.IsPublic && !p.IsInitOnly) ||
            p.CustomAttributes.Any((a) => a.AttributeType == typeof(JsonPropertyAttribute)));
    }

    private static IEnumerable<PropertyInfo> PropertiesOf(object value)
    {
        var properties = value.GetType().GetProperties().Where(
            (p) => p.CustomAttributes.All(
                a => a.AttributeType != typeof(JsonIgnoreAttribute)));

        // Ignore properties that don't have a public setter unless it has JsonPropertyAttribute
        return properties.Where((p) => p.GetSetMethod() != null ||
            p.CustomAttributes.Any((a) => a.AttributeType == typeof(JsonPropertyAttribute)));
    }
}

/// <summary>
///   When a class has this attribute DefaultThriveJSONConverter is used to serialize it
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UseThriveSerializerAttribute : Attribute
{
}

/// <summary>
///   Custom serializer for all Thrive types that don't need any special handling. They need to have the attribute
///   UseThriveSerializerAttribute to be detected
/// </summary>
internal class DefaultThriveJSONConverter : BaseThriveConverter
{
    public DefaultThriveJSONConverter(SimulationParameters simulation) : base(simulation)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        // Types with out custom attribute are supported
        return objectType.CustomAttributes.Any(
            (attr) => attr.AttributeType == typeof(UseThriveSerializerAttribute));
    }
}
