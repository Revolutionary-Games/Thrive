#include <algorithm>
#include <cmath>
#include <iostream>
#include <map>

#include "engine/serialization.h"

#include "general/thrive_math.h"
#include "simulation_parameters.h"

#include "microbe_stage/process_system.h"

using namespace thrive;

ProcessorComponent::ProcessorComponent() : Leviathan::Component(TYPE) {}

/*
void
ProcessorComponent::load(const StorageContainer& storage)
{
    Component::load(storage);
    StorageContainer processes = storage.get<StorageContainer>("processes");
    for (const std::string& id : processes.keys())
    {
        this->process_capacities[std::atoi(id.c_str())] =
processes.get<double>(id);
    }
}

StorageContainer
ProcessorComponent::storage() const
{
    StorageContainer storage = Component::storage();

    StorageContainer processes;
    for (auto entry : this->process_capacities) {
        processes.set<double>(std::to_string(static_cast<int>(entry.first)),
entry.second);
    }
    storage.set<StorageContainer>("processes", processes);


    return storage;
}
*/

void
    ProcessorComponent::setCapacity(BioProcessId id, double capacity)
{
    this->process_capacities[id] = capacity;
}

CompoundBagComponent::CompoundBagComponent() : Leviathan::Component(TYPE)
{
    storageSpace = 0;
    storageSpaceOccupied = 0;

    for(size_t id = 0; id < SimulationParameters::compoundRegistry.getSize();
        id++) {
        compounds[id].amount = 0;
        compounds[id].price = INITIAL_COMPOUND_PRICE;
        compounds[id].uninflatedPrice = INITIAL_COMPOUND_PRICE;
        compounds[id].demand = INITIAL_COMPOUND_DEMAND;
    }
}

/*
void
CompoundBagComponent::load(const StorageContainer& storage)
{
    Component::load(storage);

    StorageContainer amounts = storage.get<StorageContainer>("amounts");
    StorageContainer prices = storage.get<StorageContainer>("prices");
    StorageContainer uninflatedPrices =
storage.get<StorageContainer>("uninflatedPrices"); StorageContainer demand =
storage.get<StorageContainer>("demand");

    for (const std::string& id : amounts.keys())
    {
        CompoundId compoundId = std::atoi(id.c_str());
        this->compounds[compoundId].amount = amounts.get<double>(id);
        this->compounds[compoundId].price = prices.get<double>(id);
        this->compounds[compoundId].uninflatedPrice =
uninflatedPrices.get<double>(id); this->compounds[compoundId].demand =
demand.get<double>(id);
    }

    this->storageSpace = storage.get<float>("storageSpace");

    this->speciesName = storage.get<std::string>("speciesName");
    this->processor = static_cast<ProcessorComponent*>(Entity(this->speciesName,
            Game::instance().engine().getCurrentGameStateFromLua()).
        getComponent(ProcessorComponent::TYPE_ID));
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

        amounts.set<double>(""+id, data.amount);
        amounts.set<double>(""+id, data.price);
        amounts.set<double>(""+id, data.uninflatedPrice);
        amounts.set<double>(""+id, data.demand);
    }

    storage.set("amounts", std::move(amounts));
    storage.set("prices", std::move(prices));
    storage.set("uninflatedPrices", std::move(uninflatedPrices));
    storage.set("demand", std::move(demand));
    storage.set("speciesName", this->speciesName);
    storage.set("storageSpace", this->storageSpace);

    return storage;
}
*/
void
    CompoundBagComponent::setProcessor(ProcessorComponent* processor,
        const std::string& speciesName)
{
    this->processor = processor;
    this->speciesName = speciesName;
}

// helper methods for integrating compound bags with current, un-refactored, lua
// microbes
double
    CompoundBagComponent::getCompoundAmount(CompoundId id)
{
    return compounds[id].amount;
}

double
    CompoundBagComponent::getStorageSpaceUsed() const
{
    double sso = 0;
    for(const auto& compound : compounds) {
        double compoundAmount = compound.second.amount;
        sso += compoundAmount;
    }
    return sso;
}

void
    CompoundBagComponent::giveCompound(CompoundId id, double amt)
{
    compounds[id].amount += amt;
}

double
    CompoundBagComponent::takeCompound(CompoundId id, double to_take)
{
    double& ref = compounds[id].amount;
    double amt = ref > to_take ? to_take : ref;
    ref -= amt;
    return amt;
}

double
    CompoundBagComponent::getPrice(CompoundId compoundId)
{
    return compounds[compoundId].price;
}

double
    CompoundBagComponent::getDemand(CompoundId compoundId)
{
    return compounds[compoundId].demand;
}

// ------------------------------------ //
// ProcessSystem

void
    ProcessSystem::Run(GameWorld& world)
{
    const auto logicTime = Leviathan::TICKSPEED;
    // Iterating on each entity with a CompoundBagComponent and a
    // ProcessorComponent
    for(auto& value : CachedComponents.GetIndex()) {
        CompoundBagComponent& bag = std::get<0>(*value.second);
        auto processor = bag.processor;
        // LOG_INFO("Capacities:
        // "+std::to_string(processor->process_capacities.size()));
        for(const auto& process : processor->process_capacities) {
            BioProcessId processId = process.first;
            double processCapacity = process.second;

            double processLimitCapacity =
                processCapacity * logicTime; // big enough number.

            // Inputs.
            bool processed = false;
            for(const auto& input :
                SimulationParameters::bioProcessRegistry.getTypeData(processId)
                    .inputs) {
                CompoundId inputId = input.first;
                int inputRemoved = input.second;
                if(bag.compounds[inputId].amount >= inputRemoved) {
                    processed = true;
                    bag.compounds[inputId].amount -= inputRemoved;
                } else {
                    processed = false;
                }
            }

            // Outputs.
            if(processed) {
                for(const auto& output :
                    SimulationParameters::bioProcessRegistry
                        .getTypeData(processId)
                        .outputs) {
                    CompoundId outputId = output.first;
                    int outputGenerated = output.second;
                    bag.compounds[outputId].amount += outputGenerated;
                }
            }
            // TODO: Make sure you dont go over storage capcities
        }
        // Making sure the compound amount is not negative.
        for(const auto& compound : bag.compounds) {
            CompoundId compoundId = compound.first;
            CompoundData& compoundData = bag.compounds[compoundId];
            compoundData.amount = std::max(compoundData.amount, 0.0);
        }
    }
}
