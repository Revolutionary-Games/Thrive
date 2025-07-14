namespace Scripts;

using System;
using System.IO;
using System.Text;
using ScriptsBase.Utilities;

/// <summary>
///   Helpers for the `dotnet run`
/// </summary>
public static class TestRunningHelpers
{
    public const string RUN_SETTINGS_FILE = ".runsettings";

    public const string TEST_RUN_VERBOSITY = "normal";

    private const string RUN_SETTINGS_TEMPLATE = """
                                                 <?xml version="1.0" encoding="utf-8"?>
                                                 <RunSettings>
                                                     <RunConfiguration>
                                                         <MaxCpuCount>{0}</MaxCpuCount>
                                                         <TestAdaptersPaths>.</TestAdaptersPaths>
                                                         <ResultsDirectory>./TestResults</ResultsDirectory>
                                                         <TestSessionTimeout>{1}</TestSessionTimeout>
                                                         <TreatNoTestsAsError>true</TreatNoTestsAsError>
                                                         <EnvironmentVariables>
                                                             <GODOT_BIN>{2}</GODOT_BIN>
                                                         </EnvironmentVariables>
                                                     </RunConfiguration>
                                                     <LoggerRunSettings>
                                                         <Loggers>
                                                             <!-- Seems to cause duplicate output -->
                                                             <Logger friendlyName="console" enabled="False">
                                                                 <Configuration>
                                                                     <Verbosity>{4}</Verbosity>
                                                                 </Configuration>
                                                             </Logger>
                                                             <Logger friendlyName="html" enabled="{3}">
                                                                 <Configuration>
                                                                     <LogFileName>test-result.html</LogFileName>
                                                                 </Configuration>
                                                             </Logger>
                                                             <Logger friendlyName="trx" enabled="{3}">
                                                                 <Configuration>
                                                                     <LogFileName>test-result.trx</LogFileName>
                                                                 </Configuration>
                                                             </Logger>
                                                         </Loggers>
                                                     </LoggerRunSettings>
                                                     <GdUnit4>
                                                         <Parameters>--headless</Parameters>
                                                         <DisplayName>FullyQualifiedName</DisplayName>
                                                         <CaptureStdOut>true</CaptureStdOut>
                                                         <CompileProcessTimeout>120000</CompileProcessTimeout>
                                                     </GdUnit4>
                                                 </RunSettings>
                                                 """;

    public static void EnsureStartsWithPragmaSuppression(string file)
    {
        var text = File.ReadAllText(file);

        var requiredText = text.Contains("\r\n") ? "#pragma warning disable\r\n\r\n" : "#pragma warning disable\n\n";

        if (!text.StartsWith(requiredText))
        {
            ColourConsole.WriteNormalLine($"Adding pragma suppression to file '{file}'");
            File.WriteAllText(file, requiredText + text);
        }
        else
        {
            ColourConsole.WriteDebugLine("File already has pragma suppression");
        }
    }

    public static void GenerateRunSettings(string godot, bool fileResults)
    {
        var timeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

        var text = string.Format(RUN_SETTINGS_TEMPLATE, 1, timeout, godot, fileResults, TEST_RUN_VERBOSITY);

        File.WriteAllText(RUN_SETTINGS_FILE, text, Encoding.UTF8);
    }
}
