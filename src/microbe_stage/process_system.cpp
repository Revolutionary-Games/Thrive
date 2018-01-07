#include <iostream>
#include <cmath>
#include <algorithm>
#include <map>

#include "engine/serialization.h"
#include "game.h"

#include "general/thrive_math.h"
#include "simulation_parameters.h"

#include "microbe_stage/compound_registry.h"
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/process_system.h"

using namespace thrive;

ProcessorComponent::ProcessorComponent()
	: Leviathan::Component(TYPE)
{}

/*
void
ProcessorComponent::load(const StorageContainer& storage)
{
    Component::load(storage);
    StorageContainer processes = storage.get<StorageContainer>("processes");
    for (const std::string& id : processes.keys())
    {
        this->process_capacities[std::atoi(id.c_str())] = processes.get<double>(id);
	}
}

StorageContainer
ProcessorComponent::storage() const
{
	StorageContainer storage = Component::storage();

	StorageContainer processes;
    for (auto entry : this->process_capacities) {
        processes.set<double>(std::to_string(static_cast<int>(entry.first)), entry.second);
    }
    storage.set<StorageContainer>("processes", processes);


	return storage;
}
*/

void
ProcessorComponent::setCapacity(size_t id, double capacity)
{
    this->process_capacities[id] = capacity;
}

CompoundBagComponent::CompoundBagComponent()
	: Leviathan::Component(TYPE) {
    storageSpace = 0;
    storageSpaceOccupied = 0;

	for (size_t id = 0; id < SimulationParameters::compoundRegistry.getSize(); id++) {
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
    StorageContainer uninflatedPrices = storage.get<StorageContainer>("uninflatedPrices");
    StorageContainer demand = storage.get<StorageContainer>("demand");

    for (const std::string& id : amounts.keys())
    {
        CompoundId compoundId = std::atoi(id.c_str());
        this->compounds[compoundId].amount = amounts.get<double>(id);
        this->compounds[compoundId].price = prices.get<double>(id);
        this->compounds[compoundId].uninflatedPrice = uninflatedPrices.get<double>(id);
        this->compounds[compoundId].demand = demand.get<double>(id);
	}

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

    return storage;
}
*/

// helper methods for integrating compound bags with current, un-refactored, lua microbes
double
CompoundBagComponent::getCompoundAmount(size_t id) {
    return compounds[id].amount;
}

void
CompoundBagComponent::giveCompound(size_t id, double amt) {
    compounds[id].amount += amt;
}

double
CompoundBagComponent::takeCompound(size_t id, double to_take) {
    double& ref = compounds[id].amount;
    double amt = ref > to_take ? to_take : ref;
    ref -= amt;
    return amt;
}

double
CompoundBagComponent::getPrice(size_t compoundId) {
    return compounds[compoundId].price;
}

double
CompoundBagComponent::getDemand(size_t compoundId) {
    return compounds[compoundId].demand;
}

// ------------------------------------ //
// ProcessSystem
double
ProcessSystem::_demandSofteningFunction(double processCapacity) {
    return 2 * sigmoid(processCapacity * PROCESS_CAPACITY_DEMAND_MULTIPLIER) - 1.0;
}


double
ProcessSystem::_calculatePrice(double oldPrice, double supply, double demand) {
    // double priceAdjustment = sqrt(demand / (supply + 1));
    // return oldPrice * (COMPOUND_PRICE_MOMENTUM + priceAdjustment -
    // COMPOUND_PRICE_MOMENTUM * priceAdjustment);
    //(void)oldPrice;
    return sqrt(demand / (supply + 1)) * COMPOUND_PRICE_MOMENTUM + oldPrice * (1.0 -
        COMPOUND_PRICE_MOMENTUM);
}

std::map<double, CompoundId>
ProcessSystem::_getBreakEvenPointMap(
    BioProcessId processId,
    CompoundBagComponent &bag
) {
    std::map<double, CompoundId> outputBreakEvenPoints;

    for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
        CompoundId outputId = output.first;
        int outputGenerated = output.second;
        CompoundData &compoundData = bag.compounds[outputId];

        double breakEvenPoint = compoundData.breakEvenPoint / outputGenerated;
        outputBreakEvenPoints[breakEvenPoint] = outputId;
    }

    return outputBreakEvenPoints;
}

double
ProcessSystem::_spaceSofteningFunction(double availableSpace, double requiredSpace) {
    return 2.0 * (1.0 - sigmoid(requiredSpace / (availableSpace + 1.0) *
            STORAGE_SPACE_MULTIPLIER));
    //double MIN_AVAILABLE_SPACE = 0.001;
    //return 1.0 / (1 + requiredSpace / std::max(availableSpace, MIN_AVAILABLE_SPACE));
}

