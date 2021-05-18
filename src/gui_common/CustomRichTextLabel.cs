using System;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
///   For extra functionality added on top of normal RichTextLabel. Includes custom bbcode parser.
/// </summary>
public class CustomRichTextLabel : RichTextLabel
{
    private string extendedBbcode;

    /// <summary>
    ///   Custom Bbcode tags exclusive for Thrive. Acts more like an extension to the built-in tags.
    /// </summary>
    public enum ThriveBbCodeTag
    {
        /// <summary>
        ///   Turns compound internal name string into a bolded display name and an icon next to it.
        ///   Mainly used in organelle tooltips.
        /// </summary>
        Compound,

        /// <summary>
        ///   Turns input action string into a key prompt image of its primary input event.
        /// </summary>
        Input,
    }

    /// <summary>
    ///   This supports custom bbcode tags specific to Thrive (for example: [thrive:compound]glucose[/thrive:compound])
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: including the "thrive" namespace is a must, otherwise the tag wouldn't be detected by the
    ///     custom parser.
    ///   </para>
    /// </remarks>
    [Export]
    public string ExtendedBbcode
    {
        get => extendedBbcode;
        set
        {
            if (extendedBbcode == value)
                return;

            extendedBbcode = value;

            // Need to delay this so we can get the correct input controls from settings.
            Invoke.Instance.Queue(() => ParseCustomTags());
        }
    }

    public override void _Ready()
    {
        // Make sure bbcode is enabled
        BbcodeEnabled = true;
    }

    private void ParseCustomTags()
    {
        BbcodeText = Regex.Replace(ExtendedBbcode, @"\[thrive:([^\]]+)\](.*?)\[/thrive:([^\]]+)\]", found =>
        {
            // Defaults to whole matched string so if something fails output returns unchanged
            var output = found.Groups[0].Value;

            var input = found.Groups[2].Value;

            var bracketStart = found.Groups[1].Value;
            var bracketEnd = found.Groups[3].Value;

            // Check invalid markers
            if (bracketStart != bracketEnd)
                return output;

            var tag = (ThriveBbCodeTag)Enum.Parse(typeof(ThriveBbCodeTag), bracketStart, true);

            switch (tag)
            {
                case ThriveBbCodeTag.Compound:
                {
                    if (SimulationParameters.Instance.DoesCompoundExist(input))
                    {
                        var compound = SimulationParameters.Instance.GetCompound(input);

                        output = $"[b]{compound.Name}[/b] [font=res://src/gui_common/fonts/" +
                            $"BBCode-Image-VerticalCenterAlign-3.tres] [img=20]{compound.IconPath}[/img][/font]";
                    }

                    break;
                }

                case ThriveBbCodeTag.Input:
                {
                    if (InputMap.HasAction(input))
                    {
                        output = "[font=res://src/gui_common/fonts/BBCode-Image-VerticalCenterAlign-9.tres]" +
                            $"[img=30]{KeyPromptHelper.GetPathForAction(input)}[/img][/font]";
                    }

                    break;
                }
            }

            return output;
        });
    }
}
