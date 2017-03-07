#include <iostream>
#include <cmath>
#include <algorithm>
#include <map>

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "engine/serialization.h"

#include "microbe_stage/compound.h"
#include "microbe_stage/compound_registry.h"
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/process_system.h"

using namespace thrive;

REGISTER_COMPONENT(ProcessorComponent)

luabind::scope
ProcessorComponent::luaBindings() {
    using namespace luabind;
    return class_<ProcessorComponent, Component>("ProcessorComponent")
        .enum_("ID") [
            value("TYPE_ID", ProcessorComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &ProcessorComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("setCapacity", &ProcessorComponent::setCapacity)
    ;
}

void
ProcessorComponent::load(const StorageContainer& storage)
{
    Component::load(storage);
    StorageContainer processes = storage.get<StorageContainer>("processes");
    for (const std::string& id : processes.keys())
    {
        this->process_capacities[std::atoi(id.c_str())] = processes.get<float>(id);
	}
}

StorageContainer
ProcessorComponent::storage() const
{
	StorageContainer storage = Component::storage();

	StorageContainer processes;
    for (auto entry : this->process_capacities) {
        processes.set<float>(std::to_string(static_cast<int>(entry.first)), entry.second);
    }
    storage.set<StorageContainer>("processes", processes);


	return storage;
}

void
ProcessorComponent::setCapacity(BioProcessId id, float capacity)
{
    this->process_capacities[id] = capacity;
}

REGISTER_COMPONENT(CompoundBagComponent)

luabind::scope
CompoundBagComponent::luaBindings() {
    using namespace luabind;
    return class_<CompoundBagComponent, Component>("CompoundBagComponent")
        .enum_("ID") [
            value("TYPE_ID", CompoundBagComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CompoundBagComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("setProcessor", &CompoundBagComponent::setProcessor)
        .def("giveCompound", &CompoundBagComponent::giveCompound)
        .def("takeCompound", &CompoundBagComponent::takeCompound)
        .def("getCompoundAmount", &CompoundBagComponent::getCompoundAmount)
        .def("getPrice", &CompoundBagComponent::getPrice)
        .def("getDemand", &CompoundBagComponent::getDemand)
        .def_readwrite("storageSpace", &CompoundBagComponent::storageSpace)
    ;
}

CompoundBagComponent::CompoundBagComponent() {
    storageSpace = 0;
    storageSpaceOccupied = 0;
    for (CompoundId id : CompoundRegistry::getCompoundList()) {
        compounds[id].amount = 0;
        compounds[id].price = INITIAL_COMPOUND_PRICE;
        compounds[id].uninflatedPrice = INITIAL_COMPOUND_PRICE;
        compounds[id].demand = INITIAL_COMPOUND_DEMAND;
    }
}

void
CompoundBagComponent::load(const StorageContainer& storage)
{
    Component::load(storage);

    StorageContainer amounts = storage.get<StorageContainer>("amounts");
    StorageContainer prices = storage.get<StorageContainer>("prices");
    StorageContainer uninflatedPrices = storage.get<StorageContainer>("uninflatedPrices");
    StorageContainer demand = storage.get<StorageContainer>("demand");

    for (const std::string& id : amounts.keys())
    {
        CompoundId compoundId = std::atoi(id.c_str());
        this->compounds[compoundId].amount = amounts.get<float>(id);
        this->compounds[compoundId].price = prices.get<float>(id);
        this->compounds[compoundId].uninflatedPrice = uninflatedPrices.get<float>(id);
        this->compounds[compoundId].demand = demand.get<float>(id);
	}

	this->speciesName = storage.get<std::string>("speciesName");
	this->processor = static_cast<ProcessorComponent*>(Entity(this->speciesName).getComponent(ProcessorComponent::TYPE_ID));
}

StorageContainer
CompoundBagComponent::storage() const
{
    StorageContainer storage = Component::storage();

    StorageContainer amounts;
    StorageContainer prices;
    StorageContainer uninflatedPrices;
    StorageContainer demand;
    for (auto entry : this->compounds) {
        CompoundId id = entry.first;
        CompoundData data = entry.second;

        amounts.set<float>(""+id, data.amount);
        amounts.set<float>(""+id, data.price);
        amounts.set<float>(""+id, data.uninflatedPrice);
        amounts.set<float>(""+id, data.demand);
    }

    storage.set("amounts", std::move(amounts));
    storage.set("prices", std::move(prices));
    storage.set("uninflatedPrices", std::move(uninflatedPrices));
    storage.set("demand", std::move(demand));
    storage.set("speciesName", this->speciesName);

    return storage;
}

void
CompoundBagComponent::setProcessor(ProcessorComponent& processor, const std::string& speciesName) {
    this->processor = &processor;
    this->speciesName = speciesName;
}

// helper methods for integrating compound bags with current, un-refactored, lua microbes
float
CompoundBagComponent::getCompoundAmount(CompoundId id) {
    return compounds[id].amount;
}

void
CompoundBagComponent::giveCompound(CompoundId id, float amt) {
    compounds[id].amount += amt;
}

float
CompoundBagComponent::takeCompound(CompoundId id, float to_take) {
    float& ref = compounds[id].amount;
    float amt = ref > to_take ? to_take : ref;
    ref -= amt;
    return amt;
}

float
CompoundBagComponent::getPrice(CompoundId compoundId) {
    return compounds[compoundId].price;
}

float
CompoundBagComponent::getDemand(CompoundId compoundId) {
    return compounds[compoundId].demand;
}

luabind::scope
ProcessSystem::luaBindings() {
    using namespace luabind;
    return class_<ProcessSystem, System>("ProcessSystem")
        .def(constructor<>())
    ;
}
struct ProcessSystem::Implementation {

    EntityFilter<
        CompoundBagComponent
    > m_entities;

    void update(int);
    void updateAddedEntites(int);
    void updateRemovedEntities(int);

    static constexpr float TIME_SCALING_FACTOR = 1000;
};

ProcessSystem::ProcessSystem()
    : m_impl(new Implementation())
{

}

ProcessSystem::~ProcessSystem()
{

}

void
ProcessSystem::init(GameState* gameState)
{
    System::initNamed("ProcessSystem", gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}

void
ProcessSystem::shutdown()
{

}

void
ProcessSystem::Implementation::updateRemovedEntities(int) {
    // std::cerr << logicTime;
    // for (EntityId entityId : this->m_entities.removedEntities()) {
        // std::cerr << &entityId;
    // }
}

void
ProcessSystem::Implementation::updateAddedEntites(int) {
    // std::cerr << logicTime;
    // for (auto& value : this->m_entities.addedEntities()) {
        // std::cerr << &value;
    // }
}

double
sigmoid(double x);

float
demandSofteningFunction(float processCapacity);

double
sigmoid(double x) {
    return 1 / (1 + exp(-x));
}

float
demandSofteningFunction(float processCapacity) {
    return 2 * sigmoid(processCapacity * PROCESS_CAPACITY_DEMAND_MULTIPLIER) - 1.0;
}

void
ProcessSystem::Implementation::update(int logicTime) {
    //Iterating on each entity with a ProcessorComponent.
    for (auto& value : this->m_entities) {
        CompoundBagComponent* bag = std::get<0>(value.second);
        ProcessorComponent* processor = bag->processor;

        // Avoiding zero-division errors.
        if(bag->storageSpace > 0)
        {
            //Calculating the storage space occupied;
            bag->storageSpaceOccupied = 0;
            for (const auto& compound : bag->compounds) {
                float compoundAmount = compound.second.amount;
                bag->storageSpaceOccupied += compoundAmount;
            }

            //Phase one: setting up the compound information.
            for (const auto& compound : bag->compounds) {
                CompoundId compoundId = compound.first;
                CompoundData &compoundData = bag->compounds[compoundId];

                // Edge case to get the prices above 0 if some demand exists.
                if(compoundData.demand > 0 && compoundData.uninflatedPrice <= 0)
                    compoundData.uninflatedPrice = MIN_POSITIVE_COMPOUND_PRICE;

                // Adjusting the prices according to supply and demand.
                float priceAdjustment = compoundData.demand / (compoundData.amount + 1);
                compoundData.uninflatedPrice = compoundData.uninflatedPrice * (COMPOUND_PRICE_MOMENTUM + priceAdjustment - COMPOUND_PRICE_MOMENTUM * priceAdjustment);

                // Setting the prices to 0 if they're below MIN_POSITIVE_COMPOUND_PRICE.
                if(compoundData.price < MIN_POSITIVE_COMPOUND_PRICE) {
                    compoundData.uninflatedPrice = 0;
                    compoundData.priceReductionPerUnit = 0;
                }

                // Calculating how much the price would fall if we had one more unit,
                // To make predictions with the demand.
                else {
                    priceAdjustment = compoundData.demand / (compoundData.amount + 2);
                    float reducedPrice = compoundData.uninflatedPrice * (COMPOUND_PRICE_MOMENTUM + priceAdjustment - COMPOUND_PRICE_MOMENTUM * priceAdjustment);
                    compoundData.priceReductionPerUnit = compoundData.uninflatedPrice - reducedPrice;
                }

                //Inflating the price if the compound is useful outside of this system.
                compoundData.price = compoundData.uninflatedPrice;
                if(CompoundRegistry::isUseful(compoundId))
                {
                    compoundData.price += IMPORTANT_COMPOUND_BIAS / (compoundData.amount + 1);
                    float reducedPrice = IMPORTANT_COMPOUND_BIAS / (compoundData.amount + 2);
                    compoundData.priceReductionPerUnit += compoundData.price - reducedPrice;
                }

                // Calculating the break-even point
                if(compoundData.price <= 0.0)
                    compoundData.breakEvenPoint = 0;
                else
                    compoundData.breakEvenPoint = compoundData.price / compoundData.priceReductionPerUnit;

                // Setting the demand to 0 in order to recalculate it later.
                compoundData.demand = 0;
            }

            //Phase two: setting up the processes.
            for (const auto& process : processor->process_capacities) {
                BioProcessId processId = process.first;
                float processCapacity = process.second;

                //The maximum capacity this process could have with the current amount of input compounds.
                float processLimitCapacity = processCapacity * logicTime;

                for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
                    CompoundId inputId = input.first;
                    int inputNeeded = input.second;

                    //Limiting the process by the amount of this required compound.
                    processLimitCapacity = std::min(processLimitCapacity, bag->compounds[inputId].amount / inputNeeded);
                }

                // Calculating the desired rate, with some liberal use of linearization.

                // Calculating the price increment and the base price of the inputs
                // (the total price is rate * priceIncrement + basePrice).
                float baseInputPrice = 0;
                float inputPriceIncrement = 0;
                for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
                    CompoundId inputId = input.first;
                    int inputNeeded = input.second;
                    CompoundData &compoundData = bag->compounds[inputId];

                    inputPriceIncrement += inputNeeded * compoundData.priceReductionPerUnit;
                    baseInputPrice += inputNeeded * compoundData.price;
                }

                // Finding the rate at which the costs equal the benefits.
                // The benefit curve is piecewise lineal and continuous, and the breaking points are
                // the break-even points of the output compounds.
                // So first we have to order said break-even points.
                std::map<float, CompoundId> outputBreakEvenPoints;
                for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
                    CompoundId outputId = output.first;
                    int outputGenerated = output.second;
                    CompoundData &compoundData = bag->compounds[outputId];

                    float breakEvenPoint = compoundData.breakEvenPoint / outputGenerated;
                    outputBreakEvenPoints[breakEvenPoint] = outputId;
                }

                // Finding the piece of the function that contains the minimum
                // TODO: make it use binary search or something...
                float baseOutputPrice = 0.0;
                float outputPriceDecrement = 0.0;

                // Getting the initial revenue values
                for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
                    CompoundId outputId = output.first;
                    int outputGenerated = output.second;
                    CompoundData &compoundData = bag->compounds[outputId];

                    baseOutputPrice += compoundData.price * outputGenerated;
                    outputPriceDecrement += compoundData.priceReductionPerUnit * outputGenerated;
                }

                for (const auto& breakingPoint : outputBreakEvenPoints) {
                    float breakEvenPoint = breakingPoint.first;

                    // Calculating the cost.
                    float cost = baseInputPrice + breakEvenPoint * inputPriceIncrement;

                    // Calculating the revenue.
                    float baseOutputPrice_l = 0.0;
                    float outputPriceDecrement_l = 0.0;
                    for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
                        CompoundId outputId = output.first;
                        int outputGenerated = output.second;
                        CompoundData &compoundData = bag->compounds[outputId];

                        // The prices are never below 0.
                        if(compoundData.breakEvenPoint > breakEvenPoint) {
                            baseOutputPrice_l += compoundData.price * outputGenerated;
                            outputPriceDecrement_l += compoundData.priceReductionPerUnit * outputGenerated;
                        }
                    }

                    float revenue = baseOutputPrice_l - breakEvenPoint * outputPriceDecrement_l;

                    if(revenue < cost)
                        // We found the piece :)
                        break;

                    baseOutputPrice = baseOutputPrice_l;
                    outputPriceDecrement = outputPriceDecrement_l;
                }

                // Avoiding zero-division errors.
                float desiredRate;
                if(outputPriceDecrement + inputPriceIncrement > 0)
                    desiredRate = (baseOutputPrice - baseInputPrice) / (outputPriceDecrement + inputPriceIncrement);
                else
                    desiredRate = 0.0;

                if(desiredRate > 0.0)
                {
                    float rate = std::min(processCapacity * logicTime / 1000, processLimitCapacity);
                    rate = std::min(rate, desiredRate);

                    //Running the process at the specified rate, transforming the inputs...
                    for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
                        CompoundId inputId = input.first;
                        int inputNeeded = input.second;
                        bag->compounds[inputId].amount -= rate * inputNeeded;

                        // Increasing the input compound demand.
                        bag->compounds[inputId].demand += desiredRate * inputNeeded * demandSofteningFunction(processCapacity);
                    }

                    //...into the outputs.
                    for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
                        CompoundId outputId = output.first;
                        int outputGenerated = output.second;
                        bag->compounds[outputId].amount += rate * outputGenerated;
                    }
                }
            }
        }
    }
}

void
ProcessSystem::update(int, int logicTime)
{
    m_impl->updateRemovedEntities(logicTime);
    m_impl->updateAddedEntites(logicTime);
    m_impl->m_entities.clearChanges();
    m_impl->update(logicTime);
}