double
ProcessSystem::_getOptimalProcessRate(
    BioProcessId processId,
    CompoundBagComponent &bag,
    bool considersSpaceLimitations,
    double availableSpace
) {
    // Calculating the price increment and the base price of the inputs
    // (the total price is rate * priceIncrement + basePrice).
    double baseInputPrice = 0;
    double inputPriceIncrement = 0;
    for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
        CompoundId inputId = input.first;
        int inputNeeded = input.second;
        CompoundData &compoundData = bag.compounds[inputId];
        double inputVolume = CompoundRegistry::getCompoundUnitVolume(inputId);

        if(considersSpaceLimitations) {
            double spacePriceDecrement = ProcessSystem::_spaceSofteningFunction(
                availableSpace, inputNeeded * inputVolume);
            inputPriceIncrement += inputNeeded * compoundData.priceReductionPerUnit *
                spacePriceDecrement;
            baseInputPrice += inputNeeded * compoundData.price * spacePriceDecrement;
        }

        else {
            inputPriceIncrement += inputNeeded * compoundData.priceReductionPerUnit;
            baseInputPrice += inputNeeded * compoundData.price;
        }
    }

    // Finding the rate at which the costs equal the benefits.
    // The benefit curve is piecewise lineal and continuous, and the breaking points are
    // the break-even points of the output compounds.
    // So first we have to order said break-even points.
    std::map<double, CompoundId> outputBreakEvenPoints = ProcessSystem::_getBreakEvenPointMap(
        processId, bag);

    // Finding the piece of the function that contains the minimum
    // TODO: make it use binary search or something...
    double baseOutputPrice = 0.0;
    double outputPriceDecrement = 0.0;

    // Getting the initial revenue values
    for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
        CompoundId outputId = output.first;
        int outputGenerated = output.second;
        CompoundData &compoundData = bag.compounds[outputId];
        double outputVolume = CompoundRegistry::getCompoundUnitVolume(outputId);

        if(considersSpaceLimitations) {
            double spacePriceDecrement = ProcessSystem::_spaceSofteningFunction(
                availableSpace, outputGenerated * outputVolume);
            baseOutputPrice += compoundData.price * outputGenerated * spacePriceDecrement;
            outputPriceDecrement += compoundData.priceReductionPerUnit * outputGenerated *
                spacePriceDecrement;
        }

        else {
            baseOutputPrice += compoundData.price * outputGenerated;
            outputPriceDecrement += compoundData.priceReductionPerUnit * outputGenerated;
        }
    }

    for (const auto& breakingPoint : outputBreakEvenPoints) {
        double breakEvenPoint = breakingPoint.first;

        // Calculating the cost.
        double cost = baseInputPrice + breakEvenPoint * inputPriceIncrement;

        // Calculating the revenue.
        double baseOutputPrice_l = 0.0;
        double outputPriceDecrement_l = 0.0;
        for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
            CompoundId outputId = output.first;
            int outputGenerated = output.second;
            CompoundData &compoundData = bag.compounds[outputId];
            double outputVolume = CompoundRegistry::getCompoundUnitVolume(outputId);

            // The prices are never below 0.
            if(compoundData.breakEvenPoint > breakEvenPoint) {
                if(considersSpaceLimitations) {
                    double spacePriceDecrement = ProcessSystem::_spaceSofteningFunction(
                        availableSpace, outputGenerated * outputVolume);
                    baseOutputPrice_l += compoundData.price * outputGenerated *
                        spacePriceDecrement;
                    outputPriceDecrement_l += compoundData.priceReductionPerUnit *
                        outputGenerated * spacePriceDecrement;
                }

                else {
                    baseOutputPrice_l += compoundData.price * outputGenerated ;
                    outputPriceDecrement_l += compoundData.priceReductionPerUnit *
                        outputGenerated;
                }
            }
        }

        double revenue = baseOutputPrice_l - breakEvenPoint * outputPriceDecrement_l;

        if(revenue < cost)
            // We found the piece :)
            break;

        baseOutputPrice = baseOutputPrice_l;
        outputPriceDecrement = outputPriceDecrement_l;
    }

    // Avoiding zero-division errors.
    double desiredRate;
    if(outputPriceDecrement + inputPriceIncrement > 0)
        desiredRate = (baseOutputPrice - baseInputPrice) / (outputPriceDecrement +
            inputPriceIncrement);
    else
        desiredRate = 0.0;
    if(desiredRate <= 0.0) return 0.0;
    return desiredRate;
}

