using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
///   Controls which dynamic type classes are allowed to be loaded from json
/// </summary>
public class SerializationBinder : DefaultSerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        // TODO: switch to using an attribute on the allowed classes
        switch (typeName)
        {
            // Allowed types
            case nameof(MicrobeSpecies):
                break;
            default:
                throw new JsonException($"Dynamically typed JSON object is not allowed to be {typeName}");
        }

        return base.BindToType(assemblyName, typeName);
    }
}
