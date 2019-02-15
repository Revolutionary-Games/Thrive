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

    //! \brief Constructor for test use
    Compound(size_t id,
        const std::string& name,
        bool isCloud,
        bool isUseful,
        bool isEnvironmental,
        Ogre::ColourValue colour);

    Compound(Json::Value value);
};

} // namespace thrive
