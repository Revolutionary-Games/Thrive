#pragma once

#include "engine/component_types.h"
#include "engine/typedefs.h"

#include <Entities/Component.h>
#include <Entities/System.h>
//#include <Entities/Components.h>

#include <unordered_map>
#include <vector>

namespace Leviathan {
class GameWorld;
}



// The initial variables of the system.
#define INITIAL_COMPOUND_PRICE 0.0

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
        setCapacity(BioProcessId id, double capacity)
    {
        m_processCapacities[id] = capacity;
    }

    inline double
        getCapacity(BioProcessId id)
    {
        return m_processCapacities[id];
    }

    REFERENCE_HANDLE_UNCOUNTED_TYPE(ProcessorComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::PROCESSOR);

    std::unordered_map<BioProcessId, double> m_processCapacities;
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
        Run(GameWorld& world);

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

protected:
private:
    static constexpr double TIME_SCALING_FACTOR = 1000;
};

} // namespace thrive
