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


    // Skybox
    skybox = value["skybox"].asString();
    skyboxLightIntensity = value["skyboxLightIntensity"].asFloat();

    // Sunlight
    const auto sunlight = value["sunlight"];
    const auto sunlightColorData = sunlight["color"];
    const auto sunlightDirectionData = sunlight["direction"];

    float r = sunlightColorData["r"].asFloat();
    float g = sunlightColorData["g"].asFloat();
    float b = sunlightColorData["b"].asFloat();

    sunlightColor = Float3(r, g, b);
    sunlightIntensity = sunlight["intensity"].asFloat();

    float x = sunlightDirectionData["x"].asFloat();
    float y = sunlightDirectionData["y"].asFloat();
    float z = sunlightDirectionData["z"].asFloat();

    sunlightDirection = Float3(x, y, z);
    sunlightSourceRadius = sunlight["sourceRadius"].asFloat();

    // Eye adaptation
    const auto eyeAdaptation = value["eyeAdaptation"];
    minEyeAdaptation = eyeAdaptation["min"].asFloat();
    maxEyeAdaptation = eyeAdaptation["max"].asFloat();

    // Getting the compound information.
    Json::Value compoundData = value["compounds"];

    float averageTemperatureData = value["averageTemperature"].asFloat();
    averageTemperature = averageTemperatureData;
    std::vector<std::string> compoundInternalNames =
        compoundData.getMemberNames();

    for(std::string compoundInternalName : compoundInternalNames) {
        float amount = compoundData[compoundInternalName]["amount"].asFloat();
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

    Json::Value chunkData = value["chunks"];
    std::vector<std::string> chunkInternalNames = chunkData.getMemberNames();
    unsigned int id = 0;
    for(std::string chunkInternalName : chunkInternalNames) {
        // Get values for chunks

        std::string name = chunkData[chunkInternalName]["name"].asString();
        double density = chunkData[chunkInternalName]["density"].asDouble();
        bool dissolves = chunkData[chunkInternalName]["dissolves"].asBool();
        // Initilize chunk
        ChunkData chunk(name, density, dissolves);

        chunk.radius = chunkData[chunkInternalName]["radius"].asUInt();
        chunk.chunkScale =
            chunkData[chunkInternalName]["chunkScale"].asDouble();
        chunk.mass = chunkData[chunkInternalName]["mass"].asUInt();
        chunk.size = chunkData[chunkInternalName]["size"].asUInt();
        chunk.ventAmount =
            chunkData[chunkInternalName]["ventAmount"].asDouble();

        // Does it damge? If so how much?
        chunk.damages = chunkData[chunkInternalName]["damages"].asDouble();
        chunk.deleteOnTouch =
            chunkData[chunkInternalName]["deleteOnTouch"].asBool();

        // Get compound info
        // Getting the compound information.
        Json::Value chunkCompoundData =
            chunkData[chunkInternalName]["compounds"];
        std::vector<std::string> compoundChunkNames =
            chunkCompoundData.getMemberNames();

        // Can this support empty chunks?
        for(std::string compoundChunkName : compoundChunkNames) {
            unsigned int amount =
                chunkCompoundData[compoundChunkName]["amount"].asDouble();

            // Getting the compound id from the compound registry.
            size_t id = SimulationParameters::compoundRegistry
                            .getTypeData(compoundChunkName)
                            .id;

            chunk.chunkCompounds.emplace(std::piecewise_construct,
                std::forward_as_tuple(id),
                std::forward_as_tuple(amount, compoundChunkName));
        }

        // Add meshes
        Json::Value meshData = chunkData[chunkInternalName]["meshes"];

        for(size_t i = 0; i < meshData.size(); i++) {
            chunk.meshes.emplace_back(
                meshData[static_cast<int>(i)]["mesh"].asString(),
                meshData[static_cast<int>(i)]["texture"].asString());
        }

        // Add chunk to list
        chunks.emplace(id, std::move(chunk));
        id++;
    }
}
// ------------------------------------ //
BiomeCompoundData*
    Biome::getCompound(size_t type)
{
    const auto found = compounds.find(type);

    if(found == compounds.end())
        return nullptr;

    return &found->second;
}

CScriptArray*
    Biome::getCompoundKeys() const
{
    return Leviathan::ConvertIteratorToASArray(
        (compounds | boost::adaptors::map_keys).begin(),
        (compounds | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<uint64>");
}

ChunkData*
    Biome::getChunk(size_t type)
{
    return &chunks[type];
}

CScriptArray*
    Biome::getChunkKeys() const
{
    return Leviathan::ConvertIteratorToASArray(
        (chunks | boost::adaptors::map_keys).begin(),
        (chunks | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<uint64>");
}
// ------------------------------------ //
Json::Value
    Biome::toJSON(bool full /*= false*/) const
{
    Json::Value result = RegistryType::toJSON();

    result["background"] = background;

    // skybox
    result["skybox"] = skybox;
    result["skyboxLightIntensity"] = skyboxLightIntensity;

    result["sunlightIntensity"] = sunlightIntensity;
    result["sunlightSourceRadius"] = sunlightSourceRadius;

    Json::Value color;
    color["r"] = sunlightColor.X;
    color["g"] = sunlightColor.Y;
    color["b"] = sunlightColor.Z;
    result["sunlightColor"] = color;

    Json::Value direction;
    direction["x"] = sunlightDirection.X;
    direction["y"] = sunlightDirection.Y;
    direction["z"] = sunlightDirection.Z;
    result["sunlightDirection"] = direction;

    result["temperature"] = averageTemperature;
    Json::Value compoundsData;

    for(auto compoundRef : compounds) {
        thrive::Compound compound =
            SimulationParameters::compoundRegistry.getTypeData(
                SimulationParameters::compoundRegistry.getInternalName(
                    compoundRef.first));
        Json::Value compoundData;
        compoundData["name"] = compound.displayName;
        compoundData["amount"] = compoundRef.second.amount;
        compoundData["density"] = compoundRef.second.density;
        compoundData["dissolved"] = compoundRef.second.dissolved;
        compoundsData[compound.internalName] = compoundData;
    }

    result["compounds"] = compoundsData;

    if(full) {
        LOG_WARNING("Biome: toJSON: full is not implemented");
    }

    return result;
}

// ------------------------------------ //
// ChunkData
ChunkCompoundData*
    ChunkData::getCompound(size_t type)
{
    return &chunkCompounds[type];
}

CScriptArray*
    ChunkData::getCompoundKeys() const
{
    return Leviathan::ConvertIteratorToASArray(
        (chunkCompounds | boost::adaptors::map_keys).begin(),
        (chunkCompounds | boost::adaptors::map_keys).end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine(), "array<uint64>");
}

size_t
    ChunkData::getMeshListSize()
{
    return this->meshes.size();
}

std::string
    ChunkData::getMesh(size_t index) const
{
    // Some error checking
    if(index >= 0 && index < this->meshes.size()) {
        return this->meshes.at(index).mesh;
    } else {
        throw Leviathan::InvalidArgument(
            "Mesh at index " + std::to_string(index) + " does not exist!");
    }
}

std::string
    ChunkData::getTexture(size_t index) const
{
    // Some error checking
    if(index >= 0 && index < this->meshes.size()) {
        return this->meshes.at(index).texture;
    } else {
        throw Leviathan::InvalidArgument(
            "Texture at index " + std::to_string(index) + " does not exist!");
    }
}
