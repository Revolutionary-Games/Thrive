using System;
using System.Collections.Generic;
using System.Globalization;
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

    private static readonly List<string> PronounceablePermutation = new List<string>
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

        MutateBehaviour(parent, mutated);

        MutateMicrobeOrganelles(parent.Organelles, mutated.Organelles, mutated.IsBacteria);

        // Update the genus if the new species is different enough
        if (NewGenus(mutated, parent))
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
        else
        {
            mutated.Genus = parent.Genus;
        }

        // If the new species is a eukaryote, mark this as such
        var nucleus = simulation.GetOrganelleType("nucleus");
        if (mutated.Organelles.Any(o => o.Definition == nucleus))
        {
            mutated.IsBacteria = false;
        }

        // Update colour and membrane
        var colour = mutated.IsBacteria ? RandomProkaryoteColour() : RandomEukaryoteColour();
        if (random.Next(0, 101) <= 20)
        {
            mutated.MembraneType = RandomMembraneType(simulation);
            if (mutated.MembraneType != simulation.GetMembrane("single"))
            {
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

        mutated.RepositionToOrigin();
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

        // Override the default species starting name to have more variability in the names
        var nameGenerator = SimulationParameters.Instance.NameGenerator;
        temp.Epithet = nameGenerator.GenerateNameSection();
        temp.Genus = nameGenerator.GenerateNameSection();

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
        if (PronounceablePermutation.Contains(part1) ||
            PronounceablePermutation.Contains(part2) ||
            PronounceablePermutation.Contains(part3))
        {
            return true;
        }

        return false;
    }

    private void MutateBehaviour(MicrobeSpecies parent, MicrobeSpecies mutated)
    {
        // Variables used in AI to determine general behaviour mutate these
        float aggression = parent.Behaviour.Aggression + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float fear = parent.Behaviour.Fear + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float activity = parent.Behaviour.Activity + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float focus = parent.Behaviour.Focus + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);
        float opportunism = parent.Behaviour.Opportunism + random.Next(
            Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION);

        // Make sure not over or under our scales
        // This used to be a method as well
        mutated.Behaviour.Aggression = aggression.Clamp(0.0f, Constants.MAX_SPECIES_AGGRESSION);
        mutated.Behaviour.Fear = fear.Clamp(0.0f, Constants.MAX_SPECIES_FEAR);
        mutated.Behaviour.Activity = activity.Clamp(0.0f, Constants.MAX_SPECIES_ACTIVITY);
        mutated.Behaviour.Focus = focus.Clamp(0.0f, Constants.MAX_SPECIES_FOCUS);
        mutated.Behaviour.Opportunism = opportunism.Clamp(0.0f, Constants.MAX_SPECIES_OPPORTUNISM);
    }

    /// <summary>
    ///   Creates a mutated version of parentOrganelles in organelles
    /// </summary>
    private void MutateMicrobeOrganelles(OrganelleLayout<OrganelleTemplate> parentOrganelles,
        OrganelleLayout<OrganelleTemplate> mutatedOrganelles, bool isBacteria)
    {
        var nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

        for (var iteration = 0; iteration < 10; iteration++)
        {
            mutatedOrganelles.Clear();

            // Chance to replace each organelle randomly
            foreach (var parentOrganelle in parentOrganelles)
            {
                var organelle = (OrganelleTemplate)parentOrganelle.Clone();

                // Chance to replace or remove if not a nucleus
                if (organelle.Definition != nucleus)
                {
                    if (random.Next(0.0f, 1.0f) < Constants.MUTATION_DELETION_RATE / Math.Sqrt(parentOrganelles.Count))
                    {
                        // Don't copy over this organelle, removing this one from the new species
                        continue;
                    }

                    if (random.Next(0.0f, 1.0f) < Constants.MUTATION_REPLACEMENT_RATE)
                    {
                        organelle = new OrganelleTemplate(GetRandomOrganelle(isBacteria),
                            organelle.Position, organelle.Orientation);
                    }
                }

                // Copy the organelle
                try
                {
                    mutatedOrganelles.Add(organelle);
                }
                catch (ArgumentException)
                {
                    // Add the organelle randomly back to the list to make
                    // sure we don't throw it away
                    AddNewOrganelle(mutatedOrganelles, organelle.Definition);
                }
            }

            // We can insert new organelles at the end of the list
            for (int i = 0; i < 6; ++i)
            {
                if (random.Next(0.0f, 1.0f) < Constants.MUTATION_CREATION_RATE)
                {
                    if (random.Next(0.0f, 1.0f) < Constants.MUTATION_NEW_ORGANELLE_CHANCE)
                    {
                        AddNewOrganelle(mutatedOrganelles, GetRandomOrganelle(isBacteria));
                    }
                    else
                    {
                        // Duplicate an existing organelle, but only if there are any organelles where that is legal
                        var organellesThatCanBeDuplicated =
                            parentOrganelles.Organelles.Where(organelle => !organelle.Definition.Unique).ToList();
                        if (organellesThatCanBeDuplicated.Any())
                        {
                            AddNewOrganelle(mutatedOrganelles,
                                organellesThatCanBeDuplicated.Random(random).Definition);
                        }
                        else
                        {
                            AddNewOrganelle(mutatedOrganelles, GetRandomOrganelle(isBacteria));
                        }
                    }
                }
            }

            if (isBacteria)
            {
                if (random.Next(0.0f, 1.0f) <= Constants.MUTATION_BACTERIA_TO_EUKARYOTE)
                {
                    AddNewOrganelle(mutatedOrganelles, nucleus);
                }
            }

            // Disallow creating empty species as that throws an exception when trying to spawn
            if (mutatedOrganelles.Count < 1)
            {
                // Add the first parent species organelle
                AddNewOrganelle(mutatedOrganelles, parentOrganelles[0].Definition);

                // If still empty, copy the first organelle of the parent
                if (mutatedOrganelles.Count < 1)
                    mutatedOrganelles.Add((OrganelleTemplate)parentOrganelles[0].Clone());
            }

            // If the mutation has no islands, we can use this iteration
            if (mutatedOrganelles.GetIslandHexes().Count == 0)
                return;
        }

        GD.PrintErr("Could not create a valid mutation after 10 retries.");
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
                    for (int radius = 1; radius <= 3; ++radius)
                    {
                        // Offset by hex offset multiplied by a factor to check for greater range
                        var hexOffset = Hex.HexNeighbourOffset[(Hex.HexSide)side];
                        hexOffset *= radius;
                        result.Position = pos + hexOffset;

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
        }

        // We didnt find an open spot, this doesn't make much sense
        throw new ArgumentException("Mutation code could not find a good position " +
            "for a new organelle");
    }

    private MembraneType RandomMembraneType(SimulationParameters simulation)
    {
        // Could perhaps use a weighted entry model here... the
        // earlier one is listed, the more likely currently (I
        // think). That may be an issue.
        if (random.Next(0, 101) < 50)
        {
            return simulation.GetMembrane("single");
        }

        if (random.Next(0, 101) < 50)
        {
            return simulation.GetMembrane("double");
        }

        if (random.Next(0, 101) < 50)
        {
            return simulation.GetMembrane("cellulose");
        }

        if (random.Next(0, 101) < 50)
        {
            return simulation.GetMembrane("chitin");
        }

        if (random.Next(0, 101) < 50)
        {
            return simulation.GetMembrane("calciumCarbonate");
        }

        return simulation.GetMembrane("silica");
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

    private Color RandomEukaryoteColour(float? opaqueness = null)
    {
        opaqueness ??= RandomOpacity();

        return RandomColour(opaqueness.Value);
    }

    private Color RandomProkaryoteColour(float? opaqueness = null)
    {
        opaqueness ??= RandomOpacityBacteria();

        return RandomColour(opaqueness.Value);
    }

    private Color RandomColour(float opaqueness)
    {
        return new Color(RandomColourChannel(), RandomColourChannel(), RandomColourChannel(),
            opaqueness);
    }

    /// <summary>
    ///   Used to determine if a newly mutated species needs to be in a different genus.
    /// </summary>
    /// <param name="species1">The first species. Function is not order-dependent.</param>
    /// <param name="species2">The second species. Function is not order-dependent.</param>
    /// <returns>True if the two species should be a new genus, false otherwise.</returns>
    private bool NewGenus(MicrobeSpecies species1, MicrobeSpecies species2)
    {
        var species1UniqueOrganelles = species1.Organelles.Select(o => o.Definition).ToHashSet();
        var species2UniqueOrganelles = species2.Organelles.Select(o => o.Definition).ToHashSet();

        return species1UniqueOrganelles.Union(species2UniqueOrganelles).Count()
            - species1UniqueOrganelles.Intersect(species2UniqueOrganelles).Count()
            >= Constants.DIFFERENCES_FOR_GENUS_SPLIT;
    }

    private string MutateWord(string name)
    {
        StringBuilder newName = new StringBuilder(name);
        int changeLimit = 1;
        int letterChangeLimit = 2;
        int letterChanges = 0;
        int changes = 0;

        // Case of 1-letter words, e.g. Primum B - necessary as it otherwise triggers infinite recursion
        if (newName.Length == 1)
        {
            var letter = newName.ToString(0, 1);
            bool isVowel = Vowels.Contains(letter);
            List<string> letterPool;

            // 50% chance to just take another consonant/vowel
            switch (random.Next(0, 3))
            {
                // 33% Chance to replace the letter by a similar one - Primum P
                case 0:
                {
                    letterPool = isVowel ? Vowels : Consonants;
                    newName.Erase(0, 1);
                    newName.Insert(0, letterPool.Random(random));
                    break;
                }

                // 33% Chance to replace the letter by the next similar one - Primum C
                case 1:
                {
                    letterPool = isVowel ? Vowels : Consonants;

                    // Take next letter in the pool (cycle if necessary);
                    var nextIndex = letterPool.FindIndex(item => item == letter) + 1;
                    nextIndex = nextIndex < letterPool.Count ? nextIndex : 0;

                    var nextLetterInPool = letterPool.ElementAt(nextIndex);

                    newName.Erase(0, 1);
                    newName.Insert(0, nextLetterInPool);
                    break;
                }

                // 33% Chance to add a second letter - Primum Ba
                case 2:
                {
                    letterPool = !isVowel ? Vowels : Consonants;
                    newName.Insert(1, letterPool.Random(random));
                    break;
                }
            }

            changes++;
        }

        for (int i = 1; i < newName.Length; ++i)
        {
            if (changes <= changeLimit && i > 1)
            {
                // Index we are adding or erasing chromosomes at
                int index = newName.Length - i - 1;

                // Are we a vowel or are we a consonant?
                var part = newName.ToString(index, 2);
                bool isPermute = PronounceablePermutation.Contains(part);
                if (random.Next(0, 21) <= 10 && isPermute)
                {
                    newName.Erase(index, 2);
                    changes++;
                    newName.Insert(index, PronounceablePermutation.Random(random));
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
                bool isVowel = Vowels.Contains(part);

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
            bool isVowel = Vowels.Contains(part);

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
            // We didnt change our word at all, try recursively until we do
            return MutateWord(name);
        }

        // Convert to lower case
        string lowercase = newName.ToString().ToLower(CultureInfo.InvariantCulture);

        // Convert first letter to upper case
        string result = char.ToUpper(lowercase[0], CultureInfo.InvariantCulture) + lowercase.Substring(1);

        return result;
    }
}
