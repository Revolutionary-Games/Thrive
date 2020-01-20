#pragma once

#include "biomes.h"
#include "simulation/star_and_planet_generator.h"
#include "species.h"

#include <Common/ReferenceCounted.h>

#include <unordered_map>
#include <unordered_set>

namespace thrive {

constexpr auto INITIAL_SPECIES_POPULATION = 100;

//! An object that represents a patch
class Patch : public Leviathan::ReferenceCounted {
public:
    struct SpeciesInPatch {

        Species::pointer species;
        int population = 0;
    };

protected:
    // These are protected for only constructing properly reference
    // counted instances through MakeShared
    friend ReferenceCounted;
    Patch(const std::string& name, int32_t id, const Biome& biomeTemplate);

public:
    virtual ~Patch() = default;

    //! \brief Adds a connection to patch with id
    //! \returns True if this was new, false if already added
    bool
        addNeighbour(int32_t id);

    //! \brief Returns all species in this patch
    const auto&
        getSpecies() const
    {
        return speciesInPatch;
    }

    //! \brief Looks for a species with the specified name in this patch
    Species::pointer
        searchSpeciesByName(const std::string& name) const;

    //! \brief Adds a new species to this patch
    //! \returns True when added. False if the species was already in this patch
    bool
        addSpecies(const Species::pointer& species,
            int population = INITIAL_SPECIES_POPULATION);

    //! \brief Removes a species from this patch
    //! \returns True when a species was removed
    bool
        removeSpecies(const Species::pointer& species);

    //! \brief Updates a species population in this patch
    //! \returns True on success
    bool
        updateSpeciesPopulation(const Species::pointer& species,
            int newPopulation);

    int
        getSpeciesPopulation(const Species::pointer& species);

    uint64_t
        getSpeciesCount() const;

    Species::pointer
        getSpecies(uint64_t index) const;

    //! \brief Makes a JSON object representing this patch, including biome and
    //! species data
    Json::Value
        toJSON() const;

    int32_t
        getId() const
    {
        return patchId;
    }

    const std::string&
        getName() const
    {
        return name;
    }

    Biome&
        getBiome()
    {
        return biome;
    }

    const Biome&
        getBiome() const
    {
        return biome;
    }

    const Biome&
        getBiomeTemplate() const
    {
        return biomeTemplate;
    }

    const auto&
        getNeighbours() const
    {
        return adjacentPatches;
    }

    bool
        addSpeciesWrapper(Species* species, int32_t population)
    {
        return addSpecies(Species::WrapPtr(species), population);
    }

    Species*
        getSpeciesWrapper(uint64_t index) const
    {
        const auto result = getSpecies(index);
        if(result)
            result->AddRef();
        return result.get();
    }

    int32_t
        getSpeciesPopulationWrapper(Species* species)
    {
        return getSpeciesPopulation(Species::WrapPtr(species));
    }

    //! \brief Set coordinates for the patch to be displayed in the gui
    void
        setScreenCoordinates(Float2 coordinates);

    //! Get current coordinates for the patch to be displayed in the gui
    Float2
        getScreenCoordinates() const
    {
        return screenCoordinates;
    }

    //! Factory for scripts
    static Patch*
        factory(const std::string& name,
            int32_t id,
            const Biome& biomeTemplate);

    REFERENCE_COUNTED_PTR_TYPE(Patch);

    //! \brief Creates a clone from this Patch doesn't deep clone the species as
    //! that doesn't make sense
    Patch::pointer
        clone() const;

private:
    const int32_t patchId;
    std::string name;

    //! Where the patch should be displayed in the gui.
    Float2 screenCoordinates;

    Biome biome;

    //! This is a copy of biome that is set on construction and never allowed to
    //! change
    const Biome biomeTemplate;

    //! Species in this patch. The Species objects are shared with other
    //! patches. They are contained in SpeciesInPatch struct to allow per patch
    //! properties
    std::vector<SpeciesInPatch> speciesInPatch;

    //! Links to other patches. These don't use Patch::pointer because that
    //! doesn't support weak references
    std::unordered_set<int32_t> adjacentPatches;
};


//! A mesh of connected Patches and the planet
class PatchMap : public Leviathan::ReferenceCounted {
protected:
    // These are protected for only constructing properly reference
    // counted instances through MakeShared
    friend ReferenceCounted;
    PatchMap() = default;

public:
    ~PatchMap() = default;

    //! \brief Adds a new patch to the map
    //! \returns True on success. False if the id is duplicate or there is some
    //! other problem
    bool
        addPatch(const Patch::pointer& patch);

    //! \returns True when the map is valid and has no invalid references
    bool
        verify();

    //! \brief Finds a species in the current patch map with name
    //!
    //! This starts from the current patch and then falls back to checking all
    //! patches. This is done to improve performance as it is likely that
    //! species in the current patch are looked up
    Species::pointer
        findSpeciesByName(const std::string& name);

    //! \brief Updates the global population numbers in Species
    void
        updateGlobalPopulations();

    //! \brief Removes species from patches where their population is <= 0
    void
        removeExtinctSpecies(bool playerCantGoExtinct = false);

    //! \brief Makes a JSON object representing the entire map
    Json::Value
        toJSON() const;

    //! \brief Returns JSON as a string
    std::string
        toJSONString() const;

    Patch::pointer
        getCurrentPatch();

    //! \brief Sets the current patch
    //! \returns True if the id was valid, false otherwise
    bool
        setCurrentPatch(int32_t newId);

    inline int32_t
        getCurrentPatchId() const
    {
        return currentPatchId;
    }

    Patch::pointer
        getPatch(int32_t id);

    bool
        addPatchWrapper(Patch* patch)
    {
        return addPatch(Patch::WrapPtr(patch));
    }

    Patch*
        getCurrentPatchWrapper()
    {
        const auto ptr = getCurrentPatch();
        if(ptr)
            ptr->AddRef();
        return ptr.get();
    }

    Patch*
        getPatchWrapper(int32_t id)
    {
        const auto ptr = getPatch(id);
        if(ptr)
            ptr->AddRef();
        return ptr.get();
    }

    Species*
        findSpeciesByNameWrapper(const std::string& name)
    {
        const auto ptr = findSpeciesByName(name);
        if(ptr)
            ptr->AddRef();
        return ptr.get();
    }

    auto&
        getPatches()
    {
        return patches;
    }

    const auto&
        getPatches() const
    {
        return patches;
    }

    auto&
        getPlanet()
    {
        return planet;
    }

    Planet*
        getPlanetWrapper()
    {
        const auto ptr = getPlanet();
        if(ptr)
            ptr->AddRef();
        return ptr.get();
    }

    void
        setPlanet(Planet::pointer& newPlanet)
    {
        planet = newPlanet;
    }

    CScriptArray*
        getPatchesWrapper() const;

    //! Factory for scripts
    static PatchMap*
        factory();

    REFERENCE_COUNTED_PTR_TYPE(PatchMap);

    //! \brief Clones this PatchMap to help store previous populations
    PatchMap::pointer
        clone() const;

private:
    std::unordered_map<int32_t, Patch::pointer> patches;
    int32_t currentPatchId = 0;
    Planet::pointer planet;
};

} // namespace thrive
