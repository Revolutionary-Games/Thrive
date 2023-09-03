namespace Scripts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using ScriptsBase.Checks.FileTypes;
using ScriptsBase.Translation;
using ScriptsBase.Utilities;

/// <summary>
///   Extract translations from Godot scene files
/// </summary>
public class GodotSceneTranslationExtractor : TranslationExtractorBase
{
    /// <summary>
    ///   Text that when put in a Node description, disables text extraction from that node
    /// </summary>
    private const string PlaceHolderMarker = "PLACEHOLDER";

    /// <summary>
    ///   Tooltip text is always extracted even when <see cref="PlaceHolderMarker"/> is used. That is except when this
    ///   text is in the Node's editor description, then the tooltip is not extracted. It is completely intentional
    ///   that this text contains the placeholder marker, and the code is written to assume that tooltip extraction
    ///   can't be disabled unless other text extraction is also disabled.
    /// </summary>
    private const string TooltipPlaceHolderMarker = "TOOLTIP_PLACEHOLDER";

    private const string TooltipPropertyName = "hint_tooltip";

    private const int ItemListStride = 5;

    private static readonly Regex GodotPropertyStr = new(@"^([A-Za-z0-9_]+)\s*=\s*""(.+)""$", RegexOptions.Compiled);

    private static readonly Regex GodotEditorDescription =
        new(@"editor_description_""\s*:\s*""([^""]+)", RegexOptions.Compiled);

    private static readonly Regex OptionButtonOptions =
        new(@"^(items)\s*=\s*\[\s*(.+)\s*\]\s*$", RegexOptions.Compiled);

    private readonly IReadOnlyCollection<string> propertiesToLookFor;

    private readonly bool itemsEnabled;

    private readonly List<(string Property, ExtractedTranslation Translation)> translationsForCurrentNode = new();

    private string? lastNodeName;

    // We might want to implement node type specific extraction at some point, so this variable is kept
    // ReSharper disable once NotAccessedField.Local
    private string? lastNodeType;

    private bool ignoreLastNode;
    private bool ignoreLastNodeTooltip;

    /// <summary>
    ///   Sets the properties to look for. Note that option button items are extracted when "items" is present in the
    ///   property names.
    /// </summary>
    /// <param name="propertiesToLookFor">The property names from nodes to extract</param>
    public GodotSceneTranslationExtractor(IReadOnlyCollection<string> propertiesToLookFor) : base(".tscn")
    {
        this.propertiesToLookFor = propertiesToLookFor;

        itemsEnabled = propertiesToLookFor.Contains("items");
    }

    public override async IAsyncEnumerable<ExtractedTranslation> Handle(string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = File.OpenText(path);

        int lineNumber = 0;

        while (true)
        {
            ++lineNumber;
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line == null)
                break;

            var match = TscnCheck.GodotNodeRegex.Match(line);

            if (match.Success)
            {
                // Found a Godot node start, which means that the previous node ended
                foreach (var result in ProcessPreviousNode())
                {
                    yield return result;
                }

                var type = match.Groups.Count > 2 ? match.Groups[2].Value : null;
                StartNode(match.Groups[1].Value, type);

                // Tab controls have a translatable key in their name
                if (type == TscnCheck.TAB_CONTROL_TYPE)
                {
                    translationsForCurrentNode.Add((match.Groups[1].Value,
                        new ExtractedTranslation(match.Groups[1].Value, path, lineNumber)));
                }

                continue;
            }

            match = GodotPropertyStr.Match(line);

            if (match.Success)
            {
                HandleProperty(match.Groups[1].Value, match.Groups[2].Value, path, lineNumber);
                continue;
            }

            // Option button is handled specially from other property types
            if (itemsEnabled)
            {
                match = OptionButtonOptions.Match(line);

                if (match.Success)
                {
                    HandleOptionsButton(match.Groups[1].Value, match.Groups[2].Value, path, lineNumber);
                    continue;
                }
            }

            match = GodotEditorDescription.Match(line);

            if (match.Success)
            {
                HandleEditorDescription(match.Groups[1].Value);
            }
        }

        // Output any remaining stuff if the last Godot node in the file had translations
        foreach (var result in ProcessPreviousNode())
        {
            yield return result;
        }
    }

    private void StartNode(string name, string? type)
    {
        translationsForCurrentNode.Clear();

        lastNodeName = name;
        lastNodeType = type;

        ignoreLastNode = false;
        ignoreLastNodeTooltip = false;
    }

    private void HandleProperty(string name, string value, string file, int line)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (propertiesToLookFor.All(p => p != name))
            return;

        translationsForCurrentNode.Add((name, new ExtractedTranslation(value, file, line)));
    }

    private void HandleOptionsButton(string name, string data, string file, int line)
    {
        var values = data.Split(",", StringSplitOptions.TrimEntries);

        if (values.Length % ItemListStride != 0)
        {
            ColourConsole.WriteErrorLine($"Failed to parse Godot items syntax at {file}:{line}");
            return;
        }

        for (int i = 0; i < values.Length; i += ItemListStride)
        {
            var item = values[i];

            if (item.StartsWith('\"'))
                item = item.Substring(1, item.Length - 2);

            translationsForCurrentNode.Add((name, new ExtractedTranslation(item, file, line)));
        }
    }

    private IEnumerable<ExtractedTranslation> ProcessPreviousNode()
    {
        // Exit if no node is read yet
        if (lastNodeName == null)
            yield break;

        if (ignoreLastNode && ignoreLastNodeTooltip)
            yield break;

        foreach (var (property, translation) in translationsForCurrentNode)
        {
            if (ignoreLastNode)
            {
                // Only tooltip allowed when a placeholder node
                if (string.Compare(property, TooltipPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
                    yield return translation;

                continue;
            }

            // The case where only tooltip would be ignored is not very useful, so we don't check that or support
            // that combination of state variables here

            // We are allowed to pass all translations through
            yield return translation;
        }
    }

    private void HandleEditorDescription(string description)
    {
        if (description.Contains(PlaceHolderMarker))
        {
            ignoreLastNode = true;
        }

        if (description.Contains(TooltipPlaceHolderMarker))
        {
            ignoreLastNodeTooltip = true;
        }
    }
}
