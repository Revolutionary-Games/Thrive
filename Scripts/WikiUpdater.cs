using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Scripts;
using ScriptsBase.Models;
using ScriptsBase.Utilities;

public static class WikiUpdater
{
    private const string ORGANELLE_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Organelles";
    private const string WIKI_FILE = "simulation_parameters/common/wiki.json";
    private const string ENGLISH_TRANSLATION_FILE = "locale/en.po";

    // TODO get these from JSON?
    private static readonly string[] customBbcodeCompounds = {
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
        "Temperature"
        };

    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        var organellesRootTask = FetchOrganelleRootPage(cancellationToken);
        var organellesTask = FetchOrganellePages(cancellationToken);

        var (organellesRoot, translatedOrganellesRoot) = await organellesRootTask;
        var (organelles, translatedOrganelles) = await organellesTask;

        var wiki = new Wiki(organellesRoot, organelles);
        await JsonWriteHelper.WriteJsonWithBom(WIKI_FILE, wiki, cancellationToken);
        ColourConsole.WriteSuccessLine($"Updated wiki at {WIKI_FILE}, running translations update");

        var localizationUpdater = new LocalizationUpdate(new LocalizationOptionsBase() { Quiet = true });
        if (!localizationUpdater.Run(cancellationToken).Result)
            return false;

        ColourConsole.WriteSuccessLine("Translations update succeeded, inserting English strings for wiki content");

        await InsertEnglishPageContent(
            organelles.Append(organellesRoot).ToList(),
            translatedOrganelles.Append(translatedOrganellesRoot).ToList(),
            cancellationToken);

        ColourConsole.WriteSuccessLine("Successfully updated English translations for wiki content");

