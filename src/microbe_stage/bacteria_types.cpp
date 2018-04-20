#include "bacteria_types.h"
#include "bioprocesses.h"
#include "compounds.h"
#include "simulation_parameters.h"

using namespace thrive;

BacteriaType::BacteriaType() {}

BacteriaType::BacteriaType(Json::Value value)
{
    capacity = value["capacity"].asUInt();
    mass = value["mass"].asDouble();
    health = value["health"].asUInt();
    spawnDensity = value["spawnDensity"].asDouble();

    // Getting the starting compounds.
    std::vector<std::string> compoundInternalNames =
        value["startingCompounds"].getMemberNames();
    for(std::string compoundInternalName : compoundInternalNames) {
        unsigned int amount =
            value["startingCompounds"][compoundInternalName].asUInt();

        // Getting the compound id from the compound registry.
        size_t id = SimulationParameters::compoundRegistry
                        .getTypeData(compoundInternalName)
                        .id;

        startingCompounds.emplace(id, amount);
    }

    // Getting the compounds that compose this bacterium.
    compoundInternalNames = value["composition"].getMemberNames();
    for(std::string compoundInternalName : compoundInternalNames) {
        unsigned int amount =
            value["composition"][compoundInternalName].asUInt();

        // Getting the compound id from the compound registry.
        size_t id = SimulationParameters::compoundRegistry
                        .getTypeData(compoundInternalName)
                        .id;

        composition.emplace(id, amount);
    }

    // Getting the compounds that compose this bacterium.
    std::vector<std::string> processInternalNames =
        value["processes"].getMemberNames();
    for(std::string processInternalName : processInternalNames) {
        double amount = value["processes"][processInternalName].asDouble();

        size_t id = SimulationParameters::bioProcessRegistry
                        .getTypeData(processInternalName)
                        .id;

        processes.emplace(id, amount);
    }
}
