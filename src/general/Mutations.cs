using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Generates mutations for species
/// </summary>
public class Mutations
{
    private static readonly List<string> Vowels = new List<string>
    {
        "a", "e", "i", "o", "u",
    };

    private static readonly List<string> PronoucablePermutation = new List<string>
    {
        "th", "sh", "ch", "wh", "Th", "Sh", "Ch", "Wh",
    };

    private static readonly List<string> Consonants = new List<string>
    {
        "b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
        "n", "p", "q", "s", "t", "v", "w", "x", "y", "z",
    };

    [JsonProperty]
    private Random random = new Random();

    /// <summary>
    ///   Creates a mutated version of a species
    /// </summary>
    public MicrobeSpecies CreateMutatedSpecies(MicrobeSpecies parent, MicrobeSpecies mutated)
    {
        if (parent.Organelles.Count < 1)
        {
            throw new ArgumentException("Can't create a mutated version of an empty species");
        }

        var simulation = SimulationParameters.Instance;
        var nameGenerator = simulation.NameGenerator;

        mutated.IsBacteria = parent.IsBacteria;

        // Mutate the epithet
        if (random.Next(0, 101) < Constants.MUTATION_WORD_EDIT)
        {
            mutated.Epithet = MutateWord(parent.Epithet);
        }
        else
        {
            mutated.Epithet = nameGenerator.GenerateNameSection();
        }

        mutated.Genus = parent.Genus;

        MutateBehaviour(parent, mutated);

        if (random.Next(0, 101) <= Constants.MUTATION_CHANGE_GENUS)
        {
            // We can do more fun stuff here later
            if (random.Next(0, 101) < Constants.MUTATION_WORD_EDIT)
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

        var colour = mutated.IsBacteria ? RandomProkayroteColour() : RandomColour();

        if (random.Next(0, 101) <= 20)
        {
            // Could perhaps use a weighted entry model here... the
            // earlier one is listed, the more likely currently (I
            // think). That may be an issue.
            if (random.Next(0, 101) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("single");
            }
            else if (random.Next(0, 101) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("double");
                colour.a = RandomOpacityChitin();
            }
            else if (random.Next(0, 101) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("cellulose");
            }
            else if (random.Next(0, 101) < 50)
            {
                mutated.MembraneType = simulation.GetMembrane("chitin");
                colour.a = RandomOpacityChitin();
            }
            else if (random.Next(0, 101) < 50)
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

        mutated.Colour = colour;

        mutated.MembraneRigidity = Math.Max(Math.Min(parent.MembraneRigidity +
            random.Next(-25, 26) / 100.0f, 1), -1);

        mutated.UpdateInitialCompounds();

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

    private static bool IsPermute(StringBuilder newName, int index)
    {
        var part1 = newName.ToString(index - 1, 2);
        var part2 = newName.ToString(index - 2, 2);
        var part3 = newName.ToString(index, 2);
        if (PronoucablePermutation.Any(item => item == part1) ||
            PronoucablePermutation.Any(item => item == part2) ||
            PronoucablePermutation.Any(item => item == part3))
        {
            return true;
        }

        return false;
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
        var nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

        organelles.Clear();

        // Delete or replace an organelle randomly
        for (int i = 0; i < parentOrganelles.Count; i++)
        {
            bool copy = true;

            var organelle = parentOrganelles[i];

            if (parentOrganelles.Count < 2)
            {
                // Removing last organelle would be silly
            }
            else if (organelle.Definition != nucleus)
            {
                // Chance to replace or remove if not a nucleus

                if (random.Next(0.0f, 1.0f) < Constants.MUTATION_DELETION_RATE)
                {
                    copy = false;
                }
                else if (random.Next(0.0f, 1.0f) < Constants.MUTATION_REPLACEMENT_RATE)
                {
                    copy = false;

                    var replacer = new OrganelleTemplate(GetRandomOrganelle(isBacteria),
                        organelle.Position, organelle.Orientation);

                    // The replacing organelle might not fit at the same position
                    try
                    {
                        organelles.Add(replacer);
                    }
                    catch (ArgumentException)
                    {
                        // Couldn't replace it
                        copy = true;
                    }
                }
            }

            if (!copy)
                continue;

            // Copy the organelle
            try
            {
                organelles.Add((OrganelleTemplate)organelle.Clone());
            }
            catch (ArgumentException)
            {
                // Add the organelle randomly back to the list to make
                // sure we don't throw it away
                AddNewOrganelle(organelles, organelle.Definition);
            }
        }

        // Can add up to 6 new organelles (Which should allow AI to catch up to player more
        // We can insert new organelles at the end of the list
        if (random.Next(0.0f, 1.0f) < Constants.MUTATION_CREATION_RATE)
        {
            AddNewOrganelle(organelles, GetRandomOrganelle(isBacteria));
        }

        /*
        Probability of mutation occuring 5 time(s) = 0.15 = 1.0E-5
        Probability of mutation NOT occuring = (1 - 0.1)5 = 0.59049
        Probability of mutation occuring = 1 - (1 - 0.1)5 = 0.40951
        */

        // We can insert new organelles at the end of the list
        for (int n = 0; n < 5; ++n)
        {
            if (random.Next(0.0f, 1.0f) < Constants.MUTATION_EXTRA_CREATION_RATE)
            {
                AddNewOrganelle(organelles, GetRandomOrganelle(isBacteria));
            }
        }

        if (isBacteria)
        {
            if (random.Next(0.0f, 100.0f) <= Constants.MUTATION_BACTERIA_TO_EUKARYOTE)
            {
                AddNewOrganelle(organelles, nucleus);
            }
        }

        // Disallow creating empty species as that throws an exception when trying to spawn
        if (organelles.Count < 1)
        {
            // Add the first parent species organelle
            AddNewOrganelle(organelles, parentOrganelles[0].Definition);

            // If still empty, copy the first organelle of the parent
            if (organelles.Count < 1)
                organelles.Add((OrganelleTemplate)parentOrganelles[0].Clone());
        }
    }

    /// <summary>
    ///   Adds a new organelle to a mutation result
    /// </summary>
    private void AddNewOrganelle(OrganelleLayout<OrganelleTemplate> organelles,
        OrganelleDefinition organelle)
    {
        try
        {
            organelles.Add(GetRealisticPosition(organelle, organelles));
        }
        catch (ArgumentException)
        {
            // Failing to add a mutation is not serious
        }
    }

    private OrganelleDefinition GetRandomOrganelle(bool isBacteria)
    {
        if (isBacteria)
        {
            return SimulationParameters.Instance.GetRandomProkaryoticOrganelle(random);
        }

        return SimulationParameters.Instance.GetRandomEukaryoticOrganelle(random);
    }

    private OrganelleTemplate GetRealisticPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles)
    {
        var result = new OrganelleTemplate(organelle, new Hex(0, 0), 0);

        // Loop through all the organelles and find an open spot to
        // place our new organelle attached to existing organelles
        // This almost always is over at the first iteration, so its
        // not a huge performance hog
        foreach (var otherOrganelle in existingOrganelles.OrderBy(_ => random.Next()))
        {
            // The otherOrganelle is the organelle we wish to be next to
            // Loop its hexes and check positions next to them
            foreach (var hex in otherOrganelle.RotatedHexes)
            {
                // Offset by hexes in organelle we are looking at
                var pos = otherOrganelle.Position + hex;

                for (int side = 1; side <= 6; ++side)
                {
                    // Offset by hex offset
                    result.Position = pos + Hex.HexNeighbourOffset[(Hex.HexSide)side];

                    // TODO: checking one or two extra hexes in the direction would make this succeed more often

                    // Check every possible rotation value.
                    for (int rotation = 0; rotation <= 5; ++rotation)
                    {
                        result.Orientation = rotation;

                        if (existingOrganelles.CanPlace(result))
                        {
                            return result;
                        }
                    }
                }
            }
        }

        // We didnt find an open spot, this doesn't make much sense
        throw new ArgumentException("Mutation code could not find a good position " +
            "for a new organelle");
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
                if (random.Next(0, 21) <= 10 && isPermute)
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
            if (random.Next(0, 121) <= 1 && changes <= changeLimit)
            {
                // Index we are adding or erasing chromosomes at
                int index = newName.Length - i - 1;

                // Are we a vowel or are we a consonant?
                var part = newName.ToString(index, 1);
                bool isVowel = Vowels.Any(item => item == part);

                bool isPermute = false;
                if (i > 1 && index - 2 >= 0)
                {
                    if (IsPermute(newName, index))
                        isPermute = true;
                }

                string original = newName.ToString(index, 1);

                if (!isVowel && newName.ToString(index, 1) != "r" && !isPermute)
                {
                    newName.Erase(index, 1);
                    changes++;
                    switch (random.Next(0, 6))
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
                    if (random.Next(0, 21) <= 10)
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
            if (index - 2 > 0 && i > 1)
            {
                if (IsPermute(newName, index))
                    isPermute = true;
            }

            // Are we a vowel or are we a consonant?
            var part = newName.ToString(index, 1);
            bool isVowel = Vowels.Any(item => item == part);

            // 50 percent chance replace
            if (random.Next(0, 21) <= 10 && changes <= changeLimit)
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
