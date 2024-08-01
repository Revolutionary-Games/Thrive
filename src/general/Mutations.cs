using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

/// <summary>
///   Generates mutations for species
/// </summary>
public class Mutations
{
    private static readonly List<string> Vowels = new()
    {
        "a", "e", "i", "o", "u",
    };

    private static readonly List<string> PronounceablePermutation = new()
    {
        "th", "sh", "ch", "wh", "Th", "Sh", "Ch", "Wh",
    };

    private static readonly List<string> Consonants = new()
    {
        "b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
        "n", "p", "q", "s", "t", "v", "w", "x", "y", "z",
    };

    [JsonProperty]
    private XoShiRo256starstar random;

    [JsonConstructor]
    public Mutations(XoShiRo256starstar random)
    {
        this.random = random;
    }

    public Mutations()
    {
        random = new XoShiRo256starstar();
    }

    // TODO: proper unit testing (note the code here relies on Godot colour type, and loading the thrive simulation
    // parameters). See: https://github.com/Revolutionary-Games/Thrive/issues/4963
    public static void TestConsistentGenerationWithSeed()
    {
        int seed = 234234565;
        int steps = 10;

        var workMemory = new MutationWorkMemory();

        string firstSpecies;
        string secondSpecies;
        string differentSpecies;

        // Create the mutated species
        {
            var mutator = new Mutations(new XoShiRo256starstar(seed));

            var species =
                mutator.CreateRandomSpecies(new MicrobeSpecies(1, "Test", "species"), 1, true, workMemory, steps);

            firstSpecies = species.StringCode;
        }

        {
            var mutator = new Mutations(new XoShiRo256starstar(seed + 1));

            var species =
                mutator.CreateRandomSpecies(new MicrobeSpecies(1, "Test", "species"), 1, true, workMemory, steps);

            secondSpecies = species.StringCode;
        }

        {
            var mutator = new Mutations(new XoShiRo256starstar(seed + 2));

            var species =
                mutator.CreateRandomSpecies(new MicrobeSpecies(1, "Test", "species"), 1, true, workMemory, steps);

            differentSpecies = species.StringCode;
        }

        // Compare their JSON serialized forms to ensure that the seed resulted in the same mutations each time
        if (firstSpecies != secondSpecies)
            throw new Exception("Mutations from the same seed didn't result in the same generated species");

        if (firstSpecies == differentSpecies)
            throw new Exception("Different seeds resulted in same mutations");
    }