        return true;
    }

    private static async Task<(Wiki.Page, Wiki.Page)> FetchOrganelleRootPage(CancellationToken cancellationToken)
    {
        ColourConsole.WriteSuccessLine($"Fetching organelle root page");

        var body = (await RetrieveHtmlDocument(ORGANELLE_CATEGORY, cancellationToken)).Body!;

        var sections = GetMainBodySections(body);
        var translatedSections = sections.Select(section => TranslateSection(section, "ORGANELLES_ROOT")).ToList();

        var untranslatedPage = new Wiki.Page("WIKI_PAGE_ORGANELLES_ROOT", "OrganellesRoot", ORGANELLE_CATEGORY, translatedSections);
        var translatedPage = new Wiki.Page("Organelles", "OrganellesRoot", ORGANELLE_CATEGORY, sections);

        ColourConsole.WriteSuccessLine($"Populated content for organelle root page");

        return (untranslatedPage, translatedPage);
    }

    private static async Task<(List<Wiki.Page>, List<Wiki.Page>)> FetchOrganellePages(CancellationToken cancellationToken)
    {
        ColourConsole.WriteSuccessLine($"Fetching organelle pages");

        var categoryBody = (await RetrieveHtmlDocument(ORGANELLE_CATEGORY, cancellationToken)).Body!;
        var organelles = categoryBody.QuerySelectorAll(".mw-category-group > ul > li");

        var organellePagesUntranslated = new List<Wiki.Page>();
        var organellePagesTranslated = new List<Wiki.Page>();

        foreach (var organelle in organelles)
        {
            var name = organelle.TextContent.Trim();
            var url = $"https://wiki.revolutionarygamesstudio.com/wiki/{name.Replace(" ", "_")}";

            var untranslatedOrganelleName = name.ToUpperInvariant().Replace(" ", "_");

            ColourConsole.WriteSuccessLine($"Found organelle {name}");

            var body = (await RetrieveHtmlDocument(url, cancellationToken)).Body!;
            
            var internalName = body.QuerySelector("#info-box-internal-name")!.TextContent.Trim();
            var sections = GetMainBodySections(body);

            var untranslatedSections = sections.Select(section => TranslateSection(section, untranslatedOrganelleName)).ToList();

            organellePagesTranslated.Add(new(name, internalName, url, sections));
            organellePagesUntranslated.Add(new($"WIKI_PAGE_{untranslatedOrganelleName}", internalName, url, untranslatedSections));

            ColourConsole.WriteSuccessLine($"Populated content for organelle with internal name {internalName}");
        }

        return (organellePagesUntranslated, organellePagesTranslated);
    }

    private static List<(string?, string)> GetMainBodySections(IHtmlElement body)
    {
        var sections = new List<(string?, string)>() { (null, "") };

        var children = body.QuerySelector(".mw-parser-output")!.Children;
        foreach (var child in children)
        {
            if (child.TagName == "H2")
            {
                sections.Add((child.TextContent, ""));
                continue;
            }

            var text = "";
            switch (child.TagName)
            {
                case "P":
                    text = ConvertTextToBbcode(child.InnerHtml) + "\n\n";
                    break;
                case "UL":
                    text = child.Children
                            .Where(c => c.TagName == "LI")
                            .Select(li => $"[indent]•   {ConvertTextToBbcode(li.InnerHtml)}[/indent]")
                            .Aggregate((a, b) => a + "\n" + b) + "\n\n";
                    break;
                default:
                    // Ignore all other tag types
                    continue;
            }

            sections[^1] = (sections[^1].Item1, sections[^1].Item2 + text);
        }

        return sections.Select(s => (s.Item1, s.Item2.Trim())).ToList();
    }

    private static (string?, string) TranslateSection((string?, string) section, string pageName)
    {
        var sectionName = section.Item1?.ToUpperInvariant().Replace(" ", "_");
        var heading = sectionName != null ? $"WIKI_HEADING_{sectionName}" : null;
        var body = sectionName != null ? $"WIKI_{pageName}_{sectionName}" : $"WIKI_{pageName}_INTRO";

        return (heading, body);
    }

    private static string ConvertTextToBbcode(string paragraph)
    {
        foreach (var compound in customBbcodeCompounds)
        {
            paragraph = paragraph.Replace(
                $"<b>{compound}</b>",
                $"[thrive:compound type=\"{compound.ToLowerInvariant().Replace(" ", "")}\"][/thrive:compound]");
        }

        return paragraph
            .Replace("\n", "")
            .Replace("<b>", "[b]")
            .Replace("</b>", "[/b]")
            .Replace("<i>", "[i]")
            .Replace("</i>", "[/i]")
            .Replace("\"", "\\\"");
    }

    private static async Task InsertEnglishPageContent(
        List<Wiki.Page> untranslatedPages,
        List<Wiki.Page> translatedPages,
        CancellationToken cancellationToken)
    {
        if (untranslatedPages.Count() != translatedPages.Count())
            throw new Exception("Untranslated and translated page counts don't match");

        var lines = new List<string>(await File.ReadAllLinesAsync(ENGLISH_TRANSLATION_FILE, Encoding.UTF8, cancellationToken));

        for (var i = 0; i < untranslatedPages.Count(); i++)
        {
            var untranslatedPage = untranslatedPages[i];
            var translatedPage = translatedPages[i];

            ReplaceTranslationValue(untranslatedPage.Name, translatedPage.Name, lines);

            for (var j = 0; j < untranslatedPage.Sections.Count(); j++)
            {
                var untranslatedSection = untranslatedPage.Sections[j];
                var translatedSection = translatedPage.Sections[j];

                ReplaceTranslationValue(untranslatedSection.SectionBody, translatedSection.SectionBody, lines);

                if (untranslatedSection.SectionHeading != null && translatedSection.SectionHeading != null)
                    ReplaceTranslationValue(untranslatedSection.SectionHeading, translatedSection.SectionHeading, lines);
            }
        }

        await File.WriteAllLinesAsync(ENGLISH_TRANSLATION_FILE, lines, new UTF8Encoding(false), cancellationToken);
    }

    private static async Task<IHtmlDocument> RetrieveHtmlDocument(string url, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var parser = new HtmlParser();

        var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync(cancellationToken));

        if (document.Body == null)
            throw new Exception("Parsed document has no body");

        return document;
    }
    
    private static void ReplaceTranslationValue(string key, string content, List<string> lines)
    {
        var keyIndex = lines.FindIndex(l => l.Contains(key));

        if (keyIndex == -1)
            throw new Exception($"Key {key} was not found in English translations file");

        var i = keyIndex + 1;
        while (lines[i] != "")
            lines.RemoveAt(i);

        var linesToInsert = content.Split("\n");

        if (linesToInsert.Count() == 1)
        {
            lines.Insert(keyIndex + 1, $"msgstr \"{linesToInsert[0]}\"");
        }
        else
        {
            lines.Insert(keyIndex + 1, $"msgstr \"\"");
            for (var j = 0; j < linesToInsert.Count(); j++)
            {
                var line = linesToInsert[j];
                var textToInsert = j == linesToInsert.Count() - 1 ? $"\"{line}\"" : $"\"{line}\\n\"";
                lines.Insert(keyIndex + 2 + j, textToInsert);
            }
        }

        if (lines[keyIndex - 1] == "#, fuzzy")
            lines.RemoveAt(keyIndex - 1);
    }

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
            public Page(string name, string internalName, string url, List<(string?, string)> sections)
            {
                Name = name;
                InternalName = internalName;
                Url = url;

                Sections = sections.Select(s => new Section(s.Item1, s.Item2)).ToList();
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
}