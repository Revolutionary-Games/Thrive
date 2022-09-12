﻿namespace Scripts;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using DevCenterCommunication.Models;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

public static class CreditsUpdater
{
    public const string CREDITS_FILE = "simulation_parameters/common/credits.json";

    private const string DEVELOPERS_PAGE = "https://wiki.revolutionarygamesstudio.com/wiki/Team_Members";
    private const string DONATIONS_PAGE = "https://wiki.revolutionarygamesstudio.com/wiki/Donations";

    private const string PATRONS_FILE = "Scripts/patrons.json";
    private const string PATRONS_DOWNLOAD = "https://dev.revolutionarygamesstudio.com/admin/patreon";

    private const string TRANSLATORS_FILE = "Scripts/translators.json";

    private const string TRANSLATORS_DOWNLOAD =
        "https://translate.revolutionarygamesstudio.com/projects/thrive/thrive-game/#reports";

    private const string TRANSLATORS_EXTRA_INSTRUCTIONS =
        "set start date to 1.1.2015 and end date to current date and format to JSON";

    private const string AUTOMATIC_GENERATION_COMMENT =
        "This file is automatically generated by the 'credits' tool in the Scripts folder!" +
        "Part of the data is fetched from the Thrive developer wiki";

    private static readonly TimeSpan DonationDisplayCutoff = TimeSpan.FromDays(365 * 5);

    private static readonly TimeSpan FileAgeThreshold = TimeSpan.FromDays(1);

    private static readonly HashSet<string> IgnoredWeblateUsers = new()
    {
        "noreply+90@weblate.org",
        "Deleted User",
        "Weblate Admin",
    };

    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        // TODO: enable these again
        if (!CheckFile(PATRONS_FILE, PATRONS_DOWNLOAD))
            return false;

        await using var patronsReader = File.OpenRead(PATRONS_FILE);
        var patrons =
            await JsonSerializer.DeserializeAsync<PatreonCredits>(patronsReader,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancellationToken) ?? throw new NullDecodedJsonException();

        if (!CheckFile(TRANSLATORS_FILE, TRANSLATORS_DOWNLOAD, TRANSLATORS_EXTRA_INSTRUCTIONS))
            return false;

        await using var translatorsReader = File.OpenRead(TRANSLATORS_FILE);

        // The JSON is structured like the following so we need to deserialize as raw JSON:
        /* [
              {
                "Arabic": [
                  [
                    "email1@example.com",
                    "name",
                    10
                  ]
                ]
              },
              {
                "Bulgarian": [
                  [
                    "email2@example.com",
                    "name 2",
                    1
                  ],
                  [
                    "email3@example.com",
                    "Name LastName",
                    19
                  ]
                ]
              },
              ...
         */
        var parsedTranslators = JsonNode.Parse(translatorsReader);
        var rawTranslators = parsedTranslators as JsonArray ??
            throw new Exception("failed to JSON parse translators");

        var translators = ProcessTranslators(rawTranslators);

        var developersTask = FetchWikiDevelopers(cancellationToken);
        var donationsTask = FetchWikiDonations(cancellationToken);

        var developers = await developersTask;
        var donations = PruneOldDonations(await donationsTask);

        var credits = new Credits(developers, donations, translators, patrons);

        await JsonWriteHelper.WriteJsonWithBom(CREDITS_FILE, credits, cancellationToken);

        ColourConsole.WriteSuccessLine($"Updated credits at {CREDITS_FILE}");