    /// <summary>
    ///   Creates a mutated version of a species
    /// </summary>
    public MicrobeSpecies CreateMutatedSpecies(MicrobeSpecies parent, MicrobeSpecies mutated, float creationRate,
        bool lawkOnly, MutationWorkMemory workMemory)
    {
        if (parent.Organelles.Count < 1)
        {
            throw new ArgumentException("Can't create a mutated version of an empty species");
        }

        if (mutated.PlayerSpecies)
        {
            // This is mostly a sanity check against bugs elsewhere in the code
            throw new ArgumentException("Don't mutate the player species");
        }

        var simulation = SimulationParameters.Instance;
        var nameGenerator = simulation.NameGenerator;

        // Keeps track of how "evolved" from the starting species, this species is
        mutated.Generation = parent.Generation + 1;

        mutated.IsBacteria = parent.IsBacteria;

        // Mutate the epithet
        if (random.Next(0, 100) < Constants.MUTATION_WORD_EDIT)
        {
            mutated.Epithet = MutateWord(parent.Epithet, true);
        }
        else
        {
            mutated.Epithet = nameGenerator.GenerateNameSection(random, true);
        }

        MutateBehaviour(parent, mutated);

        MutateMicrobeOrganelles(parent.Organelles, mutated.Organelles, mutated.IsBacteria, creationRate, lawkOnly,
            workMemory);

        // Update the genus if the new species is different enough
        if (NewGenus(mutated, parent))
        {
            // We can do more fun stuff here later
            if (random.Next(0, 100) < Constants.MUTATION_WORD_EDIT)
            {
                mutated.Genus = MutateWord(parent.Genus);
            }
            else
            {
                mutated.Genus = nameGenerator.GenerateNameSection(random);
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
        if (random.Next(0, 100) < 20)
        {
            mutated.MembraneType = RandomMembraneType(simulation);
            if (mutated.MembraneType != simulation.GetMembrane("single"))
            {
                colour.A = RandomOpacityChitin();
            }
        }
        else
        {
            mutated.MembraneType = parent.MembraneType;
        }

        mutated.Colour = colour;

        mutated.MembraneRigidity = Math.Max(Math.Min(parent.MembraneRigidity +
            random.Next(-25, 26) / 100.0f, 1), -1);

        mutated.OnEdited();

        return mutated;
    }

    /// <summary>
    ///   Creates a fully random species starting with one cytoplasm
    /// </summary>
    public MicrobeSpecies CreateRandomSpecies(MicrobeSpecies mutated, float creationRate, bool lawkOnly,
        MutationWorkMemory workMemory, int steps = 5)
    {
        // Temporarily create species with just cytoplasm to start mutating from
        var temp = new MicrobeSpecies(int.MaxValue, string.Empty, string.Empty);

        GameWorld.SetInitialSpeciesProperties(temp, workMemory.WorkingMemory1, workMemory.WorkingMemory2);

        // Override the default species starting name to have more variability in the names
        var nameGenerator = SimulationParameters.Instance.NameGenerator;
        temp.Epithet = nameGenerator.GenerateNameSection(random, true);
        temp.Genus = nameGenerator.GenerateNameSection(random);

        for (int step = 0; step < steps; ++step)
        {
            CreateMutatedSpecies(temp, mutated, creationRate, lawkOnly, workMemory);

            temp = (MicrobeSpecies)mutated.Clone();
        }

        mutated.OnEdited();

        return mutated;
    }

    private static bool IsPermute(StringBuilder newName, int index)
    {
        var part1 = newName.ToString(index - 1, 2);
        var part2 = newName.ToString(index - 2, 2);
        var part3 = newName.ToString(index, 2);

        return PronounceablePermutation.Contains(part1) ||
            PronounceablePermutation.Contains(part2) ||
            PronounceablePermutation.Contains(part3);
    }

    private void MutateBehaviour(Species parent, Species mutated)
    {
        mutated.Behaviour = parent.Behaviour.CloneObject();
        mutated.Behaviour.Mutate(random);
    }

    /// <summary>
    ///   Creates a mutated version of parentOrganelles in organelles
    /// </summary>
    private void MutateMicrobeOrganelles(OrganelleLayout<OrganelleTemplate> parentOrganelles,
        OrganelleLayout<OrganelleTemplate> mutatedOrganelles, bool isBacteria, float creationRate, bool lawkOnly,
        MutationWorkMemory workMemory)
    {
        var nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

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
                    organelle = new OrganelleTemplate(GetRandomOrganelle(isBacteria, lawkOnly),
                        organelle.Position, organelle.Orientation);
                }
            }

            // Copy the organelle
            if (mutatedOrganelles.CanPlace(organelle, workMemory.WorkingMemory1, workMemory.WorkingMemory2))
            {
                mutatedOrganelles.AddFast(organelle, workMemory.WorkingMemory1, workMemory.WorkingMemory2);
            }
            else
            {
                // Add the organelle randomly back to the list to make sure we don't throw it away. Note that this
                // can still fail if unable to find any valid position.
                AddNewOrganelle(mutatedOrganelles, organelle.Definition, workMemory.WorkingMemory1,
                    workMemory.WorkingMemory2);
            }
        }

