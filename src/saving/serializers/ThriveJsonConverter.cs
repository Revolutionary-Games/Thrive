using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Saving;

/// <summary>
///   Main JSON conversion class for Thrive handling all our custom stuff
/// </summary>
public class ThriveJsonConverter : IDisposable
{
    private static readonly ThriveJsonConverter InstanceValue = new(new SaveContext(SimulationParameters.Instance));

    private readonly SaveContext context;

    private readonly JsonConverter[] thriveConverters;
    private readonly List<JsonConverter> thriveConvertersDynamicDeserialize;
    private readonly DynamicDeserializeObjectConverter dynamicObjectDeserializeConverter;

    private readonly ThreadLocal<JsonSerializerSettings> currentJsonSettings = new();
    private bool disposed;

    private ThriveJsonConverter(SaveContext context)
    {
        this.context = context;

        // All of the thrive serializers need to be registered here
        thriveConverters = new JsonConverter[]
        {
            new RegistryTypeConverter(context),
            new GodotColorConverter(),
            new GodotBasisConverter(),
            new GodotQuatConverter(),
            new PackedSceneConverter(),
            new SystemVector4ArrayConverter(),
            new RandomConverter(),
            new ConvexPolygonShapeConverter(),

            new CompoundCloudPlaneConverter(context),

            new CallbackConverter(),

            // Specific Godot Node converter types

            // Fallback Godot Node converter, this is before default serializer to make Node types with scene loaded
            // attribute work correctly. Unfortunately this means it is not possible to force a Node derived class
            // to not use this
            new BaseNodeConverter(context),

            new EntityReferenceConverter(context),
            new UnsavedEntitiesConverter(context),
            new EntityWorldConverter(context),

            // Converter for all types with a specific few attributes for this to be enabled
            new DefaultThriveJSONConverter(context),
        };

        dynamicObjectDeserializeConverter = new DynamicDeserializeObjectConverter(context);
        thriveConvertersDynamicDeserialize = new List<JsonConverter> { dynamicObjectDeserializeConverter };
        thriveConvertersDynamicDeserialize.AddRange(thriveConverters);
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
        return PerformWithSettings(settings =>
            JsonConvert.SerializeObject(@object, type, Constants.SAVE_FORMATTING, settings));
    }

    public T? DeserializeObject<T>(string json)
    {
        return PerformWithSettings(settings => JsonConvert.DeserializeObject<T>(json, settings));
    }

    /// <summary>
    ///   Deserializes a fully dynamic object from json (object type is defined only in the json).
    ///   Note that this uses the deserializer type for <see cref="object"/> which means that no custom deserializer
    ///   logic works! That means this is only usable for basic types. Other types must have an interface or other
    ///   base type and be used through <see cref="DeserializeObject{T}"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Even though this uses only basic deserialization this uses the <see cref="BaseThriveConverter"/> through
    ///     <see cref="DynamicDeserializeObjectConverter"/> for the base object of the deserialized string. So some
    ///     of our custom logic works, but for example <see cref="Node"/> deserialization won't use the specialized
    ///     Node logic.
    ///   </para>
    /// </remarks>
    /// <param name="json">JSON text to parse</param>
    /// <returns>The created object</returns>
    /// <exception cref="JsonException">If invalid json or the dynamic type is not allowed</exception>
    public object? DeserializeObjectDynamic(string json)
    {
        return PerformWithSettings(settings =>
        {
            // enable hack conversion
            settings.Converters = thriveConvertersDynamicDeserialize;
            dynamicObjectDeserializeConverter.ResetConversionCounter();

            try
            {
                return JsonConvert.DeserializeObject<object>(json, settings);
            }
            finally
            {
                // disable hack conversion
                settings.Converters = thriveConverters;
            }
        });
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

