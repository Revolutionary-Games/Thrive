// ------------------------------------ //
#include "common_steps.h"

#include "auto-evo_script_helpers.h"

using namespace thrive;
using namespace autoevo;
// ------------------------------------ //
// LambdaStep
LambdaStep::LambdaStep(std::function<void(RunResults&)> operation) :
    m_operation(operation)
{}
// ------------------------------------ //
// FindBestMutation
FindBestMutation::FindBestMutation(const PatchMap::pointer& map,
    const Species::pointer& species,
    int mutationsToTry,
    bool allowNoMutation /*= true*/) :
    m_map(map),
    m_species(species), m_tryNoMutation(allowNoMutation),
    m_mutationsToTry(mutationsToTry)
{}
// ------------------------------------ //
bool
    FindBestMutation::step(RunResults& resultsStore)
{
    bool ran = false;

    if(m_tryNoMutation) {

        auto config =
            SimulationConfiguration::MakeShared<SimulationConfiguration>();
        config->steps = 10;

        const auto result = simulatePatchMapPopulations(m_map, config);

        const int population = result->getGlobalPopulation(m_species);

        if(population > m_bestScore) {

            m_bestScore = population;
            m_bestIsNoMutation = true;
            m_bestMutation = nullptr;
        }

        m_tryNoMutation = false;
        ran = true;
    }

    if(m_mutationsToTry > 0 && !ran) {

        const auto mutated = getMutationForSpecies(m_species);

        auto config =
            SimulationConfiguration::MakeShared<SimulationConfiguration>();
        config->steps = 10;
        config->excludedSpecies.push_back(m_species);
        config->extraSpecies.push_back(mutated);

        const auto result = simulatePatchMapPopulations(m_map, config);

        const int population = result->getGlobalPopulation(mutated);

        if(population > m_bestScore) {

            m_bestScore = population;
            m_bestIsNoMutation = false;
            m_bestMutation = mutated;
        }

        --m_mutationsToTry;
        ran = true;
    }


    if(!m_tryNoMutation && m_mutationsToTry <= 0) {
        return true;
    } else {
        return false;
    }
}
// ------------------------------------ //
int
    FindBestMutation::getTotalSteps() const
{
    return (m_tryNoMutation ? 1 : 0) + m_mutationsToTry;
}
// ------------------------------------ //
// CalculatePopulation
CalculatePopulation::CalculatePopulation(const PatchMap::pointer& map)
{
    const auto& patches = map->getPatches();
    m_patches.reserve(patches.size());

    for(const auto& [id, patch] : patches) {

        m_patches.push_back(patch);
    }
}
// ------------------------------------ //
bool
    CalculatePopulation::step(RunResults& resultsStore)
{
    if(m_currentPatchIndex > m_patches.size()) {
        LOG_ERROR("Invalid patch index in CalculatePopulation: " +
                  std::to_string(m_currentPatchIndex));
        return true;
    }

    const auto& patch = m_patches[m_currentPatchIndex];

    simulatePatchPopulations(patch, resultsStore,
        SimulationConfiguration::MakeShared<SimulationConfiguration>());

    ++m_currentPatchIndex;

    return m_currentPatchIndex >= m_patches.size();
}
// ------------------------------------ //
int
    CalculatePopulation::getTotalSteps() const
{
    return static_cast<int>(m_patches.size());
}