        // We can insert new organelles at the end of the list
        for (int i = 0; i < Math.Ceiling(6 * creationRate); ++i)
        {
            if (random.Next(0.0f, 1.0f) < Constants.MUTATION_CREATION_RATE)
            {
                if (random.Next(0.0f, 1.0f) < Constants.MUTATION_NEW_ORGANELLE_CHANCE)
                {
                    AddNewOrganelle(mutatedOrganelles, GetRandomOrganelle(isBacteria, lawkOnly),
                        workMemory.WorkingMemory1, workMemory.WorkingMemory2);
                }
                else
                {
                    // Duplicate an existing organelle, but only if there are any organelles where that is legal
                    var organellesThatCanBeDuplicated =
                        parentOrganelles.Organelles.Where(o => !o.Definition.Unique).ToList();
                    if (organellesThatCanBeDuplicated.Any())
                    {
                        AddNewOrganelle(mutatedOrganelles,
                            organellesThatCanBeDuplicated.Random(random).Definition, workMemory.WorkingMemory1,
                            workMemory.WorkingMemory2);
                    }
                    else
                    {
                        AddNewOrganelle(mutatedOrganelles, GetRandomOrganelle(isBacteria, lawkOnly),
                            workMemory.WorkingMemory1, workMemory.WorkingMemory2);
                    }
                }
            }
        }

        if (isBacteria)
        {
            if (random.Next(0.0f, 1.0f) <= Constants.MUTATION_BACTERIA_TO_EUKARYOTE)
            {
                AddNewOrganelle(mutatedOrganelles, nucleus, workMemory.WorkingMemory1, workMemory.WorkingMemory2);
            }
        }

        // Disallow creating empty species as that throws an exception when trying to spawn
        if (mutatedOrganelles.Count < 1)
        {
            // Add the first parent species organelle
            AddNewOrganelle(mutatedOrganelles, parentOrganelles[0].Definition, workMemory.WorkingMemory1,
                workMemory.WorkingMemory2);

            // If still empty, copy the first organelle of the parent
            if (mutatedOrganelles.Count < 1)
            {
                mutatedOrganelles.AddFast((OrganelleTemplate)parentOrganelles[0].Clone(), workMemory.WorkingMemory1,
                    workMemory.WorkingMemory2);
            }
        }

        var islandHexes = workMemory.WorkingMemory2;
        mutatedOrganelles.GetIslandHexes(islandHexes, workMemory.WorkingMemory3, workMemory.WorkingMemory2,
            workMemory.WorkingMemory4);

        // Attach islands
        while (islandHexes.Count > 0)
        {
            mutatedOrganelles.ComputeHexCache(workMemory.WorkingMemory3, workMemory.WorkingMemory2);

            // TODO: skip this enumerator allocation
            var mainHexes = workMemory.WorkingMemory3.Except(islandHexes);

            // Compute the shortest hex distance
            Hex minSubHex = default;
            int minDistance = int.MaxValue;
            foreach (var mainHex in mainHexes)
            {
                foreach (var islandHex in islandHexes)
                {
                    var sub = islandHex - mainHex;
                    int distance = (Math.Abs(sub.Q) + Math.Abs(sub.Q + sub.R) + Math.Abs(sub.R)) / 2;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minSubHex = sub;

                        // early exit if minDistance == 2 (distance 1 == direct neighbour => not an island)
                        if (minDistance == 2)
                            break;
                    }
                }

                // early exit if minDistance == 2 (distance 1 == direct neighbour => not an island)
                if (minDistance == 2)
                    break;
            }

            // Calculate the path to move island organelles.
            // If statement is there because otherwise the path could be (0, 0).
            if (minSubHex.Q != minSubHex.R)
                minSubHex.Q = (int)(minSubHex.Q * (minDistance - 1.0) / minDistance);

            minSubHex.R = (int)(minSubHex.R * (minDistance - 1.0) / minDistance);

            // Move all island organelles by minSubHex
            // TODO: remove the lambdas from here that allocate memory and the LINQ usage
            foreach (var organelle in mutatedOrganelles.Where(o => islandHexes.Any(h =>
                         o.Definition.GetRotatedHexes(o.Orientation).Contains(h - o.Position))))
            {
                organelle.Position -= minSubHex;
            }

