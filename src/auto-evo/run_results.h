#pragma once

#include "microbe_stage/patch.h"
#include "microbe_stage/species.h"

#include <Common/ReferenceCounted.h>

namespace thrive { namespace autoevo {

class RunResults;



//! \brief Container for results before they are applied.
//!
//! This is needed as earlier parts of an auto-evo run may not affect the latter
//! parts
class RunResults : public Leviathan::ReferenceCounted {
public:
    struct SpeciesResult {
        Species::pointer species;

        std::map<int32_t, int> newPopulationInPatches;

        //! \note null means no changes
        Species::pointer mutatedProperties;

        //! List of patches this species has spread to
        //! \todo Doesn't do anything yet
        //! The first part of the tuple is the patch id, the second is the
        //! population in that patch
        std::vector<std::tuple<int32_t, int>> spreadPatches;
    };

protected:
    // These are protected for only constructing properly reference
    // counted instances through MakeShared
    friend ReferenceCounted;
    friend RunResults*
        runResultsFactory();

    RunResults() = default;

public:
    void
        addMutationResultForSpecies(const Species::pointer& species,
            const Species::pointer& mutated);

    void
        addPopulationResultForSpecies(const Species::pointer& species,
            int32_t patch,
            int newPopulation);

    void
        applyResults(const PatchMap::pointer& map, bool skipMutations);

    //! \brief Sums up the populations of a species (ignores negative
    //! population)
    //! \exception Leviathan::InvalidArgument if no population is found for the
    //! species
    int
        getGlobalPopulation(const Species::pointer& species) const;

    //! variant of getGlobalPopulation for a single patch
    int
        getPopulationInPatch(const Species::pointer& species,
            int32_t patch) const;

    //! \brief Prints to log a summary of the results
    void
        printSummary(
            const PatchMap::pointer& previousPopulations = nullptr) const;

    //! \brief Makes summary text
    //! \param playerReadable if true ids are removed from the output
    std::string
        makeSummary(const PatchMap::pointer& previousPopulations = nullptr,
            bool playerReadable = false) const;

    void
        addPopulationResultForSpeciesWrapper(Species* species,
            int32_t patch,
            int32_t newPopulation)
    {
        addPopulationResultForSpecies(
            Species::WrapPtr(species), patch, newPopulation);
    }

    int32_t
        getPopulationInPatchWrapper(Species* species, int32_t patch)
    {
        return getPopulationInPatch(Species::WrapPtr(species), patch);
    }

    static RunResults*
        factory();


    REFERENCE_COUNTED_PTR_TYPE(RunResults);

private:
    std::vector<SpeciesResult> m_results;
};

}} // namespace thrive::autoevo
