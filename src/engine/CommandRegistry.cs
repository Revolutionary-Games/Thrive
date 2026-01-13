using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Singleton managing Commands
/// </summary>
[GodotAutoload]
public partial class CommandRegistry : Node
{
    private static CommandRegistry? instance;

    private FrozenDictionary<string, Command[]>? commands;
    private Task? registerCommandsTask;

    private CommandRegistry()
    {
        ExecuteRegisterCommandsTask();

        instance = this;
    }

    public static CommandRegistry? GetInstance()
    {
        return instance;
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
    /// <param name="invoker">The invoker console. Can be null.</param>
    /// <param name="cmd">The input command string, including the parameters</param>
    /// <returns><code>true</code> if and only if the command execution succeeds.</returns>
    public bool Execute(DebugConsole? invoker, string cmd)
    {
        if (registerCommandsTask != null && !registerCommandsTask!.IsCompleted)
        {
            GD.PrintErr("CommandRegistry: Still loading. Hold your horses.");
            return false;
        }

        var span = cmd.AsSpan();
        var tokenizer = new SpanTokenizer(span);

        if (!tokenizer.MoveNext(out var cmdNameSpan, out _))
            return false;

        string cmdName = cmdNameSpan.ToString().ToLower();

        if (!commands!.TryGetValue(cmdName, out var candidates))
        {
            GD.PrintErr($"Unknown command: {cmdName}");
            return false;
        }

        var rawArgs = new List<(string Value, bool IsQuoted)>();

        while (tokenizer.MoveNext(out var token, out var isQuoted))
        {
            rawArgs.Add((token.ToString(), isQuoted));
        }

        bool failed = true;
        if (candidates.Any(command => TryExecuteCandidate(command, invoker, rawArgs, out failed)))
        {
            return !failed;
        }

        GD.PrintErr($"Command '{cmdName}': No overload matched arguments. Found {candidates.Length} candidates.");
        return false;
    }

    private static object ParseSpanToType(ReadOnlySpan<char> token, Type type, bool isQuoted)
    {
        if (type == typeof(string) || isQuoted)
        {
            return Unescape(token);
        }

        if (type == typeof(int))
            return int.Parse(token);
        if (type == typeof(float))
            return float.Parse(token);
        if (type == typeof(double))
            return double.Parse(token);

        if (type == typeof(bool))
        {
            return token is "1" || token.Equals("true", StringComparison.OrdinalIgnoreCase) || token.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        return Convert.ChangeType(token.ToString(), type);
    }

    private static string Unescape(ReadOnlySpan<char> source)
    {
        int backslashIndex = source.IndexOf('\\');
        if (backslashIndex == -1)
        {
            return source.ToString();
        }

        Span<char> buffer = stackalloc char[source.Length];
        int writeIndex = 0;

        for (int i = 0; i < source.Length; i++)
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

                i++;
            }
            else
            {
                buffer[writeIndex++] = c;
            }
        }

        return new string(buffer[..writeIndex]);
    }

    [Command("help", "Shows hints and infos about all the registered commands.")]
    private static void Help()
    {
        var cmds = instance?.commands!;
        int count = cmds.Values.Sum(v => v.Length);

        GD.Print($"Total registered Commands: {count}");

        foreach (var group in cmds)
        {
            foreach (var cmd in group.Value)
            {
                var paramsInfo = string.Join(", ", cmd.MethodInfo.GetParameters().Select(p => p.ParameterType.Name));
                GD.Print($"{cmd.CommandName}({paramsInfo}): {cmd.HelpText}");
            }
        }
    }

    /// <summary>
    ///   Tries to execute a possibly overloaded command candidate.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    /// <param name="invoker">The invoker console, which is injected if required.</param>
    /// <param name="rawArgs">The raw command arguments.</param>
    /// <param name="failed">false iff the command execution succeeds without any exception.</param>
    /// <returns>
    ///   true iff the command candidate has the correct arguments for execution. This value is not
    ///   affected by a command execution failure due to reasons different from argument mismatch, e.g. a command
    ///   execution failure.
    /// </returns>
    private bool TryExecuteCandidate(Command command, DebugConsole? invoker, List<(string Value, bool IsQuoted)> rawArgs, out bool failed)
    {
        failed = true;

        var method = command.MethodInfo;
        var parameters = method.GetParameters();

        bool requiresInvoker = parameters.Length > 0 && parameters[0].ParameterType == typeof(DebugConsole);
        int paramOffset = requiresInvoker ? 1 : 0;
        int expectedArgs = parameters.Length - paramOffset;

        if (rawArgs.Count != expectedArgs)
        {
            // Argument count mismatch. This candidate is bad.
            return false;
        }

        object?[] invokeArgs = new object[parameters.Length];

        if (requiresInvoker)
            invokeArgs[0] = invoker;

        try
        {
            for (int i = 0; i < expectedArgs; i++)
            {
                var targetType = parameters[i + paramOffset].ParameterType;
                var (val, isQuoted) = rawArgs[i];

                invokeArgs[i + paramOffset] = ParseSpanToType(val.AsSpan(), targetType, isQuoted);
            }
        }
        catch (Exception)
        {
            // This exception happens if the parameter conversion fails. We gracefully return false hoping to find a
            // better candidate for command execution.
            return false;
        }

        try
        {
            method.Invoke(null, invokeArgs);
        }
        catch (Exception e)
        {
            // A serious exception happened during command method invocation, we need to log this.
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
        registerCommandsTask = Task.Run(RegisterCommands);
    }

    /// <summary>
    ///   Looks for methods that have the CommandAttribute by looking into the Assemblies, and registers them.
    /// </summary>
    private void RegisterCommands()
    {
        var allMethods = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name!.StartsWith("Thrive"))
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            .Select(m => (Method: m, Attr: m.GetCustomAttribute<CommandAttribute>()))
            .Where(x => x.Attr != null);

        var tempDict = new Dictionary<string, List<Command>>();

        foreach (var (method, attr) in allMethods)
        {
            if (!method.IsStatic)
            {
                GD.PushWarning($"CommandRegistry: Ignored '{attr!.CommandName}'. Method must be static.");
                continue;
            }

            var name = attr!.CommandName.ToLower();
            var cmd = new Command(method, name, attr.HelpText);

            if (!tempDict.TryGetValue(name, out var list))
            {
                list = [];
                tempDict[name] = list;
            }

            list.Add(cmd);
        }

        commands = tempDict.ToFrozenDictionary(
            k => k.Key,
            v => v.Value.ToArray());

        GD.Print($"CommandRegistry: Loaded. Command groups: {commands.Count}.");
    }

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

    public record struct Command(MethodInfo MethodInfo, string CommandName, string HelpText);
}
