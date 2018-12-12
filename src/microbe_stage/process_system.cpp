#include <algorithm>
#include <cmath>
#include <iostream>
#include <map>

#include "engine/serialization.h"

#include "general/thrive_math.h"
#include "simulation_parameters.h"

#include "microbe_stage/process_system.h"

#include <Entities/GameWorld.h>

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

double
    ProcessorComponent::getCapacity(BioProcessId id)
{
    return this->process_capacities[id];
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
        compounds[id].usedLastTime = INITIAL_COMPOUND_PRICE;
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

void
    CompoundBagComponent::setCompound(CompoundId id, double amt)
{
    compounds[id].amount = amt;
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
    CompoundBagComponent::getUsedLastTime(CompoundId compoundId)
{
    return compounds[compoundId].usedLastTime;
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
    if(!world.GetNetworkSettings().IsAuthoritative)
        return;

    const auto logicTime = Leviathan::TICKSPEED;
    // Iterating on each entity with a CompoundBagComponent and a
    // ProcessorComponent
    for(auto& value : CachedComponents.GetIndex()) {

        CompoundBagComponent& bag = std::get<0>(*value.second);
        ProcessorComponent* processor = bag.processor;

        if(!processor) {
            LOG_ERROR("Compound Bag Lacks Processor component");
            continue;
        }
        // Set all compounds to price 0 initially, set used ones to 1, this way
        // we can purge unused compounds, I think we may be able to merge this
        // and the bottom for loop, but im not sure how to go about that yet.
        for(auto& compound : bag.compounds) {
            CompoundData& compoundData = compound.second;
            compoundData.price = 0;
        }

        // LOG_INFO("Capacities:
        // "+std::to_string(processor->process_capacities.size()));
        for(const auto& process : processor->process_capacities) {
            BioProcessId processId = process.first;
            double processCapacity = process.second;

            // Processes are now every second
            double processLimitCapacity = logicTime;
            // This should not do anything if the cell has no room to hold the
            // new compounds and it shouldn't just keep draining compounds if
            // you lack the stuff you need to Do your processes
            bool processed = false;
            // Can your cell do the process without waste?
            bool canDoProcess = true;

            // If capcity is 0 dont do it
            if(processCapacity != 0.0f) {

                // Loop through to make sure you can follow through with your
                // whole process so nothing gets wasted as that would be
                // frusterating, its two more for loops, yes but it should only
                // really be looping at max two or three times anyway. also make
                // sure you wont run out of space when you do add the compounds.
                // Input
                for(const auto& input : SimulationParameters::bioProcessRegistry
                                            .getTypeData(processId)
                                            .inputs) {
                    CompoundId inputId = input.first;
                    // Set price of used compounds to 1, we dont want to purge
                    // those
                    bag.compounds[inputId].price = 1;
                    double inputRemoved = ((input.second * processCapacity) /
                                           (processLimitCapacity));
                    if(bag.compounds[inputId].amount < inputRemoved) {
                        canDoProcess = false;
                    }
                }
                // Output
                // Dont loop if you dont need to so check if canDoProcess has
                // already been set to false
                if(canDoProcess) {
                    for(const auto& output :
                        SimulationParameters::bioProcessRegistry
                            .getTypeData(processId)
                            .outputs) {
                        CompoundId outputId = output.first;
                        // For now lets assume compounds we produce are also
                        // useful
                        bag.compounds[outputId].price = 1;
                        double outputAdded =
                            ((output.second * processCapacity) /
                                (processLimitCapacity));

                        if(bag.getCompoundAmount(outputId) + outputAdded >
                            bag.storageSpace) {
                            canDoProcess = false;
                        }
                    }

                }
                // Even if you cannot do the process, you still need to know the
                // price I want to keep this code as simplistic as possible so
                // we can comprehend it, so i might just add a new method
                // specifically for calculating prices and call it as this seems
                // messy.
                else {
                    for(const auto& output :
                        SimulationParameters::bioProcessRegistry
                            .getTypeData(processId)
                            .outputs) {
                        CompoundId outputId = output.first;
                        // For now lets assume compounds we produce are also
                        // useful
                        bag.compounds[outputId].price = 1;
                    }
                }
                // Only carry out this process if you have all the required
                // ingrediants, and if something weird happens and you suddenly
                // lose your capability, just remove what you can and get out
                // and next time you will be unable

                if(canDoProcess) {
                    // Inputs.
                    for(const auto& input :
                        SimulationParameters::bioProcessRegistry
                            .getTypeData(processId)
                            .inputs) {
                        CompoundId inputId = input.first;
                        double inputRemoved =
                            ((input.second * processCapacity) /
                                (processLimitCapacity));
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
                            double outputGenerated =
                                ((output.second * processCapacity) /
                                    (processLimitCapacity));
                            bag.compounds[outputId].amount += outputGenerated;
                        }
                    }
                }
            }
        }
        // Making sure the compound amount is not negative.
        for(auto& compound : bag.compounds) {
            CompoundData& compoundData = compound.second;
            compoundData.amount = std::max(compoundData.amount, 0.0);
            // That way we always have a running tally of what process was set
            // to what despite clearing the price every run cycle
            compoundData.usedLastTime = compoundData.price;
        }
    }
}
