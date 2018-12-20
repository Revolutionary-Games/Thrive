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

class ProcessorComponent : public Leviathan::Component {

public:
    ProcessorComponent();
    /*
    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;
    */

    std::unordered_map<BioProcessId, double> process_capacities;
    void
        setCapacity(BioProcessId id, double capacity);
    double
        getCapacity(BioProcessId id);

    REFERENCE_HANDLE_UNCOUNTED_TYPE(ProcessorComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::PROCESSOR);
};

// Helper structure to store the economic information of the compounds.
struct CompoundData {
    double amount;
    double price;
    double usedLastTime;
};

//! \todo This component depends on an instance of processor so that needs
//! registering
class CompoundBagComponent : public Leviathan::Component {
public:
    CompoundBagComponent();

    /*
    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;
    */

    double storageSpace;
    double storageSpaceOccupied;
    ProcessorComponent* processor = nullptr;
    std::string speciesName;
    std::unordered_map<CompoundId, CompoundData> compounds;

    void
        setProcessor(ProcessorComponent* processor,
            const std::string& speciesName);

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
