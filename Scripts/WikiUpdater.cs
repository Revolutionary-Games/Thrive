using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Scripts;
using ScriptsBase.Models;
using ScriptsBase.Utilities;

public static class WikiUpdater
{
    private const string ORGANELLE_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Organelles";
    private const string WIKI_FILE = "simulation_parameters/common/wiki.json";
    private const string ENGLISH_TRANSLATION_FILE = "locale/en.po";
    private const string TEMP_TRANSLATION_FILE = "en.po.temp_wiki";

    /// <summary>
    ///   Compounds to replace with custom BBCode when appearing in bold on wiki pages.
    /// </summary>
    private static readonly string[] CustomBbcodeCompounds =
    {
        // TODO: get these values from English translations of the names in organelles.json
        "ATP",
        "Ammonia",
        "Carbon Dioxide",
        "Glucose",
        "Hydrogen Sulfide",
        "Iron",
        "Mucilage",
        "Nitrogen",
        "Oxygen",
        "OxyToxy",
        "Phosphates",
        "Sunlight",
        "Temperature",
    };

    /// <summary>
    ///   Inserts selected content from the online wiki into the game files. See
    ///   https://wiki.revolutionarygamesstudio.com/wiki/Thriveopedia for instructions.
    /// </summary>
    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        var organellesRootTask = FetchOrganellesRootPage(cancellationToken);
        var organellesTask = FetchOrganellePages(cancellationToken);

        var organellesRoot = await organellesRootTask;
        var organelles = await organellesTask;

        var untranslatedWiki = new Wiki(
            organellesRoot.UntranslatedPage,
            organelles.Select(o => o.UntranslatedPage).ToList());

        await JsonWriteHelper.WriteJsonWithBom(WIKI_FILE, untranslatedWiki, cancellationToken);
        ColourConsole.WriteSuccessLine($"Updated wiki at {WIKI_FILE}, running translations update");

        var localizationUpdater = new LocalizationUpdate(new LocalizationOptionsBase { Quiet = true });
        if (!await localizationUpdater.Run(cancellationToken))
            return false;

        ColourConsole.WriteSuccessLine("Translations update succeeded, inserting English strings for wiki content");

        await InsertTranslatedPageContent(
            organelles.Append(organellesRoot).ToList(),
            cancellationToken);

        ColourConsole.WriteSuccessLine("Successfully updated English translations for wiki content");

