// Implements an algorithm for AI species to select beneficial migrations
// Note: this should be relative cheap and shouldn't be perfect. This is executed
// multiple times and the best migration (with the most resulting global population)
// is selected

//! Just finds a random patch this species can spread from
//! \returns False if no migration could be created
bool createMigrationForSpecies(const Species@ species, const PatchMap@ map,
    SpeciesMigration@ result)
{
    auto@ patches = map.getPatches();

    int attemptsLeft = 5;

    while(attemptsLeft > 0){
        // This isn't perfectly random but this is good enough for now
        int randomStart = GetEngine().GetRandom().GetNumber(0, patches.length() - 1);

        if(attemptsLeft == 1)
            randomStart = 0;

        for(uint i = randomStart; i < patches.length(); ++i){

            const auto patch = patches[i];

            const auto population = patch.getSpeciesPopulation(species);
            if(population < AUTO_EVO_MINIMUM_MOVE_POPULATION)
                continue;

            // Select a random adjacent target patch
            auto adjacent = patch.getNeighbours();

            if(adjacent.length() < 1)
                continue;

            // TODO: could prefer patches this species is not already
            // in or about to go extinct, or really anything other
            // than random selection
            const auto target = adjacent[GetEngine().GetRandom().GetNumber(
                    0, adjacent.length() - 1)];

            // Calculate random amount of population to send
            int moveAmount = int(GetEngine().GetRandom().GetFloat(
                    population * AUTO_EVO_MINIMUM_MOVE_POPULATION_FRACTION,
                    population * AUTO_EVO_MAXIMUM_MOVE_POPULATION_FRACTION));

            if(moveAmount > 0){
                // Move is a success
                result.fromPatch = patch.getId();
                result.toPatch = target;
                result.population = moveAmount;
                return true;
            }
        }

        --attemptsLeft;
    }

    // Could not find a valid move
    return false;
}
