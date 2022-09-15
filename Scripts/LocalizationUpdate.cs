namespace Scripts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Models;
using ScriptsBase.ToolBases;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

public class LocalizationUpdate : LocalizationUpdateBase<LocalizationOptionsBase>
{
    private const string BABEL_CONFIG_FILE = "babelrc";

    // List of locales, edit this to add new ones:
    private static readonly List<string> ThriveLocales = new()
    {
        "ar",
        "af",
        "bg",
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
        "tr",
        "uk",
        "vi",
        "lt",
        "lv",
        "zh_CN",
        "zh_TW",
    };

    /// <summary>
    ///   This variable holds what the translation system is looking for. Method names, JSON property names that need
    ///   to be translated have to be listed here.
    /// </summary>
    private static readonly IEnumerable<string> FunctionsAndPropertiesThatAreTranslated = new List<string>
    {
        "CancelText",
        "ChartName",
        "ConfirmText",
        "Description",
        "DialogText",
        "DisplayName",
        "ErrorMessage",
        "ExtendedBbcode",
        "LineEdit",
        "LocalizedString",
        "PauseButtonTooltip",
        "PlayButtonTooltip",
        "ProcessesDescription",
        "TranslationServer.Translate",
        "WindowTitle",
        "dialog_text",
        "hint_tooltip",
        "placeholder_text",
        "text",
        "window_title",
    };

    /// <summary>
    ///   List of folders relative to <see cref="LocaleFolder"/> location to extract translations in
    /// </summary>
    private static readonly IEnumerable<string> FoldersToExtractFrom = new List<string>
    {
        "../simulation_parameters",
        "../assets",
        "../src",
    };

    // This constructor is needed for checks to be able to
    public LocalizationUpdate(LocalizationOptionsBase opts) : base(opts)
    {
    }

    public LocalizationUpdate(Program.LocalizationOptions opts) : this((LocalizationOptionsBase)opts)
    {
    }

    protected override IReadOnlyList<string> Locales => ThriveLocales;
    protected override string LocaleFolder => "locale";

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

        AddLineWrapSettings(startInfo);

        startInfo.ArgumentList.Add(targetFile);
        startInfo.ArgumentList.Add(TranslationTemplateFile);

        return RunTranslationTool(startInfo, cancellationToken);
    }

    protected override ProcessStartInfo GetParametersToRunExtraction()
    {
        var pyBabel = ExecutableFinder.Which("pybabel");

        if (pyBabel == null)
        {
            ColourConsole.WriteErrorLine(
                "pybabel is missing. Please install it and retry (and make sure it is in PATH)");
            throw new Exception("pybabel not found");
        }

        var startInfo = new ProcessStartInfo(pyBabel)
        {
            WorkingDirectory = LocaleFolder,
        };
        startInfo.ArgumentList.Add("extract");
        startInfo.ArgumentList.Add("-F");
        startInfo.ArgumentList.Add(BABEL_CONFIG_FILE);

        foreach (var extractable in FunctionsAndPropertiesThatAreTranslated)
        {
            startInfo.ArgumentList.Add("-k");
            startInfo.ArgumentList.Add(extractable);
        }

        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(TranslationTemplateFileName);

        foreach (var folder in FoldersToExtractFrom)
        {
            startInfo.ArgumentList.Add(folder);
        }

        return startInfo;
    }

    protected override async Task<bool> PostProcessTranslations(CancellationToken cancellationToken)
    {
        await base.PostProcessTranslations(cancellationToken);

        // Remove trailing whitespace
        if (!options.Quiet)
            ColourConsole.WriteInfoLine("Removing trailing whitespace in .po files...");

        foreach (var locale in Locales)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var target = Path.Join(LocaleFolder, GetTranslationFileNameForLocale(locale));

            bool changed = false;

            // TODO: there doesn't seem to be a simple way to avoid keeping the entire file in memory
            var lines = await File.ReadAllLinesAsync(target, Encoding.UTF8, cancellationToken);

            var trimmed = lines.Select(l => l.TrimEnd()).ToList();

            if (!lines.SequenceEqual(trimmed))
            {
                changed = true;
            }

            // Babel generates identically formatted files. So when the first one doesn't have trailing spaces,
            // nor will the others.
            if (!changed)
                break;

            await File.WriteAllLinesAsync(target, trimmed, Encoding.UTF8, cancellationToken);
            ColourConsole.WriteWarningLine($"Removed trailing whitespace in {target}");
        }

        return true;
    }
}
