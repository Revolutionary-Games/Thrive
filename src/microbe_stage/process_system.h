#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include <boost/range/adaptor/map.hpp>
#include <vector>
#include <unordered_map>

// The minimum positive price a compound can have.
#define MIN_POSITIVE_COMPOUND_PRICE 0.00001

// The "willingness" of the compound prices to change.
// (between 0.0 and 1.0)
#define COMPOUND_PRICE_MOMENTUM 0.1

// How much the "important" compounds get their price inflated.
#define IMPORTANT_COMPOUND_BIAS 100.0

// Used to soften the demand according to the process capacity.
#define PROCESS_CAPACITY_DEMAND_MULTIPLIER 10.0

// The initial variables of the system.
#define INITIAL_COMPOUND_PRICE 10.0
#define INITIAL_COMPOUND_DEMAND 1.0

namespace luabind {
class scope;
}

namespace thrive {

class ProcessorComponent : public Component {
    COMPONENT(Processor)

public:
    static luabind::scope
    luaBindings();

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    std::unordered_map<BioProcessId, float> process_capacities;
    void
    setCapacity(BioProcessId, float);
};

// Helper structure to store the economic information of the compounds.
struct CompoundData {
    float amount;
    float uninflatedPrice;
    float price;
    float demand;
    float priceReductionPerUnit;
    float breakEvenPoint;
};

class CompoundBagComponent : public Component {
    COMPONENT(CompoundBag)

public:
    static luabind::scope
    luaBindings();

    CompoundBagComponent();

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    float storageSpace;
    float storageSpaceOccupied;
    ProcessorComponent* processor;
    std::string speciesName;
    std::unordered_map<CompoundId, CompoundData> compounds;

    void
    setProcessor(ProcessorComponent& processor, const std::string& speciesName);

    float
    getCompoundAmount(CompoundId);

    float
    getPrice(CompoundId);

    float
    getDemand(CompoundId);

    float
    takeCompound(CompoundId, float); // remove up to a certain amount of compound, returning how much was removed

    void
    giveCompound(CompoundId, float);
};

class ProcessSystem : public System {

public:
    static luabind::scope
    luaBindings();

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
    void init(GameState* gameState) override;

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
