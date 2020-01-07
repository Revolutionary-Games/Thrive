#pragma once

#include "general/json_registry.h"

#include <Common/Types.h>

namespace thrive {

class MembraneType : public RegistryType {
public:
    bool cellWall = false;
    std::string normalTexture;
    std::string damagedTexture;
    float movementFactor = 1;
    float osmoregulationFactor = 1;
    float resourceAbsorptionFactor = 1;
    float hitpoints = 100;
    float physicalResistance = 1;
    float toxinResistance = 1;
    int editorCost = 50;

    MembraneType();

    MembraneType(Json::Value value);
};

} // namespace thrive
