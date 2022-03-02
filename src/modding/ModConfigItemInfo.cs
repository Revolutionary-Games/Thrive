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
    public enum ConfigType
    {
        Integer,
    }

    public string? ID { get; set; }

    [JsonProperty("Display Name")]
    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? Type { get; set; }

    // Exclusively for Enums/Option type
    public object? Options { get; set; }

    public object? Value { get; set; }

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
    /// <returns> Returns the current value of the Config Item. </returns>
    public object UpdateInternalValue()
    {
        var nodeChildren = GetChildren();
        if (nodeChildren.Count > 1)
        {
            var childUIElement = GetChild(1);
            switch (Type?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty)
            {
                case "integer":
                case "integer range":
                case "float":
                case "float range":
                    var numberUIElement = childUIElement as Range;
                    if (numberUIElement != null)
                    {
                        Value = Convert.ChangeType(numberUIElement.Value, typeof(float));
                    }

                    break;
                case "option":
                    var optionUIElement = childUIElement as OptionButton;
                    if (optionUIElement != null)
                    {
                        Value = Convert.ChangeType(optionUIElement.Selected, typeof(int));
                    }

                    break;
                case "boolean":
                    var buttonUIElement = childUIElement as Button;
                    if (buttonUIElement != null)
                    {
                        Value = Convert.ChangeType(buttonUIElement.Pressed, typeof(bool));
                    }

                    break;
                case "string":
                    var stringUIElement = childUIElement as LineEdit;
                    if (stringUIElement != null)
                    {
                        Value = Convert.ChangeType(stringUIElement.Text, typeof(string));
                    }

                    break;
                case "colour":
                case "colour with alpha":
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
            case "integer":
            case "integer range":
            case "float":
            case "float range":
                var numberUIElement = childUIElement as Range;
                if (numberUIElement != null)
                {
                    numberUIElement.Value = Convert.ToDouble(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "boolean":
                var buttonUIElement = childUIElement as Button;
                if (buttonUIElement != null)
                {
                    buttonUIElement.Pressed = Convert.ToBoolean(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "string":
                var stringUIElement = childUIElement as LineEdit;
                if (stringUIElement != null)
                {
                    stringUIElement.Text = Convert.ToString(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "option":
                var optionUIElement = childUIElement as OptionButton;
                if (optionUIElement != null)
                {
                    optionUIElement.Selected = Convert.ToInt32(Value, CultureInfo.CurrentCulture);
                }

                break;
            case "colour":
            case "colour with alpha":
                var colorUIElement = childUIElement as ColorPickerButton;
                if (colorUIElement != null && Value != null)
                {
                    colorUIElement.Color = new Color((string)Convert.ChangeType(Value, typeof(string)));
                }

                break;
        }
    }

    /// <summary>
    ///   Gets all the option this Config Item can have if it the mod is a Option type.
    /// </summary>
    /// <returns> Returns a list of strings of all the options the OptionButton can have </returns>
    public List<string> GetAllOptions()
    {
        var optionsJArray = Options as JArray;
        if (optionsJArray == null)
        {
            GD.PrintErr("optionsJArray is null, probably not a option type.");
            return new List<string>();
        }

        return optionsJArray.ToObject<List<string>>()!;
    }

    public override int GetHashCode()
    {
        return (ID, Type).GetHashCode();
    }
}
