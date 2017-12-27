#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "engine/typedefs.h"

#include <boost/range/adaptor/map.hpp>
#include <vector>
#include <unordered_map>

// The minimum positive price a compound can have.
#define MIN_POSITIVE_COMPOUND_PRICE 0.00001

// The "willingness" of the compound prices to change.
// (between 0.0 and 1.0)
#define COMPOUND_PRICE_MOMENTUM 0.2

// How much the "important" compounds get their price inflated.
#define IMPORTANT_COMPOUND_BIAS 1000.0

// How important the storage space is considered.
#define STORAGE_SPACE_MULTIPLIER 2.0

// Used to soften the demand according to the process capacity.
#define PROCESS_CAPACITY_DEMAND_MULTIPLIER 15.0

// The initial variables of the system.
#define INITIAL_COMPOUND_PRICE 10.0
#define INITIAL_COMPOUND_DEMAND 1.0

namespace sol {
class state;
}

namespace thrive {

class ProcessorComponent : public Component {
    COMPONENT(Processor)

public:
    static void luaBindings(sol::state &lua);

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    std::unordered_map<BioProcessId, double> process_capacities;
    void
    setCapacity(BioProcessId, double);
};

// Helper structure to store the economic information of the compounds.
struct CompoundData {
    double amount;
    double uninflatedPrice;
    double price;
    double demand;
    double priceReductionPerUnit;
    double breakEvenPoint;
};

class CompoundBagComponent : public Component {
    COMPONENT(CompoundBag)

public:
    static void luaBindings(sol::state &lua);

    CompoundBagComponent();

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    double storageSpace;
    double storageSpaceOccupied;
    ProcessorComponent* processor = nullptr;
    std::string speciesName;
    std::unordered_map<CompoundId, CompoundData> compounds;

    void
    setProcessor(ProcessorComponent& processor, const std::string& speciesName);

    double
    getCompoundAmount(CompoundId);

    double
    getStorageSpaceUsed() const;

    double
    getPrice(CompoundId);

    double
    getDemand(CompoundId);

    double
    takeCompound(CompoundId, double); // remove up to a certain amount of compound, returning how much was removed

    void
    giveCompound(CompoundId, double);
};

class ProcessSystem : public System {

public:
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    ProcessSystem();

    /**
    * @brief Destructor
    */
    ~ProcessSystem();

    /**
    * @brief Initializes the system
    *
    */
    void init(GameStateData* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int renderTime, int logicTime) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
