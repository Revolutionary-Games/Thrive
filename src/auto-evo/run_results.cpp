// ------------------------------------ //
#include "run_results.h"

#include "auto-evo_script_helpers.h"

using namespace thrive;
using namespace autoevo;
// ------------------------------------ //
RunResults*
    RunResults::factory()
{
    return new RunResults();
}
// ------------------------------------ //
void
    RunResults::addMutationResultForSpecies(const Species::pointer& species,
        const Species::pointer& mutated)
{
    makeSureResultExistsForSpecies(species);

    for(auto& entry : m_results) {
        if(entry.species == species) {

            entry.mutatedProperties = mutated;
            return;
        }
    }

    LOG_ERROR("RunResults: add result logic error, no matching species found");
}

void
    RunResults::addPopulationResultForSpecies(const Species::pointer& species,
        int32_t patch,
        int newPopulation)
{
    makeSureResultExistsForSpecies(species);

    for(auto& entry : m_results) {
        if(entry.species == species) {

            entry.newPopulationInPatches[patch] = newPopulation;
            return;
        }
    }

    LOG_ERROR("RunResults: add result logic error, no matching species found");
}

void
    RunResults::addMigrationResultForSpecies(const Species::pointer& species,
        int32_t fromPatch,
        int32_t toPatch,
        int populationAmount)
{
    makeSureResultExistsForSpecies(species);

    for(auto& entry : m_results) {
        if(entry.species == species) {

            entry.spreadToPatches.push_back(
                std::make_tuple(fromPatch, toPatch, populationAmount));
            return;
        }
    }

    LOG_ERROR("RunResults: add result logic error, no matching species found");
}
// ------------------------------------ //
void
    RunResults::applyResults(const PatchMap::pointer& map, bool skipMutations)
{
    for(const auto& entry : m_results) {

        if(!skipMutations && entry.mutatedProperties) {
            LOG_INFO("Applying mutation to species: " + entry.species->name);
            applySpeciesMutation(entry.species, entry.mutatedProperties);
        }

        for(const auto [patchId, population] : entry.newPopulationInPatches) {

            auto patch = map->getPatch(patchId);

            if(patch) {
                if(!patch->updateSpeciesPopulation(entry.species, population)) {
                    LOG_ERROR("RunResults failed to update population for a "
                              "species in a patch");
                }

            } else {
                LOG_ERROR("RunResults has a species population change in a "
                          "patch with invalid id: " +
                          std::to_string(patchId));
            }
        }

        for(const auto [fromPatch, toPatch, amount] : entry.spreadToPatches) {

            auto from = map->getPatch(fromPatch);
            auto to = map->getPatch(toPatch);

            if(!from || !to) {
                LOG_ERROR("RunResults has a species migration to/from a "
                          "patch with invalid id: " +
                          std::to_string(fromPatch) + ", " +
                          std::to_string(toPatch));
            }

            const auto remainingPopulation =
                from->getSpeciesPopulation(entry.species) - amount;
            const auto newPopulation =
                to->getSpeciesPopulation(entry.species) + amount;

            if(!from->updateSpeciesPopulation(
                   entry.species, remainingPopulation)) {
                LOG_ERROR("RunResults failed to update population for a "
                          "species in a patch it moved from");
            }

            if(!to->updateSpeciesPopulation(entry.species, newPopulation)) {

                if(!to->addSpecies(entry.species, newPopulation)) {

                    LOG_ERROR("RunResults failed to update population and also "
                              "add species failed on migration target patch");
                }
            }
        }
    }
}
// ------------------------------------ //
int
    RunResults::getGlobalPopulation(const Species::pointer& species) const
{
    for(const auto& entry : m_results) {
        if(entry.species == species) {

            int result = 0;

            for(const auto [patch, population] : entry.newPopulationInPatches) {

                result += std::max(population, 0);
            }

            return result;
        }
    }

    throw InvalidArgument("no population found for requested species");
}

int
    RunResults::getPopulationInPatch(const Species::pointer& species,
        int32_t patch) const
{
    for(const auto& entry : m_results) {
        if(entry.species == species) {
            for(const auto [currentPatch, population] :
                entry.newPopulationInPatches) {

                if(currentPatch == patch)
                    return population;
            }

            break;
        }
    }

    throw InvalidArgument("no population found for requested species");
}
// ------------------------------------ //
void
    RunResults::printSummary(
        const PatchMap::pointer& previousPopulations /*= nullptr*/) const
{
    LOG_INFO("Start of auto-evo results summary (entries: " +
             std::to_string(m_results.size()) + ")");

    LOG_WRITE(makeSummary(previousPopulations, false));

    LOG_INFO("End of results summary");
}

