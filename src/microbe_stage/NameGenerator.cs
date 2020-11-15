using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class NameGenerator : IRegistryType
{
    public List<string> PrefixCofix;
    public List<string> PrefixesV;
    public List<string> PrefixesC;
    public List<string> CofixesV;
    public List<string> CofixesC;
    public List<string> SuffixesV;
    public List<string> SuffixesC;

    /// <summary>
    ///   List of all suffixes
    /// </summary>
    [JsonIgnore]
    public List<string> Suffixes;

    /// <summary>
    /// Unused
    /// </summary>
    /// <value>The name of the internal.</value>
    public string InternalName { get; set; }

    /// <summary>
    ///   Generates a single name section
    /// </summary>
    public string GenerateNameSection(Random random = null)
    {
        if (random == null)
            random = new Random();

        string newName;

        if (random.Next(0, 101) >= 10)
        {
            switch (random.Next(0, 4))
            {
                case 0:
                    newName = PrefixesC.Random(random) + SuffixesV.Random(random);
                    break;
                case 1:
                    newName = PrefixesV.Random(random) + SuffixesC.Random(random);
                    break;
                case 2:
                    newName = PrefixesV.Random(random) + CofixesC.Random(random) +
                        SuffixesV.Random(random);
                    break;
                case 3:
                    newName = PrefixesC.Random(random) + CofixesV.Random(random) +
                        SuffixesC.Random(random);
                    break;
                default:
                    throw new Exception("unreachable");
            }
        }
        else
        {
            // Developer Easter Eggs and really silly long names here
            // Our own version of wigglesoworthia for example
            switch (random.Next(0, 4))
            {
                case 0:
                case 1:
                    newName = PrefixCofix.Random(random) + Suffixes.Random(random);
                    break;
                case 2:
                    newName = PrefixesV.Random(random) + CofixesC.Random(random) +
                        Suffixes.Random(random);
                    break;
                case 3:
                    newName = PrefixesC.Random(random) + CofixesV.Random(random) +
                        Suffixes.Random(random);
                    break;
                default:
                    throw new Exception("unreachable");
            }
        }

        // TODO: DO more stuff here to improve names (remove double
        // letters when the prefix ends with and the cofix starts with
        // the same letter Remove weird things that come up like "rc"
        // (Implemented through vowels and consonants)
        return newName;
    }

    public void Check(string name)
    {
        if (PrefixCofix.Count < 1)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "PrefixCofix is empty");
        }

        if (PrefixesV.Count < 1)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "PrefixesV is empty");
        }

        if (PrefixesC.Count < 1)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "PrefixesC is empty");
        }

        if (CofixesV.Count < 1)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "CofixesV is empty");
        }

        if (CofixesC.Count < 1)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "CofixesC is empty");
        }

        if (SuffixesV.Count < 1)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "SuffixesV is empty");
        }

        if (SuffixesC.Count < 1)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "SuffixesC is empty");
        }
    }

    public void Resolve(SimulationParameters parameters)
    {
        _ = parameters;

        Suffixes = new List<string>();
        Suffixes.AddRange(SuffixesC);
        Suffixes.AddRange(SuffixesV);
    }

    public void ApplyTranslations()
    {
    }
}
