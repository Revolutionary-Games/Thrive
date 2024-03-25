using System;
using System.Numerics;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Xoshiro.Base;
using Xoshiro.PRNG32;
using Xoshiro.PRNG64;

/// <summary>
///   Support for JSON loading and saving of custom Thrive random types (<see cref="Xoshiro"/>).
/// </summary>
public class RandomConverter : JsonConverter
{
    private const string SeedFieldName0 = "s0";
    private const string SeedFieldName1 = "s1";
    private const string SeedFieldName2 = "s2";
    private const string SeedFieldName3 = "s3";

    private const string JsonField0 = "s0";
    private const string JsonField1 = "s1";
    private const string JsonField2 = "s2";
    private const string JsonField3 = "s3";

    // These are used to store the data when deserializing before constructing the random instance
    private readonly uint[] initialValueStorage32 = new uint[4];
    private readonly ulong[] initialValueStorage64 = new ulong[4];

    private readonly Type xoshiro32BitBase = typeof(Xoshiro32Base);
    private readonly Type xoshiro64BitBase = typeof(Xoshiro64Base);

    // 256** variant
    private readonly Type xoshiro256StarStar = typeof(XoShiRo256starstar);

    private readonly FieldInfo field0Xoshiro256StarStar = typeof(XoShiRo256starstar).GetField(SeedFieldName0,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field1Xoshiro256StarStar = typeof(XoShiRo256starstar).GetField(SeedFieldName1,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field2Xoshiro256StarStar = typeof(XoShiRo256starstar).GetField(SeedFieldName2,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field3Xoshiro256StarStar = typeof(XoShiRo256starstar).GetField(SeedFieldName3,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    // 256+ variant
    private readonly Type xoshiro256Plus = typeof(XoShiRo256plus);

    private readonly FieldInfo field0Xoshiro256Plus = typeof(XoShiRo256plus).GetField(SeedFieldName0,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field1Xoshiro256Plus = typeof(XoShiRo256plus).GetField(SeedFieldName1,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field2Xoshiro256Plus = typeof(XoShiRo256plus).GetField(SeedFieldName2,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field3Xoshiro256Plus = typeof(XoShiRo256plus).GetField(SeedFieldName3,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    // 128** variant
    private readonly Type xoshiro128StarStar = typeof(XoShiRo128starstar);

    private readonly FieldInfo field0Xoshiro128StarStar = typeof(XoShiRo128starstar).GetField(SeedFieldName0,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field1Xoshiro128StarStar = typeof(XoShiRo128starstar).GetField(SeedFieldName1,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field2Xoshiro128StarStar = typeof(XoShiRo128starstar).GetField(SeedFieldName2,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field3Xoshiro128StarStar = typeof(XoShiRo128starstar).GetField(SeedFieldName3,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    // 128+ variant
    private readonly Type xoshiro128Plus = typeof(XoShiRo128plus);

    private readonly FieldInfo field0Xoshiro128Plus = typeof(XoShiRo128plus).GetField(SeedFieldName0,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field1Xoshiro128Plus = typeof(XoShiRo128plus).GetField(SeedFieldName1,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field2Xoshiro128Plus = typeof(XoShiRo128plus).GetField(SeedFieldName2,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    private readonly FieldInfo field3Xoshiro128Plus = typeof(XoShiRo128plus).GetField(SeedFieldName3,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new RandomTypeSeedFieldNotFoundException();

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        switch (value)
        {
            case XoShiRo256starstar:
            {
                writer.WritePropertyName(JsonField0);
                serializer.Serialize(writer, field0Xoshiro256StarStar.GetValue(value));

                writer.WritePropertyName(JsonField1);
                serializer.Serialize(writer, field1Xoshiro256StarStar.GetValue(value));

                writer.WritePropertyName(JsonField2);
                serializer.Serialize(writer, field2Xoshiro256StarStar.GetValue(value));

                writer.WritePropertyName(JsonField3);
                serializer.Serialize(writer, field3Xoshiro256StarStar.GetValue(value));
                break;
            }

            case XoShiRo256plus:
            {
                writer.WritePropertyName(JsonField0);
                serializer.Serialize(writer, field0Xoshiro256Plus.GetValue(value));

                writer.WritePropertyName(JsonField1);
                serializer.Serialize(writer, field1Xoshiro256Plus.GetValue(value));

                writer.WritePropertyName(JsonField2);
                serializer.Serialize(writer, field2Xoshiro256Plus.GetValue(value));

                writer.WritePropertyName(JsonField3);
                serializer.Serialize(writer, field3Xoshiro256Plus.GetValue(value));
                break;
            }

            case XoShiRo128starstar:
            {
                writer.WritePropertyName(JsonField0);
                serializer.Serialize(writer, field0Xoshiro128StarStar.GetValue(value));

                writer.WritePropertyName(JsonField1);
                serializer.Serialize(writer, field1Xoshiro128StarStar.GetValue(value));

                writer.WritePropertyName(JsonField2);
                serializer.Serialize(writer, field2Xoshiro128StarStar.GetValue(value));

                writer.WritePropertyName(JsonField3);
                serializer.Serialize(writer, field3Xoshiro128StarStar.GetValue(value));
                break;
            }

            case XoShiRo128plus:
            {
                writer.WritePropertyName(JsonField0);
                serializer.Serialize(writer, field0Xoshiro128Plus.GetValue(value));

                writer.WritePropertyName(JsonField1);
                serializer.Serialize(writer, field1Xoshiro128Plus.GetValue(value));

                writer.WritePropertyName(JsonField2);
                serializer.Serialize(writer, field2Xoshiro128Plus.GetValue(value));

                writer.WritePropertyName(JsonField3);
                serializer.Serialize(writer, field3Xoshiro128Plus.GetValue(value));
                break;
            }

            default:
                throw new JsonException("Unknown xoshiro random type to serialize");
        }

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonException("Expected start of JSON object");

        reader.Read();

        bool is32Bit = xoshiro32BitBase.IsAssignableFrom(objectType);

        // In case the JSON is badly formed, reset the value storages to not allow duplicating a random that was
        // previously read
        if (is32Bit)
        {
            initialValueStorage32[0] = 0;
            initialValueStorage32[1] = 0;
            initialValueStorage32[2] = 0;
            initialValueStorage32[3] = 0;
        }
        else
        {
            initialValueStorage64[0] = 0;
            initialValueStorage64[1] = 0;
            initialValueStorage64[2] = 0;
            initialValueStorage64[3] = 0;
        }

        int seedsRead = 0;

        while (reader.TokenType == JsonToken.PropertyName)
        {
            var name = reader.Value as string;

            if (name == null)
                throw new JsonException("Expected JSON property name at this point");

            reader.Read();

            if (reader.TokenType != JsonToken.Integer)
                throw new JsonException("Expected an integer seed value");

            bool read = true;

            if (is32Bit)
            {
                switch (name)
                {
                    case JsonField0:
                        initialValueStorage32[0] = ReadValue32(reader);
                        break;

                    case JsonField1:
                        initialValueStorage32[1] = ReadValue32(reader);
                        break;
                    case JsonField2:
                        initialValueStorage32[2] = ReadValue32(reader);
                        break;
                    case JsonField3:
                        initialValueStorage32[3] = ReadValue32(reader);
                        break;

                    default:
                        GD.PrintErr($"Unknown JSON property name in random, ignoring it: {name}");
                        read = false;
                        break;
                }
            }
            else
            {
                switch (name)
                {
                    case JsonField0:
                        initialValueStorage64[0] = ReadValue64(reader);
                        break;

                    case JsonField1:
                        initialValueStorage64[1] = ReadValue64(reader);
                        break;
                    case JsonField2:
                        initialValueStorage64[2] = ReadValue64(reader);
                        break;
                    case JsonField3:
                        initialValueStorage64[3] = ReadValue64(reader);
                        break;

                    default:
                        GD.PrintErr($"Unknown JSON property name in random, ignoring it: {name}");
                        read = false;
                        break;
                }
            }

            if (read)
            {
                // This protects against missing all random data but badly formed JSON could only specify a single
                // value to get greatly reduced randomness from the random generator
                ++seedsRead;
            }

            reader.Read();
        }

        // There used to be backwards compatibility with no saved random state, but there should be a save breakage
        // point so we can now enforce random state existing
        if (seedsRead < 4)
            throw new JsonException("Expected 4 seed values for random");

        if (objectType == xoshiro256StarStar)
        {
            if (is32Bit)
                throw new Exception("Invalid random seed configuration");

            return new XoShiRo256starstar(initialValueStorage64);
        }

        if (objectType == xoshiro256Plus)
        {
            if (is32Bit)
                throw new Exception("Invalid random seed configuration");

            return new XoShiRo256plus(initialValueStorage64);
        }

        if (objectType == xoshiro128StarStar)
        {
            if (!is32Bit)
                throw new Exception("Invalid random seed configuration");

            return new XoShiRo128starstar(initialValueStorage32);
        }

        if (objectType == xoshiro128Plus)
        {
            if (!is32Bit)
                throw new Exception("Invalid random seed configuration");

            return new XoShiRo128plus(initialValueStorage32);
        }

        throw new UnknownXoshiroConcreteTypeException();
    }

    public override bool CanConvert(Type objectType)
    {
        return xoshiro32BitBase.IsAssignableFrom(objectType) || xoshiro64BitBase.IsAssignableFrom(objectType);
    }

    private static ulong ReadValue64(JsonReader reader)
    {
        switch (reader.Value ?? throw new JsonException("Read integer is null"))
        {
            case BigInteger bigInteger:
                return (ulong)bigInteger;
            case ulong alreadyCorrect:
                return alreadyCorrect;
            case long almostCorrect:
                return (ulong)almostCorrect;
            case int basicNumber:
                return (ulong)basicNumber;
        }

        throw new JsonException($"Unknown value type to convert to ulong: {reader.Value.GetType()}");
    }

    private static uint ReadValue32(JsonReader reader)
    {
        switch (reader.Value ?? throw new JsonException("Read integer is null"))
        {
            case BigInteger bigInteger:
                return (uint)bigInteger;
            case uint alreadyCorrect:
                return alreadyCorrect;
            case int almostCorrect:
                return (uint)almostCorrect;
        }

        throw new JsonException($"Unknown value type to convert to uint: {reader.Value.GetType()}");
    }

    private class UnknownXoshiroConcreteTypeException : JsonException
    {
        public UnknownXoshiroConcreteTypeException() : base(
            "Unknown xoshiro random to create. Concrete type must be in the JSON field to make this " +
            "deserializer know what to create.")
        {
        }
    }

    private class RandomTypeSeedFieldNotFoundException : Exception
    {
        public RandomTypeSeedFieldNotFoundException() : base(
            "Could not find expected seed field by name in random generator")
        {
        }
    }
}
