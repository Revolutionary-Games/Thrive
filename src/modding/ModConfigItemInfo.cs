using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Class that holds the info of a mod config item from a 'mod_config.json' file
/// </summary>
public class ModConfigItemInfo : HBoxContainer
{
    public string? ID { get; set; }

    [JsonProperty("Display Name")]
    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? Type { get; set; }

    // Exclusively for Enums/Option type
    public object? Options { get; set; }

    public object? Value { get; set; }

    public Control? ConfigNode { get; set; }

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

    /// <summary>
    ///   Updates the Value variable based on the gui element
    /// </summary>
    public object UpdateInternalValue()
    {
        var nodeChildren = GetChildren();
        if (nodeChildren.Count > 1)
        {
            var childUIElement = GetChild(1);
            switch (Type?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty)
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
                    if (numberUIElement != null)
                    {
                        Value = numberUIElement.Value;
                    }

                    break;
                case "option":
                case "enum":
                case "o":
                    var optionUIElement = childUIElement as OptionButton;
                    if (optionUIElement != null)
                    {
                        Value = optionUIElement.Selected;
                    }

                    break;
                case "bool":
                case "boolean":
                case "b":
                    var buttonUIElement = childUIElement as Button;
                    if (buttonUIElement != null)
                    {
                        Value = buttonUIElement.Pressed;
                    }

                    break;
                case "string":
                case "s":
                    var stringUIElement = childUIElement as LineEdit;
                    if (stringUIElement != null)
                    {
                        Value = stringUIElement.Text;
                    }

                    break;
                case "color":
                case "colour":
                case "c":
                case "alphacolor":
                case "alphacolour":
                case "ac":
                    var colorUIElement = childUIElement as ColorPickerButton;
                    if (colorUIElement != null)
                    {
                        Value = colorUIElement.Color.ToHtml();
                    }

                    break;
            }
        }

        return Value!;
    }

    /// <summary>
    ///   Updates the UI element based on the Value variable
    /// </summary>
    public void UpdateUI()
    {
        if (GetChildCount() < 2)
        {
            return;
        }

        var childUIElement = GetChild(1);
        switch (Type?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty)
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
                if (numberUIElement != null)
                {
                    numberUIElement.Value = Convert.ToDouble(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "bool":
            case "boolean":
            case "b":
                var buttonUIElement = childUIElement as Button;
                if (buttonUIElement != null)
                {
                    buttonUIElement.Pressed = Convert.ToBoolean(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "string":
            case "s":
                var stringUIElement = childUIElement as LineEdit;
                if (stringUIElement != null)
                {
                    stringUIElement.Text = Convert.ToString(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "option":
            case "enum":
            case "o":
                var optionUIElement = childUIElement as OptionButton;
                if (optionUIElement != null)
                {
                    optionUIElement.Selected = Convert.ToInt32(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "color":
            case "colour":
            case "c":
            case "alphacolor":
            case "alphacolour":
            case "ac":
                var colorUIElement = childUIElement as ColorPickerButton;
                if (colorUIElement != null && Value != null)
                {
                    colorUIElement.Color = new Color((string)Value);
                }

                break;
        }
    }

    /// <summary>
    ///   Returns a list of string based on the options the OptionButton can have
    /// </summary>
    public List<string> GetAllOptions()
    {
        var optionsJArray = Options as JArray;
        if (optionsJArray == null)
        {
            return new List<string>();
        }

        return optionsJArray.ToObject<List<string>>()!;
    }

    public override int GetHashCode()
    {
        return (ID, Type).GetHashCode();
    }
}