    private JsonSerializerSettings CreateSettings()
    {
        var referenceResolver = new ReferenceResolver();

        return new JsonSerializerSettings
        {
            // PreserveReferencesHandling = PreserveReferencesHandling.Objects,

            // We need to be careful to not deserialize untrusted data with this serializer
            TypeNameHandling = TypeNameHandling.Auto,

            // This blocks most types from using typename handling
            SerializationBinder = new SerializationBinder(),

            Converters = thriveConverters,

            ReferenceResolverProvider = () => referenceResolver,

            // Even though we have our custom converters the JSON library wants to mess with us so we need to force it
            // to ignore these. This has the slight downside that if someone forgets to add
            // UseThriveSerializerAttribute when reference loops exist, this probably causes a stack overflow
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,

            TraceWriter = GetTraceWriter(Settings.Instance.JSONDebugMode, JSONDebug.ErrorHasOccurred),
        };
    }

    private ITraceWriter? GetTraceWriter(JSONDebug.DebugMode debugMode, bool errorHasOccurred)
    {
        if (debugMode == JSONDebug.DebugMode.AlwaysDisabled)
            return null;

        if (debugMode == JSONDebug.DebugMode.AlwaysEnabled || errorHasOccurred)
        {
            return new MemoryTraceWriter();
        }

        return null;
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

            // Reset context as a new JSON serialize operation has been started
            context.Reset();
        }

        try
        {
            return func(settings);
        }
        catch (Exception e)
        {
            // Don't do our special automatic debug enabling if debug writing is already on
            if (JSONDebug.ErrorHasOccurred || settings.TraceWriter != null)
                throw;

            JSONDebug.ErrorHasOccurred = true;

            if (Settings.Instance.JSONDebugMode == JSONDebug.DebugMode.Automatic)
            {
                GD.Print("JSON error happened, retrying with debug printing (mode is automatic), first exception: ",
                    e);

                // Seems like the json library doesn't have nullability annotations
                currentJsonSettings.Value = null!;
                PerformWithSettings(func);

                // If we get here, we didn't get another exception...
                // So we could maybe re-throw the first exception so that we fail like we should
                GD.PrintErr("Expected an exception for the second try at JSON operation, but it succeeded, " +
                    "re-throwing the original exception");
                throw;
            }
            else
            {
                throw;
            }
        }
        finally
        {
            if (!recursive)
            {
                currentJsonSettings.Value = null!;

                if (settings.TraceWriter != null)
                {
                    JSONDebug.OnTraceFinished(settings.TraceWriter);

                    // This shouldn't get reused so no point in creating a new instance here
                    settings.TraceWriter = null;
                }
            }
            else
            {
                settings.TraceWriter?.Trace(TraceLevel.Info, "Exited recursive object", null);
            }
        }
    }
}

/// <summary>
///   Base for all the thrive json converter types.
///   this is used to allow access to the global information that shouldn't be saved.
/// </summary>
public abstract class BaseThriveConverter : JsonConverter
{
    // ref handling approach from: https://stackoverflow.com/a/53716866/4371508
    public const string REF_PROPERTY = "$ref";
    public const string ID_PROPERTY = "$id";

    protected readonly ISaveContext? Context;

    // type handling approach from: https://stackoverflow.com/a/29822170/4371508
    // and https://stackoverflow.com/a/29826959/4371508
    private const string TYPE_PROPERTY = "$type";

    private static readonly List<string> DefaultOnlyChildCopyIgnore = new();

    private static readonly ConcurrentQueue<List<(int Order, string Name, object? Value, Type FieldType)>>
        DelayWriteProperties = new();

    private static readonly IComparer<(int Order, string Name, object? Value, Type FieldType)> PropertyOrderComparer =
        new OrderComparer();

    private static readonly Type ObjectBaseType = typeof(object);
    private static readonly Type AlwaysDynamicAttribute = typeof(JSONAlwaysDynamicTypeAttribute);
    private static readonly Type SceneLoadedAttribute = typeof(SceneLoadedClassAttribute);
    private static readonly Type BaseDynamicTypeAllowedAttribute = typeof(JSONDynamicTypeAllowedAttribute);
    private static readonly Type JsonPropertyAttribute = typeof(JsonPropertyAttribute);
    private static readonly Type JsonIgnoreAttribute = typeof(JsonIgnoreAttribute);

