using System;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
///   Controls which dynamic type classes are allowed to be loaded from json (or with specific BinaryFormatters)
/// </summary>
public class SerializationBinder : DefaultSerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        var type = base.BindToType(assemblyName, typeName);

        if (type.CustomAttributes.Any(attr =>
                attr.AttributeType == typeof(JSONDynamicTypeAllowedAttribute) ||
                attr.AttributeType == typeof(JSONAlwaysDynamicTypeAttribute) ||
                attr.AttributeType == typeof(SceneLoadedClassAttribute)))
        {
            // Allowed type
            return type;
        }

        if (typeof(int) == type)
            return type;

        if (typeof(Vector4) == type)
            return type;

        if (typeof(Vector4[,]) == type)
            return type;

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

/// <summary>
///   When a class has this the dynamic type is always written (compared to JSONDynamicTypeAllowedAttribute only
///   adding if the current variable type differs from its contents), used for Godot derived classes that need to be
///   easily loadable from a single collection.
/// </summary>
/// <remarks>
///   <para>
///     For example the MicrobeStage dynamic entities use this so that they can be stored in a single list
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class JSONAlwaysDynamicTypeAttribute : Attribute
{
}
