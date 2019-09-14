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
    specularColors = Float4(r, g, b, 1.0);

    r = sData["r"].asFloat();
    g = sData["g"].asFloat();
    b = sData["b"].asFloat();
    diffuseColors = Float4(r, g, b, 1.0);

    r = uData["r"].asFloat();
    g = uData["g"].asFloat();
    b = uData["b"].asFloat();
    upperAmbientColor = Float4(r, g, b, 1.0);

    r = lData["r"].asFloat();
    g = lData["g"].asFloat();
    b = lData["b"].asFloat();
    lowerAmbientColor = Float4(r, g, b, 1.0);

    // Getting the compound information.
    Json::Value compoundData = value["compounds"];
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
