using System.Collections.Generic;
using System.Globalization;
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
    private const string STAGES_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Stages";
    private const string CONCEPTS_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Concepts";
    private const string DEVELOPMENT_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Development";

    private const string WIKI_FILE = "simulation_parameters/common/wiki.json";
    private const string ENGLISH_TRANSLATION_FILE = "locale/en.po";
    private const string TEMP_TRANSLATION_FILE = "en.po.temp_wiki";

    private const string INFO_BOX_SELECTOR = ".wikitable > tbody";
    private const string CATEGORY_PAGES_SELECTOR = ".mw-category-group > ul > li";

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

        // ReSharper disable once StringLiteralTypo
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
        var organellesRootTask = FetchRootPage(ORGANELLE_CATEGORY, "Organelles", cancellationToken);
        var stagesRootTask = FetchRootPage(STAGES_CATEGORY, "Stages", cancellationToken);
        var conceptsRootTask = FetchRootPage(CONCEPTS_CATEGORY, "Concepts", cancellationToken);
        var developmentRootTask = FetchRootPage(DEVELOPMENT_CATEGORY, "Development", cancellationToken);

        var organellesTask = FetchPagesFromCategory(ORGANELLE_CATEGORY, "Organelle", cancellationToken);
        var stagesTask = FetchPagesFromCategory(STAGES_CATEGORY, "Stage", cancellationToken);
        var conceptsTask = FetchPagesFromCategory(CONCEPTS_CATEGORY, "Concept", cancellationToken);
        var developmentPagesTask = FetchPagesFromCategory(DEVELOPMENT_CATEGORY, "Development Page", cancellationToken);

        var organellesRoot = await organellesRootTask;
        var developmentRoot = await developmentRootTask;
        var conceptsRoot = await conceptsRootTask;
        var stagesRoot = await stagesRootTask;

        var organelles = await organellesTask;
        var developmentPages = await developmentPagesTask;
        var concepts = await conceptsTask;
        var stages = await stagesTask;

        var untranslatedWiki = new Wiki()
        {
            OrganellesRoot = organellesRoot.UntranslatedPage,
            StagesRoot = stagesRoot.UntranslatedPage,
            ConceptsRoot = conceptsRoot.UntranslatedPage,
            DevelopmentRoot = developmentRoot.UntranslatedPage,

            Organelles = organelles.Select(o => o.UntranslatedPage).ToList(),
            Stages = stages.Select(s => s.UntranslatedPage).ToList(),
            Concepts = concepts.Select(c => c.UntranslatedPage).ToList(),
            DevelopmentPages = developmentPages.Select(p => p.UntranslatedPage).ToList(),
        };

        await JsonWriteHelper.WriteJsonWithBom(WIKI_FILE, untranslatedWiki, cancellationToken);
        ColourConsole.WriteSuccessLine($"Updated wiki at {WIKI_FILE}, running translations update");

        var localizationUpdater = new LocalizationUpdate(new LocalizationOptionsBase { Quiet = true });
        if (!await localizationUpdater.Run(cancellationToken))
            return false;

        ColourConsole.WriteSuccessLine("Translations update succeeded, inserting English strings for wiki content");

        var pages = new List<TranslationPair>()
        {
            organellesRoot,
            stagesRoot,
            conceptsRoot,
            developmentRoot,
        };

        pages.AddRange(organelles);
        pages.AddRange(stages);
        pages.AddRange(concepts);
        pages.AddRange(developmentPages);

        await InsertTranslatedPageContent(pages, cancellationToken);

        ColourConsole.WriteSuccessLine("Successfully updated English translations for wiki content");

        return true;
    }

    private static async Task<TranslationPair> FetchRootPage(string url, string categoryName, CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine($"Fetching {categoryName.ToLowerInvariant()} root page");

        var body = (await HtmlReader.RetrieveHtmlDocument(url, cancellationToken)).Body!;

        var fullPageName = $"{categoryName}Root";
        var translationKey = $"{categoryName.ToUpperInvariant()}_ROOT";

        var sections = GetMainBodySections(body);
        var untranslatedSections = sections.Select(section => UntranslateSection(section, translationKey)).ToList();

        var untranslatedPage = new Wiki.Page($"WIKI_PAGE_{translationKey}", fullPageName, url,
            untranslatedSections);
        var translatedPage = new Wiki.Page(categoryName, fullPageName, url, sections);

        ColourConsole.WriteSuccessLine($"Populated content for {categoryName.ToLowerInvariant()} root page");

        return new TranslationPair(untranslatedPage, translatedPage);
    }

    private static async Task<List<TranslationPair>> FetchPagesFromCategory(string url, string pageType, CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine($"Fetching {pageType} pages");

        // Get the list of pages from a category page on the wiki
        var categoryBody = (await HtmlReader.RetrieveHtmlDocument(url, cancellationToken)).Body!;
        var pages = categoryBody.QuerySelectorAll(CATEGORY_PAGES_SELECTOR);

        var allPages = new List<TranslationPair>();
        var textInfo = CultureInfo.InvariantCulture.TextInfo;

        foreach (var page in pages)
        {
            var name = page.TextContent.Trim();
            var pageUrl = $"https://wiki.revolutionarygamesstudio.com/wiki/{name.Replace(" ", "_")}";

            var untranslatedPageName = name.ToUpperInvariant().Replace(" ", "_");

            ColourConsole.WriteInfoLine($"Found {pageType} {name}");

            var body = (await HtmlReader.RetrieveHtmlDocument(pageUrl, cancellationToken)).Body!;

            var translatedInfobox = new List<InfoboxField>();
            var untranslatedInfobox = new List<InfoboxField>();

            var internalName = textInfo.ToTitleCase(name).Replace(" ", string.Empty);

            if (body.QuerySelector(".wikitable") != null)
            {
                internalName = body.QuerySelector("#info-box-internal-name")!.TextContent.Trim();
                (untranslatedInfobox, translatedInfobox) = GetInfoBoxFields(body, internalName);
            }

            var sections = GetMainBodySections(body);
            var untranslatedSections = sections.Select(
                section => UntranslateSection(section, untranslatedPageName)).ToList();

            var untranslatedPage = new Wiki.Page(
                $"WIKI_PAGE_{untranslatedPageName}",
                internalName,
                pageUrl,
                untranslatedSections,
                untranslatedInfobox);
            var translatedPage = new Wiki.Page(
                name,
                internalName,
                pageUrl,
                sections,
                translatedInfobox);

            allPages.Add(new TranslationPair(untranslatedPage, translatedPage));

            ColourConsole.WriteSuccessLine($"Populated content for {pageType} with internal name {internalName}");
        }

        return allPages;
    }

    /// <summary>
    ///   Extracts all the fields of an associated page's infobox and generates translation keys
    /// </summary>
    /// <param name="body">The body of the page</param>
    /// <param name="internalName">Internal name of the page</param>
    /// <returns>A tuple containing the translated and untranlsated versions of the detected fields</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when an infobox is not found</exception>
    private static (List<InfoboxField> Untranslated, List<InfoboxField> Translated)
        GetInfoBoxFields(IHtmlElement body, string internalName)
    {
        ColourConsole.WriteInfoLine($"Extracting infobox content for page with internal name {internalName}");

        // Get the infobox element
        var infobox = body.QuerySelector(INFO_BOX_SELECTOR);

        if (infobox == null)
        {
            throw new System.InvalidOperationException(
                $"Did not find infobox on page with internal name {internalName}");
        }

        var translated = new List<InfoboxField>();
        var untranslated = new List<InfoboxField>();

        foreach (var row in infobox.Children)
        {
            if (row.Children.Count() <= 1)
                continue;

            // Parse the HTML table to get keys and values
            var value = row.Children.Where(e => e.LocalName == "td").First();
            var translatedKey = row.Children.Where(e => e.LocalName == "th").First().TextContent.Trim();
            var id = value.Id;

            if (id == null)
                continue;

            var textContent = value.TextContent!.Trim();

            if (textContent == internalName)
                continue;

            // Format the found content for use in translation files
            var untranlsatedKey = id.Replace("#", string.Empty).ToUpperInvariant().Replace("-", "_");
            var untranslatedValue = textContent.ToUpperInvariant()
                .Replace(' ', '_')
                .Replace(",", "_COMMA")
                .Replace("(", "_BRACKET_")
                .Replace("__", "_");

            // Remove any leftorver characters that are not supposed to be present in translation keys
            var validUntraslatedValue = Regex.Replace(untranslatedValue, @"[^A-Z0-9_]", string.Empty);

            untranslated.Add(new InfoboxField(untranlsatedKey, validUntraslatedValue));
            translated.Add(new InfoboxField(translatedKey, textContent));
        }

        ColourConsole.WriteInfoLine($"Completed extracting infobox content for page with internal name {internalName}");

        return (untranslated, translated);
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
                case "H3":
                    text = $"[b][u]{child.Children.Where(c => c.ClassList.Contains("mw-headline")).First().TextContent}[/u][/b]\n\n";
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

        var formatted = paragraph
            .Replace("\n", string.Empty)
            .Replace("<b>", "[b]")
            .Replace("</b>", "[/b]")
            .Replace("<i>", "[i]")
            .Replace("</i>", "[/i]")
            .Replace("<br>", "\n")
            .Replace("\"", "\\\"");

        // Remove any HTML tags leftover to not pollute the page
        return Regex.Replace(formatted, @"<[^>]*>", string.Empty);

            // .Replace("<code>", "[code]")
            // .Replace("</code>", "[/code]")
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
            translationPairs.TryAdd(untranslatedPage.Name, translatedPage.Name);

            // Translate infobox
            var untranslatedInfobox = untranslatedPage!.InfoboxData;
            var translatedInfobox = translatedPage!.InfoboxData;

            for (int i = 0; i < untranslatedInfobox.Count; i++)
            {
                // Skip adding translations for numbers
                if (Regex.IsMatch(translatedInfobox[i].InfoboxValue, @"^[0-9,. ]*$"))
                    continue;

                translationPairs.TryAdd(untranslatedInfobox[i].InfoboxKey, translatedInfobox[i].InfoboxKey);
                translationPairs.TryAdd(untranslatedInfobox[i].InfoboxValue, translatedInfobox[i].InfoboxValue);
            }

            for (var i = 0; i < untranslatedPage.Sections.Count; ++i)
            {
                var untranslatedSection = untranslatedPage.Sections[i];
                var translatedSection = translatedPage.Sections[i];

                // Translate body sections
                translationPairs.TryAdd(untranslatedSection.SectionBody, translatedSection.SectionBody);

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
        [JsonInclude]
        public Page OrganellesRoot { get; init; } = null!;

        [JsonInclude]
        public List<Page> Organelles { get; init; } = null!;

        [JsonInclude]
        public Page StagesRoot { get; init; } = null!;

        [JsonInclude]
        public List<Page> Stages { get; init; } = null!;

        [JsonInclude]
        public Page ConceptsRoot { get; init; } = null!;

        [JsonInclude]
        public List<Page> Concepts { get; init; } = null!;

        [JsonInclude]
        public Page DevelopmentRoot { get; init; } = null!;

        [JsonInclude]
        public List<Page> DevelopmentPages { get; init; } = null!;

        public class Page
        {
            public Page(string name, string internalName, string url, List<Section> sections,
                List<InfoboxField> infobox = null!)
            {
                Name = name;
                InternalName = internalName;
                Url = url;
                Sections = sections;
                InfoboxData = infobox ?? new();
            }

            [JsonInclude]
            public string Name { get; }

            [JsonInclude]
            public string InternalName { get; }

            [JsonInclude]
            public string Url { get; }

            [JsonInclude]
            public List<Section> Sections { get; }

            [JsonInclude]
            public List<InfoboxField> InfoboxData { get; }

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

    private class InfoboxField
    {
        public InfoboxField(string key, string value)
        {
            InfoboxKey = key;
            InfoboxValue = value;
        }

        [JsonInclude]
        public string InfoboxKey { get; }

        [JsonInclude]
        public string InfoboxValue { get; }
    }
}
