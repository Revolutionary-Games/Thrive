﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Saving;

/// <summary>
///   Main JSON conversion class for Thrive handling all our custom stuff
/// </summary>
public class ThriveJsonConverter : IDisposable
{
    private static readonly ThriveJsonConverter InstanceValue = new(new SaveContext(SimulationParameters.Instance));

    // ReSharper disable once NotAccessedField.Local
    /// <summary>
    ///   This variable is kept just in case accessing the context after the constructor is useful
    /// </summary>
    private readonly SaveContext context;

    private readonly JsonConverter[] thriveConverters;
    private readonly List<JsonConverter> thriveConvertersDynamicDeserialize;

    private readonly ThreadLocal<JsonSerializerSettings> currentJsonSettings = new();
    private bool disposed;

    // private IReferenceResolver referenceResolver = new Default;

    private ThriveJsonConverter(SaveContext context)
    {
        this.context = context;

        // All of the thrive serializers need to be registered here
        thriveConverters = new JsonConverter[]
        {
            new RegistryTypeConverter(context),
            new GodotColorConverter(),
            new GodotBasisConverter(),
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

            new EntityReferenceConverter(),

            // Converter for all types with a specific few attributes for this to be enabled
            new DefaultThriveJSONConverter(context),
        };

        thriveConvertersDynamicDeserialize = new List<JsonConverter> { new DynamicDeserializeObjectConverter(context) };
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
        return PerformWithSettings(
            settings => JsonConvert.SerializeObject(@object, type, Constants.SAVE_FORMATTING, settings));
    }

    public T? DeserializeObject<T>(string json)
    {
        return PerformWithSettings(settings => JsonConvert.DeserializeObject<T>(json, settings));
    }

