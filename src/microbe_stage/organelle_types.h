#pragma once

#include "general/json_registry.h"

#include "Common/Types.h"

#include <map>
#include <string>
#include <vector>

namespace thrive {

class SimulationParameters;

class OrganelleType : public RegistryType {
public:
    OrganelleType();

    OrganelleType(Json::Value value);

    std::string name = "invalid";
    std::string gene = "invalid";
    std::string mesh;
    std::string texture = "unset";
    float mass = 0;
    float chanceToCreate = 0;
    float prokaryoteChance = 0;
    std::map<std::string, std::map<std::string, double>> components;
    std::map<std::string, float> processes;
    std::vector<Int2> hexes;
    std::map<size_t, double> initialComposition;
    int mpCost = 0;
};

} // namespace thrive
