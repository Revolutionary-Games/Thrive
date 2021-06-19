using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   For extra functionality added on top of normal RichTextLabel. Includes custom bbcode parser.
/// </summary>
public class CustomRichTextLabel : RichTextLabel
{
    private string extendedBbcode;

    /// <summary>
    ///   Custom Bbcodes exclusive for Thrive. Acts more like an extension to the built-in tags.
    /// </summary>
    public enum ThriveBbCode
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
            // Value is always changed here even if the text didn't change, as translations may have changed
            // the compound names anyway
            extendedBbcode = value;

            // Need to delay this so we can get the correct input controls from settings.
            Invoke.Instance.Queue(ParseCustomTags);
        }
    }

    public override void _Ready()
    {
        // Make sure bbcode is enabled
        BbcodeEnabled = true;
    }

    /// <summary>
    ///   Parses ExtendedBbcode for any custom Thrive tags and applying the final result
    ///   into this RichTextLabel's bbcode text.
    /// </summary>
    private void ParseCustomTags()
    {
        var result = new StringBuilder(extendedBbcode.Length);
        var currentTagBlock = new StringBuilder(50);

        var tagStack = new Stack<string[]>();

        var isIteratingTag = false;
        var isIteratingContent = false;

        // The index of a closing bracket in a last iterated opening tag, used
        // to retrieve the tagged substring
        var lastStartingTagEndIndex = 0;

        for (int index = 0; index < extendedBbcode.Length; ++index)
        {
            var character = extendedBbcode[index];

            var validNextCharacter = index + 1 < extendedBbcode.Length && extendedBbcode[index + 1] != '[';

            // Opening bracket found, try to parse it
            if (character == '[' && validNextCharacter && !isIteratingTag &&
                index < extendedBbcode.Length)
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
                // TODO: make this try to add entire substrings at once instead of appending per character
                result.Append(character);
            }

            if (isIteratingTag)
            {
                // Keep iterating until we hit a closing bracket
                if (character != ']')
                {
                    currentTagBlock.Append(character);

                    if (character == '[' || index == extendedBbcode.Length - 1)
                    {
                        // No closing bracket found, just write normally to the final string and abort trying to parse
                        result.Append($"[{currentTagBlock}");
                        isIteratingTag = false;
                    }

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

                var leftHandSide = tagBlock.Split(":");

                // Invalid tag syntax, probably not a thrive tag or missing a part
                if (leftHandSide.Length != 2)
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                // Custom bbcode Thrive tag namespace
                var bbcodeNamespace = leftHandSide[0];

                // Tag name (and subtag if this is an opening tag)
                var splitTagBlock = StringUtils.SplitByWhiteSpace(leftHandSide[1], true);

                // Not a thrive custom tag, don't parse this
                if (!bbcodeNamespace.Contains("thrive"))
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                // Tag seems okay, next step is to try parse the content and the closing tag

                // Is a closing tag
                if (bbcodeNamespace.BeginsWith("/"))
                {
                    var chunks = tagStack.Peek();

                    var bbcode = chunks[0];

                    // Closing tag doesn't match opening tag or vice versa, aborting parsing
                    if (tagStack.Count == 0 || bbcode != splitTagBlock[0])
                    {
                        result.Append($"[{tagBlock}]");
                        isIteratingTag = false;
                        continue;
                    }

                    // Finally try building the bbcode template for the tagged substring

                    var closingTagStartIndex = extendedBbcode.Find("[", lastStartingTagEndIndex);

                    var input = extendedBbcode.Substr(
                        lastStartingTagEndIndex + 1, closingTagStartIndex - lastStartingTagEndIndex - 1);

                    ThriveBbCode parsedTag;

                    if (Enum.TryParse(bbcode, true, out parsedTag))
                    {
                        // Leave out bbcode, all that's left should be the attributes
                        var attributes = chunks.Skip(1).ToArray();

                        // Success!
                        result.Append(BuildTemplateForTag(input, parsedTag, attributes));
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
                else
                {
                    isIteratingContent = true;
                    tagStack.Push(splitTagBlock);
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
    /// <param name="bbcode">Custom Thrive bbcode-styled tags</param>
    /// <param name="attributes">Attributes specifying an additional functionality to the bbcode.</param>
    private string BuildTemplateForTag(string input, ThriveBbCode bbcode, string[] attributes = null)
    {
        // Defaults to input so if something fails output returns unchanged
        var output = input;

        switch (bbcode)
        {
            case ThriveBbCode.Compound:
            {
                if (!SimulationParameters.Instance.DoesCompoundExist(input))
                {
                    GD.Print($"Compound: \"{input}\" doesn't exist, referenced in bbcode");
                    break;
                }

                var compound = SimulationParameters.Instance.GetCompound(input);

                var name = compound.Name;

                // Parse attributes if there is any
                // ReSharper disable MergeSequentialChecksWhenPossible
                if (attributes != null && attributes.Length > 0)
                {
                    if (attributes[0].BeginsWith("text="))
                    {
                        var split = attributes[0].Split("=");

                        if (split.Length != 2)
                        {
                            GD.PrintErr("Compound BBCode tag: `text` attribute is specified but missing a value");
                            break;
                        }

                        var value = split[1];

                        if (!value.BeginsWith("\"") || !value.EndsWith("\"", StringComparison.CurrentCulture))
                            break;

                        name = value.Substr(1, value.Length - 2);
                    }

                    // if (... other tags ...)
                }

                // ReSharper restore MergeSequentialChecksWhenPossible

                output = $"[b]{name}[/b] [font=res://src/gui_common/fonts/" +
                    $"BBCode-Image-VerticalCenterAlign-3.tres] [img=20]{compound.IconPath}[/img][/font]";

                break;
            }

            case ThriveBbCode.Input:
            {
                if (!InputMap.HasAction(input))
                {
                    GD.Print($"Input action: \"{input}\" doesn't exist, referenced in bbcode");
                    break;
                }

                output = "[font=res://src/gui_common/fonts/BBCode-Image-VerticalCenterAlign-9.tres]" +
                    $"[img=30]{KeyPromptHelper.GetPathForAction(input)}[/img][/font]";

                break;
            }
        }

        return output;
    }
}
