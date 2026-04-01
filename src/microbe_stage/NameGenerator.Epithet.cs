using System;
using Xoshiro.PRNG64;

/// <summary>
///   The microbe species name generator.
/// </summary>
public partial class NameGenerator
{
    public string GenerateEpithetName(Random? random, MicrobeSpecies? speciesOld, MicrobeSpecies speciesNew)
    {
        var stringBuilder = GetBuffer();
        random ??= new XoShiRo256starstar();

        if (speciesNew.NamingState is null)
            throw new Exception("GenerateEpithetName should be called after GenerateGenusName.");

        var gender = speciesNew.NamingState.Gender;

        ModifiedLegacyGenerateNameRoot(random, stringBuilder);
        GenerateGenderedSuffix(random, stringBuilder, gender, false);

        stringBuilder[0] = char.ToLowerInvariant(stringBuilder[0]);

        return stringBuilder.ToString();
    }
}
