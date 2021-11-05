using System;
using System.Collections.Generic;
using System.Globalization;
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
    ///   Custom BBCodes exclusive for Thrive. Acts more like an extension to the built-in tags.
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

        /// <summary>
        ///   Retrieves value by key. Special handling for every key. Expandable.
        /// </summary>
        Constant,
    }

    /// <summary>
    ///   This supports custom bbcode tags specific to Thrive (for example: [thrive:compound type="glucose"]
    ///   [/thrive:compound])
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: including "thrive" namespace in a tag is a must, otherwise the custom parser wouldn't parse it.
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

    public override void _EnterTree()
    {
        base._EnterTree();
        InputDataList.InputsRemapped += OnInputsRemapped;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputDataList.InputsRemapped -= OnInputsRemapped;
    }

    public override void _Ready()
    {
        // Make sure bbcode is enabled
        BbcodeEnabled = true;
    }

    public override void _Draw()
    {
        // A workaround to get RichTextLabel's height properly update on tooltip size change
        // See https://github.com/Revolutionary-Games/Thrive/issues/2236
        // Queue to run on the next frame due to null RID error with some bbcode image display if otherwise
#pragma warning disable CA2245 // Necessary for workaround
        Invoke.Instance.Queue(() => BbcodeText = BbcodeText);
#pragma warning restore CA2245
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
                        // No closing bracket found, just write normally into the final string and abort
                        // trying to parse
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

                // Invalid bbcode syntax, probably not a thrive bbcode or missing a part
                if (leftHandSide.Length != 2)
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                // Custom bbcode Thrive namespace
                var bbcodeNamespace = leftHandSide[0];

                // Not a thrive bbcode, don't parse this
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

                    // Finally try building the bbcode template for the enclosed substring

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
                        // BBCode is not present in the enum
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
    /// <param name="input">The string enclosed by the custom tags</param>
    /// <param name="bbcode">Custom Thrive bbcode-styled tags</param>
    /// <param name="attributes">Attributes specifying additional functionalities to the bbcode.</param>
    private string BuildTemplateForTag(string input, ThriveBbCode bbcode, List<string> attributes)
    {
        // Defaults to input so if something fails output returns unchanged
        var output = input;

        var simulationParameters = SimulationParameters.Instance;

        switch (bbcode)
        {
            case ThriveBbCode.Compound:
            {
                var pairs = StringUtils.ParseKeyValuePairs(attributes);

                // Used to fallback to the old method if type attribute is not specified. Should be removed in
                // roughly 6 months to give time for translations to be updated
                // TODO: remove this fallback once it makes sense to do so
                // https://github.com/Revolutionary-Games/Thrive/issues/2434
                var fallback = false;

                var internalName = string.Empty;

                if (pairs.TryGetValue("type", out string value))
                {
                    if (!value.StartsAndEndsWith("\""))
                        break;

                    internalName = value.Substring(1, value.Length - 2);
                }

                // Handle fallback
                if (!string.IsNullOrEmpty(input) && string.IsNullOrEmpty(internalName))
                {
                    GD.Print("Compound type not specified in bbcode, fallback to using input as " +
                        $"the internal name: {input}");
                    internalName = input;
                    fallback = true;
                }

                // Check compound existence and aborts if it's not valid
                if (!simulationParameters.DoesCompoundExist(internalName))
                {
                    GD.Print($"Compound: \"{internalName}\" doesn't exist, referenced in bbcode");
                    break;
                }

                var compound = simulationParameters.GetCompound(internalName);

                // Just use the default compound's display name if input text is not specified
                if (!fallback && string.IsNullOrEmpty(input))
                    input = compound.Name;

                output = $"[b]{(fallback ? compound.Name : input)}[/b] [font=res://src/gui_common/fonts/" +
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

            case ThriveBbCode.Constant:
                var parsedAttributes = StringUtils.ParseKeyValuePairs(attributes);
                parsedAttributes.TryGetValue("format", out string format);

                switch (input)
                {
                    case "OXYTOXY_DAMAGE":
                    {
                        output = Constants.OXYTOXY_DAMAGE.ToString(format, CultureInfo.CurrentCulture);
                        break;
                    }

                    case "ENGULF_DAMAGE":
                    {
                        output = Constants.ENGULF_DAMAGE.ToString(format, CultureInfo.CurrentCulture);
                        break;
                    }

                    case "PILUS_BASE_DAMAGE":
                    {
                        output = Constants.PILUS_BASE_DAMAGE.ToString(format, CultureInfo.CurrentCulture);
                        break;
                    }

                    case "BINDING_ATP_COST_PER_SECOND":
                    {
                        output = Constants.BINDING_ATP_COST_PER_SECOND.ToString(format, CultureInfo.CurrentCulture);
                        break;
                    }

                    case "ENGULFING_ATP_COST_PER_SECOND":
                    {
                        output = Constants.ENGULFING_ATP_COST_PER_SECOND.ToString(format, CultureInfo.CurrentCulture);
                        break;
                    }

                    case "EDITOR_TIME_JUMP_MILLION_YEARS":
                    {
                        output = Constants.EDITOR_TIME_JUMP_MILLION_YEARS.ToString(format, CultureInfo.CurrentCulture);
                        break;
                    }

                    default:
                    {
                        GD.Print($"Constant: \"{input}\" doesn't exist, referenced in bbcode");
                        break;
                    }
                }

                break;
        }

        return output;
    }

    private void OnInputsRemapped(object sender, EventArgs args)
    {
        ParseCustomTags();
    }
}
