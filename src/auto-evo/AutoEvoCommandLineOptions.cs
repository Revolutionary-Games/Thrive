using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
///   Validates an Auto-Evo request before creating a world or starting work.
/// </summary>
public sealed class AutoEvoCommandLineOptions
{
    public int Generations { get; private set; } = 1;

    public long? Seed { get; private set; }

    public static AutoEvoCommandLineOptions Parse(IEnumerable<string> arguments)
    {
        var options = new AutoEvoCommandLineOptions();
        var seen = new HashSet<string>();

        foreach (var argument in arguments)
        {
            var parts = argument.Split('=', 2);
            if (!seen.Add(parts[0]))
                throw new ArgumentException($"Duplicate Auto-Evo option: {parts[0]}");

            if (argument == "--auto-evo")
                continue;

            if (parts.Length != 2)
                throw new ArgumentException($"Expected --generations=N or --seed=N, got: {argument}");

            switch (parts[0])
            {
                case "--generations":
                {
                    if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var generations) ||
                        generations < 1)
                    {
                        throw new ArgumentException("Generations must be a positive integer");
                    }

                    options.Generations = generations;
                    break;
                }

                case "--seed":
                {
                    if (!long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                            out var seed))
                    {
                        throw new ArgumentException("Seed must be a signed 64-bit integer");
                    }

                    options.Seed = seed;
                    break;
                }

                default:
                    throw new ArgumentException($"Unknown Auto-Evo option: {parts[0]}");
            }
        }

        return options;
    }
}
