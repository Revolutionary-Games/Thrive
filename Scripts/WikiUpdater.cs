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

    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        var organellesTask = FetchOrganellePages(cancellationToken);

        var (organelles, translatedOrganelles) = await organellesTask;

        var wiki = new Wiki(organelles);
        await JsonWriteHelper.WriteJsonWithBom(WIKI_FILE, wiki, cancellationToken);
        ColourConsole.WriteSuccessLine($"Updated wiki at {WIKI_FILE}, running translations update");

        var localizationUpdater = new LocalizationUpdate(new LocalizationOptionsBase() { Quiet = true });
        if (!localizationUpdater.Run(cancellationToken).Result)
            return false;

        ColourConsole.WriteSuccessLine("Translations update succeeded, inserting English strings for wiki content");

        await InsertEnglishPageContent(organelles, translatedOrganelles, cancellationToken);

        ColourConsole.WriteSuccessLine("Successfully updated English translations for wiki content");

        return true;
    }

    private static async Task<(List<Wiki.Page>, List<Wiki.Page>)> FetchOrganellePages(CancellationToken cancellationToken)
    {
        var categoryBody = (await RetrieveHtmlDocument(ORGANELLE_CATEGORY, cancellationToken)).Body!;
        var organelles = categoryBody.QuerySelectorAll(".mw-category-group > ul > li");

        var organellePagesUntranslated = new List<Wiki.Page>();
        var organellePagesTranslated = new List<Wiki.Page>();

        foreach (var organelle in organelles)
        {
            var name = organelle.TextContent.Trim();
            var url = $"https://wiki.revolutionarygamesstudio.com/wiki/{name}";

            var untranslatedName = name.ToUpperInvariant().Replace(" ", "_");

            ColourConsole.WriteSuccessLine($"Found organelle {name}");

            var body = (await RetrieveHtmlDocument(url, cancellationToken)).Body!;
            
            var internalName = body.QuerySelector("#info-box-internal-name")!.TextContent.Trim();

            organellePagesUntranslated.Add(new($"{untranslatedName}_PAGE_TITLE", internalName, url, new()
                {
                    (null, $"{untranslatedName}_WIKI_DESCRIPTION"),
                    ("REQUIREMENTS_HEADING", $"{untranslatedName}_WIKI_REQUIREMENTS"),
                    ("PROCESSES_HEADING", $"{untranslatedName}_WIKI_PROCESSES"),
                    ("MODIFICATIONS_HEADING", $"{untranslatedName}_WIKI_MODIFICATIONS"),
                    ("EFFECTS_HEADING", $"{untranslatedName}_WIKI_EFFECTS"),
                    ("UPGRADES_HEADING", $"{untranslatedName}_WIKI_UPGRADES"),
                    ("STRATEGY_HEADING", $"{untranslatedName}_WIKI_STRATEGY"),   
                    ("SCIENTIFIC_BACKGROUND_HEADING", $"{untranslatedName}_WIKI_SCIENTIFIC_BACKGROUND"),                                                                             
                }
            ));

            ColourConsole.WriteSuccessLine($"Added organelle with internal name {internalName}");

            var description = body.QuerySelector("#organelle-description");
            var requirements = body.QuerySelector("#organelle-requirements");
            var processes = body.QuerySelector("#organelle-processes");
            var modifications = body.QuerySelector("#organelle-modifications");
            var effects = body.QuerySelector("#organelle-effects");
            var upgrades = body.QuerySelector("#organelle-upgrades");
            var strategy = body.QuerySelector("#organelle-strategy");
            var scientificBackground = body.QuerySelector("#organelle-scientific-background");

            var descriptionText = ConvertHtmlToBbcode(description!.InnerHtml);
            var requirementsText = ConvertHtmlToBbcode(requirements!.InnerHtml);
            var processesText = ConvertHtmlToBbcode(processes!.InnerHtml);
            var modificationsText = ConvertHtmlToBbcode(modifications!.InnerHtml);
            var effectsText = ConvertHtmlToBbcode(effects!.InnerHtml);
            var upgradesText = ConvertHtmlToBbcode(upgrades!.InnerHtml);
            var strategyText = ConvertHtmlToBbcode(strategy!.InnerHtml);
            var scientificBackgroundText = ConvertHtmlToBbcode(scientificBackground!.InnerHtml);

            organellePagesTranslated.Add(new(name, internalName, url, new()
                {
                    (null, descriptionText),
                    ("Requirements", requirementsText),
                    ("Processes", processesText),
                    ("Modifications", modificationsText),
                    ("Effects", effectsText),
                    ("Upgrades", upgradesText),
                    ("Strategy", strategyText),   
                    ("Scientific Background", scientificBackgroundText),                                                                             
                }
            ));

            ColourConsole.WriteSuccessLine($"Populated English content for organelle with internal name {internalName}");
        }

        return (organellePagesUntranslated, organellePagesTranslated);
    }

    private static async Task InsertEnglishPageContent(
        List<Wiki.Page> untranslatedPages,
        List<Wiki.Page> translatedPages,
        CancellationToken cancellationToken)
    {
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

    private static string ConvertHtmlToBbcode(string html) =>
        html.Replace("\n", "")
            .Replace("</p>", "\n\n")
            .Replace("<p>", "")
            .Replace("<b>", "[b]")
            .Replace("</b>", "[/b]")
            .Replace("</b>", "[/b]")
            .Replace("<i>", "[i]")
            .Replace("</i>", "[/i]")
            // Godot 3 doesn't support lists in BBCode, so add custom formatting
            .Replace("<ul><li>", "[indent]•   ")
            .Replace("<li>", "\n[indent]•   ")
            .Replace("</li>", "[/indent]")
            .Replace("</ul>", "\n\n")
            .Trim();
    
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
        public Wiki(List<Page> organelles)
        {
            Organelles = organelles;
        }

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