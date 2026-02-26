using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
///   The microbe species name generator.
/// </summary>
public partial class NameGenerator
{
    private static readonly HashSet<char> Vocals = ['a', 'e', 'i', 'o', 'u'];
    private static readonly HashSet<char> Bilabials = ['p', 'b', 'm'];

    private static readonly FrozenDictionary<(char, char), char> VocalicTransitions = new Dictionary<(char, char), char>
    {
        { ('o', 'a'), 'o' },
        { ('e', 'a'), 'e' },
        { ('e', 'u'), 'u' },
    }.ToFrozenDictionary();

    private static StringBuilder? stringBuffer;

    public enum GrammaticalGender
    {
        Masculine,
        Feminine,
        Neuter,
        Ambiguous,
    }

    private static StringBuilder GetBuffer()
    {
        stringBuffer ??= new StringBuilder(64);

        stringBuffer.Length = 0;
        return stringBuffer;
    }

    private static T GetRandomElement<T>(Random random, T[] array)
    {
        return array[random.Next(array.Length)];
    }

    private static void PhonotacticsFriendlyAppend(StringBuilder stringBuilder, string data)
    {
        if (stringBuilder.Length == 0)
        {
            stringBuilder.Append(data);
            return;
        }

        var next = data[0];
        var previous = stringBuilder[^1];

        if (Vocals.Contains(next) && Vocals.Contains(previous))
        {
            // Both are vocals
            if (next == previous)
            {
                --stringBuilder.Length;
            }
            else if (VocalicTransitions.TryGetValue((previous, next), out var vocal))
            {
                --stringBuilder.Length;
                stringBuilder.Append(vocal);
                stringBuilder.Append(data, 1, data.Length - 1);

                return;
            }
        }
        else if (!Vocals.Contains(next) && !Vocals.Contains(previous))
        {
            switch (previous)
            {
                // Both are consonants
                case 'n' when Bilabials.Contains(next):
                    // n before bilabial becomes m
                    --stringBuilder.Length;
                    stringBuilder.Append('m');
                    break;
                case 'n' when next is 'r' or 'l':
                    // n before liquid gets assimilated
                    --stringBuilder.Length;
                    stringBuilder.Append(next);
                    break;
            }
        }

        stringBuilder.Append(data);
    }

    private static Dictionary<string, double> CalculateRelevantQualities(MicrobeSpecies species)
    {
        Dictionary<string, double> relevantQualities = new();

        // Speed
        var baseSpeed = species.BaseSpeed;
        var speedWeight = Math.Abs(Clamp((baseSpeed - 30) * 0.05, -1, 1));
        switch (baseSpeed)
        {
            case > 40:
                relevantQualities.Add("fast", speedWeight);
                break;
            case < 15:
                relevantQualities.Add("slow", speedWeight);
                break;
        }

        // Preferred and tolerated temperature
        var preferredTemperature = species.Tolerances.PreferredTemperature;
        var temperatureWeight = Math.Abs(Clamp((preferredTemperature - 50) * 0.02, -1, 1));
        switch (preferredTemperature)
        {
            case > 80:
                relevantQualities.Add("hot", temperatureWeight);
                break;
            case < 0:
                relevantQualities.Add("cold", temperatureWeight);
                break;
        }

        // Size
        var size = species.BaseHexSize;
        var logSize = Math.Log2(size);
        var sizeWeight = Math.Abs(Clamp(1.0 / 6.0 * logSize - 1 + 5.0 / 42.0 * logSize, -1, 1));
        switch (sizeWeight)
        {
            case > 50:
                relevantQualities.Add("big", sizeWeight);
                break;
            case <= 1:
                relevantQualities.Add("small", sizeWeight);
                break;
        }

        return relevantQualities;
    }

    private static string CalculateColourAdjective(MicrobeSpecies species)
    {
        var colour = species.Colour;
        var whiteness = (double)Math.Min(colour.R, Math.Min(colour.G, colour.B));
        var r = colour.R - whiteness;
        var g = colour.G - whiteness;
        var b = colour.B - whiteness;
        var yellowness = Math.Min(r, g);
        var redness = Math.Max(0, r - yellowness);
        var greenness = Math.Max(0, g - yellowness);

        var wantedAdjective = whiteness switch
        {
            > 0.9 => "white",
            < 0.1 => "black",
            _ => string.Empty,
        };

        if (yellowness > 0.8)
        {
            wantedAdjective = "yellow";
        }
        else if (redness > 0.8)
        {
            wantedAdjective = "red";
        }
        else if (greenness > 0.8)
        {
            wantedAdjective = "green";
        }
        else if (b > 0.8)
        {
            wantedAdjective = "blue";
        }

        return wantedAdjective;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Clamp(double value, double min, double max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private T GetRandomElement<T>(Random random, IList<T> list)
    {
        return list[random.Next(list.Count)];
    }

    public record NamingState(bool GenusIsNumbered = false, bool GenusIsProto = false, string GenusRoot = "",
        GrammaticalGender Gender = GrammaticalGender.Neuter, OrganelleDefinition? NumberedOrganelle = null)
    {
        public bool GenusIsNumbered = GenusIsNumbered;
        public bool GenusIsProto = GenusIsProto;
        public string GenusRoot = GenusRoot;
        public GrammaticalGender Gender = Gender;
        public OrganelleDefinition? NumberedOrganelle = NumberedOrganelle;
    }
}
