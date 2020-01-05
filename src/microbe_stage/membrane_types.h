#pragma once

#include "general/json_registry.h"

#include <Common/Types.h>

namespace thrive {

class MembraneType : public RegistryType {
public:
    bool cellWall = false;
    std::string normalTexture;
    std::string damagedTexture;
    float hitpoints = 100;

    MembraneType();

    MembraneType(Json::Value value);
};

} // namespace thrive
