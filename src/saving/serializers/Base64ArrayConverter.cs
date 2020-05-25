using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

/// <summary>
///   Saves an array in base 64 form to JSON in order to save space
/// </summary>
public class Base64ArrayConverter : BaseThriveConverter
{
    public Base64ArrayConverter(ISaveContext context) : base(context)
    {
    }

    public static FieldInfo GetFieldForConstructorArgument(string name, List<FieldInfo> fields)
    {
        foreach (var field in fields)
        {
            if (field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return field;
        }

        return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsArray;
    }

    protected override (object read, bool performed) ReadCustomJson(JsonReader reader, Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var encoded = serializer.Deserialize<string>(reader);

        if (string.IsNullOrEmpty(encoded))
            return (null, true);

        BinaryFormatter bf = new BinaryFormatter { Binder = new SerializationBinder() };

        using (var dataReader = new MemoryStream(Convert.FromBase64String(encoded)))
        {
            int count = (int)bf.Deserialize(dataReader);
            int dimensions = (int)bf.Deserialize(dataReader);

            throw new NotImplementedException();
        }
    }

    protected override bool WriteCustomJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var type = value.GetType();
        var elementType = type.GetElementType();
        var fields = FieldsOf(elementType).ToList();
        var asArray = (Array)value;

        var constructor = DetectConstructorToUse(elementType, fields);

        var usedFields = constructor.GetParameters()
            .Select((param) => GetFieldForConstructorArgument(param.Name, fields)).ToList();

        int fieldSize = usedFields.Sum((field) => Marshal.SizeOf(field.FieldType));

        int count = asArray.Length;
        int dimensions = asArray.Rank;

        BinaryFormatter bf = new BinaryFormatter();

        // Preallocate some size to the memory stream
        using (var dataWriter = new MemoryStream { Capacity = (fieldSize * count * dimensions) + 4 })
        {
            // Size
            bf.Serialize(dataWriter, count);
            bf.Serialize(dataWriter, dimensions);

            // Objects
            for (int dimension = 0; dimension < dimensions; ++dimension)
            {
                for (int i = 0; i < count; ++i)
                {
                    var obj = asArray.GetValue(i, dimension);

                    foreach (var field in usedFields)
                    {
                        bf.Serialize(dataWriter, obj);
                    }
                }
            }

            serializer.Serialize(writer, Convert.ToBase64String(dataWriter.GetBuffer()));
        }

        return true;
    }

    private static ConstructorInfo DetectConstructorToUse(Type type, List<FieldInfo> fields)
    {
        foreach (var constructor in type.GetConstructors())
        {
            var paramList = constructor.GetParameters();

            if (paramList.Length < 1)
                continue;

            // Just detect the first constructor that has corresponding fields as the best
            bool matches = true;

            foreach (var param in paramList)
            {
                if (GetFieldForConstructorArgument(param.Name, fields) == null)
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return constructor;
        }

        throw new ArgumentException("couldn't find constructor to serialize base64 array items with");
    }
}
