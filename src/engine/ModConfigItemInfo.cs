using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ModConfigItemInfo : HBoxContainer
{
    public string ID { get; set; }

    [JsonProperty("Display Name")]
    public string DisplayName { get; set; }

    public string Description { get; set; }

    public string Type { get; set; }

    // Exclusively for Enums/Option type
    public object Options { get; set; }

    public object Value { get; set; }

    [JsonProperty("Min")]
    public float MinimumValue { get; set; }

    [JsonProperty("Max")]
    public float MaximumValue { get; set; } = 99f;

    public override bool Equals(object other)
    {
        var item = other as ModConfigItemInfo;

        if (item == null)
        {
            return false;
        }

        return ID == item.ID && Type == item.Type;
    }

    public object UpdateInternalValue()
    {
        var nodeChildren = GetChildren();
        if (nodeChildren.Count > 1)
        {
            var childUIElement = GetChild(1);
            switch (Type.ToLower(CultureInfo.InvariantCulture))
            {
                case "int":
                case "integer":
                case "i":
                case "int range":
                case "integer range":
                case "ir":
                case "float":
                case "f":
                case "float range":
                case "fr":
                    var numberUIElement = childUIElement as Range;
                    Value = numberUIElement.Value;
                    break;
                case "option":
                case "enum":
                case "o":
                    var optionUIElement = childUIElement as OptionButton;
                    Value = optionUIElement.Selected;
                    break;
                case "bool":
                case "boolean":
                case "b":
                    var buttonUIElement = childUIElement as Button;
                    Value = buttonUIElement.Pressed;
                    break;
                case "string":
                case "s":
                    var stringUIElement = childUIElement as LineEdit;
                    Value = stringUIElement.Text;
                    break;
                case "color":
                case "colour":
                case "c":
                case "alphacolor":
                case "alphacolour":
                case "ac":
                    var colorUIElement = childUIElement as ColorPickerButton;
                    Value = colorUIElement.Color.ToHtml();
                    break;
            }
        }

        return Value;
    }

    public void UpdateUI()
    {
        if (GetChildCount() < 2)
        {
            return;
        }

        var childUIElement = GetChild(1);
        switch (Type.ToLower(CultureInfo.InvariantCulture))
        {
                case "int":
                case "integer":
                case "i":
                case "int range":
                case "integer range":
                case "ir":
                case "float":
                case "f":
                case "float range":
                case "fr":
                    var numberUIElement = childUIElement as Range;
                    numberUIElement.Value = Convert.ToDouble(Value ?? default(double), CultureInfo.InvariantCulture);
                    break;
                case "bool":
                case "boolean":
                case "b":
                    var buttonUIElement = childUIElement as Button;
                    buttonUIElement.Pressed = (bool)(Value ?? default(bool));
                    break;
                case "string":
                case "s":
                    var stringUIElement = childUIElement as LineEdit;
                    stringUIElement.Text = (string)(Value ?? default(string));
                    break;
                case "option":
                case "enum":
                case "o":
                    var optionUIElement = childUIElement as OptionButton;
                    optionUIElement.Selected = Convert.ToInt32(Value ?? default(int), CultureInfo.InvariantCulture);
                    break;
                case "color":
                case "colour":
                case "c":
                case "alphacolor":
                case "alphacolour":
                case "ac":
                    var colorUIElement = childUIElement as ColorPickerButton;
                    colorUIElement.Color = new Color((string)Value ?? default(string));
                    break;
                default:
                    break;
        }
    }

    public List<string> GetAllOptions()
    {
        var optionsJArray = Options as JArray;
        if (optionsJArray == null)
        {
            return new List<string>();
        }

        return optionsJArray.ToObject<List<string>>();
    }

    public override int GetHashCode()
    {
        return (ID, Type).GetHashCode();
    }
}
