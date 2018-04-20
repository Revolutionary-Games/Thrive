#pragma once

#include "general/json_registry.h"

#include <jsoncpp/json.h>
#include <map>
#include <string>
#include <unordered_map>

namespace thrive {

class SimulationParameters;

class BacteriaType : public RegistryType {
public:
    std::string mesh = "default_bacterium.mesh"; // TODO: have a default mesh so
                                                 // we can test bacteria.
    unsigned int capacity = 100;
    double mass = 1.0;
    unsigned int health = 1;
    double spawnDensity = 0.0;

    std::map<size_t, double> processes;
    std::unordered_map<size_t, unsigned int> startingCompounds;
    std::unordered_map<size_t, unsigned int> composition;

    BacteriaType();

    BacteriaType(Json::Value value);
};

} // namespace thrive