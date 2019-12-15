#pragma once

#include "run_results.h"

#include "microbe_stage/patch.h"
#include "microbe_stage/species.h"

//! \file Contains helper functions for calling the script sides of auto-evo

namespace thrive { namespace autoevo {

//! \brief Holds data for a species migration
struct SpeciesMigration : public Leviathan::ReferenceCounted {
    // These are protected for only constructing properly reference
    // counted instances through MakeShared
    friend ReferenceCounted;
    SpeciesMigration(Species::pointer species) : species(species) {}

    SpeciesMigration(Species::pointer species,
        int fromPatch,
        int toPatch,
        int population) :
        species(species),
        fromPatch(fromPatch), toPatch(toPatch), population(population)
    {}

public:
    bool
        valid() const
    {
        return species && fromPatch >= 0 && toPatch >= 0 && population > 0;
    }

    // Access helpers for scripts
    // The returned Species (and other objects) have increased ref count
    const Species*
        getSpecies() const;


    REFERENCE_COUNTED_PTR_TYPE(SpeciesMigration);

public:
    Species::pointer species;
    int32_t fromPatch;
    int32_t toPatch;
    int32_t population;
};

//! \brief Configuration for simulating species populations
class SimulationConfiguration : public Leviathan::ReferenceCounted {

    // These are protected for only constructing properly reference
    // counted instances through MakeShared
    friend ReferenceCounted;
    SimulationConfiguration() = default;

public:
    // Access helpers for scripts
    // The returned Species (and other objects) have increased ref count
    uint64_t
        getExcludedSpeciesCount() const;
    Species const*
        getExcludedSpecies(uint64_t index) const;

    uint64_t
        getExtraSpeciesCount() const;
    Species const*
        getExtraSpecies(uint64_t index) const;

    uint64_t
        getMigrationsCount() const;
    const SpeciesMigration*
        getMigration(uint64_t index) const;

    REFERENCE_COUNTED_PTR_TYPE(SimulationConfiguration);

public:
    int32_t steps = 1;

    //! Any species listed here will be excluded from the simulation run
    std::vector<Species::pointer> excludedSpecies;

    //! Extra species to include in the simulation run
    //! \note The per-patch population is taken from the Species global
    //! population property
    std::vector<Species::pointer> extraSpecies;

    //! Any migrations listed here are performed *before* the simulation is ran
    std::vector<SpeciesMigration::pointer> migrations;
};

//! \returns a mutated version of a species
Species::pointer
    getMutationForSpecies(const Species::pointer& species);

//! \brief Applies the gene code and other property changes to a species
void
    applySpeciesMutation(const Species::pointer& species,
        const Species::pointer& mutation);

//! \brief Simulates an entire patch map at once
RunResults::pointer
    simulatePatchMapPopulations(const PatchMap::pointer& map,
        const SimulationConfiguration::pointer& config);

//! \brief Simulates a single patch forwards in time and stores the final
//! populations
//! \note Automatically increments results refcount for scripts
void
    simulatePatchPopulations(const Patch::pointer& patch,
        RunResults& results,
        const SimulationConfiguration::pointer& config);

//! \returns a valid move (or nullptr) for a species
SpeciesMigration::pointer
    getMigrationForSpecies(const PatchMap::pointer& map,
        const Species::pointer& species);

}} // namespace thrive::autoevo
