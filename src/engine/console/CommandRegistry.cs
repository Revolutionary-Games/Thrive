using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Command registry and processor.
/// </summary>
public class CommandRegistry : IDisposable
{
    private static CommandRegistry? instance;

    private FrozenDictionary<string, Command[]>? commands;

    private Task? registerCommandsTask;

    private CommandRegistry()
    {
        ExecuteRegisterCommandsTask();
    }

    public static CommandRegistry Instance => instance ?? throw new InstanceNotLoadedYetException();

    public static void Initialize()
    {
        if (instance != null)
        {
            GD.PrintErr("CommandRegistry: Already initialized.");
            return;
        }

        instance = new CommandRegistry();
    }

    public static void Shutdown()
    {
        if (instance == null)
        {
            GD.PrintErr("CommandRegistry: Not initialized.");
            return;
        }

        instance.Dispose();
        instance = null;
    }

    /// <summary>
    ///   Returns true iff the CommandRegistry has already registered all the commands.
    /// </summary>
    public bool HasLoaded()
    {
        return registerCommandsTask is { IsCompleted: true };
    }

    /// <summary>
    ///   Looks for the specified command, and if it exists, it tries to execute it.<br/>
    ///   If the command execution fails, this method indirectly returns the result in the standard console.<br/>
    ///   The command is run directly by the caller thread.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="cmd">The input command string, including the parameters</param>
    /// <returns><code>true</code> if and only if the command execution succeeds.</returns>
    public bool Execute(CommandContext context, string cmd)
    {
        if (registerCommandsTask != null && !registerCommandsTask!.IsCompleted)
        {
            context.PrintWarning("CommandRegistry: Still loading. Hold your horses.");
            return false;
        }

        var span = cmd.AsSpan();
        var tokenizer = new SpanTokenizer(span);

        if (!tokenizer.MoveNext(out var cmdNameSpan, out _))
            return false;

        var commandName = cmdNameSpan.ToString().ToLowerInvariant();

        if (!commands!.TryGetValue(commandName, out var candidates))
        {
            context.PrintErr($"Unknown command: {commandName}");
            return false;
        }

        var rawArgs = new List<(string Value, bool IsQuoted)>();

        while (tokenizer.MoveNext(out var token, out var isQuoted))
        {
            rawArgs.Add((token.ToString(), isQuoted));
        }

        foreach (var command in candidates)
        {
            if (TryExecuteCandidate(command, context, rawArgs, false, out bool failed))
            {
                return !failed;
            }
        }

        // As a last resort, we try to force string parsing wherever possible.
        // Relying on this should be discouraged when designing commands, but it's useful to cover edge-cases where
        // a value is not present in an enum, like for the load commands.
        foreach (var command in candidates)
        {
            if (TryExecuteCandidate(command, context, rawArgs, true, out bool failed))
            {
                return !failed;
            }
        }

        context.PrintErr(
            $"Command '{commandName}': No overload matched arguments. Found {candidates.Length} candidates.");
        return false;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            registerCommandsTask?.Dispose();
    }

