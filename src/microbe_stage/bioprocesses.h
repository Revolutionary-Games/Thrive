#pragma once

#include "general/json_registry.h"

#include <map>

namespace thrive {

class SimulationParameters;

class BioProcess : public RegistryType {
public:
    // The amount of compounds required/obtained.
    std::map<size_t, unsigned int> inputs;
    std::map<size_t, unsigned int> outputs;

    BioProcess();

    BioProcess(Json::Value value);
};

} // namespace thrive
