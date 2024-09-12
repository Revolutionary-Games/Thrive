namespace Scripts;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.PO;
using ScriptsBase.Checks;
using ScriptsBase.Utilities;
using ThriveScriptsShared;

public static class TranslationProgressTool
{
    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        var parser = LocalizationCheckBase.CreateParser();

        var progressValues = new Dictionary<string, float>();

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

            progressValues[name] = (float)progress;
        }

        // Sort for consistent order in file
        var objectToWrite =
            new TranslationsInfo(progressValues.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));

        await JsonWriteHelper.WriteJsonWithBom(ThriveScriptConstants.TRANSLATIONS_PROGRESS_FILE, objectToWrite,
            cancellationToken);

        ColourConsole.WriteSuccessLine(
            $"Updated translations progress at {ThriveScriptConstants.TRANSLATIONS_PROGRESS_FILE}");

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
}