        return true;
    }

    private static async Task<TranslationPair> FetchOrganellesRootPage(CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine("Fetching organelles root page");

        var body = (await HtmlReader.RetrieveHtmlDocument(ORGANELLE_CATEGORY, cancellationToken)).Body!;

        var sections = GetMainBodySections(body);
        var untranslatedSections = sections.Select(section => UntranslateSection(section, "ORGANELLES_ROOT")).ToList();

        var untranslatedPage = new Wiki.Page("WIKI_PAGE_ORGANELLES_ROOT", "OrganellesRoot", ORGANELLE_CATEGORY,
            untranslatedSections);
        var translatedPage = new Wiki.Page("Organelles", "OrganellesRoot", ORGANELLE_CATEGORY, sections);

        ColourConsole.WriteSuccessLine("Populated content for organelle root page");

        return new TranslationPair(untranslatedPage, translatedPage);
    }

    private static async Task<List<TranslationPair>> FetchOrganellePages(CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine("Fetching organelle pages");

        // Get the list of organelles from the category page on the wiki
        var categoryBody = (await HtmlReader.RetrieveHtmlDocument(ORGANELLE_CATEGORY, cancellationToken)).Body!;
        var organelles = categoryBody.QuerySelectorAll(".mw-category-group > ul > li");

        var organellePages = new List<TranslationPair>();

        foreach (var organelle in organelles)
        {
            var name = organelle.TextContent.Trim();
            var url = $"https://wiki.revolutionarygamesstudio.com/wiki/{name.Replace(" ", "_")}";

            var untranslatedOrganelleName = name.ToUpperInvariant().Replace(" ", "_");

            ColourConsole.WriteInfoLine($"Found organelle {name}");

            var body = (await HtmlReader.RetrieveHtmlDocument(url, cancellationToken)).Body!;

            // Get the internal name for cross-referencing against in-game data for the organelle
            var internalName = body.QuerySelector("#info-box-internal-name")!.TextContent.Trim();

            var sections = GetMainBodySections(body);
            var untranslatedSections = sections.Select(
                section => UntranslateSection(section, untranslatedOrganelleName)).ToList();

            var untranslatedPage = new Wiki.Page(
                $"WIKI_PAGE_{untranslatedOrganelleName}",
                internalName,
                url,
                untranslatedSections);
            var translatedPage = new Wiki.Page(
                name,
                internalName,
                url,
                sections);

            organellePages.Add(new TranslationPair(untranslatedPage, translatedPage));

            ColourConsole.WriteSuccessLine($"Populated content for organelle with internal name {internalName}");
        }

        return organellePages;
    }

    /// <summary>
    ///   Extracts page sections from the main article body and converts to BBCode. Sections are delineated by h2 tags,
    ///   which are taken as the headings (or null for the first section).
    /// </summary>
    /// <param name="body">Body content of the whole page</param>
    private static List<Wiki.Page.Section> GetMainBodySections(IHtmlElement body)
    {
        var sections = new List<Wiki.Page.Section> { new(null, string.Empty) };

        var children = body.QuerySelector(".mw-parser-output")!.Children;
        foreach (var child in children)
        {
            if (child.TagName == "H2")
            {
                // Complete the previous section and start a new one with this heading
                sections.Add(new Wiki.Page.Section(child.TextContent, string.Empty));
                continue;
            }

            string text;
            switch (child.TagName)
            {
                case "P":
                    text = ConvertTextToBbcode(child.InnerHtml) + "\n\n";
                    break;
                case "UL":

                    // Godot 3 does not support lists in BBCode, so use custom formatting
                    text = child.Children
                        .Where(c => c.TagName == "LI")
                        .Select(li => $"[indent]—   {ConvertTextToBbcode(li.InnerHtml)}[/indent]")
                        .Aggregate((a, b) => a + "\n" + b) + "\n\n";
                    break;
                default:
                    // Ignore all other tag types
                    continue;
            }

            // Concatenate this tag with the rest of the section so far
            sections[^1] = new Wiki.Page.Section(sections[^1].SectionHeading, sections[^1].SectionBody + text);
        }

        return sections.Select(s => new Wiki.Page.Section(s.SectionHeading, s.SectionBody.Trim())).ToList();
    }

    /// <summary>
    ///   Returns an equivalent section of a wiki page where the heading and body have been replaced with appropriate
    ///   translation keys.
    /// </summary>
    private static Wiki.Page.Section UntranslateSection(Wiki.Page.Section section, string pageName)
    {
        var sectionName = section.SectionHeading?.ToUpperInvariant().Replace(" ", "_");
        var heading = sectionName != null ? $"WIKI_HEADING_{sectionName}" : null;
        var body = sectionName != null ? $"WIKI_{pageName}_{sectionName}" : $"WIKI_{pageName}_INTRO";

        return new Wiki.Page.Section(heading, body);
    }

    /// <summary>
    ///   Converts HTML for a single paragraph into BBCode. Paragraph must not contain lists, headings, etc.
    /// </summary>
    private static string ConvertTextToBbcode(string paragraph)
    {
        // Process our custom BBCode first
        foreach (var compound in CustomBbcodeCompounds)
        {
            var compoundText = compound.ToLowerInvariant().Replace(" ", string.Empty);
            paragraph = paragraph.Replace(
                $"<b>{compound}</b>",
                $"[thrive:compound type=\"{compoundText}\"][/thrive:compound]");
        }

        return paragraph
            .Replace("\n", string.Empty)
            .Replace("<b>", "[b]")
            .Replace("</b>", "[/b]")
            .Replace("<i>", "[i]")
            .Replace("</i>", "[/i]")
            .Replace("\"", "\\\"");
    }

    /// <summary>
    ///   Inserts into en.po the English translations for all the translation keys in a list of wiki pages.
    /// </summary>
    private static async Task InsertTranslatedPageContent(List<TranslationPair> pages,
        CancellationToken cancellationToken)
    {
        // Create the whole list of values to replace first, then replace asynchronously based on read lines
        var translationPairs = new Dictionary<string, string>();

        foreach (var page in pages)
        {
            var untranslatedPage = page.UntranslatedPage;
            var translatedPage = page.TranslatedPage;

            // Translate page names
            translationPairs.Add(untranslatedPage.Name, translatedPage.Name);

            for (var i = 0; i < untranslatedPage.Sections.Count; ++i)
            {
                var untranslatedSection = untranslatedPage.Sections[i];
                var translatedSection = translatedPage.Sections[i];

                // Translate body sections
                translationPairs.Add(untranslatedSection.SectionBody, translatedSection.SectionBody);

                if (untranslatedSection.SectionHeading != null && translatedSection.SectionHeading != null)
                {
                    // Translate headings if present and not already in the list to translate
                    translationPairs.TryAdd(untranslatedSection.SectionHeading, translatedSection.SectionHeading);
                }
            }
        }

        var reader = File.ReadLinesAsync(ENGLISH_TRANSLATION_FILE, Encoding.UTF8, cancellationToken);
        var writer = new StreamWriter(File.Create(TEMP_TRANSLATION_FILE), new UTF8Encoding(false));

        var keyLinePattern = new Regex(@"^msgid ""(.*)""$");

        var isReplacingValue = false;
        var isFuzzyValue = false;
        await foreach (var line in reader)
        {
            if (string.IsNullOrEmpty(line))
            {
                // Blank lines mark the end of a value we might be replacing, so stop replacing
                isReplacingValue = false;
                await writer.WriteLineAsync(line);
                continue;
            }

            if (isReplacingValue)
            {
                // Skip lines for values we're replacing
                continue;
            }

            if (line == "#, fuzzy")
            {
                // Defer adding fuzzy labels so we can include/exclude them based on the next line
                isFuzzyValue = true;
                continue;
            }

            var keyLineMatch = keyLinePattern.Match(line);
            if (!keyLineMatch.Success)
            {
                // Copy existing lines that we don't want to replace
                await writer.WriteLineAsync(line);
                continue;
            }

            var key = keyLineMatch.Groups[1].Value;
            if (translationPairs.TryGetValue(key, out var value))
            {
                // Skip fuzzy labels if present, since we know this value
                isFuzzyValue = false;
                isReplacingValue = true;

                // Add the key line
                await writer.WriteLineAsync(line);

                // Add the value line(s)
                var linesToInsert = value.Split("\n");
                if (linesToInsert.Length == 1)
                {
                    await writer.WriteLineAsync($"msgstr \"{value}\"");
                }
                else
                {
                    await writer.WriteLineAsync("msgstr \"\"");

                    // Split the content over multiple lines with correct formatting
                    for (var i = 0; i < linesToInsert.Length; ++i)
                    {
                        var lineToInsert = linesToInsert[i];
                        var isLastLine = i == linesToInsert.Length - 1;
                        await writer.WriteLineAsync(isLastLine ? $"\"{lineToInsert}\"" : $"\"{lineToInsert}\\n\"");
                    }
                }
            }
            else
            {
                if (isFuzzyValue)
                {
                    // Re-insert the fuzzy label, since it's not a value we know from the wiki
                    await writer.WriteLineAsync("#, fuzzy");
                    isFuzzyValue = false;
                }

                await writer.WriteLineAsync(line);
            }
        }

        writer.Dispose();
        File.Move(TEMP_TRANSLATION_FILE, ENGLISH_TRANSLATION_FILE, true);
    }

    /// <summary>
    ///   Game wiki on our side. Must match the game's GameWiki class. It's currently not shared as there is no
    ///   common module for the scripts and the game code.
    /// </summary>
    private class Wiki
    {
        public Wiki(Page organellesRoot, List<Page> organelles)
        {
            OrganellesRoot = organellesRoot;
            Organelles = organelles;
        }

        [JsonInclude]
        public Page OrganellesRoot { get; }

        [JsonInclude]
        public List<Page> Organelles { get; }

        public class Page
        {
            public Page(string name, string internalName, string url, List<Section> sections)
            {
                Name = name;
                InternalName = internalName;
                Url = url;
                Sections = sections;
            }

            [JsonInclude]
            public string Name { get; }

            [JsonInclude]
            public string InternalName { get; }

            [JsonInclude]
            public string Url { get; }

            [JsonInclude]
            public List<Section> Sections { get; }

            public class Section
            {
                public Section(string? heading, string body)
                {
                    SectionHeading = heading;
                    SectionBody = body;
                }

                [JsonInclude]
                public string? SectionHeading { get; }

                [JsonInclude]
                public string SectionBody { get; }
            }
        }
    }

    /// <summary>
    ///   The untranslated and translated (English) versions of a single wiki page.
    /// </summary>
    private class TranslationPair
    {
        public TranslationPair(Wiki.Page untranslatedPage, Wiki.Page translatedPage)
        {
            UntranslatedPage = untranslatedPage;
            TranslatedPage = translatedPage;
        }

        public Wiki.Page UntranslatedPage { get; }

        public Wiki.Page TranslatedPage { get; }
    }
}
