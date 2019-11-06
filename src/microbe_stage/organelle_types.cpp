#include "organelle_types.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"

#include "Common/Types.h"

#include <map>
#include <string>
#include <vector>

using namespace thrive;

OrganelleType::OrganelleType() {}

OrganelleType::OrganelleType(Json::Value value)
{
    name = value["name"];
    gene = value["gene"].asString();
    mesh = value["mesh"].asString();
    texture = value["texture"].asString();
    mass = value["mass"].asFloat();
    chanceToCreate = value["chanceToCreate"].asFloat();
    prokaryoteChance = value["prokaryoteChance"].asFloat();

    Json::Value componentData = value["components"];
    std::vector<std::string> componentNames = componentData.getMemberNames();

    for(std::string componentName : componentNames) {
        Json::Value parameterData = componentData[componentName];
        Json::Value parameterNames = parameterData.getMemberNames();
        std::map<std::string, double> parameters;

        for(std::string parameterName : parameterNames) {
            parameters[parameterName] = parameterData[parameterName].asDouble();
        }

        components[componentName] = parameters;
    }

    Json::Value processData = value["processes"];
    std::vector<std::string> processNames = processData.getMemberNames();

    for(std::string processName : processNames) {
        processes[processName] = processData[processName].asDouble();
    }

    // Getting the hex information.
    Json::Value hexArray = value["hexes"];

    for(Json::Value hex : hexArray) {
        hexes.push_back(Int2(hex["q"].asInt(), hex["r"].asInt()));
    }

    // Getting the initial composition information.
    Json::Value initialCompositionData = value["initialComposition"];
    std::vector<std::string> compoundInternalNames =
        initialCompositionData.getMemberNames();

    for(std::string compoundInternalName : compoundInternalNames) {
        // Getting the compound id from the compound registry.
        size_t id = SimulationParameters::compoundRegistry
                        .getTypeData(compoundInternalName)
                        .id;

        initialComposition[id] =
            initialCompositionData[compoundInternalName].asDouble();
    }

    mpCost = value["mpCost"].asInt();
}
