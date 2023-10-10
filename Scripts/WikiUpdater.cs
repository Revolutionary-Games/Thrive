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
    private const string WIKI_FILE = "simulation_parameters/common/wiki.json";
    private const string ENGLISH_TRANSLATION_FILE = "locale/en.po";

    private const string ORGANELLE_CATEGORY = "https://wiki.revolutionarygamesstudio.com/wiki/Category:Organelles";

    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        var organellesTask = FetchOrganellePages(cancellationToken);

        var (organelles, organellesEnglishContent) = await organellesTask;

        var wiki = new Wiki(organelles);
        await JsonWriteHelper.WriteJsonWithBom(WIKI_FILE, wiki, cancellationToken);
        ColourConsole.WriteSuccessLine($"Updated wiki at {WIKI_FILE}, running translations update");

        var localizationUpdater = new LocalizationUpdate(new LocalizationOptionsBase() { Quiet = true });
        if (!localizationUpdater.Run(cancellationToken).Result)
            return false;

        ColourConsole.WriteSuccessLine("Translations update succeeded, inserting English strings for wiki content");

        await InsertEnglishContentForOrganelles(organellesEnglishContent, cancellationToken);

        ColourConsole.WriteSuccessLine("Successfully updated English translations for wiki content");

        return true;
    }

    private static async Task<(List<OrganelleWikiPage>, Dictionary<string, OrganelleWikiPage.OrganelleSections>)> FetchOrganellePages(CancellationToken cancellationToken)
    {
        var categoryBody = (await RetrieveHtmlDocument(ORGANELLE_CATEGORY, cancellationToken)).Body!;
        var organelles = categoryBody.QuerySelectorAll(".mw-category-group > ul > li");

        var organellePages = new List<OrganelleWikiPage>();
        var organellePagesInEnglish = new Dictionary<string, OrganelleWikiPage.OrganelleSections>();

        foreach (var organelle in organelles)
        {
            var name = organelle.TextContent.Trim();
            var url = $"https://wiki.revolutionarygamesstudio.com/wiki/{name}";

            ColourConsole.WriteSuccessLine($"Found organelle {name}");

            var body = (await RetrieveHtmlDocument(url, cancellationToken)).Body!;
            
            var internalName = body.QuerySelector("#info-box-internal-name")!.TextContent.Trim();

            organellePages.Add(new(internalName, url));

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

            var englishContent = new OrganelleWikiPage.OrganelleSections(
                descriptionText,
                requirementsText,
                processesText,
                modificationsText,
                effectsText,
                upgradesText,
                strategyText,
                scientificBackgroundText
            );

            organellePagesInEnglish.Add(internalName, englishContent);

            ColourConsole.WriteSuccessLine($"Populated English content for organelle with internal name {internalName}");
        }

        return (organellePages, organellePagesInEnglish);
    }

    private static async Task InsertEnglishContentForOrganelles(
        Dictionary<string, OrganelleWikiPage.OrganelleSections> organellesEnglishContent,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>(await File.ReadAllLinesAsync(ENGLISH_TRANSLATION_FILE, Encoding.UTF8, cancellationToken));

        foreach (var organelle in organellesEnglishContent)
        {
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_DESCRIPTION", organelle.Value.Description, lines);
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_REQUIREMENTS", organelle.Value.Requirements, lines);
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_PROCESSES", organelle.Value.Processes, lines);
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_MODIFICATIONS", organelle.Value.Modifications, lines);
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_EFFECTS", organelle.Value.Effects, lines);
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_UPGRADES", organelle.Value.Upgrades, lines);
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_STRATEGY", organelle.Value.Strategy, lines);
            InsertPageSection($"{organelle.Key.ToUpperInvariant()}_WIKI_SCIENTIFIC_BACKGROUND", organelle.Value.ScientificBackground, lines);
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
            .Replace("<ul><li>", "[indent]•   ")
            .Replace("<li>", "\n[indent]•   ")
            .Replace("</li>", "[/indent]")
            .Replace("</ul>", "\n\n")
            .Trim();
    
    private static void InsertPageSection(string key, string content, List<string> lines)
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
        public Wiki(List<OrganelleWikiPage> organelles)
        {
            Organelles = organelles;
        }

        [JsonInclude]
        public List<OrganelleWikiPage> Organelles { get; }
    }

    private class OrganelleWikiPage
    {
        public OrganelleWikiPage(
            string internalName,
            string url)
        {
            InternalName = internalName;
            Url = url;

            Sections = new OrganelleSections(internalName);
        }

        [JsonInclude]
        public string InternalName { get; }

        [JsonInclude]
        public string Url { get; }

        [JsonInclude]
        public OrganelleSections Sections { get; }

        public class OrganelleSections
        {
            public OrganelleSections(string internalName)
            {
                var internalNameUpper = internalName.ToUpperInvariant();

                Description = $"{internalNameUpper}_WIKI_DESCRIPTION";
                Requirements = $"{internalNameUpper}_WIKI_REQUIREMENTS";
                Processes = $"{internalNameUpper}_WIKI_PROCESSES";
                Modifications = $"{internalNameUpper}_WIKI_MODIFICATIONS";
                Effects = $"{internalNameUpper}_WIKI_EFFECTS";
                Upgrades = $"{internalNameUpper}_WIKI_UPGRADES";
                Strategy = $"{internalNameUpper}_WIKI_STRATEGY";
                ScientificBackground = $"{internalNameUpper}_WIKI_SCIENTIFIC_BACKGROUND";
            }

            public OrganelleSections(
                string description,
                string requirements,
                string processes,
                string modifications,
                string effects,
                string upgrades,
                string strategy,
                string scientificBackground)
            {
                Description = description;
                Requirements = requirements;
                Processes = processes;
                Modifications = modifications;
                Effects = effects;
                Upgrades = upgrades;
                Strategy = strategy;
                ScientificBackground = scientificBackground;
            }

            [JsonInclude]
            public string Description { get; }

            [JsonInclude]
            public string Requirements { get; }

            [JsonInclude]
            public string Processes { get; }

            [JsonInclude]
            public string Modifications { get; }

            [JsonInclude]
            public string Effects { get; }

            [JsonInclude]
            public string Upgrades { get; }

            [JsonInclude]
            public string Strategy { get; }

            [JsonInclude]
            public string ScientificBackground { get; }
        }
    }
}