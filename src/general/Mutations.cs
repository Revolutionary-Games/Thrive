using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   Generates mutations for species
/// </summary>
public class Mutations
{
    private static readonly List<string> Vowels = new List<string>()
        {
        "a", "e", "i", "o", "u",
        };
    private static readonly List<string> PronoucablePermutation = new List<string>()
        {
        "th", "sh", "ch", "wh", "Th", "Sh", "Ch", "Wh",
        };
    private static readonly List<string> Consonants = new List<string>()
        {
        "b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
        "n", "p", "q", "s", "t", "v", "w", "x", "y", "z",
        };

    private Random random = new Random();

    /// <summary>
    ///   Creates a mutated version of a species
    /// </summary>
    public MicrobeSpecies CreateMutatedSpecies(MicrobeSpecies parent, MicrobeSpecies mutated)
    {
        var simulation = SimulationParameters.Instance;
        var nameGenerator = simulation.NameGenerator;

        mutated.IsBacteria = parent.IsBacteria;

        // Mutate the epithet
        if (random.Next(0, 100) < Constants.MUTATION_WORD_EDIT)
        {
            mutated.Epithet = MutateWord(parent.Epithet);
        }
        else
        {
            mutated.Epithet = nameGenerator.GenerateNameSection();
        }

        mutated.Genus = parent.Genus;

        MutateBehaviour(parent, mutated);

        if (random.Next(0, 100) <= Constants.MUTATION_CHANGE_GENUS)
        {
            // We can do more fun stuff here later
            if (random.Next(0, 100) < Constants.MUTATION_WORD_EDIT)
            {
                mutated.Genus = MutateWord(parent.Genus);
            }
            else
            {
                mutated.Genus = nameGenerator.GenerateNameSection();
            }
        }

        MutateMicrobeOrganelles(parent.Organelles, mutated.Organelles, mutated.IsBacteria);

        // There is a small chance of evolving into a eukaryote
        var nucleus = simulation.GetOrganelleType("nucleus");

        if (mutated.Organelles.Any(o => o.Definition == nucleus))
        {
            mutated.IsBacteria = false;
        }

        var colour = mutated.IsBacteria ? RandomProkayroteColour() :
            RandomColour();

        if (random.Next(0, 100) <= 20)
        {
            // Could perhaps use a weighted entry model here... the
            // earlier one is listed, the more likely currently (I
            // think). That may be an issue.
            if (random.Next(0, 100) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("single");
            }
            else if (random.Next(0, 100) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("double");
                colour.a = RandomOpacityChitin();
            }
            else if (random.Next(0, 100) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("cellulose");
            }
            else if (random.Next(0, 100) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("chitin");
                colour.a = RandomOpacityChitin();
            }
            else if (random.Next(0, 100) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("calcium_carbonate");
                colour.a = RandomOpacityChitin();
            }
            else
            {
                mutated.MembraneType = simulation.GetMembrane("silica");
                colour.a = RandomOpacityChitin();
            }
        }
        else
        {
            mutated.MembraneType = parent.MembraneType;
        }

        mutated.MembraneRigidity = Math.Max(Math.Min(parent.MembraneRigidity +
                random.Next(-25, 25) / 100.0f, 1), -1);

        // If you have iron (f is the symbol for rusticyanin)
        var rusticyanin = simulation.GetOrganelleType("rusticyanin");
        var chemo = simulation.GetOrganelleType("chemoplast");
        var chemoProtein = simulation.GetOrganelleType("chemoSynthesizingProteins");

        if (mutated.Organelles.Any(o => o.Definition == rusticyanin))
        {
            mutated.SetInitialCompoundsForIron();
        }
        else if (mutated.Organelles.Any(o => o.Definition == chemo ||
                o.Definition == chemoProtein))
        {
            mutated.SetInitialCompoundsForChemo();
        }
        else
        {
            mutated.SetInitialCompoundsForDefault();
        }

        return mutated;
    }

