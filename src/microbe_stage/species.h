#pragma once

#include "general/json_registry.h"

#include "membrane_system.h"

#include <OgreColourValue.h>
#include <map>
#include <string>
#include <vector>

namespace thrive {

class SimulationParameters;

class Species : public RegistryType {
public:
    double spawnDensity = 0.0;
    Ogre::ColourValue colour;
    bool isBacteria;
    std::string genus;
    std::string epithet;
    double fear;
    double aggression;
    double opportunism;
    double activity;
    double focus;
    MEMBRANE_TYPE speciesMembraneType;
    std::map<size_t, unsigned int> startingCompounds;
    std::map<int, size_t> organelles; // TODO: get a position as the key.

    Species();

    Species(Json::Value value);
};

} // namespace thrive
