using System;
using System.Collections.Generic;
using System.Text;
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
    ///     NOTE: including "thrive" namespace in the tag is a must, otherwise the custom parser wouldn't parse it.
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

    /// <summary>
    ///   Parses a custom tagged substring into a templated bbcode.
    /// </summary>
    private void ParseCustomTags()
    {
        var result = new StringBuilder();
        var currentTagBlock = new StringBuilder();

        var tagStack = new Stack<string>();

        var isIteratingTag = false;
        var isIteratingContent = false;

        // The index of a closing bracket in a last iterated opening tag, used
        // to retrieve the tagged substring
        var lastStartingTagEndIndex = 0;

        for (int index = 0; index < ExtendedBbcode.Length; index++)
        {
            var character = ExtendedBbcode[index];

            // Opening bracket found, try to parse it
            if (character == '[' && ExtendedBbcode[index + 1] != '[' && !isIteratingTag)
            {
                // Clear previous tag
                currentTagBlock.Clear();

                isIteratingTag = true;

                // Skip once, so the bracket doesn't get added into the final string
                continue;
            }

            // Character is not a tag, write it normally into the final string
            if (!isIteratingTag && !isIteratingContent)
            {
                result.Append(character);
            }

            if (isIteratingTag)
            {
                // Keep iterating until we hit closing bracket
                if (character != ']')
                {
                    currentTagBlock.Append(character);
                    continue;
                }

                // Closing bracket encountered, proceed with validating the tag

                var tagBlock = currentTagBlock.ToString();

                // Namespace divisor not found
                if (!tagBlock.Contains(":"))
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                var splitTagBlock = tagBlock.Split(":");

                // Invalid tag syntax, probably not a thrive tag or missing a part
                if (splitTagBlock.Length != 2)
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                var tagNamespace = splitTagBlock[0];
                var tagIdentifier = splitTagBlock[1];

                // Not a thrive custom tag, don't parse this
                if (!tagNamespace.Contains("thrive"))
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                // Tag seems okay, next step is to try parse the tagged substring and closing tag

                var isClosingTag = tagNamespace.BeginsWith("/");

                if (isClosingTag)
                {
                    // Closing tag doesn't match opening tag or vice versa, aborting parsing
                    if (tagStack.Count == 0 || tagStack.Peek() != tagIdentifier)
                    {
                        result.Append($"[{tagBlock}]");
                        isIteratingTag = false;
                        continue;
                    }

                    // Finally try building the bbcode template for the tagged substring

                    var closingTagStartIndex = ExtendedBbcode.Find("[", lastStartingTagEndIndex);

                    var input = ExtendedBbcode.Substr(
                        lastStartingTagEndIndex + 1, closingTagStartIndex - lastStartingTagEndIndex - 1);

                    ThriveBbCodeTag parsedTag;

                    if (Enum.TryParse(tagStack.Peek(), true, out parsedTag))
                    {
                        // Success!
                        result.Append(BuildTemplateForTag(input, parsedTag));
                    }
                    else
                    {
                        // Tag is not present in the enum
                        result.Append(input);
                        GD.PrintErr($"Failed parsing custom thrive tag: {tagStack.Peek()}, it probably doesn't exist");
                    }

                    isIteratingContent = false;
                    tagStack.Pop();
                }
                else if (!isClosingTag)
                {
                    isIteratingContent = true;
                    tagStack.Push(tagIdentifier);
                }

                lastStartingTagEndIndex = index;

                // Finished iterating tag
                isIteratingTag = false;
            }
        }

        // Apply the final string into this RichTextLabel's bbcode text
        BbcodeText = result.ToString();
    }

    /// <summary>
    ///   Returns a templated bbcode string for the given custom tag.
    /// </summary>
    /// <param name="input">The string tagged by custom tags</param>
    /// <param name="tag">Custom Thrive bbcode-styled tags</param>
    private string BuildTemplateForTag(string input, ThriveBbCodeTag tag)
    {
        // Defaults to input so if something fails output returns unchanged
        var output = input;

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
    }
}
