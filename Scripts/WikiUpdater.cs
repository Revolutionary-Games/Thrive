using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Scripts;
using ScriptsBase.Models;
using ScriptsBase.Utilities;

public class WikiUpdater
{
    private const string ORGANELLE_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Organelles";
    private const string STAGES_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Stages";
    private const string MECHANICS_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Mechanics";
    private const string DEVELOPMENT_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Development";

    private const string WIKI_FILE = "simulation_parameters/common/wiki.json";
    private const string ENGLISH_TRANSLATION_FILE = "locale/en.po";
    private const string TEMP_TRANSLATION_FILE = "en.po.temp_wiki";

    private const string INFO_BOX_SELECTOR = ".wikitable > tbody";
    private const string CATEGORY_PAGES_SELECTOR = ".mw-category-group > ul > li";
    private const string IGNORE_PAGE_SELECTOR = "[href=\"/wiki/Category:Only_Online\"]";

    /// <summary>
    ///   List of compound names, used to differentate between using the thrive:compound and
    ///   thrive:icon bbcode tags
    /// </summary>
    private readonly string[] compoundNames =
    {
        "glucose",
        "ammonia",
        "phosphates",
        "iron",
        "hydrogensulfide",

        "oxytoxy",
        "mucilage",

        "atp",

        "oxygen",
        "nitrogen",
        "carbondioxide",

        "sunlight",
        "temperature",
    };

    /// <summary>
    ///   List of regexes for domains we're allowing Thriveopedia content to link to.
    /// </summary>
    private readonly Regex[] whitelistedDomains =
    {
        new(@".*\.wikipedia\.org\/.*"),
        new(@".*\.revolutionarygamesstudio\.com\/.*"),
    };

    /// <summary>
    ///   Mapping from English page names to internal page names, required for inter-page linking in game.
    /// </summary>
    private readonly Dictionary<string, string> pageNames = new();

    /// <summary>
    ///   Inserts selected content from the online wiki into the game files. See
    ///   https://wiki.revolutionarygamesstudio.com/wiki/Thriveopedia for instructions.
    /// </summary>
    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        var organellesRootTask = FetchRootPage(ORGANELLE_CATEGORY, "Organelles", cancellationToken);
        var stagesRootTask = FetchRootPage(STAGES_CATEGORY, "Stages", cancellationToken);
        var mechanicsRootTask = FetchRootPage(MECHANICS_CATEGORY, "Mechanics", cancellationToken);
        var developmentRootTask = FetchRootPage(DEVELOPMENT_CATEGORY, "Development", cancellationToken);

        var organellesTask = FetchPagesFromCategory(ORGANELLE_CATEGORY, "Organelle", cancellationToken);
        var stagesTask = FetchPagesFromCategory(STAGES_CATEGORY, "Stage", cancellationToken);
        var mechanicsTask = FetchPagesFromCategory(MECHANICS_CATEGORY, "Mechainc", cancellationToken);
        var developmentPagesTask = FetchPagesFromCategory(DEVELOPMENT_CATEGORY, "Development Page", cancellationToken);

        var organellesRootRaw = await organellesRootTask;
        var stagesRootRaw = await stagesRootTask;
        var mechanicsRootRaw = await mechanicsRootTask;
        var developmentRootRaw = await developmentRootTask;

        var organellesRaw = await organellesTask;
        var stagesRaw = await stagesTask;
        var mechanicsRaw = await mechanicsTask;
        var developmentPagesRaw = await developmentPagesTask;
        ColourConsole.WriteSuccessLine("Fetched all wiki pages");

        var organellesRoot = ProcessRootPage(organellesRootRaw, "Organelles", ORGANELLE_CATEGORY);
        var stagesRoot = ProcessRootPage(stagesRootRaw, "Stages", STAGES_CATEGORY);
        var mechanicsRoot = ProcessRootPage(mechanicsRootRaw, "Mechanics", MECHANICS_CATEGORY);
        var developmentRoot = ProcessRootPage(developmentRootRaw, "Development", DEVELOPMENT_CATEGORY);

        var organelles = ProcessPagesFromCategory(organellesRaw);
        var stages = ProcessPagesFromCategory(stagesRaw);
        var mechanics = ProcessPagesFromCategory(mechanicsRaw);
        var developmentPages = ProcessPagesFromCategory(developmentPagesRaw);
        ColourConsole.WriteSuccessLine("Processed all wiki pages");

