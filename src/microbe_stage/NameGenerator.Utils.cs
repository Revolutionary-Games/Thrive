using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;

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
        { ('o', 'u'), 'u' },
    }.ToFrozenDictionary();

    private static StringBuilder? stringBuffer;

    public enum GrammaticalGender
    {
        Masculine,
        Feminine,
        Neuter,
        Ambiguous,
    }

    private enum BacteriaShape
    {
        Coccum,
        Bacillum,
        Fusiform,
        Vibrion,
        Unknown,
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

        // Preferred temperature
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

        // Membrane rigidity
        var rigidity = 0.5 * (species.MembraneRigidity + 1.0);
        rigidity = rigidity * rigidity * rigidity * rigidity * Math.Sign(rigidity);
        switch (rigidity)
        {
            case > 0.5:
                relevantQualities.Add("rigid", rigidity);
                break;
            case < 0.5:
                relevantQualities.Add("fluid", rigidity);
                break;
        }

        return relevantQualities;
    }

    private static Dictionary<string, double> CalculateRelevantTolerances(MicrobeSpecies species)
    {
        Dictionary<string, double> relevantTolerances = new();

        var tolerances = species.Tolerances;

        // Oxygen resistance
        var oxygenResistance = tolerances.OxygenResistance;
        switch (oxygenResistance)
        {
            case > 0.1f:
                relevantTolerances.Add("oxygen", oxygenResistance);
                break;
        }

        // Preferred temperature
        var preferredTemperature = tolerances.PreferredTemperature;
        var temperatureWeight = Math.Abs(Clamp((preferredTemperature - 50) * 0.02, -1, 1));
        switch (preferredTemperature)
        {
            case > 80:
                relevantTolerances.Add("hot", temperatureWeight);
                break;
            case < 0:
                relevantTolerances.Add("cold", temperatureWeight);
                break;
        }

        // Pressure
        var pressure = tolerances.PressureMinimum + tolerances.PressureTolerance;
        var logPressure = Math.Log10(pressure);
        switch (logPressure)
        {
            case > 7:
                var logPressureWeight = Clamp(logPressure / 8.0, -1.0, 1.0);
                relevantTolerances.Add("pressure", logPressureWeight);
                break;
        }

        // Membrane rigidity
        var uvResistance = tolerances.UVResistance;
        switch (uvResistance)
        {
            case > 0.5f:
                relevantTolerances.Add("sunlight", uvResistance);
                break;
        }

        return relevantTolerances;
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

    /// <summary>
    ///   Transforms GrammaticalGender to a string without allocating.
    /// </summary>
    private static string GenderToString(GrammaticalGender gender)
    {
        return gender switch
        {
            GrammaticalGender.Masculine => "masculine",
            GrammaticalGender.Feminine => "feminine",
            GrammaticalGender.Neuter => "neuter",
            GrammaticalGender.Ambiguous => "ambiguous",
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null),
        };
    }

    private static GrammaticalGender GenderFromString(string gender)
    {
        return gender switch
        {
            "masculine" => GrammaticalGender.Masculine,
            "feminine" => GrammaticalGender.Feminine,
            "neuter" => GrammaticalGender.Neuter,
            "ambiguous" => GrammaticalGender.Ambiguous,
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null),
        };
    }

    private static string ExtractWeightedKey(Random random, Dictionary<string, double> dictionary)
    {
        if (dictionary.Count == 0)
            return string.Empty;

        double maxRandom = 0.0;
        foreach (var weight in dictionary.Values)
        {
            maxRandom += Math.Abs(weight);
        }

        var randomValue = random.NextDouble() * maxRandom;

        double cumulative = 0.0;
        string key = string.Empty;
        foreach (var entry in dictionary)
        {
            cumulative += Math.Abs(entry.Value);

            if (cumulative < randomValue)
                continue;

            key = entry.Key;
            break;
        }

        return key;
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

    private GrammaticalGender GenerateGenderedSuffix(Random random, StringBuilder stringBuilder,
        GrammaticalGender? parent, bool useBacteria, MicrobeSpecies? bacterium)
    {
        GrammaticalGender gender;

        // If the parent gender is ambiguous, we can consistently reroll the wanted grammatical gender for the new
        // generation.
        if (parent == GrammaticalGender.Ambiguous)
        {
            gender = GetRandomElement(random, (GrammaticalGender[])Enum.GetValuesAsUnderlyingType<GrammaticalGender>());
        }
        else
        {
            gender = parent ?? GetRandomElement(random,
                (GrammaticalGender[])Enum.GetValuesAsUnderlyingType<GrammaticalGender>());
        }

        var genderName = GenderToString(gender);

        string genderedSuffix;
        if (useBacteria)
        {
            // We want to generate a bacterium suffix.

            if (bacterium is null)
            {
                genderedSuffix = config.BacteriaShapes.Random(random)!.TryGetValue(genderName, out var suffixes) ?
                    suffixes.Random(random) :
                    config.Suffixes[genderName].Random(random);
            }
            else
            {
                // If a species is provided, we generate the bacterium suffix based on the shape.

                gender = GenerateGenderedBacteriumSuffix(random, gender, bacterium, out var suffixes);

                genderedSuffix = suffixes.Random(random);
            }
        }
        else
        {
            genderedSuffix = config.Suffixes[genderName].Random(random);
        }

        PhonotacticsFriendlyAppend(stringBuilder, genderedSuffix);

        return gender;
    }

    private GrammaticalGender GenerateGenderedBacteriumSuffix(Random random, GrammaticalGender parent,
        MicrobeSpecies bacterium, out List<string> suffixes)
    {
        var @override = parent;

        // We do some principal component analysis on the organelle layout to see what shape this is.
        var centroid = Hex.AxialToCartesian(bacterium.Organelles.CenterOfMass);

        int count = bacterium.Organelles.Count;

        // Calculate the covariance matrix of the layout and the minimum squared distance to centroid.
        float varX = 0f;
        float varY = 0f;
        float cov = 0f;

        float minSquaredDistanceToCentroid = float.MaxValue;
        for (int i = 0; i < count; ++i)
        {
            var organelle = bacterium.Organelles[i];

            Vector3 pos = Hex.AxialToCartesian(organelle.Position);

            float dx = pos.X - centroid.X;
            float dy = pos.Z - centroid.Z;

            varX += dx * dx;
            varY += dy * dy;
            cov += dx * dy;

            float distanceSquared = dx * dx + dy * dy;

            if (distanceSquared < minSquaredDistanceToCentroid)
                minSquaredDistanceToCentroid = distanceSquared;
        }

        varX /= count;
        varY /= count;
        cov /= count;

        // Compute the eigenvalues of the covariance matrix
        float trace = varX + varY;
        float determinant = varX * varY - cov * cov;
        float gap = (float)Math.Sqrt(Math.Max(0, trace * trace - 4 * determinant));
        float lambda1 = (trace + gap) / 2.0f;
        float lambda2 = (trace - gap) / 2.0f;
        float absLambda1 = MathF.Abs(lambda1);
        float absLambda2 = MathF.Abs(lambda2);
        float signLambda1 = (absLambda1 < 0.0001f) ? +1.0f : MathF.Sign(lambda1);
        float signLambda2 = (absLambda2 < 0.0001f) ? +1.0f : MathF.Sign(lambda1);
        float adjustedLambda1 = MathF.Max(absLambda1, Constants.DEFAULT_HEX_SIZE) * signLambda1;
        float adjustedLambda2 = MathF.Max(absLambda2, Constants.DEFAULT_HEX_SIZE) * signLambda2;
        float aspectRatio = (float)Math.Sqrt(adjustedLambda1 / adjustedLambda2);

        bool isCentroidOutside = minSquaredDistanceToCentroid > 0.75f * Constants.DEFAULT_HEX_SIZE *
            Constants.DEFAULT_HEX_SIZE;

        var shape = count switch
        {
            // Comma-shaped bacterium, aka vibrions.
            < 10 when isCentroidOutside && aspectRatio > 1.2f => BacteriaShape.Vibrion,

            // Spherical bacterium, aka a coccum. The count condition is based on the fourth hexagonal number.
            // Huge prokaryotes aren't usually considered cocci.
            < 37 when aspectRatio < 1.2f => BacteriaShape.Coccum,

            // Spindle-shaped bacterium, aka a fusiform bacterium. It's long and thin.
            < 40 when aspectRatio > 3f => BacteriaShape.Fusiform,

            // Standard bacilli. These are rod-shaped bacteria that are not too long nor too large, and aren't cocci.
            < 10 => BacteriaShape.Bacillum,

            // This is likely a large and amorphous prokaryote, so we avoid using standard nomenclature and fall back
            // to a simple suffix.
            _ => BacteriaShape.Unknown,
        };

        var lookupKey = shape switch
        {
            BacteriaShape.Vibrion => "vibrion",
            BacteriaShape.Coccum => "coccum",
            BacteriaShape.Bacillum => "bacillum",
            BacteriaShape.Fusiform => "fusiform",
            BacteriaShape.Unknown => string.Empty,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var bacteriaShapes = config.BacteriaShapes;
        var genderName = GenderToString(parent);
        if (!bacteriaShapes.TryGetValue(lookupKey, out var genderedSuffixes))
        {
            if (shape != BacteriaShape.Unknown)
            {
                throw new Exception($"Bacteria shapes not mapped for key {lookupKey} in species_names.json!");
            }

            // We don't know what shape this is, so fall back to regular suffixes.
            suffixes = config.Suffixes[genderName];

            return parent;
        }

        if (!genderedSuffixes.TryGetValue(genderName, out var suffixesNullable))
        {
            // If the specific gender isn't mapped, we switch to a new available one.
            var newKey = genderedSuffixes.Keys.ToHashSet().Random(random);
            suffixes = genderedSuffixes[newKey];

            @override = GenderFromString(newKey);
        }
        else
        {
            suffixes = suffixesNullable;
        }

        return @override;
    }

    public record NamingState(bool GenusIsNumbered = false, bool GenusIsProto = false, string GenusRoot = "",
        GrammaticalGender Gender = GrammaticalGender.Neuter, INameGenerationTarget? Target = null)
    {
        public bool GenusIsNumbered = GenusIsNumbered;
        public bool GenusIsProto = GenusIsProto;
        public string GenusRoot = GenusRoot;
        public GrammaticalGender Gender = Gender;
        public INameGenerationTarget? Target = Target;
    }
}