    /// <summary>
    ///   Deserializes a fully dynamic object from json (object type is defined only in the json)
    /// </summary>
    /// <param name="json">JSON text to parse</param>
    /// <returns>The created object</returns>
    /// <exception cref="JsonException">If invalid json or the dynamic type is not allowed</exception>
    public object? DeserializeObjectDynamic(string json)
    {
        return PerformWithSettings(settings =>
        {
            // enable hack conversion
            settings.Converters = thriveConvertersDynamicDeserialize;

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
                GD.PrintErr(
                    "Expected an exception for the second try at JSON operation, but it succeeded, " +
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

    public static IEnumerable<FieldInfo> FieldsOf(object value, bool handleClassJSONSettings = true)
    {
        var type = value.GetType();
        var fields = type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(p => p.CustomAttributes.All(
            a => a.AttributeType != typeof(JsonIgnoreAttribute) &&
                a.AttributeType != typeof(CompilerGeneratedAttribute)));

        // Ignore fields that aren't public unless it has JsonPropertyAttribute
        fields = fields.Where(p =>
            (p.IsPublic && !p.IsInitOnly) ||
            p.CustomAttributes.Any(a => a.AttributeType == typeof(JsonPropertyAttribute)));

        // Ignore fields that are marked export without explicit JSON property
        fields = fields.Where(p =>
            !ExportWithoutExplicitJson(p.CustomAttributes));

        if (handleClassJSONSettings)
        {
            var settings = type.GetCustomAttribute<JsonObjectAttribute>();

            if (settings is { MemberSerialization: MemberSerialization.OptIn })
            {
                // Ignore all fields not opted in
                fields = fields.Where(
                    p => p.CustomAttributes.Any(a => a.AttributeType == typeof(JsonPropertyAttribute)));
            }
        }

        return fields;
    }

    public static IEnumerable<PropertyInfo> PropertiesOf(object value, bool handleClassJSONSettings = true)
    {
        var type = value.GetType();
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(
            p => p.CustomAttributes.All(
                a => a.AttributeType != typeof(JsonIgnoreAttribute)));

        // Ignore properties that don't have a public setter unless it has JsonPropertyAttribute
        properties = properties.Where(p => p.GetSetMethod() != null ||
            p.CustomAttributes.Any(a => a.AttributeType == typeof(JsonPropertyAttribute)));

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
                    p => p.CustomAttributes.Any(a => a.AttributeType == typeof(JsonPropertyAttribute)));
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

        return customAttributeData.All(attr => attr.AttributeType != typeof(JsonPropertyAttribute));
    }

    public static bool IsIgnoredGodotMember(string name, Type type)
    {
        return typeof(Node).IsAssignableFrom(type) && BaseNodeConverter.IsIgnoredGodotNodeMember(name);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var customRead = ReadCustomJson(reader, objectType, existingValue, serializer);

        if (customRead.Performed)
            return customRead.Read;

        if (reader.TokenType != JsonToken.StartObject)
            return null;

        if (serializer.ReferenceResolver == null)
            throw new InvalidOperationException("JsonSerializer must have reference resolver");

        var item = JObject.Load(reader);

        // Detect ref to already loaded object
        var refId = item[REF_PROPERTY];

        if (refId is { Type: JTokenType.String })
        {
            return serializer.ReferenceResolver.ResolveReference(serializer, refId.ValueNotNull<string>());
        }

        var objId = item[ID_PROPERTY];

        // Detect dynamic typing
        var type = item[TYPE_PROPERTY];

        if (type is { Type: JTokenType.String })
        {
            if (serializer.TypeNameHandling != TypeNameHandling.None)
            {
                var parts = type.ValueNotNull<string>().Split(',').Select(p => p.Trim()).ToList();

                if (parts.Count != 2 && parts.Count != 1)
                    throw new JsonException($"invalid {TYPE_PROPERTY} format");

                // Change to loading the other type
                objectType = serializer.SerializationBinder.BindToType(
                    parts.Count > 1 ? parts[1] : null, parts[0]);
            }
        }

        if (objectType == typeof(DynamicDeserializeObjectConverter))
            throw new JsonException("Dynamic dummy deserialize used object didn't specify type");

        // Detect scene loaded type
        bool sceneLoad = objectType.CustomAttributes.Any(
            attr => attr.AttributeType == typeof(SceneLoadedClassAttribute));

        var instance = !sceneLoad ?
            CreateDeserializedInstance(reader, objectType, serializer, item, out var alreadyConsumedItems) :
            CreateDeserializedFromScene(objectType, out alreadyConsumedItems);

        // Store the instance before loading properties to not break on recursive references
        if (objId is { Type: JTokenType.String })
        {
            serializer.ReferenceResolver.AddReference(serializer, objId.ValueNotNull<string>(), instance);
        }

        RunPrePropertyDeserializeActions(instance);

        bool Skip(string name, string key)
        {
            return SkipMember(name) || alreadyConsumedItems.Contains(key);
        }

        foreach (var field in FieldsOf(instance))
        {
            var name = DetermineKey(item, field.Name);

            if (Skip(field.Name, name))
                continue;

            var newData = ReadMember(name, field.FieldType, item, instance, reader, serializer);

            if (!UsesOnlyChildAssign(field.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                    out var data))
            {
                field.SetValue(instance, newData);
            }
            else
            {
                ApplyOnlyChildProperties(newData, field.GetValue(instance), serializer, data!);
            }
        }

        foreach (var property in PropertiesOf(instance))
        {
            var name = DetermineKey(item, property.Name);

            if (Skip(property.Name, name) || !item.ContainsKey(name))
            {
                continue;
            }

            if (!UsesOnlyChildAssign(property.GetCustomAttributes(typeof(AssignOnlyChildItemsOnDeserializeAttribute)),
                    out var data))
            {
                var set = property.GetSetMethodOnDeclaringType();

                if (set == null)
                {
                    throw new InvalidOperationException(
                        $"Json property used on a property ({name}) that has no (private) setter");
                }

                set.Invoke(instance, new[]
                {
                    ReadMember(name, property.PropertyType, item, instance, reader,
                        serializer),
                });
            }
            else
            {
                ApplyOnlyChildProperties(ReadMember(name, property.PropertyType, item, instance, reader,
                    serializer), property.GetValue(instance), serializer, data!);
            }
        }

        ReadCustomExtraFields(item, instance, reader, objectType, existingValue, serializer);

        // It is now assumed (because loading scenes and keeping references) that all loaded Nodes are deleted by
        // someone else (other than the ones that have just their properties grabbed in deserialize)

        RunPostPropertyDeserializeActions(instance);

        return instance;
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
                // Write the type of the instance always as we can't detect if the value matches the type of the field
                // We can at least check that the actual type is a subclass of something allowing dynamic typing
                bool baseIsDynamic = type.BaseType?.CustomAttributes.Any(attr =>
                    attr.AttributeType == typeof(JSONDynamicTypeAllowedAttribute)) == true;

                // If the current uses scene creation, dynamic type needs to be also in that case output
                bool currentIsAlwaysDynamic = type.CustomAttributes.Any(attr =>
                    attr.AttributeType == typeof(JSONAlwaysDynamicTypeAttribute) ||
                    attr.AttributeType == typeof(SceneLoadedClassAttribute));

                if (baseIsDynamic || currentIsAlwaysDynamic)
                {
                    writer.WritePropertyName(TYPE_PROPERTY);

                    writer.WriteValue(type.AssemblyQualifiedNameWithoutVersion());
                }
            }

            // First time writing, write all fields and properties
            foreach (var field in FieldsOf(value))
            {
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
                    GD.PrintErr("Problem trying to save a property: ", e);
                    writer.WritePropertyName(property.Name);
                    serializer.Serialize(writer, null);
                    continue;
                }

                WriteMember(property.Name, memberValue, property.PropertyType, type, writer,
                    serializer);
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
    ///   Default member writer for thrive types. Has special handling for some Thrive types,
    ///   others use default serializers
    /// </summary>
    protected virtual void WriteMember(string name, object memberValue, Type memberType, Type objectType,
        JsonWriter writer,
        JsonSerializer serializer)
    {
        if (SkipMember(name))
            return;

        writer.WritePropertyName(name);

        // Special handle types (none currently)

        // Use default serializer on everything else
        serializer.Serialize(writer, memberValue);
    }

    protected virtual void WriteCustomExtraFields(JsonWriter writer, object value, JsonSerializer serializer)
    {
    }

    protected virtual object? ReadMember(string name, Type memberType, JObject item, object? instance,
        JsonReader reader,
        JsonSerializer serializer)
    {
        var value = item[name];

        // Special handle types (none currently)

        // Use default get on everything else
        return value?.ToObject(memberType, serializer);
    }

    protected virtual void ReadCustomExtraFields(JObject item, object instance, JsonReader reader, Type objectType,
        object? existingValue, JsonSerializer serializer)
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

    private void RunPrePropertyDeserializeActions(object instance)
    {
        HandleSaveLoadedTracked(instance);
    }

    private void HandleSaveLoadedTracked(object instance)
    {
        // Any loaded object supports knowing if it was loaded from a save
        if (instance is ISaveLoadedTracked tracked)
        {
            tracked.IsLoadedFromSave = true;
        }
    }

    private void RunPostPropertyDeserializeActions(object instance)
    {
        // TODO: these should be called after loading the whole object tree
        if (instance is ISaveLoadable loadable)
            loadable.FinishLoading(Context);
    }

    private bool UsesOnlyChildAssign(IEnumerable<Attribute> customAttributes,
        out AssignOnlyChildItemsOnDeserializeAttribute? data)
    {
        data = customAttributes.FirstOrDefault() as AssignOnlyChildItemsOnDeserializeAttribute;

        if (data == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Figures out what constructor to call for an object that is to be loaded from JSON
    /// </summary>
    /// <returns>The created object</returns>
    /// <exception cref="JsonException">If can't be created</exception>
    private object CreateDeserializedInstance(JsonReader reader, Type objectType, JsonSerializer serializer,
        JObject item, out HashSet<string> alreadyConsumedItems)
    {
        // Find a constructor we can call
        ConstructorInfo? constructor = null;

        // Consider private constructors but ignore those that do not have the [JsonConstructor] attribute.
        var privateJsonConstructors = objectType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Where(
            c => c.CustomAttributes.Any(a => a.AttributeType == typeof(JsonConstructorAttribute)));

        var potentialConstructors = objectType.GetConstructors().Concat(
            privateJsonConstructors);

        foreach (var candidate in potentialConstructors)
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

        alreadyConsumedItems = new HashSet<string>();

        foreach (var param in constructor.GetParameters())
        {
            alreadyConsumedItems.Add(DetermineKey(item, param.Name));
        }

        // Load constructor params
        object?[] constructorArgs = constructor.GetParameters()
            .Select(p => ReadMember(DetermineKey(item, p.Name),
                p.ParameterType, item, null, reader, serializer)).ToArray();

        var instance = constructor.Invoke(constructorArgs);

        // Early load is also supported for non-scene loaded objects
        if (instance is IGodotEarlyNodeResolve early)
        {
            early.ResolveNodeReferences();
        }

        return instance;
    }

    /// <summary>
    ///   Creates a deserialized object by loading a Godot scene and instantiating it
    /// </summary>
    /// <returns>Instance of the scene loaded object</returns>
    /// <exception cref="JsonException">If couldn't create a new instance</exception>
    private object CreateDeserializedFromScene(Type objectType, out HashSet<string> alreadyConsumedItems)
    {
        alreadyConsumedItems = new HashSet<string>();

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
        HandleSaveLoadedTracked(instance);

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

    private void RecursivelyApplyEarlyNodeResolve(object instance)
    {
        // Child objects are also told if they are loaded from a save
        HandleSaveLoadedTracked(instance);

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
        RunPrePropertyDeserializeActions(target);
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
        return objectType.CustomAttributes.Any(
            attr => attr.AttributeType == UseSerializerAttribute || attr.AttributeType == SceneLoadedAttribute);
    }
}
