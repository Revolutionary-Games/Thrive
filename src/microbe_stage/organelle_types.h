#pragma once

#include "general/json_registry.h"

#include <Common/Types.h>

#include <map>
#include <string>
#include <vector>

class CScriptArray;

namespace thrive {

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
    std::map<std::string, std::map<std::string, Json::Value>> components;
    std::map<std::string, float> processes;
    std::vector<Int2> hexes;
    std::map<std::string, double> initialComposition;
    int mpCost = 0;

    CScriptArray*
        getComponentKeys() const;

    CScriptArray*
        getComponentParameterKeys(std::string component);

    double
        getComponentParameterAsDouble(std::string component,
            std::string parameter);

    std::string
        getComponentParameterAsString(std::string component,
            std::string parameter);

    CScriptArray*
        getProcessKeys() const;

    float
        getProcessTweakRate(std::string process);

    CScriptArray*
        getHexes() const;

    CScriptArray*
        getInitialCompositionKeys() const;

    double
        getInitialComposition(std::string compound);
};

} // namespace thrive
