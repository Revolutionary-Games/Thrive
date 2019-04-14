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
    double density = 1.0f;
    double dissolved = 0.0f;

    BiomeCompoundData() {}

    BiomeCompoundData(unsigned int amount, double density, double dissolved) :
        amount(amount), density(density), dissolved(dissolved)
    {}
};

class SimulationParameters;

class Biome : public RegistryType {
public:
    std::map<size_t, BiomeCompoundData> compounds;
    std::string background = "error";

    Ogre::ColourValue specularColors;
    Ogre::ColourValue diffuseColors;
    // Ambient Lights
    Ogre::ColourValue upperAmbientColor;
    Ogre::ColourValue lowerAmbientColor;

    float lightPower;
    Biome();

    Biome(Json::Value value);

    BiomeCompoundData*
        getCompound(size_t type);
    CScriptArray*
        getCompoundKeys() const;
};

} // namespace thrive
