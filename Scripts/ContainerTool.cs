namespace Scripts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.ToolBases;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

public class ContainerTool : ContainerToolBase<Program.ContainerOptions>
{
    public const string CrossCompilerClangName = "x86_64-w64-mingw32-clang";
    public const string CrossCompilerClangName32Bit = "i686-w64-mingw32-clang";

    private readonly Regex clangVersionRegex = new(@"clang version ([\d\.]+\s*\(.+\))$\s*target:\s*([\w-]+)$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    /// <summary>
    ///   Tries to match references to gcc but not matching packages needed to be installed for wine
    /// </summary>
    private readonly Regex gccReferenceButNoWineFalsePositive = new(
        @"[^(mingw64)-b]+-?(lib)?gcc(?![\d-\.]+el\d+\.alma)(?!\s+i686)",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private readonly string programPrintedText = "this is a really random string";

    // ReSharper disable StringLiteralTypo
    private readonly string simpleMainSourceCode = @"#include <iostream>
    int main(){
        std::cout << ""this is a really random string"" << std::endl;
        return 0;
    }
        ";

    // ReSharper restore StringLiteralTypo

    public ContainerTool(Program.ContainerOptions options) : base(options)
    {
        ColourConsole.WriteInfoLine($"Selected image type to build: {options.Image}");
    }

    protected override string ExportFileNameBase => options.Image switch
    {
        ImageType.CI => "godot-ci",
        ImageType.NativeBuilder => "native-builder",
        ImageType.NativeBuilderCross => "native-builder-cross",
        _ => throw new InvalidOperationException("Unknown image type"),
    };

    protected override string ImagesAndConfigsFolder => "podman";

    protected override (string BuildRelativeFolder, string? TargetToStopAt) DefaultImageToBuild => options.Image switch
    {
        ImageType.CI => ("ci", null),
        ImageType.NativeBuilder => ("native_builder", null),
        ImageType.NativeBuilderCross => ("native_builder", "llvm-cross"),
        _ => throw new InvalidOperationException("Unknown image type"),
    };

    protected override string ImageNameBase => $"thrive/{ExportFileNameBase}";

    protected override bool SaveByDefault =>
        options.Image is not (ImageType.NativeBuilder or ImageType.NativeBuilderCross);

    protected override async Task<ProcessRunHelpers.ProcessResult> RunImageBuild(ProcessStartInfo startInfo,
        bool capture, CancellationToken cancellationToken)
    {
        var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!capture)
        {
            return await ProcessRunHelpers.RunProcessWithOutputStreamingAsync(startInfo, cancelSource.Token, line =>
            {
                ColourConsole.WriteNormalLine(line);
                CheckCancellationFromOutput(line, cancelSource);
            }, line =>
            {
                // Use a normal line write here as a bunch of tools write to stderr things that aren't really errors
                ColourConsole.WriteNormalLine(line);
                CheckCancellationFromOutput(line, cancelSource);
            });
        }

        // Need to do a bit tricky output manipulation to keep the output for reading and pass it in the result
        var output = new StringBuilder();
        var errorOutput = new StringBuilder();

        var result = await ProcessRunHelpers.RunProcessWithOutputStreamingAsync(startInfo, cancelSource.Token,
            line =>
            {
                // We can output things in realtime here unlike what the base class does, but maybe for clarity this
                // shouldn't do that to avoid duplicate output
                // ColourConsole.WriteNormalLine(line);
                if (CheckCancellationFromOutput(line, cancelSource))
                {
                    ColourConsole.WriteErrorLine("The line that caused cancellation: " + line);
                }

                output.Append(line);
            }, line =>
            {
                if (CheckCancellationFromOutput(line, cancelSource))
                {
                    ColourConsole.WriteErrorLine("The (error) line that caused cancellation: " + line);
                }

                errorOutput.Append(line);
            });

        result.StdOut.Append(output);
        result.ErrorOut.Append(errorOutput);
        return result;
    }

    protected override Task<bool> PostCheckBuild(string tagOrId)
    {
        if (options.Image == ImageType.NativeBuilder)
        {
            return CheckClangProducesCleanExecutables(tagOrId);
        }

        if (options.Image == ImageType.NativeBuilderCross)
        {
            return CheckClangCanCrossCompileToWindows(tagOrId);
        }

        return CheckDotnetSdkWasInstalled(tagOrId);
    }

    protected override IEnumerable<string> ImagesToPullIfTheyAreOld()
    {
        if (options.Image is ImageType.NativeBuilder or ImageType.NativeBuilderCross)
        {
            // To update the image the relevant Dockerfile must also be updated
            // ReSharper disable once StringLiteralTypo
            yield return "almalinux:9";
        }

        // For now the CI uses the Fedora images so they can never be that old
    }

    /// <summary>
    ///   Detects if a podman build is going badly and cancels it
    /// </summary>
    /// <param name="line">The current output line</param>
    /// <param name="tokenSource">Token to cancel on error</param>
    /// <returns>True when cancellation just started</returns>
    private bool CheckCancellationFromOutput(string line, CancellationTokenSource tokenSource)
    {
        if (tokenSource.IsCancellationRequested)
            return false;

        if (line.Contains("Unable to link against LLVM libc"))
        {
            ColourConsole.WriteErrorLine("Canceling build as unable to link against LLVM libc detected " +
                "(this would likely result in a clang that doesn't use libc++ without dependency on gcc)");

            tokenSource.CancelAfter(TimeSpan.FromSeconds(0.3));
            return true;
        }

        return false;
    }

    private async Task<bool> CheckClangProducesCleanExecutables(string tagOrId)
    {
        var startInfo = new ProcessStartInfo("podman");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add(tagOrId);
        startInfo.ArgumentList.Add("bash");
        startInfo.ArgumentList.Add("-c");

        var command = new StringBuilder();

        command.Append("clang --version && ");
        command.Append("echo '");
        command.Append(simpleMainSourceCode);
        command.Append("' > /main.cpp && ");

        // Use lld, the libc++, and compiler_rt flags are mandatory for this to work
        command.Append("clang -fuse-ld=lld -stdlib=libc++ --rtlib=compiler-rt ");

        // Link statically to the standard libraries to produce an executable free of any references to gcc_s
        command.Append("/usr/lib64/x86_64-unknown-linux-gnu/libc++.a " +
            "/usr/lib64/x86_64-unknown-linux-gnu/libc++abi.a /usr/lib64/x86_64-unknown-linux-gnu/libunwind.a ");

        // Standard compile stuff and testing to see if the executable runs
        command.Append("-std=c++20 -v /main.cpp -o /out && /out && ");
        command.Append("ldd -v /out");

        ColourConsole.WriteDebugLine("In container check command:");
        ColourConsole.WriteDebugLine(command.ToString());

        startInfo.ArgumentList.Add(command.ToString());

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, CancellationToken.None, true);

        var fullOutput = result.FullOutput;
        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine(
                $"Failed to run podman command to determine if clang install succeeded:\n{fullOutput}");
            return false;
        }

