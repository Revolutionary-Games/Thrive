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
        const std::vector<OrganelleTemplate::pointer>& organelles,
        const MembraneType& membraneType,
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
    const float osmoregulation = ATP_COST_FOR_OSMOREGULATION * hexCount *
                                 membraneType.osmoregulationFactor;

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