    /// <summary>
    ///   Creates a fully random species starting with one cytoplasm
    /// </summary>
    public MicrobeSpecies CreateRandomSpecies(MicrobeSpecies mutated, int steps = 5)
    {
        // Temporarily create species with just cytoplasm to start mutating from
        var temp = new MicrobeSpecies(int.MaxValue);

        GameWorld.SetInitialSpeciesProperties(temp);

        // TODO: in the old code GenerateNameSection was used to
        // override the default species name here

        for (int step = 0; step < steps; ++step)
        {
            CreateMutatedSpecies(temp, mutated);
            temp = (MicrobeSpecies)mutated.Clone();
        }

        return mutated;
    }

    private void MutateBehaviour(MicrobeSpecies parent, MicrobeSpecies mutated)
    {
        // Variables used in AI to determine general behavior mutate these
        float aggression = parent.Aggression + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float fear = parent.Fear + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float activity = parent.Activity + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float focus = parent.Focus + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float opportunism = parent.Opportunism + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);

        // Make sure not over or under our scales
        // This used to be a method as well
        mutated.Aggression = aggression.Clamp(0.0f, Constants.MAX_SPECIES_AGRESSION);
        mutated.Fear = fear.Clamp(0.0f, Constants.MAX_SPECIES_FEAR);
        mutated.Activity = activity.Clamp(0.0f, Constants.MAX_SPECIES_ACTIVITY);
        mutated.Focus = focus.Clamp(0.0f, Constants.MAX_SPECIES_FOCUS);
        mutated.Opportunism = opportunism.Clamp(0.0f, Constants.MAX_SPECIES_OPPORTUNISM);
    }

    /// <summary>
    ///   Creates a mutated version of parentOrganelles in organelles
    /// </summary>
    private void MutateMicrobeOrganelles(OrganelleLayout<OrganelleTemplate> parentOrganelles,
        OrganelleLayout<OrganelleTemplate> organelles, bool isBacteria)
    {
        organelles.RemoveAll();
    }

    private float RandomColourChannel()
    {
        return random.Next(Constants.MIN_COLOR, Constants.MAX_COLOR);
    }

    private float RandomMutationColourChannel()
    {
        return random.Next(Constants.MIN_COLOR_MUTATION, Constants.MAX_COLOR_MUTATION);
    }

    private float RandomOpacity()
    {
        return random.Next(Constants.MIN_OPACITY, Constants.MAX_OPACITY);
    }

    private float RandomOpacityChitin()
    {
        return random.Next(Constants.MIN_OPACITY_CHITIN, Constants.MAX_OPACITY_CHITIN);
    }

    private float RandomOpacityBacteria()
    {
        return random.Next(Constants.MIN_OPACITY, Constants.MAX_OPACITY + 1);
    }

    private float RandomMutationOpacity()
    {
        return random.Next(Constants.MIN_OPACITY_MUTATION, Constants.MAX_OPACITY_MUTATION);
    }

    /// <summary>
    ///   TODO: rename to something more sensible and rename RandomColourChannels to this
    /// </summary>
    private Color RandomColour(float? opaqueness = null)
    {
        if (!opaqueness.HasValue)
            opaqueness = RandomOpacity();

        return RandomColourChannels(opaqueness.Value);
    }

    private Color RandomProkayroteColour(float? opaqueness = null)
    {
        if (!opaqueness.HasValue)
            opaqueness = RandomOpacityBacteria();

        return RandomColourChannels(opaqueness.Value);
    }

    private Color RandomColourChannels(float opaqueness)
    {
        return new Color(RandomColourChannel(), RandomColourChannel(), RandomColourChannel(),
            opaqueness);
    }

    private string MutateWord(string name)
    {
        StringBuilder newName = new StringBuilder(name);
        int changeLimit = 1;
        int letterChangeLimit = 2;
        int letterChanges = 0;
        int changes = 0;

        for (int i = 1; i < newName.Length; i++)
        {
            if (changes <= changeLimit && i > 1)
            {
                // Index we are adding or erasing chromosomes at
                int index = newName.Length - i - 1;

                // Are we a vowel or are we a consonant?
                var part = newName.ToString(index, 2);
                bool isPermute = PronoucablePermutation.Any(item => item == part);
                string original = newName.ToString(index, 2);
                if (random.Next(0, 20) <= 10 && isPermute)
                {
                    newName.Erase(index, 2);
                    changes++;
                    newName.Insert(index, PronoucablePermutation.Random(random));
                }
            }
        }

        // 2% chance each letter
        for (int i = 1; i < newName.Length; i++)
        {
            if (random.Next(0, 120) <= 1 && changes <= changeLimit)
            {
                // Index we are adding or erasing chromosomes at
                int index = newName.Length - i - 1;

                // Are we a vowel or are we a consonant?
                var part = newName.ToString(index, 1);
                bool isVowel = Vowels.Any(item => item == part);

                bool isPermute = false;
                if (i > 1)
                {
                    var part1 = newName.ToString(index - 1, 2);
                    var part2 = newName.ToString(index - 2, 2);
                    var part3 = newName.ToString(index, 2);
                    if (PronoucablePermutation.Any(item => item == part1) ||
                        PronoucablePermutation.Any(item => item == part2) ||
                        PronoucablePermutation.Any(item => item == part3))
                    {
                        isPermute = true;
                    }
                }

                string original = newName.ToString(index, 1);

                if (!isVowel && newName.ToString(index, 1) != "r" && !isPermute)
                {
                    newName.Erase(index, 1);
                    changes++;
                    switch (random.Next(0, 5))
                    {
                    case 0:
                        newName.Insert(index, Vowels.Random(random)
                            + Consonants.Random(random));
                        break;
                    case 1:
                        newName.Insert(index, Consonants.Random(random)
                            + Vowels.Random(random));
                        break;
                    case 2:
                        newName.Insert(index, original + Consonants.Random(random));
                        break;
                    case 3:
                        newName.Insert(index, Consonants.Random(random) + original);
                        break;
                    case 4:
                        newName.Insert(index, original + Consonants.Random(random)
                            + Vowels.Random(random));
                        break;
                    case 5:
                        newName.Insert(index, Vowels.Random(random) +
                            Consonants.Random(random) + original);
                        break;
                    }
                }

                // If is vowel
                else if (newName.ToString(index, 1) != "r" && !isPermute)
                {
                    newName.Erase(index, 1);
                    changes++;
                    if (random.Next(0, 20) <= 10)
                    {
                        newName.Insert(index, Consonants.Random(random) +
                            Vowels.Random(random) + original);
                    }
                    else
                    {
                        newName.Insert(index, original + Vowels.Random(random) +
                            Consonants.Random(random));
                    }
                }
            }
        }

        // Ignore the first letter and last letter
        for (int i = 1; i < newName.Length; i++)
        {
            // Index we are adding or erasing chromosomes at
            int index = newName.Length - i - 1;

            bool isPermute = false;
            if (i > 1)
            {
                var part1 = newName.ToString(index - 1, 2);
                var part2 = newName.ToString(index - 2, 2);
                var part3 = newName.ToString(index, 2);
                if (PronoucablePermutation.Any(item => item == part1) ||
                    PronoucablePermutation.Any(item => item == part2) ||
                    PronoucablePermutation.Any(item => item == part3))
                {
                    isPermute = true;
                }
            }

            // Are we a vowel or are we a consonant?
            var part = newName.ToString(index, 1);
            bool isVowel = Vowels.Any(item => item == part);

            // 50 percent chance replace
            if (random.Next(0, 20) <= 10 && changes <= changeLimit)
            {
                if (!isVowel && newName.ToString(index, 1) != "r" && !isPermute)
                {
                    newName.Erase(index, 1);
                    letterChanges++;
                    newName.Insert(index, Consonants.Random(random));
                }
                else if (!isPermute)
                {
                    newName.Erase(index, 1);
                    letterChanges++;
                    newName.Insert(index, Vowels.Random(random));
                }
            }
        }

        // Our base case
        if (letterChanges < letterChangeLimit && changes == 0)
        {
            // We didnt change our word at all, try again recursviely until we do
            return MutateWord(name);
        }

        // TODO: C# probably has better ways to handle case conversions

        // Convert to lower case
        for (int i = 1; i < newName.Length - 1; i++)
        {
            if (newName[i] >= 65 && newName[i] <= 92)
            {
                newName[i] = (char)(newName[i] + 32);
            }
        }

        // Convert first letter to upper case
        if (newName[0] >= 97 && newName[0] <= 122)
        {
            newName[0] = (char)(newName[0] - 32);
        }

        return newName.ToString();
    }
}
