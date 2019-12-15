#pragma once

#include "auto-evo_script_helpers.h"
#include "run_results.h"
#include "run_step.h"

#include <future>

namespace thrive { namespace autoevo {

//! \brief Generic step for one-off things
//!
//! If a similar step is used multiple times it should be made into a class
class LambdaStep : public RunStep {
public:
    LambdaStep(std::function<void(RunResults&)> operation);

    bool
        step(RunResults& resultsStore) override
    {
        m_operation(resultsStore);
        return true;
    }

    int
        getTotalSteps() const override
    {
        return 1;
    }

private:
    std::function<void(RunResults&)> m_operation;
};


//! \brief Step that finds the best mutation for a single species
class FindBestMutation : public RunStep {
public:
    FindBestMutation(const PatchMap::pointer& map,
        const Species::pointer& species,
        int mutationsToTry,
        bool allowNoMutation = true);

    bool
        step(RunResults& resultsStore) override;

    int
        getTotalSteps() const override;

private:
    const PatchMap::pointer m_map;
    const Species::pointer m_species;
    bool m_tryNoMutation;
    int m_mutationsToTry;

    Species::pointer m_bestMutation;
    int m_bestScore = -1;
    bool m_bestIsNoMutation = false;
};

//! \brief Step that finds the best migration for a single species
class FindBestMigration : public RunStep {
public:
    FindBestMigration(const PatchMap::pointer& map,
        const Species::pointer& species,
        int migrationsToTry);

    bool
        step(RunResults& resultsStore) override;

    int
        getTotalSteps() const override;

private:
    const PatchMap::pointer m_map;
    const Species::pointer m_species;
    int m_migrationsToTry;

    SpeciesMigration::pointer m_bestMigration;
    int m_bestScore = -1;
};


//! \brief Step that calculate the populations for all species
class CalculatePopulation : public RunStep {
public:
    CalculatePopulation(const PatchMap::pointer& map);

    bool
        step(RunResults& resultsStore) override;

    int
        getTotalSteps() const override;

private:
    std::vector<Patch::pointer> m_patches;
    size_t m_currentPatchIndex = 0;
};

}} // namespace thrive::autoevo