        ColourConsole.WriteDebugLine("Container check output:");
        ColourConsole.WriteDebugLine(fullOutput);

        var match = clangVersionRegex.Match(fullOutput);

        if (!match.Success)
        {
            ColourConsole.WriteErrorLine(
                $"Could not determine installed clang version in image, output:\n{fullOutput}");
            return false;
        }

        // Detect bad stuff in the output (of ldd presumably)
        if (fullOutput.Contains("gcc"))
        {
            ColourConsole.WriteErrorLine(
                $"Compiled clang version includes references to gcc (libraries), it is not clean, " +
                $"output:\n{fullOutput}");
            return false;
        }

        // TODO: is there a way to verify the static libraries in /usr/lib64/x86_64-unknown-linux-gnu/ do not contain
        // any gcc symbols that might end up in the created executables / libraries?

        // Just in case the program compiled but could not run
        if (!fullOutput.Contains(programPrintedText))
        {
            ColourConsole.WriteErrorLine(
                $"Expected test program didn't print what it should have, full output:\n{fullOutput}");
            return false;
        }

        var installedVersion = $"{match.Groups[1].Value} ({match.Groups[2].Value})";

        ColourConsole.WriteInfoLine(
            $"Verified image has clang ({installedVersion}) that can compile executables without gcc lib pollution");
        return true;
    }

    private async Task<bool> CheckClangCanCrossCompileToWindows(string tagOrId, bool testExecute = true)
    {
        var startInfo = new ProcessStartInfo("podman");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add(tagOrId);
        startInfo.ArgumentList.Add("bash");
        startInfo.ArgumentList.Add("-c");

        var command = new StringBuilder();

        command.Append("echo '");
        command.Append(simpleMainSourceCode);
        command.Append("' > /main.cpp && ");

        // Target windows
        command.Append(CrossCompilerClangName);

        // command.Append(" -fuse-ld=lld ");

        // Link statically, and need to mention the standard libraries (unwind is automatically specified)
        command.Append(" -static -lc++ -lc++abi  ");

        command.Append("-std=c++20 -v /main.cpp -o /out.exe && ");

        // This is a simple tool to list windows dependencies: https://github.com/gsauthof/pe-util
        // -a parameter fails when not statically linking (and when trying to resolve the whitelist skipped libraries
        // with error:
        // No such file or directory [/usr/x86_64-w64-mingw32/sys-root/mingw/bin])
        // ReSharper disable StringLiteralTypo
        command.Append("echo 'peldd:' && peldd --no-wlist /out.exe && echo 'peldd ended' ");

        // ReSharper restore StringLiteralTypo

        if (testExecute)
        {
            // A bit silly to install wine just to test this, but the other option would be to not test this at all...
            // This is just about 250MB download, so it is relatively fine.

            // Default repos don't have wine so need to enable EPEL first
            command.Append(" && dnf install yum-utils -y && dnf config-manager --set-enabled crb && ");
            command.Append("dnf install epel-release -y && ");

            command.Append("dnf install wine -y && ");

            command.Append("wine /out.exe");

            ColourConsole.WriteInfoLine("Will install wine to test that generated executable works, " +
                "this might take a bit of time to download...");
        }

        ColourConsole.WriteDebugLine("In container check command (cross compile):");
        ColourConsole.WriteDebugLine(command.ToString());

        startInfo.ArgumentList.Add(command.ToString());

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, CancellationToken.None, true);

        ColourConsole.WriteNormalLine("Check commands in container finished running");

        var fullOutput = result.FullOutput;
        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine(
                $"Failed to run podman command to check cross compilation works:\n{fullOutput}");
            return false;
        }

        ColourConsole.WriteDebugLine("Cross compilation check output:");
        ColourConsole.WriteDebugLine(fullOutput);

        // Detect bad stuff in the output (of the ldd equivalent, presumably)
        // Using plain "gcc" fails as that's output by dnf when installing wine
        var match = gccReferenceButNoWineFalsePositive.Match(fullOutput);
        if (match.Success)
        {
            ColourConsole.WriteNormalLine($"Detected text: {match.Value} (start index: {match.Index})");

            ColourConsole.WriteErrorLine(
                $"Cross compiled executable has references to gcc (libraries), output:\n{fullOutput}");
            return false;
        }

        if (testExecute)
        {
            // Just in case the program compiled but could not run
            if (!fullOutput.Contains(programPrintedText))
            {
                ColourConsole.WriteErrorLine(
                    $"Expected test program didn't run with wine, full output:\n{fullOutput}");
                return false;
            }
        }
        else
        {
            ColourConsole.WriteWarningLine("Not testing if the resulting executable can run with wine");
        }

        ColourConsole.WriteInfoLine("Verified image has clang that can cross compile to windows");
        return true;
    }
}
