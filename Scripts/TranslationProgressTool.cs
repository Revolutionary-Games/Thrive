namespace Scripts;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.PO;
using ScriptsBase.Checks;
using ScriptsBase.Utilities;

public static class TranslationProgressTool
{
    // TODO: share this constant with Thrive once a common module is created
    public const string TRANSLATIONS_PROGRESS_FILE = "simulation_parameters/common/translations_info.json";

    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        var parser = LocalizationCheckBase.CreateParser();

        var progressValues = new Dictionary<string, double>();

        foreach (var file in Directory.EnumerateFiles("locale", "*.po", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);

            using var reader = File.OpenText(file);

            var parseResult = parser.Parse(reader);

            if (!parseResult.Success)
            {
                ColourConsole.WriteErrorLine($"Failed to parse po file: {file}");
                return false;
            }

            var progress = CalculateProgress(parseResult.Catalog);

            ColourConsole.WriteNormalLine($"Progress of {file}: {progress * 100}%");

            progressValues[name] = progress;
        }

        // Sort for consistent order in file
        var objectToWrite = new TranslationInfo(
            progressValues.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));

        await JsonWriteHelper.WriteJsonWithBom(TRANSLATIONS_PROGRESS_FILE, objectToWrite, cancellationToken);

        ColourConsole.WriteSuccessLine($"Updated translations progress at {TRANSLATIONS_PROGRESS_FILE}");

        return true;
    }

    public static double CalculateProgress(POCatalog catalog)
    {
        long translated = 0;
        long unTranslatedOrFuzzy = 0;

        foreach (var entry in catalog)
        {
            bool empty = true;

            foreach (var translation in entry)
            {
                if (!string.IsNullOrEmpty(translation))
                {
                    empty = false;
                    break;
                }
            }

            foreach (var poComment in entry.Comments)
            {
                if (poComment.Kind == POCommentKind.Flags)
                {
                }
            }

            if (empty)
            {
                ++unTranslatedOrFuzzy;
                continue;
            }

            bool fuzzy = false;

            foreach (var poComment in entry.Comments)
            {
                if (poComment is POFlagsComment flagsComment)
                {
                    if (flagsComment.Flags.Contains("fuzzy"))
                    {
                        fuzzy = true;
                        break;
                    }
                }
            }

            if (fuzzy)
            {
                ++unTranslatedOrFuzzy;
                continue;
            }

            ++translated;
        }

        var total = translated + unTranslatedOrFuzzy;

        if (total < 1)
            return 0;

        return (total - unTranslatedOrFuzzy) / (double)total;
    }

    private class TranslationInfo
    {
        public TranslationInfo(Dictionary<string, double> translationProgress)
        {
            TranslationProgress = translationProgress;
        }

        [JsonInclude]
        public Dictionary<string, double> TranslationProgress { get; private set; }
    }
}
