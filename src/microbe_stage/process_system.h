#pragma once

#include "biomes.h"
#include "membrane_types.h"
#include "organelle_template.h"

#include "engine/component_types.h"
#include "engine/typedefs.h"

#include <Entities/Component.h>
#include <Entities/System.h>

#include <unordered_map>
#include <vector>

namespace Leviathan {
class GameWorld;
}

// The initial variables of the system.
constexpr auto INITIAL_COMPOUND_PRICE = 0.0f;

namespace thrive {

class CellStageWorld;

//! \brief Specifies what processes a cell can perform
//! \todo To reduce duplication and excess memory usage the processes should
//! be moved to a new class ProcessConfiguration
//! which would be ReferenceCounted and shared
//! `ProcessConfiguration::pointer m_processes`
class ProcessorComponent : public Leviathan::Component {
public:
    ProcessorComponent();
    ProcessorComponent(ProcessorComponent&& other) noexcept;

    ProcessorComponent&
        operator=(const ProcessorComponent& other);
    ProcessorComponent&
        operator=(ProcessorComponent&& other) noexcept;

    inline void
        setProcessRate(BioProcessId id, float rate)
    {
        m_processRates[id] = rate;
    }

    //! \brief Used to reset the previous rates when rebuilding the process list
    void
        clearProcessRates()
    {
        m_processRates.clear();
    }

    REFERENCE_HANDLE_UNCOUNTED_TYPE(ProcessorComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::PROCESSOR);

    std::unordered_map<BioProcessId, double> m_processRates;
};

// Helper structure to store the economic information of the compounds.
struct CompoundData {
    double amount;
    double price;
    double usedLastTime;
};

//! \brief A thing that holds compounds
class CompoundBagComponent : public Leviathan::Component {
public:
    CompoundBagComponent();

    double storageSpace;
    double storageSpaceOccupied;
    std::unordered_map<CompoundId, CompoundData> compounds;

    double getCompoundAmount(CompoundId);

    double
        getStorageSpaceUsed() const;

    double getPrice(CompoundId);


    double getUsedLastTime(CompoundId);

    double
        takeCompound(CompoundId, double); // remove up to a certain amount of
                                          // compound, returning how much was
                                          // removed

    void
        giveCompound(CompoundId, double);


    void
        setCompound(CompoundId, double);

    REFERENCE_HANDLE_UNCOUNTED_TYPE(CompoundBagComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::COMPOUND_BAG);
};

class ProcessSystem
    : public Leviathan::System<
          std::tuple<CompoundBagComponent&, ProcessorComponent&>> {
public:
    /**
     * @brief Updates the system
     */
    void
        Run(GameWorld& world, float elapsed);

    void
        CreateNodes(
            const std::vector<std::tuple<CompoundBagComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<ProcessorComponent*, ObjectID>>&
                seconddata,
            const ComponentHolder<CompoundBagComponent>& firstholder,
            const ComponentHolder<ProcessorComponent>& secondholder)
    {
        TupleCachedComponentCollectionHelper(
            CachedComponents, firstdata, seconddata, firstholder, secondholder);
    }

    void
        DestroyNodes(
            const std::vector<std::tuple<CompoundBagComponent*, ObjectID>>&
                firstdata,
            const std::vector<std::tuple<ProcessorComponent*, ObjectID>>&
                seconddata)
    {
        CachedComponents.RemoveBasedOnKeyTupleList(firstdata);
        CachedComponents.RemoveBasedOnKeyTupleList(seconddata);
    }

    void
        setProcessBiome(const Biome& biome);

    double
        getDissolved(CompoundId compoundData);

    // These are some process related query functions

    //! \brief Computes the process numbers for given organelles given the
    //! active biome data
    //! \returns The data as a JSON string
    std::string
        computeOrganelleProcessEfficiencies(
            const std::vector<OrganelleTemplate::pointer>& organelles,
            const Biome& biome) const;

    //! \brief Computes the energy balance for the given organelles in biome
    //! \returns The data as a JSON string
    std::string
        computeEnergyBalance(
            const std::vector<OrganelleTemplate::pointer>& organelles,
            const MembraneType& membraneType,
            const Biome& biome) const;


protected:
private:
    Biome currentBiome;
    static constexpr double TIME_SCALING_FACTOR = 1000;
};

} // namespace thrive