std::string
    patchNameResolveHelper(const PatchMap::pointer& patches, int32_t patchId)
{
    if(!patches)
        return std::to_string(patchId);

    const auto patch = patches->getPatch(patchId);

    if(!patch)
        return std::to_string(patchId) + " (invalid)";

    return patch->getName();
}

std::string
    RunResults::makeSummary(
        const PatchMap::pointer& previousPopulations /*= nullptr*/,
        bool playerReadable /*= false*/) const
{
    bool resolveMoves = true;

    std::stringstream sstream;

    const auto outputPopulationForPatch = [&](const Species::pointer& species,
                                              int32_t patch, int population) {
        sstream << "  ";

        if(!playerReadable) {
            sstream << patch;
        }

        sstream << " " << patchNameResolveHelper(previousPopulations, patch);

        Patch::pointer patchObj;

        if(previousPopulations) {
            patchObj = previousPopulations->getPatch(patch);
        }

        sstream << " population: " << population;

        if(patchObj) {

            sstream << " previous: " << patchObj->getSpeciesPopulation(species);
        }

        sstream << "\n";
    };

    for(const auto& entry : m_results) {

        sstream << entry.species->getFormattedName(!playerReadable) << ":"
                << "\n";

        if(entry.mutatedProperties) {
            sstream << " has a mutation";

            if(!playerReadable)
                sstream << ", gene code: "
                        << entry.mutatedProperties->stringCode;

            sstream << "\n";
        }

        if(!entry.spreadToPatches.empty()) {
            sstream << " spread to patches:\n";

            for(const auto [from, to, population] : entry.spreadToPatches) {
                if(playerReadable) {
                    sstream
                        << "  "
                        << patchNameResolveHelper(previousPopulations, to)
                        << " by sending: " << population << " population"
                        << " from patch: "
                        << patchNameResolveHelper(previousPopulations, from);
                } else {
                    sstream << "  " << to << " pop: " << population
                            << " from: " << from;
                }
                sstream << "\n";
            }
        }

        sstream << " population in patches:\n";
        for(const auto [patch, population] : entry.newPopulationInPatches) {

            auto adjustedPopulation = population;

            if(resolveMoves) {
                adjustedPopulation +=
                    countSpeciesSpreadPopulation(entry.species, patch);
            }

            outputPopulationForPatch(entry.species, patch, adjustedPopulation);
        }

        // Also print new patches the species moved to (as the moves don't get
        // included in newPopulationinpatches
        if(resolveMoves) {
            for(const auto [unused1, to, unused2] : entry.spreadToPatches) {
                UNUSED(unused1);
                UNUSED(unused2);

                bool found = false;

                for(const auto [patch, unused] : entry.newPopulationInPatches) {
                    UNUSED(unused);

                    if(patch == to) {
                        found = true;
                        break;
                    }
                }

                if(!found) {
                    outputPopulationForPatch(entry.species, to,
                        countSpeciesSpreadPopulation(entry.species, to));
                }
            }
        }

        if(playerReadable)
            sstream << "\n";
    }

    return sstream.str();
}
// ------------------------------------ //
void
    RunResults::makeSureResultExistsForSpecies(const Species::pointer& species)
{
    for(const auto& entry : m_results) {
        if(entry.species == species) {
            return;
        }
    }

    m_results.emplace_back(SpeciesResult{species});
}

int
    RunResults::countSpeciesSpreadPopulation(const Species::pointer& species,
        int32_t targetPatch) const
{
    int totalPopulation = 0;

    for(const auto& entry : m_results) {

        if(entry.species != species)
            continue;
        for(const auto [from, to, population] : entry.spreadToPatches) {

            if(from == targetPatch) {
                totalPopulation -= population;
            } else if(to == targetPatch) {
                totalPopulation += population;
            }
        }

        return totalPopulation;
    }

    LOG_ERROR("RunResults: no species entry found for counting spread "
              "population");
    return -1;
}
