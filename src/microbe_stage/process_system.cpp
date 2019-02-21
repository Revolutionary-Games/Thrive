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

ProcessorComponent::ProcessorComponent(ProcessorComponent&& other) noexcept :
    Leviathan::Component(TYPE),
    m_processCapacities(std::move(other.m_processCapacities))
{}
// ------------------------------------ //
ProcessorComponent&
    ProcessorComponent::operator=(const ProcessorComponent& other)
{
    m_processCapacities = other.m_processCapacities;
    return *this;
}

ProcessorComponent&
    ProcessorComponent::operator=(ProcessorComponent&& other) noexcept
{
    m_processCapacities = std::move(other.m_processCapacities);
    return *this;
}

// ------------------------------------ //
// CompoundBagComponent
CompoundBagComponent::CompoundBagComponent() : Leviathan::Component(TYPE)
{
    storageSpace = 0;
    storageSpaceOccupied = 0;
    for(size_t id = 0; id < SimulationParameters::compoundRegistry.getSize();
        id++) {
        compounds[id].amount = 0;
        compounds[id].price = INITIAL_COMPOUND_PRICE;
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
}*/

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
    if(compounds[id].amount > storageSpace) {
        compounds[id].amount = storageSpace;
    }
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

void
    ProcessSystem::setProcessBiome(int biomeId)
{
    curBiomeId = biomeId;
}

double
    ProcessSystem::getDissolved(CompoundId compoundData)
{
    Biome ourBiome =
        SimulationParameters::biomeRegistry.getTypeData(curBiomeId);
    return ourBiome.getCompound(compoundData)->dissolved;
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
        ProcessorComponent& processor = std::get<1>(*value.second);

        // Set all compounds to price 0 initially, set used ones to 1, this way
        // we can purge unused compounds, I think we may be able to merge this
        // and the bottom for loop, but im not sure how to go about that yet.
        for(auto& compound : bag.compounds) {
            CompoundData& compoundData = compound.second;
            compoundData.price = 0;
        }

        for(const auto& process : processor.m_processCapacities) {
            const BioProcessId processId = process.first;
            const double processCapacity = process.second;

            // If capacity is 0 dont do it
            if(processCapacity <= 0.0f)
                continue;

            // Processes are now every second
            const double processLimitCapacity = logicTime;

            // This is a sanity check for incorrect process configuration
            if(processId >=
                SimulationParameters::bioProcessRegistry.getSize()) {

                LOG_ERROR(
                    "ProcessSystem: Run: entity: " +
                    std::to_string(value.first) + " has invalid process: " +
                    std::to_string(processId) + ", process count is: " +
                    std::to_string(
                        SimulationParameters::bioProcessRegistry.getSize()));
                continue;
            }

            // This does a map lookup so only do this once
            const auto& processData =
                SimulationParameters::bioProcessRegistry.getTypeData(processId);

            // Can your cell do the process
            bool canDoProcess = true;

            // Loop through to make sure you can follow through with your
            // whole process so nothing gets wasted as that would be
            // frusterating, its two more for loops, yes but it should only
            // really be looping at max two or three times anyway. also make
            // sure you wont run out of space when you do add the compounds.
            // Input
            // Defaults to 1
            double environmentModifier = 1.0f;

            for(const auto& input : processData.inputs) {

                const CompoundId inputId = input.first;
                auto compoundData =
                    SimulationParameters::compoundRegistry.getTypeData(inputId);
                // Set price of used compounds to 1, we dont want to purge
                // those
                bag.compounds[inputId].price = 1;

                double inputRemoved =
                    ((input.second * processCapacity) / (processLimitCapacity));

                // do environmental modifier here, and save it for later
                if(compoundData.isEnvironmental) {
                    environmentModifier =
                        environmentModifier *
                        (getDissolved(inputId) / input.second);
                    inputRemoved = inputRemoved * environmentModifier;
                }

                // If not enough compound we can't do the process
                // If the compound is environmental the cell doesnt actually
                // contain it right now and theres no where to take it from
                if(!compoundData.isEnvironmental) {
                    if(bag.compounds[inputId].amount < inputRemoved ||
                        environmentModifier == 0.0f) {
                        canDoProcess = false;
                    }
                }
            }

            // Output
            // This is now always looped because the is useful part is always
            // done
            for(const auto& output : processData.outputs) {

                const CompoundId outputId = output.first;
                auto compoundData =
                    SimulationParameters::compoundRegistry.getTypeData(
                        outputId);
                // For now lets assume compounds we produce are also
                // useful
                bag.compounds[outputId].price = 1;

                double outputAdded = ((output.second * processCapacity) /
                                      (processLimitCapacity));
                // Apply the environmental modifier
                outputAdded = outputAdded * environmentModifier;

                // If no space we can't do the process, and if environmental
                // right now this isnt released anywhere
                if(!compoundData.isEnvironmental) {
                    if((bag.getCompoundAmount(outputId) + outputAdded >
                           bag.storageSpace) ||
                        environmentModifier == 0.0f) {
                        canDoProcess = false;
                    }
                }
            }

            // Only carry out this process if you have all the required
            // ingredients and enough space for the outputs
            if(canDoProcess) {
                // Inputs.
                for(const auto& input : processData.inputs) {
                    const CompoundId inputId = input.first;
                    auto compoundData =
                        SimulationParameters::compoundRegistry.getTypeData(
                            inputId);
                    double inputRemoved = ((input.second * processCapacity) /
                                           (processLimitCapacity));

                    // Apply the environmental modifier
                    inputRemoved = inputRemoved * environmentModifier;

                    // This should always be true (due to the earlier check) so
                    // it is always assumed here that the process succeeded
                    if(!compoundData.isEnvironmental) {
                        if(bag.compounds[inputId].amount >= inputRemoved) {
                            bag.compounds[inputId].amount -= inputRemoved;
                        }
                    }
                }

                // Outputs.
                for(const auto& output : processData.outputs) {
                    const CompoundId outputId = output.first;
                    auto compoundData =
                        SimulationParameters::compoundRegistry.getTypeData(
                            outputId);
                    double outputGenerated =
                        ((output.second * processCapacity) /
                            (processLimitCapacity));

                    // Apply the environmental modifier
                    outputGenerated = outputGenerated * environmentModifier;

                    if(!compoundData.isEnvironmental) {
                        bag.compounds[outputId].amount += outputGenerated;
                    }
                }
            }
        }

        // Making sure the compound amount is not negative.
        for(auto& compound : bag.compounds) {
            CompoundData& compoundData = compound.second;

            if(compoundData.amount < 0) {
                LOG_ERROR("ProcessSystem: Run: entity: " +
                          std::to_string(value.first) +
                          " has negative amount of compound: " +
                          std::to_string(compound.first) +
                          ", amount: " + std::to_string(compoundData.amount));

                compoundData.amount = 0.0;
            }

            // TODO: fix this comment I (hhyyrylainen) have no idea
            // what this does or why this is here:
            // That way we always have a running tally of what process was set
            // to what despite clearing the price every run cycle
            compoundData.usedLastTime = compoundData.price;
        }
    }
}
