using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;
using FileAccess = Godot.FileAccess;

/// <summary>
///   Main JSON conversion class for Thrive handling all our custom stuff
/// </summary>
public class ThriveJsonConverter : IDisposable
{
    private static readonly ThriveJsonConverter InstanceValue = new();

    private readonly JsonConverter[] thriveConverters;

    // TODO: (check if this can cause process lock ups) https://github.com/Revolutionary-Games/Thrive/issues/4989
    private readonly ThreadLocal<JsonSerializerSettings> currentJsonSettings = new();
    private bool disposed;

    private ThriveJsonConverter()
    {
        // All the thrive serializers need to be registered here
        thriveConverters =
        [
            new GodotColorConverter(),
            new GodotBasisConverter(),
            new GodotQuaternionConverter(),
            new NodePathConverter(),

            new CompoundConverter(),

            new ConditionSetConverter(),
        ];
    }

    public static ThriveJsonConverter Instance => InstanceValue;

    /// <summary>
    ///   Serializes the specified object to a JSON string using ThriveJsonConverter settings.
    /// </summary>
    /// <param name="object">Object to serialize</param>
    /// <param name="type">
    ///   <para>
    ///     Specifies the type of the object being serialized (optional). The dynamic type will
    ///     get written out if this is set to a base class of the object to serialize.
    ///   </para>
    /// </param>
    public string SerializeObject(object @object, Type? type = null)
    {
        return PerformWithSettings(s => JsonConvert.SerializeObject(@object, type, Constants.SAVE_FORMATTING, s));
    }

    public T? DeserializeObject<T>(string json)
    {
        return PerformWithSettings(s => JsonConvert.DeserializeObject<T>(json, s));
    }

    public T? DeserializeFile<T>(string path)
    {
        try
        {
            var json = ReadJSONFile(path);
            return PerformWithSettings(s => JsonConvert.DeserializeObject<T>(json, s));
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to deserialize file {path}: {e.Message}");
            return default;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                currentJsonSettings.Dispose();
            }

            disposed = true;
        }
    }

    private static string ReadJSONFile(string path)
    {
        string? result;
        using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Read))
        {
            result = file.GetAsText();
        }

        if (string.IsNullOrEmpty(result))
            throw new IOException($"Failed to read json file: {path}");

        return result;
    }

    private JsonSerializerSettings CreateSettings()
    {
        var referenceResolver = new ReferenceResolver();

        return new JsonSerializerSettings
        {
            // PreserveReferencesHandling = PreserveReferencesHandling.Objects,

            // We need to be careful to not deserialize untrusted data with this serializer
            TypeNameHandling = TypeNameHandling.Auto,

            // This blocks dynamic type loading
            SerializationBinder = null,

            Converters = thriveConverters,

            ReferenceResolverProvider = () => referenceResolver,

            // Even though we have our custom converters, the JSON library wants to mess with us, so we need to force
            // it to ignore these. Though we use simpler loads now, so this might be able to be removed.
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,

            // Skip writing null properties.
            // This saves a bit of data, and as saves are not manually edited, shouldn't really miss out on anything
            // by just having null values omitted. One potential pitfall is the requirement to not rely on a null
            // value to be passed to a JSON constructor
            NullValueHandling = NullValueHandling.Ignore,
        };
    }

    private T PerformWithSettings<T>(Func<JsonSerializerSettings, T> func)
    {
        JsonSerializerSettings settings;

        bool recursive = false;

        if (currentJsonSettings.Value != null)
        {
            // This is a recursive call
            recursive = true;
            settings = currentJsonSettings.Value;

            settings.TraceWriter?.Trace(TraceLevel.Info, "Entering recursive object", null);
        }
        else
        {
            settings = CreateSettings();
            currentJsonSettings.Value = settings;
        }

        try
        {
            return func(settings);
        }
        finally
        {
            if (!recursive)
            {
                currentJsonSettings.Value = null!;
            }
        }
    }
}

/// <summary>
///   Base for all the thrive JSON converter types.
///   this is used to allow access to the global information that shouldn't be saved.
/// </summary>
public abstract class BaseThriveConverter : JsonConverter
{
    // TODO: these complex handling things should now be unnecessary as saves no longer use JSON
    // ref handling approach from: https://stackoverflow.com/a/53716866/4371508
    public const string REF_PROPERTY = "$ref";
    public const string ID_PROPERTY = "$id";

