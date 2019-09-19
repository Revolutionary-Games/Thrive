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
    for(auto& entry : m_results) {
        if(entry.species == species) {

            entry.mutatedProperties = mutated;
            return;
        }
    }

    SpeciesResult result{species};
    result.mutatedProperties = mutated;

    m_results.push_back(result);
}

void
    RunResults::addPopulationResultForSpecies(const Species::pointer& species,
        int32_t patch,
        int newPopulation)
{
    for(auto& entry : m_results) {
        if(entry.species == species) {

            entry.newPopulationInPatches[patch] = newPopulation;
            return;
        }
    }

    SpeciesResult result{species};
    result.newPopulationInPatches[patch] = newPopulation;

    m_results.push_back(result);
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

        if(!entry.spreadPatches.empty()) {
            LOG_ERROR("TODO: spreadPatches applying is not done");
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

    for(const auto& entry : m_results) {

        LOG_WRITE(entry.species->getFormattedName(true) + ":");

        if(entry.mutatedProperties) {
            LOG_WRITE(" has a mutation, gene code: " +
                      entry.mutatedProperties->stringCode);
        }

        if(!entry.spreadPatches.empty()) {
            LOG_WRITE(" spread to patches: ");
            for(const auto [patch, population] : entry.spreadPatches)
                LOG_WRITE("  " + std::to_string(patch) +
                          " pop: " + std::to_string(population));
        }

        LOG_WRITE(" population in patches: ");
        for(const auto [patch, population] : entry.newPopulationInPatches) {
            std::stringstream sstream;
            sstream << "  " << patch;

            Patch::pointer patchObj;

            if(previousPopulations) {
                patchObj = previousPopulations->getPatch(patch);

                sstream << " (";
                if(patchObj) {
                    sstream << patchObj->getName();
                } else {
                    sstream << "error";
                }

                sstream << ")";
            }

            sstream << " pop: " << population;

            if(patchObj) {

                sstream << " previous: "
                        << patchObj->getSpeciesPopulation(entry.species);
            }

            LOG_WRITE(sstream.str());
        }
    }

    LOG_INFO("End of results summary");
}