        return true;
    }

    private static bool CheckFile(string path, string downloadUrl, string? extraInfo = null)
    {
        if (File.Exists(path))
        {
            if (DateTime.UtcNow - new FileInfo(path).LastWriteTime.ToUniversalTime() > FileAgeThreshold)
            {
                ColourConsole.WriteWarningLine("The downloaded file is too old. Please get a newer version.");
            }
            else
            {
                return true;
            }
        }

        ColourConsole.WriteErrorLine(
            $"A required file for credits generation is missing: {path}, please download from:");
        ColourConsole.WriteNormalLine(downloadUrl);

        if (extraInfo != null)
            ColourConsole.WriteNormalLine(extraInfo);

        return false;
    }

    /// <summary>
    ///   Processes translators to contributors and their total words changed
    /// </summary>
    /// <param name="translators">The translations data to process</param>
    /// <returns>
    ///   Translators along with their contribution amounts (word count, or maybe it's the changed translations count)
    /// </returns>
    private static List<string> ProcessTranslators(JsonArray translators)
    {
        var people = new Dictionary<string, long>();

        foreach (var item in translators)
        {
            foreach (var (_, languageData) in (JsonObject?)item ?? throw new Exception("Bad json structure"))
            {
                foreach (var personEntry in (JsonArray?)languageData ?? throw new Exception("Missing language array"))
                {
                    var person = (JsonArray?)personEntry ?? throw new Exception("Missing nested arrays");

                    var email = person[0]?.GetValue<string>() ?? throw new Exception("missing email");
                    var name = person[1]?.GetValue<string>() ?? throw new Exception("missing name");
                    var score = person[2]?.GetValue<long>() ?? throw new Exception("missing score");

                    // Ignore some users to make the credits nicer
                    if (IgnoredWeblateUsers.Contains(name) || IgnoredWeblateUsers.Contains(email))
                        continue;

                    people.TryGetValue(name, out var existingScore);

                    people[name] = existingScore + score;
                }
            }
        }

        // Sort by the total words / translations people have done
        // ThenBy ensures here consistent ordering when there are multiple people with the same amount of translation
        // work done
        return people.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Select(p => p.Key).ToList();
    }

    private static async Task<Credits.GameDevelopers> FetchWikiDevelopers(CancellationToken cancellationToken)
    {
        var document = await RetrieveHtmlDocument(DEVELOPERS_PAGE, cancellationToken);

        var result = new Credits.GameDevelopers();

        Dictionary<string, List<Credits.DeveloperPerson>>? activeSection = null;
        string? team = null;

        foreach (var element in document.Body!.QuerySelectorAll(".mw-parser-output > *"))
        {
            if (element is IHtmlHeadingElement)
            {
                if (element.TagName == "H2")
                {
                    switch (element.TextContent)
                    {
                        case "Current Team":
                            activeSection = result.Current;
                            break;
                        case "Past Developers":
                            activeSection = result.Past;
                            break;
                        case "Outside Contributors":
                            activeSection = result.Outside;
                            break;
                    }

                    team = null;
                }
                else if (element.TagName == "H3")
                {
                    team = element.TextContent;
                    continue;
                }
            }

            if (team == null || activeSection == null)
                continue;

            if (element is not IHtmlUnorderedListElement unorderedListElement)
                continue;

            foreach (var listElement in unorderedListElement.QuerySelectorAll("li"))
            {
                bool lead = listElement.QuerySelector("b") != null;
                var name = listElement.TextContent.Trim();

                if (!activeSection.TryGetValue(team, out var teamMembers))
                {
                    teamMembers = new List<Credits.DeveloperPerson>();
                    activeSection[team] = teamMembers;
                }

                teamMembers.Add(new Credits.DeveloperPerson(name, lead));
            }
        }

        return result;
    }

    /// <summary>
    ///   Fetches the donations from the wiki page
    /// </summary>
    /// <returns>A map of year -> (month -> [list of people])</returns>
    private static async Task<Dictionary<int, Dictionary<string, List<string>>>> FetchWikiDonations(
        CancellationToken cancellationToken)
    {
        var document = await RetrieveHtmlDocument(DONATIONS_PAGE, cancellationToken);

        var result = new Dictionary<int, Dictionary<string, List<string>>>();

        bool inDonators = false;

        int? year = null;
        string? month = null;

        foreach (var element in document.Body!.QuerySelectorAll(".mw-parser-output > *"))
        {
            if (element is IHtmlHeadingElement)
            {
                if (element.TagName == "H2")
                {
                    inDonators = element.TextContent == "Donators";
                }
            }

            if (!inDonators)
                continue;

            if (element.TagName == "H3")
            {
                year = int.Parse(element.TextContent);
                month = null;
                continue;
            }

            if (element.TagName == "H4")
            {
                month = element.TextContent;
                continue;
            }

            if (year == null || month == null)
                continue;

            if (element is not IHtmlUnorderedListElement unorderedListElement)
                continue;

            foreach (var listElement in unorderedListElement.QuerySelectorAll("li"))
            {
                var name = listElement.TextContent.Trim();

                if (!result.TryGetValue(year.Value, out var yearData))
                {
                    yearData = new Dictionary<string, List<string>>();
                    result[year.Value] = yearData;
                }

                if (!yearData.TryGetValue(month, out var monthData))
                {
                    monthData = new List<string>();
                    yearData[month] = monthData;
                }

                monthData.Add(name);
            }
        }

        return result;
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

    /// <summary>
    ///   Removes the old donations that shouldn't be listed in the credits anymore. Also converts years to strings
    /// </summary>
    /// <param name="donations">
    ///   The donations to filter. Donations is a dictionary of year and under that the months with then
    ///   individual names listed there.
    /// </param>
    /// <param name="cutoff">The cutoff time, or null for default</param>
    /// <returns>Filtered list of donations</returns>
    private static Dictionary<string, Dictionary<string, List<string>>> PruneOldDonations(
        Dictionary<int, Dictionary<string, List<string>>> donations, TimeSpan? cutoff = null)
    {
        cutoff ??= DonationDisplayCutoff;

        // We add 31 days to the cutoff to make the assumption below about each donation happening at the start of a
        // month to not be as impactful
        cutoff = cutoff.Value + TimeSpan.FromDays(31);

        var now = DateTime.UtcNow;

        var result = new Dictionary<string, Dictionary<string, List<string>>>();

        foreach (var (year, yearData) in donations.OrderByDescending(p => p.Key))
        {
            var yearString = year.ToString(CultureInfo.InvariantCulture);

            foreach (var (monthName, monthData) in yearData)
            {
                // Assume time to be at the start of the month to be easier to handle
                var time = new DateTime(year,
                    DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month, 1, 0, 0, 0,
                    DateTimeKind.Utc);

                if (now - time > cutoff)
                    continue;

                if (!result.TryGetValue(yearString, out var resultYear))
                {
                    resultYear = new Dictionary<string, List<string>>();
                    result[yearString] = resultYear;
                }

                // We can copy entire months at once as no one will modify our return value, and we don't know when
                // in a month a donation happened
                resultYear[monthName] = monthData;
            }
        }

        return result;
    }

    /// <summary>
    ///   Game credits on our side. Must match the game's GameCredits class. It's currently not shared as there is no
    ///   common module for the scripts and the game code.
    /// </summary>
    private class Credits
    {
        public Credits(GameDevelopers developers, Dictionary<string, Dictionary<string, List<string>>> donations,
            List<string> translators, PatreonCredits patrons)
        {
            Developers = developers;
            Donations = donations;
            Translators = translators;
            Patrons = patrons;
        }

        public string Comment { get; init; } = AUTOMATIC_GENERATION_COMMENT;

        [JsonInclude]
        public GameDevelopers Developers { get; }

        [JsonInclude]
        public Dictionary<string, Dictionary<string, List<string>>> Donations { get; }

        [JsonInclude]
        public List<string> Translators { get; }

        [JsonInclude]
        public PatreonCredits Patrons { get; }

        public class GameDevelopers
        {
            public Dictionary<string, List<DeveloperPerson>> Current { get; } = new();
            public Dictionary<string, List<DeveloperPerson>> Past { get; } = new();

            public Dictionary<string, List<DeveloperPerson>> Outside { get; } = new();
        }

        public class DeveloperPerson
        {
            public DeveloperPerson(string person, bool lead = false)
            {
                Person = person;
                Lead = lead;
            }

            [JsonInclude]
            public string Person { get; }

            [JsonInclude]
            public bool Lead { get; }
        }
    }
}
