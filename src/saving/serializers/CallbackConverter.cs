using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Converter for callback methods
/// </summary>
public class CallbackConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        string type;
        MethodInfo method;
        object? target;

        if (value is MulticastDelegate @delegate)
        {
            type = "delegate";

            method = @delegate.Method;
            target = @delegate.Target;
        }
        else
        {
            throw new JsonSerializationException("unexpected callback type to serialize");
        }

        if (method.IsStatic)
        {
            throw new JsonException("Callback is not supported on static methods");
        }

        writer.WriteStartObject();

        writer.WritePropertyName("CallbackType");
        serializer.Serialize(writer, type);

        writer.WritePropertyName("TargetType");
        if (target != null)
        {
            serializer.Serialize(writer, target.GetType().AssemblyQualifiedNameWithoutVersion());
        }
        else
        {
            writer.WriteNull();
        }

        writer.WritePropertyName("Target");
        serializer.Serialize(writer, target);

        writer.WritePropertyName("Method");
        serializer.Serialize(writer, method.Name);

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return null;

        // TODO: switch to a reader approach like other newer converters
        var item = JObject.Load(reader);

        string type;
        string targetTypeName;

        try
        {
            if (item == null)
                throw new JsonException("Object to read callback from is null");

            type = item["CallbackType"]!.ToObject<string>() ?? throw new JsonException("missing CallbackType");

            // TODO: handling for static methods (when target is null)

            targetTypeName = item["TargetType"]!.ToObject<string>() ?? throw new JsonException("missing TargetType");

            if (targetTypeName == null)
                throw new NullReferenceException();
        }
        catch (Exception e) when (
            e is NullReferenceException or ArgumentNullException)
        {
            throw new JsonException("can't read callback (missing property)", e);
        }

        if (type != "delegate")
            throw new JsonException("Callback with unknown type: " + type);

        var targetType = Type.GetType(targetTypeName);

        if (targetType == null)
        {
            throw new JsonException("Callback target type was not found: " + targetTypeName);
        }

        if (targetType.CustomAttributes.All(a => a.AttributeType != typeof(DeserializedCallbackTargetAttribute)))
        {
            throw new JsonException("Callback is not allowed to target type: " + targetType);
        }

        object target;
        string methodName;

        try
        {
            target = item["Target"]!.ToObject(targetType, serializer) ?? throw new JsonException("missing Target");
            methodName = item["Method"]!.ToObject<string>() ?? throw new JsonException("missing Method name");
        }
        catch (Exception e) when (
            e is NullReferenceException or ArgumentNullException)
        {
            throw new JsonException("can't read callback (missing property)", e);
        }

        if (target == null)
        {
            throw new JsonException("Callback target deserialization result is null");
        }

        // TODO: do we need to allow function overloading here? It currently most likely doesn't work correctly
        var method = targetType.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            throw new JsonException("Callback method in target class not found: " + methodName);
        }

        if (method.CustomAttributes.All(a => a.AttributeType != typeof(DeserializedCallbackAllowedAttribute)))
        {
            throw new JsonException("Callback is not allowed to have method: " + method);
        }

        if (method.IsStatic)
        {
            throw new JsonException("Callback is not supported on static methods");
        }

        return method.CreateDelegate(objectType, target);
    }

    public override bool CanConvert(Type objectType)
    {
        if (typeof(MulticastDelegate).IsAssignableFrom(objectType))
            return true;

        return false;
    }
}

/// <summary>
///   When a class has this attribute a callback is allowed to have that type as its target on deserialize
/// </summary>
/// <remarks>
///   <para>
///     If an object owns whatever calls the callback, it likely also needs
///     <code>[JsonObject(IsReference = true)]</code> to work correctly.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class DeserializedCallbackTargetAttribute : Attribute
{
}

/// <summary>
///   Specifies that a method is valid deserialized callback target
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DeserializedCallbackAllowedAttribute : Attribute
{
}
