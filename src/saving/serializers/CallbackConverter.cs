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

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        string type;
        MethodInfo method;
        object target;

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

        writer.WriteStartObject();

        writer.WritePropertyName("CallbackType");
        serializer.Serialize(writer, type);

        writer.WritePropertyName("TargetType");
        serializer.Serialize(writer, target.GetType().AssemblyQualifiedNameWithoutVersion());

        writer.WritePropertyName("Target");
        serializer.Serialize(writer, target);

        writer.WritePropertyName("Method");
        serializer.Serialize(writer, method);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            return null;
        }

        var item = JObject.Load(reader);

        string type;
        string targetTypeName;

        try
        {
            // ReSharper disable PossibleNullReferenceException
            type = item["CallbackType"].ToObject<string>();
            targetTypeName = item["TargetType"].ToObject<string>();

            if (targetTypeName == null)
                throw new NullReferenceException();

            // ReSharper restore PossibleNullReferenceException
        }
        catch (Exception e) when (
            e is NullReferenceException ||
            e is ArgumentNullException)
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
            // ReSharper disable PossibleNullReferenceException
            target = item["Target"].ToObject(targetType, serializer);
            methodName = item["Method"]["Name"].ToObject<string>();

            if (methodName == null)
                throw new NullReferenceException();

            // ReSharper restore PossibleNullReferenceException
        }
        catch (Exception e) when (
            e is NullReferenceException ||
            e is ArgumentNullException)
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