            mutatedOrganelles.GetIslandHexes(islandHexes, workMemory.WorkingMemory3, workMemory.WorkingMemory2,
                workMemory.WorkingMemory4);
        }
    }

    /// <summary>
    ///   Adds a new organelle to a mutation result (but only if possible to place)
    /// </summary>
    private void AddNewOrganelle(OrganelleLayout<OrganelleTemplate> organelles,
        OrganelleDefinition organelle, List<Hex> workMemory1, List<Hex> workMemory2)
    {
        var hex = GetRealisticPosition(organelle, organelles, workMemory1, workMemory2);

        if (hex == null || !organelles.CanPlace(hex, workMemory1, workMemory2))
        {
            // Failing to add a mutation is not serious
            return;
        }

        // There's no longer an exception catch here as we check above that the placement is valid. The catch on the
        // expected path was removed to make the mutations code cleaner and hopefully less prone to locking things up.
        organelles.AddFast(hex, workMemory1, workMemory2);
    }

    private OrganelleDefinition GetRandomOrganelle(bool isBacteria, bool lawkOnly)
    {
        if (isBacteria)
        {
            return SimulationParameters.Instance.GetRandomProkaryoticOrganelle(random, lawkOnly);
        }

        return SimulationParameters.Instance.GetRandomEukaryoticOrganelle(random, lawkOnly);
    }

    private OrganelleTemplate? GetRealisticPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles, List<Hex> workMemory1, List<Hex> workMemory2)
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

                            if (existingOrganelles.CanPlace(result, workMemory1, workMemory2))
                            {
                                return result;
                            }
                        }
                    }
                }
            }
        }

        // Not good to signal normal program flow with exceptions so this now returns null when not being able to find
        // a position
        return null;
    }

    private MembraneType RandomMembraneType(SimulationParameters simulation)
    {
        // Could perhaps use a weighted entry model here... the
        // earlier one is listed, the more likely currently (I
        // think). That may be an issue.
        if (random.Next(0, 100) < 50)
        {
            return simulation.GetMembrane("single");
        }

        if (random.Next(0, 100) < 50)
        {
            return simulation.GetMembrane("double");
        }

        if (random.Next(0, 100) < 50)
        {
            return simulation.GetMembrane("cellulose");
        }

        if (random.Next(0, 100) < 50)
        {
            return simulation.GetMembrane("chitin");
        }

        if (random.Next(0, 100) < 50)
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

    private string MutateWord(string name, bool lowercase = false)
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
                    newName.Remove(0, 1);
                    newName.Insert(0, letterPool.Random(random));
                    break;
                }

                // 33% Chance to replace the letter by the next similar one - Primum C
                case 1:
                {
                    letterPool = isVowel ? Vowels : Consonants;

                    // Take next letter in the pool (cycle if necessary);
                    var nextIndex = letterPool.FindIndex(l => l == letter) + 1;
                    nextIndex = nextIndex < letterPool.Count ? nextIndex : 0;

                    var nextLetterInPool = letterPool.ElementAt(nextIndex);

                    newName.Remove(0, 1);
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
                if (random.Next(0, 20) < 10 && isPermute)
                {
                    newName.Remove(index, 2);
                    changes++;
                    newName.Insert(index, PronounceablePermutation.Random(random));
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
                    newName.Remove(index, 1);
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
                    newName.Remove(index, 1);
                    changes++;
                    if (random.Next(0, 20) < 10)
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
            if (random.Next(0, 20) < 10 && changes <= changeLimit)
            {
                if (!isVowel && newName.ToString(index, 1) != "r" && !isPermute)
                {
                    newName.Remove(index, 1);
                    letterChanges++;
                    newName.Insert(index, Consonants.Random(random));
                }
                else if (!isPermute)
                {
                    newName.Remove(index, 1);
                    letterChanges++;
                    newName.Insert(index, Vowels.Random(random));
                }
            }
        }

        // Our base case
        if (letterChanges < letterChangeLimit && changes == 0)
        {
            // We didn't change our word at all, try recursively until we do
            return MutateWord(name, lowercase);
        }

        // Convert to lower case
        string lower = newName.ToString().ToLower(CultureInfo.InvariantCulture);

        if (lowercase)
            return lower;

        // Convert first letter to upper case
        string result = char.ToUpper(lower[0], CultureInfo.InvariantCulture) + lower.Substring(1);

        return result;
    }
}
