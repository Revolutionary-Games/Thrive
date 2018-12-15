#include "bioprocesses.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"

using namespace thrive;

BioProcess::BioProcess() {}

BioProcess::BioProcess(Json::Value value)
{
    // Getting the input compounds.
    std::vector<std::string> compoundInternalNames =
        value["inputs"].getMemberNames();
    for(std::string compoundInternalName : compoundInternalNames) {
        // I read that json sometiems has trouble interpretting 0.0 as a double,
        // so im adding this as a sanity check.
        double amount = 0.0f;
        amount = amount + value["inputs"][compoundInternalName].asDouble();

        // Getting the compound id from the compound registry.
        size_t id = SimulationParameters::compoundRegistry
                        .getTypeData(compoundInternalName)
                        .id;

        inputs.emplace(id, amount);
    }

    // Getting the output compounds.
    compoundInternalNames = value["outputs"].getMemberNames();
    for(std::string compoundInternalName : compoundInternalNames) {
        // I read that json sometiems has trouble interpretting 0.0 as a double,
        // so im adding this as a sanity check.
        double amount = 0.0f;
        amount = amount + value["outputs"][compoundInternalName].asDouble();

        // Getting the compound id from the compound registry.
        size_t id = SimulationParameters::compoundRegistry
                        .getTypeData(compoundInternalName)
                        .id;

        outputs.emplace(id, amount);
    }
}
