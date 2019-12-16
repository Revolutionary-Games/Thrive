// ------------------------------------ //
#include "common_steps.h"

#include "auto-evo_script_helpers.h"

using namespace thrive;
using namespace autoevo;
// ------------------------------------ //
constexpr auto STEPS_TO_SIMULATE_FOR = 10;
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
        config->steps = STEPS_TO_SIMULATE_FOR;

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
        config->steps = STEPS_TO_SIMULATE_FOR;
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
        // Store the best result
        if(m_bestIsNoMutation) {
            resultsStore.addMutationResultForSpecies(m_species, nullptr);
        } else {
            resultsStore.addMutationResultForSpecies(m_species, m_bestMutation);
        }

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
// FindBestMigration
FindBestMigration::FindBestMigration(const PatchMap::pointer& map,
    const Species::pointer& species,
    int migrationsToTry,
    bool allowNoMigration /*= true*/) :
    m_map(map),
    m_species(species), m_tryNoMigration(allowNoMigration),
    m_migrationsToTry(migrationsToTry)
{}
// ------------------------------------ //
bool
    FindBestMigration::step(RunResults& resultsStore)
{
    bool ran = false;
    if(m_tryNoMigration) {

        // TODO: this is duplicate work compared to no mutation score
        // computation so maybe there is a way to compute this score just once
        // and share it. However if the way the migration is computed is changed
        // this also needs to save the per patch results...
        auto config =
            SimulationConfiguration::MakeShared<SimulationConfiguration>();
        config->steps = STEPS_TO_SIMULATE_FOR;

        const auto result = simulatePatchMapPopulations(m_map, config);

        const int population = result->getGlobalPopulation(m_species);

        if(population > m_bestScore) {

            m_bestScore = population;
            m_bestMigration = nullptr;
        }

        m_tryNoMigration = false;
        ran = true;
    }


    if(m_migrationsToTry > 0 && !ran) {
        const auto migration = getMigrationForSpecies(m_map, m_species);

        if(!migration) {
            // Did not find a migration, this was a failed attempt
            LOG_INFO(
                "Auto-evo migration generation failed, skipping this step");
        } else {

            auto config =
                SimulationConfiguration::MakeShared<SimulationConfiguration>();
            config->steps = STEPS_TO_SIMULATE_FOR;
            config->migrations.push_back(migration);

            // TODO: this could be faster to just simulate the source and
            // destination patches (assuming in the future no global effects of
            // migrations are added, which would need a full patch map
            // simulation anyway)
            const auto result = simulatePatchMapPopulations(m_map, config);

            const int population = result->getGlobalPopulation(m_species);

            if(population > m_bestScore) {

                m_bestScore = population;
                m_bestMigration = migration;
            }
        }

        --m_migrationsToTry;
        ran = true;
    }

    if(m_migrationsToTry <= 0 && !m_tryNoMigration) {
        // All attempts exhausted

        // Store the best result
        if(m_bestMigration) {
            resultsStore.addMigrationResultForSpecies(m_species,
                m_bestMigration->fromPatch, m_bestMigration->toPatch,
                m_bestMigration->population);
        }

        return true;
    } else {
        return false;
    }
}
// ------------------------------------ //
int
    FindBestMigration::getTotalSteps() const
{
    return m_migrationsToTry + (m_tryNoMigration ? 1 : 0);
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
