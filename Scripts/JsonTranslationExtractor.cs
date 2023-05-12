namespace Scripts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptsBase.Translation;

/// <summary>
///   Extract JSON file contents for translation. Matches key names to a list of known keys and when matching, grabs
///   the child content as the translation key.
/// </summary>
public class JsonTranslationExtractor : TranslationExtractorBase
{
    private readonly IReadOnlyCollection<string> keysToLookFor;

    public JsonTranslationExtractor(IReadOnlyCollection<string> keysToLookFor) : base(".json")
    {
        this.keysToLookFor = keysToLookFor;
    }

    public override async IAsyncEnumerable<ExtractedTranslation> Handle(string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = File.OpenText(path);
        await using var jsonReader = new JsonTextReader(reader);

        var data = await JToken.LoadAsync(jsonReader, new JsonLoadSettings
        {
            LineInfoHandling = LineInfoHandling.Load,
        }, cancellationToken);

        var result = new List<ExtractedTranslation>();

        WalkJsonTree(data, path, result);

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var translation in result)
        {
            yield return translation;
        }
    }

    private void WalkJsonTree(JToken value, string path, List<ExtractedTranslation> result)
    {
        if (value is JObject jsonObject)
        {
            foreach (var property in jsonObject)
            {
                if (property.Value == null)
                    continue;

                // Check if any object keys match what we want to extract
                if (keysToLookFor.Any(k => string.Compare(k, property.Key, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    if (property.Value.Type == JTokenType.String)
                    {
                        // For some reason we need an explicit interface cast here
                        IJsonLineInfo lineInfo = property.Value;

                        if (!lineInfo.HasLineInfo())
                            throw new Exception("Could not get line number");

                        result.Add(new ExtractedTranslation(
                            property.Value.Value<string>() ?? throw new Exception("Failed to convert string node"),
                            path, lineInfo.LineNumber));
                    }
                }

                WalkJsonTree(property.Value, path, result);
            }
        }
        else if (value is JArray jsonArray)
        {
            foreach (var element in jsonArray)
            {
                WalkJsonTree(element, path, result);
            }
        }
    }
}
