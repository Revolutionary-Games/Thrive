#pragma once

#include "bioprocesses.h"
#include "engine/typedefs.h"
#include "organelle_types.h"

#include <Common/ReferenceCounted.h>
#include <Common/Types.h>

#include <variant>

class asIScriptFunction;
class asIScriptObject;
class CScriptDictionary;

namespace thrive {

//! \brief Represents the type of an organelle
//!
//! Actual concrete placed organelles are PlacedOrganelle objects. There should
//! be only a single OrganelleTemplate instance in existance for each organelle
//! defined in organelles.json
class OrganelleTemplate : public Leviathan::ReferenceCounted {
    struct OrganelleComponentType {

        OrganelleComponentType(const std::string& name);
        ~OrganelleComponentType();

        // Allow only moves
        OrganelleComponentType(const OrganelleComponentType& other) = delete;
        OrganelleComponentType
            operator=(const OrganelleComponentType& other) = delete;
        OrganelleComponentType(OrganelleComponentType&& other);


        const std::string name;
        asIScriptFunction* factoryFunction = nullptr;
        std::vector<std::variant<float, std::string>> factoryParams;
    };

    // These are protected: for only constructing properly reference
    // counted instances through MakeShared
    friend ReferenceCounted;
    OrganelleTemplate(const OrganelleType& parameters);

public:
    using OrganelleComposition = std::map<CompoundId, float>;

    ~OrganelleTemplate();

protected:
    //! Adds a hex to this organelle
    //!
    //! @param q, r
    //!  Axial coordinates of the new hex
    //!
    //! @returns success
    //!  True if the hex could be added, false if there already is a hex at
    //!  (q,r)
    //! @note This is done just once when this class is instantiated
    bool
        addHex(int q, int r);

public:
    bool
        containsHex(int q, int r) const;

    const auto&
        getHexes() const
    {
        return m_hexes;
    }

    //! \returns The hexes but rotated (rotation degrees)
    std::vector<Int2>
        getRotatedHexes(int rotation) const;

    //! \brief Script wrapper for rotated hexes, but also caches them
    //! \note This is not thread safe due to the caching
    const CScriptArray*
        getRotatedHexesWrapper(int rotation) const;

    Float3
        calculateCenterOffset() const;

    Float3
        calculateModelOffset() const;

    bool
        hasComponent(const std::string& name) const
    {
        for(const auto& component : m_components) {
            if(component.name == name)
                return true;
        }

        return false;
    }

    Int2
        getHex(uint64_t index) const
    {
        if(index >= m_hexes.size())
            throw InvalidArgument("index out of range");
        return m_hexes[index];
    }

    uint64_t
        getHexCount() const
    {
        return m_hexes.size();
    }

    uint64_t
        getComponentCount() const
    {
        return m_components.size();
    }

    //! \brief Creates a component for putting in a PlacedOrganelle
    asIScriptObject*
        createComponent(uint64_t index) const;

    TweakedProcess::pointer
        getProcess(uint64_t index) const
    {
        if(index >= m_processes.size())
            throw InvalidArgument("index out of range");
        return m_processes[index];
    }

    uint64_t
        getProcessCount() const
    {
        return m_processes.size();
    }

    // Script wrappers
    TweakedProcess*
        getProcessWrapper(uint64_t index) const
    {
        auto process = getProcess(index);
        if(process)
            process->AddRef();
        return process.get();
    }

    //! \note Increments refcount
    const CScriptDictionary*
        getInitialCompositionDictionary() const;

    float
        getChanceToCreate() const
    {
        return m_chanceToCreate;
    }

    float
        getProkaryoteChance() const
    {
        return m_prokaryoteChance;
    }

    int
        getMPCost() const
    {
        return m_mpCost;
    }

    float
        getOrganelleCost() const
    {
        return m_organelleCost;
    }


    REFERENCE_COUNTED_PTR_TYPE(OrganelleTemplate);

private:
    void
        calculateCost(const OrganelleComposition& composition);

    void
        createScriptInitialComposition();

public:
    const std::string m_name;
    const float m_mass;
    const std::string m_gene;

    //! Name of the model used for this organelle. For example "nucleus.fbx"
    const std::string m_mesh;

    //! Name of the texture used for this model
    const std::string m_texture;

private:
    // array<OrganelleComponentFactory@> components;
    std::vector<Int2> m_hexes;

    //! Stores rotated hexes
    //! The key is the number of times the rotation is done and not degrees.
    mutable std::map<int, CScriptArray*> m_rotatedHexesCache;

    //! The initial amount of compounds this organelle consists of
    OrganelleComposition m_initialComposition;

    //! m_initialComposition in script accessible form
    CScriptDictionary* m_initialCompositionDictionary = nullptr;

    std::vector<TweakedProcess::pointer> m_processes;

    //! The total number of compounds we need before we can split.
    float m_organelleCost;

    //! Chance of randomly generating this (used by procedural_microbes.as)
    float m_chanceToCreate = 0.0;
    float m_prokaryoteChance = 0.0;

    //! Cost in mutation points
    int m_mpCost = 0;

    std::vector<OrganelleComponentType> m_components;

    /*
    Organelle atributes:
    mass:   How heavy an organelle is. Affects speed, mostly.

    mpCost: The cost (in mutation points) an organelle costs in the
    microbe editor.

    mesh:   The name of the mesh file of the organelle.
    It has to be in the models folder.

    texture: The name of the texture file to use

    hexes:  A table of the hexes that the organelle occupies.

    gene:   The letter that will be used by the auto-evo system to
    identify this organelle.

    chanceToCreate: The (relative) chance this organelle will appear in a
    randomly generated or mutated microbe (to do roulette selection).

    prokaryoteChance: The (relative) chance this organelle will appear in a
    randomly generated or mutated prokaryotes (to do roulette selection).

    processes:  A table with all the processes this organelle does,
    and the capacity of the process
    */
};

} // namespace thrive