        var untranslatedWiki = new Wiki
        {
            OrganellesRoot = organellesRoot.UntranslatedPage,
            StagesRoot = stagesRoot.UntranslatedPage,
            MechanicsRoot = mechanicsRoot.UntranslatedPage,
            DevelopmentRoot = developmentRoot.UntranslatedPage,

            Organelles = organelles.Select(o => o.UntranslatedPage).ToList(),
            Stages = stages.Select(s => s.UntranslatedPage).ToList(),
            Mechanics = mechanics.Select(c => c.UntranslatedPage).ToList(),
            DevelopmentPages = developmentPages.Select(p => p.UntranslatedPage).ToList(),
        };

        await JsonWriteHelper.WriteJsonWithBom(WIKI_FILE, untranslatedWiki, cancellationToken);
        ColourConsole.WriteSuccessLine($"Updated wiki at {WIKI_FILE}, running translations update");

        var localizationUpdater = new LocalizationUpdate(new LocalizationOptionsBase { Quiet = true });
        if (!await localizationUpdater.Run(cancellationToken))
            return false;

        ColourConsole.WriteSuccessLine("Translations update succeeded, inserting English strings for wiki content");

        var pages = new List<TranslationPair>
        {
            organellesRoot,
            stagesRoot,
            mechanicsRoot,
            developmentRoot,
        };

        pages.AddRange(organelles);
        pages.AddRange(stages);
        pages.AddRange(mechanics);
        pages.AddRange(developmentPages);

        await InsertTranslatedPageContent(pages, cancellationToken);
        ColourConsole.WriteSuccessLine("Successfully updated English translations for wiki content");