    private static bool TryParseSpanToType(ReadOnlySpan<char> token, Type type, bool isQuoted, bool ignoreQuoted,
        out object? result)
    {
        result = null;

        if (type == typeof(string) && (isQuoted || ignoreQuoted))
        {
            result = Unescape(token);
            return true;
        }

        if (type == typeof(int))
        {
            if (int.TryParse(token, out int intVal))
            {
                result = intVal;
                return true;
            }

            return false;
        }

        if (type == typeof(float))
        {
            if (float.TryParse(token, out float floatVal))
            {
                result = floatVal;
                return true;
            }

            return false;
        }

        if (type == typeof(double))
        {
            if (double.TryParse(token, out double doubleVal))
            {
                result = doubleVal;
                return true;
            }

            return false;
        }

        if (type == typeof(bool))
        {
            if (token is "1" ||
                token.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            // Make sure the "false" value is allowed
            if (token is not "0" &&
                !token.Equals("false", StringComparison.OrdinalIgnoreCase) &&
                !token.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            result = false;
            return true;
        }

        if (type.IsEnum)
        {
            if (Enum.TryParse(type, token, true, out object? enumValue))
            {
                if (Enum.IsDefined(type, enumValue))
                {
                    result = enumValue;
                    return true;
                }
            }

            if (TryGetAlias(type, token, out enumValue))
            {
                if (enumValue != null)
                {
                    result = enumValue;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetAlias(Type type, ReadOnlySpan<char> token, out object? value)
    {
        foreach (var field in type.GetFields())
        {
            var attribute = field.GetCustomAttribute<AliasAttribute>(true);

            if (attribute == null)
                continue;

            foreach (var alias in attribute.Aliases)
            {
                if (alias.Equals(token, StringComparison.OrdinalIgnoreCase))
                {
                    value = field.GetValue(null);
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private static string Unescape(ReadOnlySpan<char> source)
    {
        int backslashIndex = source.IndexOf('\\');
        if (backslashIndex == -1)
        {
            return source.ToString();
        }

        Span<char> buffer = source.Length <= 256 ? stackalloc char[source.Length] : new char[source.Length];

        int writeIndex = 0;

        for (int i = 0; i < source.Length; ++i)
        {
            char c = source[i];

            if (c == '\\' && i + 1 < source.Length)
            {
                char next = source[i + 1];
                buffer[writeIndex++] = next switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => next,
                };

                ++i;
            }
            else
            {
                buffer[writeIndex++] = c;
            }
        }

        return new string(buffer[..writeIndex]);
    }

    [Command("help", false, "Shows hints and info about all the registered commands.")]
    private static void CommandHelp()
    {
        var commands = Instance.commands!;
        int count = commands.Values.Sum(v => v.Length);

        GD.Print($"Total registered Commands: {count}");

        foreach (var group in commands)
        {
            foreach (var command in group.Value)
            {
                var paramsInfo = string.Join(", ", command.MethodInfo
                    .GetParameters()
                    .Select(p => p.ParameterType.Name));
                GD.Print($"{command.CommandName}({paramsInfo}): {command.HelpText}");
            }
        }
    }

    /// <summary>
    ///   Tries to execute a possibly overloaded command candidate.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    /// <param name="context">The command context.</param>
    /// <param name="rawArgs">The raw command arguments.</param>
    /// <param name="ignoreQuoted">If set to true, then unquoted text is considered to be a match for strings.</param>
    /// <param name="failed">false iff the command execution succeeds without any exception.</param>
    /// <returns>
    ///   True iff the command candidate has the correct arguments for execution. This value is not
    ///   affected by a command execution failure due to reasons different from argument mismatch, e.g. a command
    ///   execution failure.
    /// </returns>
    private bool TryExecuteCandidate(Command command, CommandContext context,
        List<(string Value, bool IsQuoted)> rawArgs, bool ignoreQuoted, out bool failed)
    {
        failed = true;

        var method = command.MethodInfo;
        var parameters = method.GetParameters();

        bool requiresInvoker = parameters.Length > 0 && parameters[0].ParameterType == typeof(CommandContext);
        int paramOffset = requiresInvoker ? 1 : 0;
        int expectedArgs = parameters.Length - paramOffset;

        if (rawArgs.Count != expectedArgs)
        {
            // Argument count mismatch. This candidate is bad.
            return false;
        }

        object?[] invokeArgs = new object[parameters.Length];

        if (requiresInvoker)
            invokeArgs[0] = context;

        for (int i = 0; i < expectedArgs; ++i)
        {
            var targetType = parameters[i + paramOffset].ParameterType;
            var (value, isQuoted) = rawArgs[i];

            if (!TryParseSpanToType(value.AsSpan(), targetType, isQuoted, ignoreQuoted, out var parsedValue))
            {
                // This happens if the parameter conversion fails. We gracefully return false hoping to find a
                // better candidate for command execution.
                return false;
            }

            invokeArgs[i + paramOffset] = parsedValue;
        }

        if (command.IsCheat)
        {
            AchievementsManager.ReportCheatsUsed();
        }

        try
        {
            var result = method.Invoke(null, invokeArgs);

            if (result is bool success)
            {
                if (success)
                {
                    context.Print("Success", Colors.Green);
                }
                else
                {
                    context.Print("Failure", Colors.Red);
                }
            }
        }
        catch (Exception e)
        {
            // An exception happened during command method invocation, we need to log this.
            // We also return true here, because it was a good candidate parameter-wise.
            GD.PrintErr($"Command execution error: {e}");

            return true;
        }

        failed = false;

        return true;
    }

    /// <summary>
    ///   Lazily calls RegisterCommands. I found RegisterCommands to be a potential bottleneck in a future bigger
    ///   codebase, so I prefer deferring this loading process, which is non-critical, to a separate task, even though
    ///   right now it shouldn't take more than half a second.
    /// </summary>
    private void ExecuteRegisterCommandsTask()
    {
        registerCommandsTask = new Task(RegisterCommands);

        TaskExecutor.Instance.AddTask(registerCommandsTask);
    }

    /// <summary>
    ///   This method looks for methods that have the CommandAttribute by looking into the Assemblies
    ///   and registers them.
    /// </summary>
    private void RegisterCommands()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name!.StartsWith("Thrive"));

        var tempDict = new Dictionary<string, List<Command>>();

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsEnum)
                    continue;

                foreach (var method in type.GetMethods(flags))
                {
                    if (!method.IsDefined(typeof(CommandAttribute), true))
                        continue;

                    foreach (var attribute in method.GetCustomAttributes<CommandAttribute>(true))
                    {
                        var name = attribute.CommandName.ToLowerInvariant();

                        if (!method.IsStatic)
                        {
                            GD.PrintErr($"CommandRegistry: Ignored '{name}'. Method must be static.");
                            continue;
                        }

                        var isCheat = attribute.IsCheat;
                        var command = new Command(method, name, isCheat, attribute.HelpText);

                        if (!tempDict.TryGetValue(name, out var list))
                        {
                            list = [];
                            tempDict[name] = list;
                        }

                        list.Add(command);
                    }
                }
            }
        }

        commands = tempDict.ToFrozenDictionary(k => k.Key,
            v => v.Value.ToArray());

        GD.Print($"CommandRegistry: Loaded. Command groups: {commands.Count}.");
    }

    public record struct Command(MethodInfo MethodInfo, string CommandName, bool IsCheat, string HelpText);

    /// <summary>
    ///   A custom command parser.
    /// </summary>
    private ref struct SpanTokenizer(ReadOnlySpan<char> input)
    {
        private ReadOnlySpan<char> remaining = input.Trim();

        /// <summary>
        ///   Moves to the next token.
        /// </summary>
        /// <returns>false iff there are no more tokens.</returns>
        public bool MoveNext(out ReadOnlySpan<char> token, out bool isQuoted)
        {
            remaining = remaining.TrimStart();
            if (remaining.IsEmpty)
            {
                token = default;
                isQuoted = false;
                return false;
            }

            // handles strings parsing quotes
            if (remaining[0] == '"')
            {
                isQuoted = true;

                var end = remaining[1..].IndexOf('"');

                // unclosed quote, take everything
                if (end == -1)
                {
                    token = remaining[1..];
                    remaining = default;
                }
                else
                {
                    token = remaining.Slice(1, end);
                    remaining = remaining[(end + 2)..]; // skip quote + closing quote
                }
            }
            else
            {
                isQuoted = false;
                var space = remaining.IndexOf(' ');
                if (space == -1)
                {
                    token = remaining;
                    remaining = default;
                }
                else
                {
                    token = remaining[..space];
                    remaining = remaining[space..];
                }
            }

            return true;
        }
    }
}
