namespace Scripts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;


public class ReleaseInfo
{
    private const string PATCH_NOTES_LOCATION = "PatchNotes.json";

    private const string BASE_URL = "https://api.github.com/repos/Revolutionary-Games/Thrive/releases/tags/v";
    private readonly Program.ReleaseOptions options;
    private List<string> versions = new List<string>(new string[] {"0.6.0",
                                "0.5.10",
                                "0.5.9",
                                "0.5.8.1",
                                "0.5.8",
                                "0.5.7",
                                "0.5.6.1",
                                "0.5.6",
                                "0.5.5",
                                "0.5.4",
                                "0.5.3.1",
                                "0.5.3",
                                "0.5.2",
                                "0.5.1.1",
                                "0.5.1",
                                "0.5.0",
                                "0.4.3.1",
                                "0.4.3",
                                "0.4.2",
                                "0.4.1.1",
                                "0.4.1",
                                "0.4.0.2",
                                "0.4.0.1",
                                "0.4.0",
                                "0.3.4",
                                "0.3.3",
                                "0.3.2",
                                "0.3.1",
                                "0.3.0",
                                "0.2.4",
                                "0.2.3" });

    public ReleaseInfo(Program.ReleaseOptions options)
    {
        this.options = options;
    }

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        var patchList = new List<BuildInfo>();

        var client = new HttpClient();

        // Set the user agent
        client.DefaultRequestHeaders.Add("User-Agent", "Get release script");

        foreach (string version in versions)
        {
            var url = new Uri(BASE_URL + version);

            // Get the response.
            HttpResponseMessage? response = await client.GetAsync(url);

            // Get the response content.
            HttpContent responseContent = response.Content;

            string? patchNotes = null;

            // Get the stream of the content.
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                // Save the output as a string
                patchNotes = await reader.ReadToEndAsync();
            }

            var json = JsonArray.Parse(patchNotes);
            if (json["body"] != null)
            {
                patchNotes = json["body"].ToString();

                // int start = patchNotes.IndexOf("## Patch Notes");

                // patchNotes = patchNotes.Substring(start);
            }
            var info = new BuildInfo(version, patchNotes);
            patchList.Add(info);
        }

        await JsonWriteHelper.WriteJsonWithBom(PATCH_NOTES_LOCATION, patchList, cancellationToken);

        return true;
    }

    private class BuildInfo
    {
        [JsonConstructor]
        public BuildInfo(string patchNumber, string patchNotes)
        {
            PatchNumber = patchNumber;
            PatchNotes = patchNotes;
        }

        [JsonInclude]
        public string PatchNumber { get; }
        public string PatchNotes { get; }
    }
}