    // type handling approach from: https://stackoverflow.com/a/29822170/4371508
    // and https://stackoverflow.com/a/29826959/4371508
    private const string TYPE_PROPERTY = "$type";

    private static readonly ConcurrentQueue<List<(int Order, string Name, object? Value, Type FieldType)>>
        DelayWriteProperties = new();

    private static readonly IComparer<(int Order, string Name, object? Value, Type FieldType)> PropertyOrderComparer =
        new OrderComparer();

    private static readonly Type ObjectBaseType = typeof(object);
    private static readonly Type SceneLoadedAttribute = typeof(SceneLoadedClassAttribute);
    private static readonly Type JsonPropertyAttribute = typeof(JsonPropertyAttribute);
    private static readonly Type JsonIgnoreAttribute = typeof(JsonIgnoreAttribute);
    private static readonly Type ExportAttribute = typeof(ExportAttribute);

    /// <summary>
    ///   We always want to be able to read what we write into JSON
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    ///   If this is true all serialized types are assumed to be marked as using references
    /// </summary>
    public virtual bool ForceReferenceWrite => false;

    public static IEnumerable<FieldInfo> FieldsOf(object value, bool handleClassJSONSettings = true)
    {
        return FieldsOf(value.GetType(), handleClassJSONSettings);
    }

