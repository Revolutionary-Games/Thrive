#pragma once

#include "general/json_registry.h"

#include <OgreColourValue.h>
#include <map>
#include <string>

class CScriptArray;

namespace thrive {

struct BiomeCompoundData {
public:
    unsigned int amount = 0;
    double density = 1.0;

    BiomeCompoundData() {}

    BiomeCompoundData(unsigned int amount, double density) :
        amount(amount), density(density)
    {
    }
};

class SimulationParameters;

class Biome : public RegistryType {
public:
    std::map<size_t, BiomeCompoundData> compounds;
    std::string background = "error";

    Ogre::ColourValue specularColors;
    Ogre::ColourValue diffuseColors;

    Biome();

    Biome(Json::Value value);

    BiomeCompoundData*
        getCompound(size_t type);
    CScriptArray*
        getCompoundKeys() const;
};

} // namespace thrive
