using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   For extra functionality added on top of normal RichTextLabel. Includes custom bbcode parser.
/// </summary>
public partial class CustomRichTextLabel : RichTextLabel
{
    private string? extendedBbcode;

    private string? heightWorkaroundRanForString;

    private bool registeredForInputChanges;
    private bool reactToLanguageChange;

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

        /// <summary>
        ///   A crafting resource (icon) by key
        /// </summary>
        Resource,

        /// <summary>
        ///   A general purpose icon from a specific set of available icons
        /// </summary>
        Icon,
    }

    /// <summary>
    ///   Vertical alignment of an image
    /// </summary>
    public enum ImageVerticalAlignment
    {
        Top = 0,
        Center,
        Bottom,
    }

    /// <summary>
    ///   Where in the text the image references its alignment to
    /// </summary>
    public enum ImageAlignmentReferencePoint
    {
        Top = 0,
        Center,
        Bottom,
        Baseline,
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
    [Export(PropertyHint.MultilineText)]
    public string? ExtendedBbcode
    {
        get => extendedBbcode;
        set
        {
            // Value is always changed here even if the text didn't change, as translations may have changed
            // the compound names anyway
            extendedBbcode = value;

            // Need to delay this so we can get the correct input controls from settings.
            Invoke.Instance.QueueForObject(ParseCustomTags, this);
        }
    }

    /// <summary>
    ///   Note: must be set before attached to the scene or otherwise this won't apply correct signals
    /// </summary>
    [Export]
    public bool EnableTooltipsForMetaTags { get; set; } = true;

    public override void _Ready()
    {
        // Make sure bbcode is enabled
        BbcodeEnabled = true;

        Connect(RichTextLabel.SignalName.MetaClicked, new Callable(this, nameof(OnMetaClicked)));

        if (EnableTooltipsForMetaTags)
        {
            Connect(RichTextLabel.SignalName.MetaHoverStarted, new Callable(this, nameof(OnMetaHoverStarted)));
            Connect(RichTextLabel.SignalName.MetaHoverEnded, new Callable(this, nameof(OnMetaHoverEnded)));
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        // TODO: should this only register when reactToLanguageChange is true?
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;

        if (registeredForInputChanges)
        {
            InputDataList.InputsRemapped -= OnInputsRemapped;
            registeredForInputChanges = false;
        }
    }

    public override void _Draw()
    {
        // A workaround to get RichTextLabel's height properly update on tooltip size change
        // See https://github.com/Revolutionary-Games/Thrive/issues/2236
        // Queue to run on the next frame due to null RID error with some bbcode image display if otherwise
#pragma warning disable CA2245 // Necessary for workaround
        Invoke.Instance.QueueForObject(() =>
        {
            var bbCode = Text;

            // Only run this once to not absolutely tank performance with long rich text labels
            if (heightWorkaroundRanForString == bbCode)
                return;

            heightWorkaroundRanForString = bbCode;

            Text = bbCode;
        }, this);
#pragma warning restore CA2245
    }

    private static bool GetSpeciesFromMeta(string metaString, out Species? species)
    {
        // TODO: is there a way to avoid this extra memory allocation?
        var speciesCode = metaString.Substring("species:".Length);

        if (!uint.TryParse(speciesCode, out var speciesId))
        {
            GD.PrintErr("Invalid species meta format, not a number");
            species = null;
            return false;
        }

        species = ThriveopediaManager.GetActiveSpeciesData(speciesId);

        if (species == null)
        {
            GD.PrintErr("Could not find active species data to show in tooltip");
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Parses ExtendedBbcode for any custom Thrive tags and applying the final result
    ///   into this RichTextLabel's bbcode text.
    /// </summary>
    private void ParseCustomTags()
    {
        if (extendedBbcode == null)
        {
            Text = null;
            return;
        }

        var old = extendedBbcode;
        var translated = Localization.Translate(extendedBbcode);
        reactToLanguageChange = old != translated;

        try
        {
            // Parse our custom tags into standard tags and display that text
            Text = ParseCustomTagsString(translated);
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to parse bbcode string due to exception: ", e);

            // Just display the raw markup for now
            Text = translated;
        }
    }

    /// <summary>
    ///   The actual method parsing our extended bbcode, <see cref="ParseCustomTags"/>. This is a separate method
    ///   to be able to easily catch any exceptions this causes.
    /// </summary>
    /// <param name="extendedBbcode">The extended bbcode string</param>
    /// <returns>Parsed bbcode string in standard format</returns>
    private string ParseCustomTagsString(string extendedBbcode)
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

                var closingTag = bbcodeNamespace.StartsWith("/", StringComparison.InvariantCulture);

                // Not a thrive bbcode, don't parse this
                if ((!closingTag && !bbcodeNamespace.StartsWith("thrive", StringComparison.InvariantCulture)) ||
                    (closingTag && !bbcodeNamespace.StartsWith("/thrive", StringComparison.InvariantCulture)))
                {
                    result.Append($"[{tagBlock}]");
                    isIteratingTag = false;
                    continue;
                }

                // The bbcode (and its attributes if this is an opening tag)
                var splitTagBlock = StringUtils.SplitByWhitespace(leftHandSide[1], true);

                // Tag seems okay, next step is to try parse the content and the closing tag

                if (closingTag)
                {
                    if (tagStack.Count < 1)
                    {
                        // We have a closing tag with no opening tag seen
                        result.Append($"[{tagBlock}]");
                        isIteratingTag = false;
                        continue;
                    }

                    var chunks = tagStack.Peek();

                    var bbcode = chunks[0];

                    // Closing tag doesn't match opening tag or vice versa, aborting parsing
                    if (bbcode != splitTagBlock[0])
                    {
                        result.Append($"[{tagBlock}]");
                        isIteratingTag = false;
                        continue;
                    }

                    // Finally try building the bbcode template for the enclosed substring

                    var closingTagStartIndex = extendedBbcode.IndexOf("[", lastStartingTagEndIndex,
                        StringComparison.InvariantCulture);

                    var input = extendedBbcode.Substring(lastStartingTagEndIndex + 1,
                        closingTagStartIndex - lastStartingTagEndIndex - 1);

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

        // Return the final string which will be used as this RichTextLabel's bbcode text
        return result.ToString();
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

        var pairs = StringUtils.ParseKeyValuePairs(attributes);

        string GetResizedImage(string imagePath,
            int width, int height, ImageVerticalAlignment verticalAlignment = ImageVerticalAlignment.Center,
            ImageAlignmentReferencePoint textAnchorPoint = ImageAlignmentReferencePoint.Center)
        {
            if (pairs.TryGetValue("size", out string? sizeInput))
            {
                var separator = sizeInput.Find("x");

                if (separator == -1)
                {
                    width = sizeInput.ToInt();
                }
                else
                {
                    var split = sizeInput.Split("x", 2);
                    width = split[0].ToInt();
                    height = split[1].ToInt();
                }
            }

            // TODO: allow bbcode override for the vertical alignment or text anchor point?

            string vertical;

            switch (verticalAlignment)
            {
                case ImageVerticalAlignment.Top:
                    vertical = "top";
                    break;
                case ImageVerticalAlignment.Center:
                    vertical = "center";
                    break;
                case ImageVerticalAlignment.Bottom:
                    vertical = "bottom";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(verticalAlignment), verticalAlignment, null);
            }

            // Automatic reference if the same point is used
            if ((int)textAnchorPoint == (int)verticalAlignment)
            {
                return $"[img {vertical} width={width} height={height}]{imagePath}[/img]";
            }

            string reference;

            switch (textAnchorPoint)
            {
                case ImageAlignmentReferencePoint.Top:
                    reference = "top";
                    break;
                case ImageAlignmentReferencePoint.Center:
                    reference = "center";
                    break;
                case ImageAlignmentReferencePoint.Bottom:
                    reference = "bottom";
                    break;
                case ImageAlignmentReferencePoint.Baseline:
                    reference = "baseline";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAnchorPoint), textAnchorPoint, null);
            }

            return $"[img {vertical},{reference} width={width} height={height}]{imagePath}[/img]";
        }

        switch (bbcode)
        {
            case ThriveBbCode.Compound:
            {
                var internalName = string.Empty;

                if (pairs.TryGetValue("type", out string? value))
                {
                    if (!value.StartsAndEndsWith("\""))
                        break;

                    internalName = value.Substring(1, value.Length - 2);
                }

                if (string.IsNullOrEmpty(internalName))
                {
                    GD.PrintErr("Compound: Type not specified in bbcode");
                    break;
                }

                // Check compound existence and aborts if it's not valid
                if (!simulationParameters.DoesCompoundExist(internalName))
                {
                    GD.PrintErr($"Compound: \"{internalName}\" doesn't exist, referenced in bbcode");
                    break;
                }

                var compound = simulationParameters.GetCompound(internalName);

                // Just use the compound's default readable name if input text is not specified
                if (string.IsNullOrEmpty(input))
                    input = compound.Name;

                output = $"[b]{input}[/b] {GetResizedImage(compound.IconPath, 20, 0)}";

                break;
            }

            case ThriveBbCode.Input:
            {
                if (!InputManager.IsValidInputAction(input))
                {
                    GD.PrintErr($"Input action: \"{input}\" doesn't exist, referenced in bbcode");
                    break;
                }

                // First time we display an input key, we start listening for key changes so that we can change what
                // we display when keys are rebound
                if (!registeredForInputChanges)
                {
                    InputDataList.InputsRemapped += OnInputsRemapped;
                    registeredForInputChanges = true;
                }

                // TODO: add support for showing the overlay image / text saying the direction for axis type inputs
                output = GetResizedImage(KeyPromptHelper.GetPathForAction(input).Primary, 30, 0);

                break;
            }

            case ThriveBbCode.Constant:
            {
                var parsedAttributes = StringUtils.ParseKeyValuePairs(attributes);
                parsedAttributes.TryGetValue("format", out string? format);

                switch (input)
                {
                    case "OXYTOXY_DAMAGE":
                    {
                        output = Constants.OXYTOXY_DAMAGE.ToString(format, CultureInfo.CurrentCulture);
                        break;
                    }

                    case "ENGULF_COMPOUND_ABSORBING_PER_SECOND":
                    {
                        output = Constants.ENGULF_COMPOUND_ABSORBING_PER_SECOND.ToString(format,
                            CultureInfo.CurrentCulture);
                        break;
                    }

                    case "ENZYME_DIGESTION_SPEED_UP_FRACTION":
                    {
                        output = (Constants.ENZYME_DIGESTION_SPEED_UP_FRACTION * 100).ToString(format,
                            CultureInfo.CurrentCulture);
                        break;
                    }

                    case "ENZYME_DIGESTION_EFFICIENCY_BUFF_FRACTION":
                    {
                        output = (Constants.ENZYME_DIGESTION_EFFICIENCY_BUFF_FRACTION * 100).ToString(format,
                            CultureInfo.CurrentCulture);
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
                        GD.PrintErr($"Constant: \"{input}\" doesn't exist, referenced in bbcode");
                        break;
                    }
                }

                break;
            }

            case ThriveBbCode.Resource:
            {
                var internalName = string.Empty;

                if (pairs.TryGetValue("type", out string? value))
                {
                    if (!value.StartsAndEndsWith("\""))
                        break;

                    internalName = value.Substring(1, value.Length - 2);
                }

                if (string.IsNullOrEmpty(internalName))
                {
                    GD.PrintErr("Resource: Type not specified in bbcode");
                    break;
                }

                // Check compound existence and aborts if it's not valid
                if (!simulationParameters.DoesWorldResourceExist(internalName))
                {
                    GD.PrintErr($"Resource: \"{internalName}\" doesn't exist, referenced in bbcode");
                    break;
                }

                var resource = simulationParameters.GetWorldResource(internalName);

                // Resources by default don't show the name
                bool showName = false;

                if (pairs.TryGetValue("type", out value))
                {
                    showName = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                }

                if (!showName)
                {
                    output = GetResizedImage(resource.InventoryIcon, 20, 0);
                }
                else
                {
                    // When override text is not used, use the default name
                    if (string.IsNullOrEmpty(input))
                        input = resource.Name;

                    output = $"[b]{input}[/b] {GetResizedImage(resource.InventoryIcon, 20, 0)}";
                }

                break;
            }

            case ThriveBbCode.Icon:
            {
                // TODO: allow overriding size or width (Constant handling has parsing for custom format)

                switch (input)
                {
                    case "ConditionInsufficient":
                    {
                        output = GetResizedImage(GUICommon.Instance.RequirementInsufficientIconPath, 20, 0);
                        break;
                    }

                    case "ConditionFulfilled":
                    {
                        output = GetResizedImage(GUICommon.Instance.RequirementFulfilledIconPath, 20, 0);
                        break;
                    }

                    case "StorageIcon":
                    {
                        output = GetResizedImage("res://assets/textures/gui/bevel/StorageIcon.png", 20, 0);
                        break;
                    }

                    case "OsmoIcon":
                    {
                        output = GetResizedImage("res://assets/textures/gui/bevel/osmoregulationIcon.png", 20, 0);
                        break;
                    }

                    case "MovementIcon":
                    {
                        output = GetResizedImage("res://assets/textures/gui/bevel/SpeedIcon.png", 20, 0);
                        break;
                    }

                    case "MP":
                    {
                        output = GetResizedImage("res://assets/textures/gui/bevel/MP.png", 20, 0);
                        break;
                    }

                    default:
                    {
                        GD.PrintErr($"Icon: \"{input}\" doesn't exist, referenced in bbcode");
                        break;
                    }
                }

                break;
            }
        }

        return output;
    }

    private void OnInputsRemapped(object? sender, EventArgs args)
    {
        ParseCustomTags();
    }

    private void OnMetaClicked(Variant meta)
    {
        if (meta.VariantType == Variant.Type.String)
        {
            var metaString = meta.AsString();

            // TODO: should there be stronger validation than this? that this is actually an URL? Maybe Uri.TryParse
            if (metaString.StartsWith("http", StringComparison.Ordinal))
            {
                GD.Print("Opening clicked link: ", metaString);
                if (OS.ShellOpen(metaString) != Error.Ok)
                {
                    GD.PrintErr("Opening the link failed");
                }
            }
            else if (metaString.StartsWith("thriveopedia", StringComparison.Ordinal))
            {
                var pageName = metaString.Split("thriveopedia:", 2)[1];
                ThriveopediaManager.OpenPage(pageName);
            }
        }
    }

    private void OnMetaHoverStarted(Variant meta)
    {
        if (!EnableTooltipsForMetaTags)
            return;

        if (meta.VariantType != Variant.Type.String)
            return;

        var metaString = meta.AsString();

        if (metaString.StartsWith("species:", StringComparison.Ordinal))
        {
            if (!GetSpeciesFromMeta(metaString, out var species))
                return;

            if (species is not MicrobeSpecies)
                return;

            var tooltip = ToolTipManager.Instance.GetToolTip<SpeciesPreviewTooltip>("speciesPreview");
            if (tooltip != null)
            {
                tooltip.PreviewSpecies = species;
                ToolTipManager.Instance.MainToolTip = tooltip;
                ToolTipManager.Instance.Display = true;
            }
        }
    }

    private void OnMetaHoverEnded(Variant meta)
    {
        if (!EnableTooltipsForMetaTags)
            return;

        if (meta.VariantType != Variant.Type.String)
            return;

        var metaString = meta.AsString();

        if (metaString.StartsWith("species:", StringComparison.Ordinal))
        {
            if (!GetSpeciesFromMeta(metaString, out var species))
                return;

            // Hide tooltip if it was currently showing the tooltip for this species preview
            var tooltip = ToolTipManager.Instance.GetToolTip<SpeciesPreviewTooltip>("speciesPreview");
            if (tooltip != null && ToolTipManager.Instance.MainToolTip == tooltip && tooltip.PreviewSpecies == species)
            {
                ToolTipManager.Instance.MainToolTip = null;
                ToolTipManager.Instance.Display = false;
            }
        }
    }

    private void OnTranslationsChanged()
    {
        if (reactToLanguageChange)
            ParseCustomTags();
    }
}
