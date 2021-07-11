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

        var tagStack = new Stack<List<string>>();

        var isIteratingTag = false;
        var isIteratingContent = false;

        // The index of a closing bracket in a last iterated opening tag, used
        // to retrieve the substring enclosed between start and end tag
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

                // Bbcode delimiter not found
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

                // Custom bbcode Thrive namespace
                var bbcodeNamespace = leftHandSide[0];

                // Not a thrive custom bbcode, don't parse this
                if (!bbcodeNamespace.Contains("thrive"))
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                // The bbcode (and its attributes if this is an opening tag)
                var splitTagBlock = StringUtils.SplitByWhitespace(leftHandSide[1], true);

                // Tag seems okay, next step is to try parse the content and the closing tag

                // Is a closing tag
                if (bbcodeNamespace.StartsWith("/", StringComparison.InvariantCulture))
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

                    var closingTagStartIndex = extendedBbcode.IndexOf("[", lastStartingTagEndIndex,
                        StringComparison.InvariantCulture);

                    var input = extendedBbcode.Substring(
                        lastStartingTagEndIndex + 1, closingTagStartIndex - lastStartingTagEndIndex - 1);

                    if (Enum.TryParse(bbcode, true, out ThriveBbCode parsedTag))
                    {
                        // Leave out bbcode, all that's left should be the attributes
                        var attributes = chunks.Skip(1).ToList();

                        // Success!
                        result.Append(BuildTemplateForTag(input, parsedTag, attributes));
                    }
                    else
                    {
                        // BBcode is not present in the enum
                        result.Append(input);
                        GD.PrintErr($"Failed parsing custom thrive bbcode: {bbcode}, it probably doesn't exist");
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
    private string BuildTemplateForTag(string input, ThriveBbCode bbcode, List<string> attributes = null)
    {
        // Defaults to input so if something fails output returns unchanged
        var output = input;

        switch (bbcode)
        {
            case ThriveBbCode.Compound:
            {
                var pairs = StringUtils.ParseKeyValuePairs(attributes);

                var internalName = string.Empty;

                if (pairs.TryGetValue("type", out string value))
                {
                    if (!value.StartsAndEndsWith("\""))
                        break;

                    internalName = value.Substring(1, value.Length - 2);
                }

                if (!SimulationParameters.Instance.DoesCompoundExist(internalName))
                {
                    GD.Print($"Compound: \"{internalName}\" doesn't exist, referenced in bbcode");
                    break;
                }

                var compound = SimulationParameters.Instance.GetCompound(internalName);

                output = $"[b]{input}[/b] [font=res://src/gui_common/fonts/" +
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
