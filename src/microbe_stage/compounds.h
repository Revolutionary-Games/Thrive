#pragma once

#include "general/json_registry.h"

#include <Common/Types.h>

namespace thrive {

class Compound : public RegistryType {
public:
    double volume = 1.0;
    bool isCloud = false;
    bool isUseful = false;
    bool isEnvironmental = false;
    Float4 colour;

    Compound();

    //! \brief Constructor for test use
    Compound(size_t id,
        const std::string& name,
        bool isCloud,
        bool isUseful,
        bool isEnvironmental,
        Float4 colour);

    Compound(Json::Value value);
};

} // namespace thrive
