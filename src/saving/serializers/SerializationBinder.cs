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
    private static readonly Type DynamicTypeAllowedAttribute = typeof(JSONDynamicTypeAllowedAttribute);
    private static readonly Type AlwaysDynamicTypeAttribute = typeof(JSONAlwaysDynamicTypeAttribute);
    private static readonly Type SceneLoadedClassAttribute = typeof(SceneLoadedClassAttribute);
    private static readonly Type CustomizedRegistryTypeAttribute = typeof(CustomizedRegistryTypeAttribute);

    public override Type BindToType(string? assemblyName, string typeName)
    {
        var type = base.BindToType(assemblyName, typeName);
        var originalType = type;

        if (type.IsArray)
            type = type.GetElementType() ?? throw new Exception("Array type doesn't have element type");

        if (type.IsAbstract || type.IsInterface)
            throw new JsonException($"Dynamically typed JSON object is of interface or abstract type {typeName}");

        if (type.CustomAttributes.Any(attr => attr.AttributeType == DynamicTypeAllowedAttribute ||
                attr.AttributeType == AlwaysDynamicTypeAttribute || attr.AttributeType == SceneLoadedClassAttribute ||
                attr.AttributeType == CustomizedRegistryTypeAttribute))
        {
            // Allowed type
            return originalType;
        }

        if (typeof(int) == type)
            return originalType;

        if (typeof(Vector4) == type)
            return originalType;

        if (typeof(Vector4[,]) == type)
            return originalType;

        throw new JsonException($"Dynamically typed JSON object is not allowed to be {typeName}");
    }
}

/// <summary>
///   When a class has this attribute this type is allowed to be dynamically de-serialized from json,
///   as well as the type is written if something is a subclass of a type with this attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
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
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class JSONAlwaysDynamicTypeAttribute : Attribute
{
}
