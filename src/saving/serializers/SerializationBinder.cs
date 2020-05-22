using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
///   Controls which dynamic type classes are allowed to be loaded from json
/// </summary>
public class SerializationBinder : DefaultSerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        var type = base.BindToType(assemblyName, typeName);

        if (type.CustomAttributes.Any((attr) =>
            attr.AttributeType == typeof(JSONDynamicTypeAllowedAttribute)))
        {
            // Allowed type
            return type;
        }

        throw new JsonException($"Dynamically typed JSON object is not allowed to be {typeName}");
    }
}

/// <summary>
///   When a class has this attribute this type is allowed to be dynamically de-serialized from json,
///   as well as the type is written if something is a subclass of a type with this attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class JSONDynamicTypeAllowedAttribute : Attribute
{
}