void
ProcessSystem::Run(
    GameWorld &world
) {
    const auto logicTime = Leviathan::TICKSPEED;
    
    //Iterating on each entity with a CompoundBagComponent and a ProcessorComponent
    for (auto& value : CachedComponents.GetIndex()) {
        CompoundBagComponent& bag = std::get<0>(*value.second);
        ProcessorComponent& processor = std::get<1>(*value.second);

        // Calculating the storage space occupied;
        bag.storageSpaceOccupied = 0;
        for (const auto& compound : bag.compounds) {
            double compoundAmount = compound.second.amount;
            bag.storageSpaceOccupied += compoundAmount;
        }

        // Calculating the storage space available. The storage space capacity is increased
        double storageSpaceAvailable = std::max(bag.storageSpace - bag.storageSpaceOccupied,
            0.0);

        // Phase one: setting up the compound information.
        for (const auto& compound : bag.compounds) {
            CompoundId compoundId = compound.first;
            CompoundData &compoundData = bag.compounds[compoundId];

            // Edge case to get the prices above 0 if some demand exists.
            if(compoundData.demand > 0 && compoundData.uninflatedPrice <= 0)
                compoundData.uninflatedPrice = MIN_POSITIVE_COMPOUND_PRICE;

            // Adjusting the prices according to supply and demand.
            double oldPrice = compoundData.uninflatedPrice;
            compoundData.uninflatedPrice =  ProcessSystem::_calculatePrice(oldPrice,
                compoundData.amount, compoundData.demand);

            if(compoundData.demand > 0 && compoundData.uninflatedPrice <=
                MIN_POSITIVE_COMPOUND_PRICE)
            {
                compoundData.uninflatedPrice = MIN_POSITIVE_COMPOUND_PRICE;
            }

            // Setting the prices to 0 if they're below MIN_POSITIVE_COMPOUND_PRICE.
            if(compoundData.uninflatedPrice < MIN_POSITIVE_COMPOUND_PRICE) {
                compoundData.uninflatedPrice = 0;
                compoundData.priceReductionPerUnit = 0;
            }

            // Calculating how much the price would fall if we had one more unit,
            // To make predictions with the demand.
            else {
                double reducedPrice =  ProcessSystem::_calculatePrice(oldPrice,
                    compoundData.amount + 1, compoundData.demand);
                
                compoundData.priceReductionPerUnit = compoundData.uninflatedPrice -
                    reducedPrice;
            }

            //Inflating the price if the compound is useful outside of this system.
            compoundData.price = compoundData.uninflatedPrice;
            if(CompoundRegistry::isUseful(compoundId))
            {
                compoundData.price += (IMPORTANT_COMPOUND_BIAS + bag.storageSpace) /
                    (compoundData.amount + 1);
                double reducedPrice = (IMPORTANT_COMPOUND_BIAS + bag.storageSpace) /
                    (compoundData.amount + 2);
                compoundData.priceReductionPerUnit += compoundData.price - reducedPrice;
            }

            // Calculating the break-even point
            if(compoundData.price <= 0.0)
                compoundData.breakEvenPoint = 0;
            else
                compoundData.breakEvenPoint = compoundData.price /
                    compoundData.priceReductionPerUnit;

            // Setting the demand to 0 in order to recalculate it later.
            compoundData.demand = 0;
        }

        // Phase two: setting up the processes.
        for (const auto& process : processor.process_capacities) {
            BioProcessId processId = process.first;
            double processCapacity = process.second;

            double processLimitCapacity = processCapacity * logicTime; // big enough number.

            for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
                CompoundId inputId = input.first;
                int inputNeeded = input.second;

                // Limiting the process by the amount of this required compound.
                processLimitCapacity = std::min(processLimitCapacity,
                    bag.compounds[inputId].amount / inputNeeded);
            }

            // Calculating the desired rate, with some liberal use of linearization.

            // Calculating the optimal process rate without considering the storage space.
            double desiredRate = ProcessSystem::_getOptimalProcessRate(
                processId,
                bag,
                false,
                storageSpaceAvailable);

            // Calculating the optimal process rate considering the storage space.
            double desiredRateWithSpace = ProcessSystem::_getOptimalProcessRate(
                processId,
                bag,
                true,
                storageSpaceAvailable);

            desiredRateWithSpace = std::min(desiredRateWithSpace, desiredRate);
            if(desiredRate > 0.0)
            {
                double rate = std::min(processCapacity * logicTime / 1000,
                    processLimitCapacity);
                rate = std::min(rate, desiredRateWithSpace);

                // Running the process at the specified rate, transforming the inputs...
                for (const auto& input : BioProcessRegistry::getInputCompounds(processId)) {
                    CompoundId inputId = input.first;
                    int inputNeeded = input.second;
                    bag.compounds[inputId].amount -= rate * inputNeeded;

                    // Phase 3: increasing the input compound demand.
                    bag.compounds[inputId].demand += desiredRate * inputNeeded *
                        ProcessSystem::_demandSofteningFunction(processCapacity * inputNeeded);
                }

                // ...into the outputs.
                for (const auto& output : BioProcessRegistry::getOutputCompounds(processId)) {
                    CompoundId outputId = output.first;
                    int outputGenerated = output.second;
                    bag.compounds[outputId].amount += rate * outputGenerated;
                }
            }
        }

        // Making sure the compound amount is not negative.
        for (const auto& compound : bag.compounds) {
            CompoundId compoundId = compound.first;
            CompoundData &compoundData = bag.compounds[compoundId];
            compoundData.amount = std::max(compoundData.amount, 0.0);
        }
    }
}

