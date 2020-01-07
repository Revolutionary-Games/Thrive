// ------------------------------------ //
#include "process_system.h"

#include "general/thrive_math.h"
#include "simulation_parameters.h"

#include <Entities/GameWorld.h>

#include <json/json.h>

#include <algorithm>
#include <cmath>
#include <iostream>
#include <map>

using namespace thrive;
// ------------------------------------ //
// Constants for computing energy balances
constexpr auto FLAGELLA_COMPONENT_NAME = "movement";

// These constants must match what is in configs.as
constexpr auto FLAGELLA_ENERGY_COST = 7.1f;
constexpr auto ATP_COST_FOR_OSMOREGULATION = 1.0f;
constexpr auto BASE_MOVEMENT_ATP_COST = 1.0f;

// ------------------------------------ //
ProcessorComponent::ProcessorComponent() : Leviathan::Component(TYPE) {}

ProcessorComponent::ProcessorComponent(ProcessorComponent&& other) noexcept :
    Leviathan::Component(TYPE), m_processRates(std::move(other.m_processRates))
{}
// ------------------------------------ //
ProcessorComponent&
    ProcessorComponent::operator=(const ProcessorComponent& other)
{
    m_processRates = other.m_processRates;
    return *this;
}

ProcessorComponent&
    ProcessorComponent::operator=(ProcessorComponent&& other) noexcept
{
    m_processRates = std::move(other.m_processRates);
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
// ------------------------------------ //
// ProcessSystem
void
    ProcessSystem::Run(GameWorld& world, float elapsed)
{
    if(!world.GetNetworkSettings().IsAuthoritative)
        return;

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

        for(const auto& [processId, processRate] : processor.m_processRates) {
            // If rate is 0 dont do it
            // The rate specifies how fast fraction of the specified process
            // numbers this cell can do
            if(processRate <= 0.0f)
                continue;

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
            float environmentModifier = 1.0f;

            for(const auto [inputId, inputAmount] : processData.inputs) {

                auto compoundData =
                    SimulationParameters::compoundRegistry.getTypeData(inputId);
                // Set price of used compounds to 1, we dont want to purge
                // those
                bag.compounds[inputId].price = 1;

                const auto inputRemoved = inputAmount * processRate * elapsed;

                // do environmental modifier here, and save it for later
                if(compoundData.isEnvironmental) {
                    environmentModifier *= getDissolved(inputId) / inputAmount;
                } else {
                    // If not enough compound we can't do the process
                    // If the compound is environmental the cell doesnt actually
                    // contain it right now and theres no where to take it from
                    if(bag.compounds[inputId].amount < inputRemoved) {
                        canDoProcess = false;
                    }
                }
            }

            if(environmentModifier <= Leviathan::EPSILON) {
                canDoProcess = false;
            }

            // Output
            // This is now always looped (even when we can't do the process)
            // because the is useful part is needs to be always be done
            for(const auto [outputId, outputAmount] : processData.outputs) {

                auto compoundData =
                    SimulationParameters::compoundRegistry.getTypeData(
                        outputId);
                // For now lets assume compounds we produce are also
                // useful
                bag.compounds[outputId].price = 1;

                // Apply the general modifiers and
                // apply the environmental modifier
                const auto outputAdded =
                    outputAmount * processRate * elapsed * environmentModifier;

                // If no space we can't do the process, and if environmental
                // right now this isnt released anywhere
                if(compoundData.isEnvironmental) {
                    continue;
                }

                if((bag.getCompoundAmount(outputId) + outputAdded >
                       bag.storageSpace)) {
                    canDoProcess = false;
                }
            }

            // Only carry out this process if you have all the required
            // ingredients and enough space for the outputs
            if(canDoProcess) {
                // Inputs.
                for(const auto [inputId, inputAmount] : processData.inputs) {
                    auto compoundData =
                        SimulationParameters::compoundRegistry.getTypeData(
                            inputId);

                    if(compoundData.isEnvironmental)
                        continue;

                    // Note: the enviroment modifier is applied here, but not
                    // when checking if we have enough compounds. So sometimes
                    // we might not run a process when we actually would have
                    // enough compounds to run it
                    const auto inputRemoved = inputAmount * processRate *
                                              elapsed * environmentModifier;

                    // This should always be true (due to the earlier check) so
                    // it is always assumed here that the process succeeded
                    if(bag.compounds[inputId].amount >= inputRemoved) {
                        bag.compounds[inputId].amount -= inputRemoved;
                    }
                }

                // Outputs.
                for(const auto [outputId, outputAmount] : processData.outputs) {
                    auto compoundData =
                        SimulationParameters::compoundRegistry.getTypeData(
                            outputId);

                    if(compoundData.isEnvironmental)
                        continue;

                    const auto outputGenerated = outputAmount * processRate *
                                                 elapsed * environmentModifier;

                    bag.compounds[outputId].amount += outputGenerated;
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
// ------------------------------------ //
void
    ProcessSystem::setProcessBiome(const Biome& biome)
{
    currentBiome = biome;
}

double
    ProcessSystem::getDissolved(CompoundId compoundData)
{
    return currentBiome.getCompound(compoundData)->dissolved;
}
// ------------------------------------ //
Json::Value
    calculateProcessMaximumSpeed(const TweakedProcess::pointer& process,
        const Biome& biome)
{
    Json::Value result;

    const float multiplier = process->getTweakRate();

    float speedFactor = 1.f;

    Json::Value environmentInputs;
    Json::Value inputs;

    // Environmental inputs need to be processed first
    for(const auto& [compoundId, amount] : process->process.inputs) {
        const auto& data =
            SimulationParameters::compoundRegistry.getTypeData(compoundId);
        if(!data.isEnvironmental)
            continue;

        // Environmental compound that can limit the rate

        Json::Value obj;

        obj["id"] = compoundId;
        obj["amount"] = amount;
        obj["name"] = data.displayName;

        const auto availableInEnvironment =
            biome.getCompound(compoundId)->dissolved;

        obj["availableAmount"] = availableInEnvironment;

        // More than needed environment value boosts the effectiveness
        float availableRate = availableInEnvironment / amount;

        obj["availableRate"] = availableRate;

        speedFactor *= availableRate;

        environmentInputs[data.internalName] = obj;
    }

    speedFactor *= multiplier;

    // So that the speedfactor is available here
    for(const auto& [compoundId, amount] : process->process.inputs) {
        const auto& data =
            SimulationParameters::compoundRegistry.getTypeData(compoundId);
        if(data.isEnvironmental)
            continue;

        // Normal, cloud input

        Json::Value obj;
        obj["id"] = compoundId;
        obj["amount"] = amount * speedFactor;
        obj["name"] = data.displayName;

        inputs[data.internalName] = obj;
    }

    Json::Value outputs;

    for(const auto& [compoundId, amount] : process->process.outputs) {
        Json::Value obj;

        obj["id"] = compoundId;
        obj["amount"] = amount * speedFactor;

        const auto& data =
            SimulationParameters::compoundRegistry.getTypeData(compoundId);

        obj["name"] = data.displayName;

        outputs[data.internalName] = obj;
    }

    result["name"] = process->process.displayName;
    result["processName"] = process->process.internalName;
    result["processId"] = process->process.id;
    result["inputs"] = inputs;
    result["outputs"] = outputs;
    result["environment"] = environmentInputs;
    result["speedFactor"] = speedFactor;

    return result;
}

std::string
    ProcessSystem::computeOrganelleProcessEfficiencies(
        const std::vector<OrganelleTemplate::pointer>& organelles,
        const Biome& biome) const
{
    Json::Value value(Json::objectValue);
    Json::Value organellesData(Json::objectValue);
    Json::Value errors(Json::arrayValue);

    for(const auto& organelle : organelles) {
        if(!organelle) {
            errors.append(Json::Value("organelle pointer is null"));
            continue;
        }

        const auto& name = organelle->getName();

        Json::Value obj;
        Json::Value processes;

        for(const auto& process : organelle->getProcesses()) {

            processes.append(calculateProcessMaximumSpeed(process, biome));
        }

        obj["processes"] = processes;

        organellesData[name] = obj;
    }

    value["organelles"] = organellesData;

    if(errors.size() > 0)
        value["errors"] = errors;

    std::stringstream sstream;
    Json::StreamWriterBuilder builder;
    builder["indentation"] = "";
    std::unique_ptr<Json::StreamWriter> writer(builder.newStreamWriter());

    writer->write(value, &sstream);

    return sstream.str();
}
// ------------------------------------ //
std::string
    ProcessSystem::computeEnergyBalance(
        const std::vector<OrganelleTemplate::pointer>& organelles, const MembraneType& membraneType,
        const Biome& biome) const
{
    Json::Value value(Json::objectValue);
    Json::Value production(Json::objectValue);
    Json::Value consumption(Json::objectValue);
    Json::Value errors(Json::arrayValue);

    float totalATPProduction = 0.f;
    float processATPConsumption = 0.f;
    float movementATPConsumption = 0.f;

    int hexCount = 0;

    for(const auto& organelle : organelles) {
        if(!organelle) {
            errors.append(Json::Value("organelle pointer is null"));
            continue;
        }

        const auto& name = organelle->getName();

        // This uses the same efficiency computation as
        // computeOrganelleProcessEfficiencies and just reads data back from the
        // json results, because I'm too lazy to generalize the efficiency
        // computation function
        for(const auto& process : organelle->getProcesses()) {
            const auto processData =
                calculateProcessMaximumSpeed(process, biome);

            // Find process inputs and outputs that use/produce ATP and add to
            // totals
            if(processData["inputs"]["atp"]) {
                const auto amount =
                    processData["inputs"]["atp"]["amount"].asFloat();

                processATPConsumption += amount;

                if(!consumption.isMember(name)) {
                    consumption[name] = 0.f;
                }

                consumption[name] = consumption[name].asFloat() + amount;
            }

            if(processData["outputs"]["atp"]) {
                const auto amount =
                    processData["outputs"]["atp"]["amount"].asFloat();

                totalATPProduction += amount;

                if(!production.isMember(name)) {
                    production[name] = 0.f;
                }

                production[name] = production[name].asFloat() + amount;
            }
        }

        // Take special cell components that take energy into account
        if(organelle->hasComponent(FLAGELLA_COMPONENT_NAME)) {
            const auto amount = FLAGELLA_ENERGY_COST;

            movementATPConsumption += amount;

            if(!consumption.isMember(name)) {
                consumption[name] = 0.f;
            }

            consumption[name] = consumption[name].asFloat() + amount;
        }

        // Store hex count
        hexCount += organelle->getHexCount();
    }

    // Add movement consumption together
    const float baseMovementCost = BASE_MOVEMENT_ATP_COST * hexCount;
    const auto totalMovementConsumption =
        movementATPConsumption + baseMovementCost;

    consumption["baseMovement"] = baseMovementCost;

    // Add osmoregulation
    const float osmoregulation = ATP_COST_FOR_OSMOREGULATION * hexCount * membraneType.osmoregulationFactor;

    consumption["osmoregulation"] = osmoregulation;

    // Compute totals
    const auto totalATPConsumption =
        processATPConsumption + totalMovementConsumption + osmoregulation;

    const auto totalBalanceStationary =
        totalATPProduction - totalATPConsumption;
    const auto totalBalance = totalBalanceStationary + totalMovementConsumption;

    // Finish building the result object
    value["production"] = production;
    value["consumption"] = consumption;
    value["total"]["production"] = totalATPProduction;
    value["total"]["consumption"] = totalATPConsumption;
    value["total"]["balance"] = totalBalance;
    value["total"]["balanceStationary"] = totalBalanceStationary;

    if(errors.size() > 0)
        value["errors"] = errors;

    std::stringstream sstream;
    Json::StreamWriterBuilder builder;
    builder["indentation"] = "";
    std::unique_ptr<Json::StreamWriter> writer(builder.newStreamWriter());

    writer->write(value, &sstream);

    return sstream.str();
}
