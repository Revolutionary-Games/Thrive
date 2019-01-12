#pragma once

#include "general/json_registry.h"

#include <OgreColourValue.h>

namespace thrive {

class Compound : public RegistryType {
public:
    double volume = 1.0;
    bool isCloud = false;
    bool isUseful = false;
    bool isEnvironmental = false;
    Ogre::ColourValue colour;

    Compound();

    Compound(Json::Value value);
};

} // namespace thrive