        return true;
    }

    private async Task<IHtmlElement> FetchRootPage(string url, string categoryName, CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine($"Fetching {categoryName.ToLowerInvariant()} root page");
        var body = (await HtmlReader.RetrieveHtmlDocument(url, cancellationToken)).Body!;
        pageNames.Add(categoryName, $"{categoryName}Root");
        return body;
    }

    private async Task<List<IHtmlElement>> FetchPagesFromCategory(string categoryUrl,
        string pageType, CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine($"Fetching {pageType} pages");
        var allPages = new List<IHtmlElement>();

        var textInfo = CultureInfo.InvariantCulture.TextInfo;

        // Get the list of pages from the category page on the wiki
        var categoryBody = (await HtmlReader.RetrieveHtmlDocument(categoryUrl, cancellationToken)).Body!;
        var pages = categoryBody.QuerySelectorAll(CATEGORY_PAGES_SELECTOR);

        foreach (var page in pages)
        {
            var name = page.TextContent.Trim();
            var url = $"https://wiki.revolutionarygamesstudio.com/wiki/{name.Replace(" ", "_")}";

            ColourConsole.WriteInfoLine($"Found {pageType} {name}");

            var body = (await HtmlReader.RetrieveHtmlDocument(url, cancellationToken)).Body!;
            var internalName = textInfo.ToTitleCase(name).Replace(" ", string.Empty);

            // Ignore page if specified
            if (body.QuerySelector(IGNORE_PAGE_SELECTOR) != null)
            {
                ColourConsole.WriteLineWithColour($"Ignored {pageType} {name} due to Only Online category",
                    ConsoleColor.Red);
                continue;
            }

            if (body.QuerySelector(".infobox") != null)
            {
                internalName = body.QuerySelector("#info-box-internal-name")!.TextContent.Trim();
            }

            pageNames.Add(name, internalName);
            allPages.Add(body);
        }

        return allPages;
    }

    private TranslationPair ProcessRootPage(IHtmlElement body, string categoryName, string url)
    {
        var fullPageName = $"{categoryName}Root";
        var translationKey = $"{categoryName.ToUpperInvariant()}_ROOT";

        var sections = GetMainBodySections(body);
        var untranslatedSections = sections.Select(s => UntranslateSection(s, translationKey)).ToList();

        var untranslatedPage = new Wiki.Page($"WIKI_PAGE_{translationKey}", fullPageName, url,
            untranslatedSections);
        var translatedPage = new Wiki.Page(categoryName, fullPageName, url, sections);

        ColourConsole.WriteSuccessLine($"Populated content for {categoryName.ToLowerInvariant()} root page");

        return new TranslationPair(untranslatedPage, translatedPage);
    }

    private List<TranslationPair> ProcessPagesFromCategory(List<IHtmlElement> pages)
    {
        var allPages = new List<TranslationPair>();
        var textInfo = CultureInfo.InvariantCulture.TextInfo;

        foreach (var page in pages)
        {
            var name = page.QuerySelector(".mw-page-title-main")!.TextContent;
            var pageUrl = $"https://wiki.revolutionarygamesstudio.com/wiki/{name.Replace(" ", "_")}";

            var untranslatedPageName = name.ToUpperInvariant().Replace(" ", "_");

            var translatedInfobox = new List<InfoboxField>();
            var untranslatedInfobox = new List<InfoboxField>();

            var internalName = textInfo.ToTitleCase(name).Replace(" ", string.Empty);

            if (page.QuerySelector(".infobox") != null)
            {
                internalName = page.QuerySelector("#info-box-internal-name")!.TextContent.Trim();
                (untranslatedInfobox, translatedInfobox) = GetInfoBoxFields(page, internalName);
            }

            var noticeBox = page.QuerySelector(".NoticeBox");
            string? noticeSceneName = null;

            if (noticeBox != null)
            {
                // Get the relevant scene for this notice from the class
                var noticeClass = noticeBox.ClassList.First(c => c.Contains("thriveopedia"));
                var sceneNameLowercase = noticeClass
                    .Replace("thriveopedia-", string.Empty)
                    .Replace("-", " ");

                noticeSceneName = textInfo.ToTitleCase(sceneNameLowercase).Replace(" ", string.Empty) + "Notice";
            }

            var sections = GetMainBodySections(page);
            var untranslatedSections = sections.Select(s => UntranslateSection(s, untranslatedPageName)).ToList();

            var untranslatedPage = new Wiki.Page($"WIKI_PAGE_{untranslatedPageName}",
                internalName,
                pageUrl,
                untranslatedSections,
                untranslatedInfobox,
                noticeSceneName);
            var translatedPage = new Wiki.Page(name,
                internalName,
                pageUrl,
                sections,
                translatedInfobox,
                noticeSceneName);

            allPages.Add(new TranslationPair(untranslatedPage, translatedPage));

            ColourConsole.WriteSuccessLine($"Populated content for page with internal name {internalName}");
        }

        return allPages;
    }

    /// <summary>
    ///   Extracts all the fields of an associated page's infobox and generates translation keys
    /// </summary>
    /// <param name="body">The body of the page</param>
    /// <param name="internalName">Internal name of the page</param>
    /// <returns>A tuple containing the translated and untranlsated versions of the detected fields</returns>
    /// <exception cref="InvalidOperationException">Thrown when an infobox is not found</exception>
    private (List<InfoboxField> Untranslated, List<InfoboxField> Translated)
        GetInfoBoxFields(IHtmlElement body, string internalName)
    {
        ColourConsole.WriteInfoLine($"Extracting infobox content for page with internal name {internalName}");

        // Get the infobox element
        var infobox = body.QuerySelector(INFO_BOX_SELECTOR) ?? throw new InvalidOperationException(
            $"Did not find infobox on page with internal name {internalName}");

        var translated = new List<InfoboxField>();
        var untranslated = new List<InfoboxField>();

        foreach (var row in infobox.Children)
        {
            if (row.Children.Length <= 1)
                continue;

            // Parse the HTML table to get keys and values
            var value = row.Children.Where(e => e.LocalName == "td").First();
            var translatedKey = row.Children.Where(e => e.LocalName == "th").First().TextContent.Trim();
            var id = value.Id;

            if (id == null)
                continue;

            var textContent = value.TextContent.Trim();

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
            var validUntraslatedValue = Regex.Replace(untranslatedValue, "[^A-Z0-9_]", string.Empty);

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
    private List<Wiki.Page.Section> GetMainBodySections(IHtmlElement body)
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
                    text = ConvertParagraphToBbcode(child) + "\n\n";
                    break;
                case "UL":

                    // Godot 3 does not support lists in BBCode, so use custom formatting
                    text = child.Children
                        .Where(c => c.TagName == "LI")
                        .Select(l => $"[indent]—   {ConvertParagraphToBbcode(l)}[/indent]")
                        .Aggregate((a, b) => a + "\n" + b) + "\n\n";
                    break;
                case "H3":
                    var headline = child.Children
                        .Where(c => c.ClassList.Contains("mw-headline"))
                        .First();

                    text = $"[b][u]{headline.TextContent}[/u][/b]\n\n";
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
    private Wiki.Page.Section UntranslateSection(Wiki.Page.Section section, string pageName)
    {
        var sectionName = section.SectionHeading?.ToUpperInvariant().Replace(" ", "_");
        var heading = sectionName != null ? $"WIKI_HEADING_{sectionName}" : null;
        var body = sectionName != null ? $"WIKI_{pageName}_{sectionName}" : $"WIKI_{pageName}_INTRO";

        return new Wiki.Page.Section(heading, body);
    }

    /// <summary>
    ///   Converts HTML for a single paragraph into BBCode. Paragraph must not contain lists, headings, etc.
    /// </summary>
    private string ConvertParagraphToBbcode(IElement paragraph)
    {
        var bbcode = new StringBuilder();

        var children = paragraph.ChildNodes;
        foreach (var child in children)
        {
            if (child is IHtmlAnchorElement link)
            {
                bbcode.Append(ConvertLinkToBbcode(link));
            }
            else if (child is IHtmlImageElement image)
            {
                bbcode.Append(ConvertImageToBbcode(image, bbcode));
            }
            else if (child is IElement element)
            {
                if (element.TagName == "B" && element.Children.Length > 0)
                {
                    // Deal with items inside bold tags, e.g. links
                    bbcode.Append("[b]");
                    bbcode.Append(ConvertParagraphToBbcode(element));
                    bbcode.Append("[/b]");
                    continue;
                }

                bbcode.Append(ConvertTextToBbcode(element.OuterHtml));
            }
            else
            {
                bbcode.Append(ConvertTextToBbcode(child.TextContent));
            }
        }

        return bbcode.ToString();
    }

    /// <summary>
    ///   Removes the last bold text label and all subsequent text from this string.
    /// </summary>
    private void RemoveLastBoldText(StringBuilder bbcode)
    {
        var boldTextIndex = bbcode.ToString().LastIndexOf("[b]", StringComparison.Ordinal);

        if (boldTextIndex < 0)
            return;

        bbcode.Remove(boldTextIndex, bbcode.Length - boldTextIndex);
    }

    /// <summary>
    ///   Converts an HTML link element into BBCode (external or pointing at another page).
    /// </summary>
    private string ConvertLinkToBbcode(IHtmlAnchorElement link)
    {
        var isExternalLink = link.ClassName == "external text";

        if (isExternalLink)
        {
            // Use text if the link isn't whitelisted
            if (!whitelistedDomains.Any(d => d.IsMatch(link.Href)))
                return ConvertTextToBbcode(link.InnerHtml);

            return $"[color=#3796e1][url={link.Href}]{ConvertTextToBbcode(link.InnerHtml)}[/url][/color]";
        }

        var translatedPageName = link.Title!;

        if (!pageNames.TryGetValue(translatedPageName, out var internalPageName))
        {
            return ConvertTextToBbcode(link.InnerHtml);

            // throw new Exception($"Tried to create link to page {translatedPageName} but it doesn't exist");
        }

        var linkText = ConvertTextToBbcode(link.InnerHtml);
        return $"[color=#3796e1][url=thriveopedia:{internalPageName}]{linkText}[/url][/color]";
    }

    /// <summary>
    ///   Converts an HTML image into BBCode. Currently only works for compound and other icons embedded in paragraphs.
    /// </summary>
    private string ConvertImageToBbcode(IHtmlImageElement image, StringBuilder bbcode)
    {
        if (compoundNames.Contains(image.AlternativeText))
        {
            // In-game compound BBCode already has bold text label, so remove the extra one
            RemoveLastBoldText(bbcode);
            return $"[thrive:compound type=\\\"{image.AlternativeText}\\\"][/thrive:compound]";
        }

        return $"[thrive:icon]{image.AlternativeText}[/thrive:icon]";
    }

    /// <summary>
    ///   Converts formatted HTML text into BBCode.
    /// </summary>
    private string ConvertTextToBbcode(string paragraph)
    {
        return paragraph
            .Replace("\n", string.Empty)
            .Replace("<b>", "[b]")
            .Replace("</b>", "[/b]")
            .Replace("<i>", "[i]")
            .Replace("</i>", "[/i]")
            .Replace("<u>", "[u]")
            .Replace("</u>", "[/u]")
            .Replace("<br>", "\n")
            .Replace("\"", "\\\"");
    }

    /// <summary>
    ///   Inserts into en.po the English translations for all the translation keys in a list of wiki pages.
    /// </summary>
    private async Task InsertTranslatedPageContent(IEnumerable<TranslationPair> pages,
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
            var untranslatedInfobox = untranslatedPage.InfoboxData;
            var translatedInfobox = translatedPage.InfoboxData;

            for (int i = 0; i < untranslatedInfobox.Count; i++)
            {
                // Skip adding translations for numbers or "-"
                if (Regex.IsMatch(translatedInfobox[i].InfoboxValue, "^[0-9,. -]*$"))
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
        public Page MechanicsRoot { get; init; } = null!;

        [JsonInclude]
        public List<Page> Mechanics { get; init; } = null!;

        [JsonInclude]
        public Page DevelopmentRoot { get; init; } = null!;

        [JsonInclude]
        public List<Page> DevelopmentPages { get; init; } = null!;

        public class Page
        {
            public Page(string name, string internalName, string url, List<Section> sections,
                List<InfoboxField>? infobox = null, string? noticeSceneName = null)
            {
                Name = name;
                InternalName = internalName;
                Url = url;
                Sections = sections;
                InfoboxData = infobox ?? new List<InfoboxField>();
                NoticeSceneName = noticeSceneName;
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

            [JsonInclude]
            public string? NoticeSceneName { get; }

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
