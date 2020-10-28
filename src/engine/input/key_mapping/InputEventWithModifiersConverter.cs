using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class InputEventWithModifiersConverter : JsonConverter<InputEventWithModifiers>
{
    public override void WriteJson(JsonWriter writer, InputEventWithModifiers value, JsonSerializer serializer)
    {
        var val = value;
        writer.WriteStartObject();

        writer.WritePropertyName("Type");
        switch (val)
        {
            case InputEventKey inputKey:
                writer.WriteValue("Key");

                writer.WritePropertyName("Scancode");
                writer.WriteValue(inputKey.Scancode);
                break;
            case InputEventMouseButton mouseButton:
                writer.WriteValue("MouseButton");

                writer.WritePropertyName("ButtonIndex");
                writer.WriteValue(mouseButton.ButtonIndex);
                break;
            default:
                throw new NotSupportedException($"InputType {val.GetType()} not supported");
        }

        writer.WritePropertyName("Alt");
        writer.WriteValue(val.Alt);

        writer.WritePropertyName("Control");
        writer.WriteValue(val.Control);

        writer.WritePropertyName("Shift");
        writer.WriteValue(val.Shift);

        writer.WriteEndObject();
    }

    public override InputEventWithModifiers ReadJson(JsonReader reader, Type objectType,
        InputEventWithModifiers existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        var j = JObject.Load(reader);
        InputEventWithModifiers result;
        var type = j["Type"] !.Value<string>();
        switch (type)
        {
            case "Key":
                result = new InputEventKey();
                ((InputEventKey)result).Scancode = j["Scancode"] !.Value<uint>();
                break;
            case "MouseButton":
                result = new InputEventMouseButton();
                ((InputEventMouseButton)result).ButtonIndex = j["ButtonIndex"] !.Value<int>();
                break;
            default:
                throw new NotSupportedException($"InputType {type} not supported");
        }

        result.Alt = j["Alt"] !.Value<bool>();
        result.Control = j["Control"] !.Value<bool>();
        result.Shift = j["Shift"] !.Value<bool>();

        return result;
    }
}
