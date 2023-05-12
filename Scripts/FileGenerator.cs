namespace Scripts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

public class FileGenerator
{
    // TODO: share this constant with Thrive once a common module is created
    public const string OLD_RELEASES_INFO_FILE = "simulation_parameters/common/older_patch_notes.json";

    private const string GithubReleaseInfoPage = "https://api.github.com/repos/Revolutionary-Games/Thrive/releases";

    private static readonly DateTime OldReleaseCutoffTime = new(2023, 1, 1);

    private static readonly IReadOnlyCollection<Regex> PatchNotesStartRegexes = new[]
    {
        new Regex(@"# Patch Notes", RegexOptions.IgnoreCase),
        new Regex(@"# New Features", RegexOptions.IgnoreCase),
        new Regex(@"[\*#]+In this release:?[\*#\s]+", RegexOptions.IgnoreCase),
    };

    private static readonly Regex BulletPointRegex = new(@"^\s*[-\*]\s*(.+)$");

    private Program.GeneratorOptions options;

    public FileGenerator(Program.GeneratorOptions opts)
    {
        options = opts;
    }

    public async Task<int> Run(CancellationToken cancellationToken)
    {
        ColourConsole.WriteDebugLine($"Beginning generation with type: {options.Type}");

        switch (options.Type)
        {
            case FileTypeToGenerate.List:
                ColourConsole.WriteInfoLine("Available file types to generate:");

                foreach (var value in Enum.GetValues<FileTypeToGenerate>())
                {
                    // Skip the "help" option
                    if (value == FileTypeToGenerate.List)
                        continue;

                    ColourConsole.WriteNormalLine($" - {value.ToString()}");
                }

                return 0;
            case FileTypeToGenerate.OldReleaseNotes:
                if (!await DownloadAndWriteOldGithubReleases(cancellationToken))
                    return 1;

                break;
            default:
                ColourConsole.WriteErrorLine($"Unhandled file type to generate: {options.Type}");
                return 2;
        }

        ColourConsole.WriteSuccessLine($"Finished generating {options.Type}");
        return 0;
    }

    private async Task<bool> DownloadAndWriteOldGithubReleases(CancellationToken cancellationToken)
    {
        ColourConsole.WriteInfoLine("Downloading release info from Github...");

        using var client = SetupGithubClient();

        var url = $"{GithubReleaseInfoPage}?per_page=100";

        ColourConsole.WriteNormalLine($"Retrieving {url}");

        /*var response = await client.GetAsync(url, cancellationToken);

        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        ColourConsole.WriteInfoLine($"Got response: {text}");
        response.EnsureSuccessStatusCode();*/

        var releases = await client.GetFromJsonAsync<List<BasicGithubReleaseInfo>>(url, cancellationToken) ??
            throw new NullDecodedJsonException();

        // We intentionally don't handle having more than 100 releases here as this is just meant to grab the old
        // releases, and once this is ran once, this script is not really needed again

        // Filter out releases that don't belong in the old patch notes
        releases = releases.Where(r => r.PublishedAt < OldReleaseCutoffTime).Reverse().ToList();

        ColourConsole.WriteNormalLine("Processing data...");

        var data = releases.ToDictionary(r => TagNameToReleaseName(r.TagName), ProcessGithubRelease);

        ColourConsole.WriteNormalLine($"Writing {OLD_RELEASES_INFO_FILE}");

        await JsonWriteHelper.WriteJsonWithBom(OLD_RELEASES_INFO_FILE, data, cancellationToken, false);

        return true;
    }

    private string TagNameToReleaseName(string tag)
    {
        if (tag.StartsWith("v"))
            tag = tag.Substring(1);

        // Fix various wrong data
        if (!tag.Contains('-') && tag.EndsWith("rc2"))
            tag = tag.Substring(0, tag.Length - "rc2".Length) + "-rc2";

        // Ensure we ended up with a good name by parsing it
        try
        {
            Version.Parse(tag.Split("-", 2).First());

            if (tag.Count(c => c == '-') > 1)
                throw new Exception("Too many '-' characters in version number");
        }
        catch (Exception)
        {
            ColourConsole.WriteErrorLine($"Unknown format for version number: {tag}");
            throw;
        }

        return tag;
    }

    private VersionPatchNotes ProcessGithubRelease(BasicGithubReleaseInfo releaseInfo)
    {
        var stringBuilder = new StringBuilder();

        bool inPatchNotes = false;
        bool inFirstParagraph = false;

        bool seenFirstParagraph = false;

        string? firstParagraph = null;
        List<string> bulletPoints = new();

        foreach (var line in releaseInfo.Body.ReplaceLineEndings().Split('\n'))
        {
            if (!seenFirstParagraph)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                seenFirstParagraph = true;
                inFirstParagraph = true;
            }

            if (inFirstParagraph)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    // First paragraph ended
                    inFirstParagraph = false;
                    firstParagraph = stringBuilder.ToString();
                    stringBuilder.Clear();

                    continue;
                }

                stringBuilder.Append(line);

                // Markdown line endings count as spaces
                stringBuilder.Append(' ');
                continue;
            }

            if (inPatchNotes)
            {
                if ((string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) && bulletPoints.Count > 0)
                {
                    // Patch notes probably ended
                    inPatchNotes = false;
                    bulletPoints.Add(stringBuilder.ToString());
                    stringBuilder.Clear();

                    continue;
                }

                if (string.IsNullOrEmpty(line))
                    continue;

                var match = BulletPointRegex.Match(line);

                if (match.Success)
                {
                    if (stringBuilder.Length > 0)
                    {
                        // End the previous bullet point
                        bulletPoints.Add(stringBuilder.ToString());
                        stringBuilder.Clear();
                    }

                    // A bullet point has started
                    stringBuilder.Append(match.Groups[1].Value);
                }
                else
                {
                    stringBuilder.Append(line);
                }

                stringBuilder.Append(' ');
            }

            if (PatchNotesStartRegexes.Any(r => r.IsMatch(line)))
            {
                inPatchNotes = true;
            }
        }

        if (stringBuilder.Length > 0 && bulletPoints.Count > 0)
            bulletPoints.Add(stringBuilder.ToString());

        if (firstParagraph == null)
            throw new Exception("First paragraph not detected");

        if (bulletPoints.Count < 1)
            throw new Exception("No bullet points detected");

        // Trim excess whitespace around the bullet points
        bulletPoints = bulletPoints.Select(b => b.Trim()).ToList();

        return new VersionPatchNotes(firstParagraph.Trim(), bulletPoints, releaseInfo.Link);
    }

    private HttpClient SetupGithubClient()
    {
        var client = new HttpClient();
        SetUserAgent(client);

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        return client;
    }

    private void SetUserAgent(HttpClient client)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version ??
            Assembly.GetExecutingAssembly().GetName().Version;

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ThriveScripts",
            version?.ToString() ?? "unknown"));
    }

    /// <summary>
    ///   Just a few of the release fields we need for our use
    /// </summary>
    private class BasicGithubReleaseInfo
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string Link { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}