    protected BaseThriveConverter(ISaveContext? context)
    {
        Context = context;
    }

    /// <summary>
    ///   These need to always be able to read as we use json for saving so it makes no sense to
    ///   have a one-way converter
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
                properties = properties.Where(
                    p => p.CustomAttributes.Any(a => a.AttributeType == JsonPropertyAttribute));
            }
        }

        return properties;
    }

    /// <summary>
    ///   Fields and properties marked Godot.Export are skipped unless explicitly marked JsonProperty
    /// </summary>
    public static bool ExportWithoutExplicitJson(IEnumerable<CustomAttributeData> customAttributes)
    {
        var customAttributeData = customAttributes.ToList();

        bool export = customAttributeData.Any(attr => attr.AttributeType == typeof(ExportAttribute));

        if (!export)
            return false;

        return customAttributeData.All(attr => attr.AttributeType != JsonPropertyAttribute);
    }

    public static bool IsIgnoredGodotMember(string name, Type type)
    {
        return typeof(Node).IsAssignableFrom(type) && BaseNodeConverter.IsIgnoredGodotNodeMember(name);
    }

    /// <summary>
    ///   Creates a deserialized object by loading a Godot scene and instantiating it
    /// </summary>
    /// <returns>Instance of the scene loaded object</returns>
    /// <exception cref="JsonException">If couldn't create a new instance</exception>
    public static object CreateDeserializedFromScene(Type objectType, InProgressObjectDeserialization objectLoad)
    {
        var attrs = objectType.GetCustomAttributes(typeof(SceneLoadedClassAttribute), true);

        var data = (SceneLoadedClassAttribute)attrs[0];

        var scene = GD.Load<PackedScene>(data.ScenePath);

        if (scene == null)
            throw new JsonException($"Couldn't load scene ({data.ScenePath}) for scene loaded type");

        var node = scene.Instance();

        // Ensure that instance ended up being a good type
        if (!objectType.IsInstanceOfType(node))
        {
            // Clean up godot resources
            TemporaryLoadedNodeDeleter.Instance.Register(node);
            throw new JsonException("Scene loaded JSON deserialized type can't be assigned to target type");
        }

        object instance = node;

        // Let the object know first if it is loaded from a save to allow node resolve do special actions in this case
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
            throw new InvalidOperationException("unexpected JSON token when expecting object start");

        if (serializer.ReferenceResolver == null)
            throw new InvalidOperationException("JsonSerializer must have reference resolver");

        var objectLoad = new InProgressObjectDeserialization(objectType, reader, serializer, SkipMember);
        OnConfigureObjectLoad(objectLoad);

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
                if (UsesOnlyChildAssign(field.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                        out var data))
                {
                    ApplyOnlyChildProperties(value, field.GetValue(instance), serializer, data!);
                }
                else
                {
                    field.SetValue(instance, value);
                }
            }
            else if (property != null)
            {
                if (!UsesOnlyChildAssign(
                        property.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                        out var data))
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
                    ApplyOnlyChildProperties(value, property.GetValue(instance), serializer, data!);
                }
            }
        }

        if (refId != null)
            return serializer.ReferenceResolver.ResolveReference(serializer, refId);

        var instanceAtEnd = objectLoad.GetInstance();

        objectLoad.MarkStartCustomFields();

        // Protects against bugs in the custom field reading. Custom fields should be accessed in a different way
        // anyway
        objectLoad.DisallowFurtherReading();

        ReadCustomExtraFields(objectLoad, instanceAtEnd, objectType, existingValue, serializer);

        if (reader.TokenType != JsonToken.EndObject)
            throw new JsonException("should have ended at object end token in JSON");

        // It is now assumed (because loading scenes and keeping references) that all loaded Nodes are deleted by
        // someone else (other than the ones that have just their properties grabbed in deserialize)

        RunPostPropertyDeserializeActions(instanceAtEnd);

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
            (contract.IsReference == true);

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

            // Dynamic typing
            if (serializer.TypeNameHandling != TypeNameHandling.None)
            {
                // We don't know the type of field we are included in so we can't detect if the instance type
                // doesn't match the field (at least I don't think we can,
                // would be great though if we could - hhyyrylainen).
                // Seems like a limitation in the JSON library:
                // https://github.com/JamesNK/Newtonsoft.Json/issues/2126
                // So we check if the attributes want us to write the type and go with that
                bool writeType = false;

                var baseType = type.BaseType;

                while (baseType != null && baseType != ObjectBaseType)
                {
                    if (baseType.CustomAttributes.Any(attr =>
                            attr.AttributeType == BaseDynamicTypeAllowedAttribute) ||
                        HasAlwaysJSONTypeWriteAttribute(baseType))
                    {
                        writeType = true;
                        break;
                    }

                    baseType = baseType.BaseType;
                }

                if (!writeType)
                    writeType = HasAlwaysJSONTypeWriteAttribute(type);

                if (writeType)
                {
                    writer.WritePropertyName(TYPE_PROPERTY);

                    writer.WriteValue(type.AssemblyQualifiedNameWithoutVersion());
                }
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
                // Reading some godot properties already triggers problems, so we ignore those here
                if (SkipIfGodotNodeType(property.Name, type))
                    continue;

                object memberValue;
                try
                {
                    memberValue = property.GetValue(value, null);
                }
                catch (TargetInvocationException e)
                {
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

            WriteCustomExtraFields(writer, value, serializer);
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
    ///   Configures a started object load. Note for it to read custom properties for
    ///   <see cref="ReadCustomExtraFields"/> they need to be setup here
    /// </summary>
    /// <param name="objectLoad">The started object load</param>
    protected virtual void OnConfigureObjectLoad(InProgressObjectDeserialization objectLoad)
    {
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

        writer.WritePropertyName(name);

        // Special handle types (none currently)

        // Use default serializer on everything else
        serializer.Serialize(writer, memberValue, memberType);
    }

    protected virtual void WriteCustomExtraFields(JsonWriter writer, object value, JsonSerializer serializer)
    {
    }

    protected virtual void ReadCustomExtraFields(InProgressObjectDeserialization objectLoad, object instance,
        Type type, object? o, JsonSerializer jsonSerializer)
    {
    }

    protected virtual bool SkipMember(string name)
    {
        // By default IsLoadedFromSave is ignored as properties by default don't inherit attributes so this makes
        // things a bit easier when adding new types
        if (SaveApplyHelper.IsNameLoadedFromSaveName(name))
            return true;

        return false;
    }

    protected virtual bool SkipIfGodotNodeType(string name, Type type)
    {
        if (IsIgnoredGodotMember(name, type))
            return true;

        return false;
    }

    private static bool HasAlwaysJSONTypeWriteAttribute(Type type)
    {
        // If the current uses scene creation, dynamic type needs to be also in that case output
        return type.CustomAttributes.Any(attr =>
            attr.AttributeType == AlwaysDynamicAttribute ||
            attr.AttributeType == SceneLoadedAttribute);
    }

    private static bool UsesOnlyChildAssign(IEnumerable<Attribute> customAttributes,
        out AssignOnlyChildItemsOnDeserializeAttribute? data)
    {
        data = customAttributes.FirstOrDefault() as AssignOnlyChildItemsOnDeserializeAttribute;

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
            if (UsesOnlyChildAssign(field.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                    out _))
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
            if (UsesOnlyChildAssign(property.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                    out _))
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

    private void RunPostPropertyDeserializeActions(object instance)
    {
        // TODO: these should be called after loading the whole object tree
        if (instance is ISaveLoadable loadable)
            loadable.FinishLoading(Context);
    }

    /// <summary>
    ///   Applies child properties to an object that wasn't deserialized, from an object that was deserialized.
    ///   Used in conjunction with scene loading objects
    /// </summary>
    /// <param name="newData">The new object to copy non-ignored fields and properties to target</param>
    /// <param name="target">The object to apply properties to</param>
    /// <param name="serializer">Serializer to use for reference handling</param>
    /// <param name="data">Options for the child assign</param>
    /// <param name="recursive">
    ///   Set to true when called recursively. Disabled registering Node instance deletion
    /// </param>
    private void ApplyOnlyChildProperties(object? newData, object target, JsonSerializer serializer,
        AssignOnlyChildItemsOnDeserializeAttribute data, bool recursive = false)
    {
        if (target == null)
            throw new JsonSerializationException("Copy only child properties target is null");

        // If no new data, don't apply anything
        if (newData == null)
        {
            // But to support detecting if that is the case we have an interface to give the instance the info that
            // it didn't get the properties
            if (target is IChildPropertiesLoadCallback callbackReceiver)
                callbackReceiver.OnNoPropertiesLoaded();

            return;
        }

        // Need to register for deletion the orphaned Godot object
        // We avoid registering things that are child properties of things that should already be freed
        if (!recursive && newData is Node node)
        {
            TemporaryLoadedNodeDeleter.Instance.Register(node);
        }

        SaveApplyHelper.CopyJSONSavedPropertiesAndFields(target, newData,
            DefaultOnlyChildCopyIgnore);

        // Make sure target gets a chance to run stuff like normally deserialized objects
        InProgressObjectDeserialization.RunPrePropertyDeserializeActions(target);
        RunPostPropertyDeserializeActions(target);

        if (data.ReplaceReferences && serializer.ReferenceResolver != null)
        {
            UpdateObjectReference(serializer, newData, target);
        }

        // Recursively apply the reference update in case there are properties in the newData that also use the
        // attribute and register them for deletion
        foreach (var field in FieldsOf(newData))
        {
            if (UsesOnlyChildAssign(field.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                    out var recursiveChildData))
            {
                ApplyOnlyChildProperties(field.GetValue(newData), field.GetValue(target), serializer,
                    recursiveChildData!, true);
            }
        }

        foreach (var property in PropertiesOf(newData))
        {
            if (UsesOnlyChildAssign(property.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                    out var recursiveChildData))
            {
                ApplyOnlyChildProperties(property.GetValue(newData), property.GetValue(target), serializer,
                    recursiveChildData!, true);
            }
        }

        if (target is IChildPropertiesLoadCallback callbackReceiver2)
            callbackReceiver2.OnPropertiesLoaded();
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

/// <summary>
///   When a class has this attribute <see cref="DefaultThriveJSONConverter"/> is used to serialize it
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class UseThriveSerializerAttribute : Attribute
{
}

/// <summary>
///   Custom serializer for all Thrive types that don't need any special handling. They need to have the attribute
///   <see cref="UseThriveSerializerAttribute"/> to be detected
/// </summary>
internal class DefaultThriveJSONConverter : BaseThriveConverter
{
    private static readonly Type UseSerializerAttribute = typeof(UseThriveSerializerAttribute);
    private static readonly Type SceneLoadedAttribute = typeof(SceneLoadedClassAttribute);

    public DefaultThriveJSONConverter(ISaveContext context) : base(context)
    {
    }

    public DefaultThriveJSONConverter() : base(new SaveContext())
    {
    }

    public override bool CanConvert(Type objectType)
    {
        // Types with out custom attribute are supported
        if (objectType.CustomAttributes.Any(attr =>
                attr.AttributeType == UseSerializerAttribute || attr.AttributeType == SceneLoadedAttribute))
        {
            return true;
        }

        // Serializer attribute in parent type also applies it to child types
        if (objectType.GetCustomAttribute(UseSerializerAttribute, true) != null)
            return true;

        return false;
    }
}
