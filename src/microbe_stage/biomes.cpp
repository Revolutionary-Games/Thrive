#include "biomes.h"
#include "microbe_stage/compounds.h"
#include "microbe_stage/simulation_parameters.h"

#include <Script/ScriptConversionHelpers.h>
#include <Script/ScriptExecutor.h>

#include <boost/range/adaptor/map.hpp>

#include <vector>

using namespace thrive;

Biome::Biome() {}

Biome::Biome(Json::Value value)
{
    background = value["background"].asString();


    // getting colour information
    Json::Value colorData = value["colors"];
    Json::Value dData = colorData["diffuseColors"];
    Json::Value sData = colorData["specularColors"];
    Json::Value uData = colorData["upperAmbientColor"];
    Json::Value lData = colorData["lowerAmbientColor"];

    // Light power
    lightPower = value["lightPower"].asFloat();

    float r = sData["r"].asFloat();
    float g = sData["g"].asFloat();
    float b = sData["b"].asFloat();
    specularColors = Ogre::ColourValue(r, g, b, 1.0);

    r = sData["r"].asFloat();
    g = sData["g"].asFloat();
    b = sData["b"].asFloat();
    diffuseColors = Ogre::ColourValue(r, g, b, 1.0);

    r = uData["r"].asFloat();
    g = uData["g"].asFloat();
    b = uData["b"].asFloat();
    upperAmbientColor = Ogre::ColourValue(r, g, b, 1.0);

    r = lData["r"].asFloat();
    g = lData["g"].asFloat();
    b = lData["b"].asFloat();
    lowerAmbientColor = Ogre::ColourValue(r, g, b, 1.0);

    // Getting the compound information.
    Json::Value compoundData = value["compounds"];
    std::vector<std::string> compoundInternalNames =
        compoundData.getMemberNames();

    for(std::string compoundInternalName : compoundInternalNames) {
        unsigned int amount =
            compoundData[compoundInternalName]["amount"].asUInt();
        double density =
            compoundData[compoundInternalName]["density"].asDouble();
        double dissolved =
            compoundData[compoundInternalName]["dissolved"].asDouble();

        // Getting the compound id from the compound registry.
        size_t id = SimulationParameters::compoundRegistry
                        .getTypeData(compoundInternalName)
                        .id;

        compounds.emplace(std::piecewise_construct, std::forward_as_tuple(id),
            std::forward_as_tuple(amount, density, dissolved));
    }
}
// ------------------------------------ //
BiomeCompoundData*
    Biome::getCompound(size_t type)
{
    return &compounds[type];
}

CScriptArray*
    Biome::getCompoundKeys() const
{
    return Leviathan::ConvertIteratorToASArray(
        (compounds | boost::adaptors::map_keys).begin(),
        (compounds | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<uint64>");
}
