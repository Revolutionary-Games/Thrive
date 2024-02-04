namespace Scripts;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Models;
using ScriptsBase.ToolBases;
using ScriptsBase.Translation;
using ScriptsBase.Utilities;

public class LocalizationUpdate : LocalizationUpdateBase<LocalizationOptionsBase>
{
    // List of locales, edit this to add new ones:
    private static readonly List<string> ThriveLocales = new()
    {
        "ar",
        "af",
        "be",
        "bg",
        "bn",
        "ca",
        "cs",
        "da",
        "de",
        "el",
        "en",
        "eo",
        "es_AR",
        "es",
        "et",
        "fi",
        "fr",
        "frm",
        "he",
        "hr",
        "hu",
        "id",
        "ka",
        "ko",
        "la",
        "lb_LU",
        "it",
        "mk",
        "nb_NO",
        "nl",
        "nl_BE",
        "pl",
        "pt_BR",
        "pt_PT",
        "ro",
        "ru",
        "si_LK",
        "sk",
        "sr_Cyrl",
        "sr_Latn",
        "sv",
        "th_TH",
        "tok",
        "tr",
        "uk",
        "vi",
        "lt",
        "lv",
        "zh_CN",
        "zh_TW",
    };

    /// <summary>
    ///   This variable holds what the translation system is looking for in C# code files
    /// </summary>
    private static readonly IReadOnlyCollection<string> TranslatedFunctionNames = new List<string>
    {
        "Description",
        "LocalizedString",
        "TranslationServer.Translate",
    };

    /// <summary>
    ///   This lists properties in Godot scenes that are translated
    /// </summary>
    private static readonly IReadOnlyCollection<string> TranslatedSceneProperties = new List<string>
    {
        "CancelText",
        "ChartName",
        "ConfirmText",
        "Description",
        "DescriptionForController",
        "DialogText",
        "DisplayName",
        "ErrorMessage",
        "ExtendedBbcode",
        "LineEdit",
        "PauseButtonTooltip",
        "PlayButtonTooltip",
        "ProcessesDescription",
        "WindowTitle",
        "dialog_text",
        "hint_tooltip",
        "items",
        "placeholder_text",
        "text",
        "window_title",
    };

    /// <summary>
    ///   This has JSON object key's that contain translatable values
    /// </summary>
    private static readonly IReadOnlyCollection<string> TranslatedJSONKeys = new List<string>
    {
        "Description",
        "DisplayName",
        "GroupName",
        "Message",
        "Name",
        "SectionHeading",
        "SectionBody",
    };

    // This constructor is needed for checks to be able to run this
    public LocalizationUpdate(LocalizationOptionsBase opts) : base(opts)
    {
    }

    public LocalizationUpdate(Program.LocalizationOptions opts) : this((LocalizationOptionsBase)opts)
    {
    }

    /// <summary>
    ///   List of folders to extract translations in
    /// </summary>
    protected override IEnumerable<string> PathsToExtractFrom => new List<string>
    {
        "simulation_parameters",
        "assets",
        "src",
    };

    protected override IReadOnlyList<string> Locales => ThriveLocales;
    protected override string LocaleFolder => "locale";
    protected override bool AlphabeticallySortTranslationTemplate => true;

    protected override string ProjectName => "Thrive";
    protected override string ProjectOrganization => "Revolutionary Games Studio";

    protected override bool OmitReferenceLinesFromLocales => true;

    protected override Task<bool> RunTranslationCreate(string locale, string targetFile,
        CancellationToken cancellationToken)
    {
        var executable = FindTranslationTool("msginit");

        if (executable == null)
            return Task.FromResult(false);

        var startInfo = new ProcessStartInfo(executable);
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(locale);
        startInfo.ArgumentList.Add("--no-translator");

        AddLineWrapSettings(startInfo);

        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(targetFile);
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(TranslationTemplateFile);

        return RunTranslationTool(startInfo, cancellationToken);
    }

    protected override Task<bool> RunTranslationUpdate(string locale, string targetFile,
        CancellationToken cancellationToken)
    {
        var executable = FindTranslationTool("msgmerge");

        if (executable == null)
            return Task.FromResult(false);

        var startInfo = new ProcessStartInfo(executable);
        startInfo.ArgumentList.Add("--update");
        startInfo.ArgumentList.Add("--backup=none");

        if (OmitReferenceLinesFromLocales)
            startInfo.ArgumentList.Add("--no-location");

        AddLineWrapSettings(startInfo);

        startInfo.ArgumentList.Add(targetFile);
        startInfo.ArgumentList.Add(TranslationTemplateFile);

        return RunTranslationTool(startInfo, cancellationToken);
    }

    protected override List<TranslationExtractorBase> GetTranslationExtractors()
    {
        return new List<TranslationExtractorBase>
        {
            new CSharpTranslationExtractor(TranslatedFunctionNames),
            new JsonTranslationExtractor(TranslatedJSONKeys),
            new GodotSceneTranslationExtractor(TranslatedSceneProperties),
        };
    }

    protected override async Task<bool> PostProcessTranslations(CancellationToken cancellationToken)
    {
        await base.PostProcessTranslations(cancellationToken);

        // Remove trailing whitespace
        if (!options.Quiet)
            ColourConsole.WriteInfoLine("Removing trailing whitespace and line references if existing in .po files...");

        foreach (var locale in Locales)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var target = Path.Join(LocaleFolder, GetTranslationFileNameForLocale(locale));

            bool changed = false;

            // TODO: there doesn't seem to be a simple way to avoid keeping the entire file in memory
            var lines = await File.ReadAllLinesAsync(target, Encoding.UTF8, cancellationToken);

            var trimmed = lines.Select(l => l.TrimEnd()).Where(l => !l.StartsWith("#: .")).ToList();

            if (!lines.SequenceEqual(trimmed))
            {
                changed = true;
            }

            // It seems that sometimes the subsequent files only have problems so there is no longer a break here
            // Instead there's a continue here
            if (!changed)
                continue;

            await File.WriteAllLinesAsync(target, trimmed, new UTF8Encoding(false), cancellationToken);
            ColourConsole.WriteWarningLine($"Made changes to {target}");
        }

        return true;
    }
}