    public static IEnumerable<FieldInfo> FieldsOf(Type type, bool handleClassJSONSettings = true)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.CustomAttributes.All(a => a.AttributeType != JsonIgnoreAttribute &&
                a.AttributeType != typeof(CompilerGeneratedAttribute)));

        // Ignore fields that aren't public unless it has JsonPropertyAttribute
        fields = fields.Where(p =>
            (p.IsPublic && !p.IsInitOnly) ||
            p.CustomAttributes.Any(a => a.AttributeType == JsonPropertyAttribute));

        // Ignore fields that are marked export without explicit JSON property
        fields = fields.Where(p =>
            !ExportWithoutExplicitJson(p.CustomAttributes));

        if (handleClassJSONSettings)
        {
            var settings = type.GetCustomAttribute<JsonObjectAttribute>();

            if (settings is { MemberSerialization: MemberSerialization.OptIn })
            {
                // Ignore all fields not opted in
                fields = fields.Where(p => p.CustomAttributes.Any(a => a.AttributeType == JsonPropertyAttribute));
            }
        }

        // Sort for serialization order
        // TODO: this is currently not needed and as this likely would cost performance, this is not enabled
        // TODO: probably could refactor this entire method to rely less on LINQ
        // fields = fields.OrderBy(f => f.GetCustomAttribute<JsonPropertyAttribute>()?.Order ?? 0);

        return fields;
    }

    public static IEnumerable<PropertyInfo> PropertiesOf(object value, bool handleClassJSONSettings = true)
    {
        return PropertiesOf(value.GetType(), handleClassJSONSettings);
    }

    public static IEnumerable<PropertyInfo> PropertiesOf(Type type, bool handleClassJSONSettings = true)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.CustomAttributes.All(a => a.AttributeType != JsonIgnoreAttribute));

        // Ignore properties that don't have a public setter unless it has JsonPropertyAttribute
        properties = properties.Where(p => p.GetSetMethod() != null ||
            p.CustomAttributes.Any(a => a.AttributeType == JsonPropertyAttribute));

        // Ignore properties that are marked export without explicit JSON property
        properties = properties.Where(p =>
            !ExportWithoutExplicitJson(p.CustomAttributes));

        if (handleClassJSONSettings)
        {
            var settings = type.GetCustomAttribute<JsonObjectAttribute>();

            if (settings is { MemberSerialization: MemberSerialization.OptIn })
            {
                // Ignore all properties not opted in
                properties =
                    properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == JsonPropertyAttribute));
            }
        }

        // TODO: this should also probably support ordering (a bigger overhaul would be to be able to order between
        // fields and properties)

        return properties;
    }

    /// <summary>
    ///   Fields and properties marked Godot.Export are skipped unless explicitly marked JsonProperty
    /// </summary>
    public static bool ExportWithoutExplicitJson(IEnumerable<CustomAttributeData> customAttributes)
    {
        var customAttributeData = customAttributes.ToList();

        bool export = customAttributeData.Any(a => a.AttributeType == ExportAttribute);

        if (!export)
            return false;

        return customAttributeData.All(a => a.AttributeType != JsonPropertyAttribute);
    }

    /// <summary>
    ///   Creates a deserialized object by loading a Godot scene and instantiating it
    /// </summary>
    /// <returns>Instance of the scene loaded object</returns>
    /// <exception cref="JsonException">If this couldn't create a new instance</exception>
    public static object CreateDeserializedFromScene(Type objectType, InProgressObjectDeserialization objectLoad)
    {
        var attrs = objectType.GetCustomAttributes(typeof(SceneLoadedClassAttribute), true);

        var data = (SceneLoadedClassAttribute)attrs[0];

        var scene = GD.Load<PackedScene>(data.ScenePath);

        if (scene == null)
            throw new JsonException($"Couldn't load scene ({data.ScenePath}) for scene loaded type");

        var node = scene.Instantiate();

        if (node == null)
        {
            throw new JsonException("Please try restarting the game, encountered Godot scene instantiation error! " +
                $"This should only happen due to engine bugs, failed scene: {data.ScenePath}");
        }

        // Ensure that instance ended up being a good type
        if (!objectType.IsInstanceOfType(node))
        {
            throw new JsonException("Loading Nodes through JSON is no longer allowed");
        }

        object instance = node;

        // Let the object know first if it is loaded from a save to allow node resolve to do special actions in this
        // case
        objectLoad.ReceiveInstance(instance);

        // Perform early Node resolve to make loading child Node properties work
        if (data.UsesEarlyResolve)
        {
            try
            {
                ((IGodotEarlyNodeResolve)instance).ResolveNodeReferences();
            }
            catch (InvalidCastException e)
            {
                throw new JsonException("Scene loaded JSON type cast to IGodotEarlyNodeResolve failed", e);
            }

            // Recursively apply early resolve as otherwise recursive ApplyOnlyChildProperties may run into problems
            try
            {
                RecursivelyApplyEarlyNodeResolve(instance);
            }
            catch (Exception e)
            {
                throw new JsonException("Scene loaded JSON type failed to recursively find / run early resolve nodes",
                    e);
            }
        }

        return instance;
    }

    public static bool IsSpecialProperty(string name)
    {
        return name is REF_PROPERTY or ID_PROPERTY or TYPE_PROPERTY;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var customRead = ReadCustomJson(reader, objectType, existingValue, serializer);

        if (customRead.Performed)
            return customRead.Read;

        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new InvalidOperationException(
                $"unexpected JSON token ({reader.TokenType}) when expecting object start");
        }

        if (serializer.ReferenceResolver == null)
            throw new InvalidOperationException("JsonSerializer must have reference resolver");

        // TODO: caching the object deserialization object would be a pretty good memory allocation reduction
        var objectLoad = new InProgressObjectDeserialization(objectType, reader, serializer, SkipMember);

        bool readToEnd = false;
        string? refId = null;

        bool normalPropertiesStarted = false;

        while (true)
        {
            if (readToEnd)
            {
                reader.Read();

                if (reader.TokenType == JsonToken.EndObject)
                    break;

                continue;
            }

            var (name, value, field, property) = objectLoad.ReadNextProperty();

            if (name == null)
            {
                // Ran out of properties
                break;
            }

            // Detect ref to already loaded object
            if (name == REF_PROPERTY)
            {
                refId = (string?)value ?? throw new JsonException("no ref id");

                readToEnd = true;
                continue;
            }

            // ID_PROPERTY is handled by objectLoad as it needs to be able to set this as early as possible

            // Detect dynamic typing
            if (name == TYPE_PROPERTY)
            {
                var type = (string?)value ?? throw new JsonException("no type name");

                if (serializer.TypeNameHandling == TypeNameHandling.None)
                    continue;

                if (objectLoad.PropertiesAlreadyLoaded())
                    throw new JsonException("Can't change type after object property loading has started");

                var parts = type.Split(',').Select(p => p.Trim()).ToList();

                if (parts.Count != 2 && parts.Count != 1)
                    throw new JsonException($"invalid {TYPE_PROPERTY} format");

                // Change to loading the other type
                objectLoad.DynamicType = serializer.SerializationBinder.BindToType(
                    parts.Count > 1 ? parts[1] : null, parts[0]);

                continue;
            }

            // We are handling a non-special property

            if (!normalPropertiesStarted)
            {
                // Offer our initial value as a potential constructor parameter
                objectLoad.OfferPotentiallyConstructorParameter((name, value, field, property));
                objectLoad.GetInstance();

                normalPropertiesStarted = true;

                // We skip then processing the current property in case it was a constructor parameter, if it wasn't
                // we'll receive it on the next iteration of this loop
                continue;
            }

            var instance = objectLoad.GetInstance();

            if (field != null)
            {
                if (UsesOnlyChildAssign(field.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute))))
                {
                    var target = field.GetValue(instance);

                    if (target == null)
                        throw new JsonException($"Cannot copy child properties to a null value of field: {field.Name}");

                    throw new NotImplementedException("TODO: ApplyOnlyChildProperties is no longer supported in JSON");
                }

                field.SetValue(instance, value);
            }
            else if (property != null)
            {
                if (!UsesOnlyChildAssign(
                        property.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute))))
                {
                    var set = property.GetSetMethodOnDeclaringType();

                    if (set == null)
                    {
                        throw new InvalidOperationException(
                            $"Json property used on a property ({name}) that has no (private) setter");
                    }

                    set.Invoke(instance, new[] { value });
                }
                else
                {
                    var target = property.GetValue(instance);

                    if (target == null)
                    {
                        throw new JsonException(
                            $"Cannot copy child properties to a null value of property: {property.Name}");
                    }

                    throw new NotImplementedException("TODO: ApplyOnlyChildProperties is no longer supported in JSON");
                }
            }
        }

        if (refId != null)
            return serializer.ReferenceResolver.ResolveReference(serializer, refId);

        var instanceAtEnd = objectLoad.GetInstance();

        // Protects against bugs in trying to still read the object incorrectly. Not super necessary with the custom
        // fields code removed, but can probably be left to guard against deserialization bugs.
        objectLoad.DisallowFurtherReading();

        if (reader.TokenType != JsonToken.EndObject)
            throw new JsonException("should have ended at object end token in JSON");

        // It is now assumed (because loading scenes and keeping references) that all loaded Nodes are deleted by
        // someone else (other than the ones that have just their properties grabbed in deserialize)

        return instanceAtEnd;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        if (WriteCustomJson(writer, value, serializer))
            return;

        var type = value.GetType();

        var contract = serializer.ContractResolver.ResolveContract(type);

        bool reference = ForceReferenceWrite ||
            serializer.PreserveReferencesHandling != PreserveReferencesHandling.None ||
            contract.IsReference == true;

        writer.WriteStartObject();

        if (serializer.ReferenceResolver == null)
            throw new InvalidOperationException("JsonSerializer must have reference resolver");

        if (reference && serializer.ReferenceResolver.IsReferenced(serializer, value))
        {
            // Already written, just write the ref
            writer.WritePropertyName(REF_PROPERTY);
            writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
        }
        else
        {
            if (reference)
            {
                writer.WritePropertyName(ID_PROPERTY);
                writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
            }

            // We no longer handle dynamic typing
            if (serializer.TypeNameHandling != TypeNameHandling.None)
            {
            }

            List<(int Order, string Name, object? Value, Type FieldType)>? delayWriteProperties = null;

            // TODO: does this need to support higher priority properties (i.e. negative ordering)?

            // First time writing, write all fields and properties
            foreach (var field in FieldsOf(value))
            {
                var jsonCustomization = field.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonCustomization != null && jsonCustomization.Order != 0)
                {
                    if (jsonCustomization.Order < 0)
                        throw new NotSupportedException("Negative JSON priority order is not implemented");

                    if (delayWriteProperties == null)
                    {
                        if (!DelayWriteProperties.TryDequeue(out delayWriteProperties))
                            delayWriteProperties = new List<(int Order, string Name, object? Value, Type FieldType)>();
                    }

                    delayWriteProperties.Add((jsonCustomization.Order, field.Name, field.GetValue(value),
                        field.FieldType));
                    continue;
                }

                WriteMember(field.Name, field.GetValue(value), field.FieldType, type, writer, serializer);
            }

            foreach (var property in PropertiesOf(value))
            {
                object? memberValue;
                try
                {
                    memberValue = property.GetValue(value, null);
                }
                catch (TargetInvocationException e)
                {
#if DEBUG
                    if (Debugger.IsAttached)
                        Debugger.Break();
#endif

                    // ReSharper disable HeuristicUnreachableCode ConditionIsAlwaysTrueOrFalse
                    if (!Constants.CATCH_SAVE_ERRORS)
#pragma warning disable 162
                        throw;
#pragma warning restore 162

                    // Protection against disposed stuff that is very easy to make a mistake about. Seems to be caused
                    // by carelessly keeping references to other game entities that are saved.

                    // Write the property name here to make the writer at path correct
                    writer.WritePropertyName(property.Name);

                    GD.PrintErr($"Problem trying to save a property (at: {writer.Path}): ", e);

                    serializer.Serialize(writer, null);
                    continue;
                }

                var jsonCustomization = property.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonCustomization != null && jsonCustomization.Order != 0)
                {
                    if (jsonCustomization.Order < 0)
                        throw new NotSupportedException("Negative JSON priority oder is not implemented");

                    if (delayWriteProperties == null)
                    {
                        if (!DelayWriteProperties.TryDequeue(out delayWriteProperties))
                            delayWriteProperties = new List<(int Order, string Name, object? Value, Type FieldType)>();
                    }

                    delayWriteProperties.Add((jsonCustomization.Order, property.Name, memberValue,
                        property.PropertyType));
                    continue;
                }

                WriteMember(property.Name, memberValue, property.PropertyType, type, writer,
                    serializer);
            }

            if (delayWriteProperties != null)
            {
                delayWriteProperties.Sort(PropertyOrderComparer);

                foreach (var delayWrite in delayWriteProperties)
                {
                    WriteMember(delayWrite.Name, delayWrite.Value, delayWrite.FieldType, type, writer,
                        serializer);
                }

                delayWriteProperties.Clear();
                DelayWriteProperties.Enqueue(delayWriteProperties);
            }
        }

        writer.WriteEndObject();
    }

    protected virtual (object? Read, bool Performed) ReadCustomJson(JsonReader reader, Type objectType,
        object? existingValue, JsonSerializer serializer)
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
    protected virtual void WriteMember(string name, object? memberValue, Type memberType, Type objectType,
        JsonWriter writer, JsonSerializer serializer)
    {
        if (SkipMember(name))
            return;

        if (serializer.NullValueHandling == NullValueHandling.Ignore && ReferenceEquals(memberValue, null))
        {
            // Skip writing a null
            return;
        }

        writer.WritePropertyName(name);

        // Special handle types (none currently)

        // Use default serializer on everything else
        serializer.Serialize(writer, memberValue, memberType);
    }

    protected virtual bool SkipMember(string name)
    {
        return false;
    }

    private static bool UsesOnlyChildAssign(IEnumerable<Attribute> customAttributes)
    {
        var data = customAttributes.FirstOrDefault() as AssignOnlyChildItemsOnDeserializeAttribute;

        if (data == null)
        {
            return false;
        }

        return true;
    }

    private static void RecursivelyApplyEarlyNodeResolve(object instance)
    {
        // Child objects are also told if they are loaded from a save
        InProgressObjectDeserialization.HandleSaveLoadedTracked(instance);

        foreach (var field in FieldsOf(instance))
        {
            if (UsesOnlyChildAssign(field.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute))))
            {
                var value = field.GetValue(instance);

                if (value is IGodotEarlyNodeResolve earlyNodeResolve)
                {
                    earlyNodeResolve.ResolveNodeReferences();
                    RecursivelyApplyEarlyNodeResolve(value);
                }
            }
        }

        foreach (var property in PropertiesOf(instance))
        {
            if (UsesOnlyChildAssign(property.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute))))
            {
                var value = property.GetValue(instance);

                if (value is IGodotEarlyNodeResolve earlyNodeResolve)
                {
                    earlyNodeResolve.ResolveNodeReferences();
                    RecursivelyApplyEarlyNodeResolve(value);
                }
            }
        }
    }

    private void UpdateObjectReference(JsonSerializer serializer, object oldReference, object newReference)
    {
        if (serializer.ReferenceResolver!.IsReferenced(serializer, oldReference))
        {
            serializer.ReferenceResolver.AddReference(serializer,
                serializer.ReferenceResolver.GetReference(serializer, oldReference), newReference);
        }
    }

    private class OrderComparer : IComparer<(int Order, string Name, object? Value, Type FieldType)>
    {
        public int Compare((int Order, string Name, object? Value, Type FieldType) x,
            (int Order, string Name, object? Value, Type FieldType) y)
        {
            return x.Order.CompareTo(y.Order);
        }
    }
}
