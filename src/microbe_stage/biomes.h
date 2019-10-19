#pragma once

#include "general/json_registry.h"

#include <Common/Types.h>

#include <map>
#include <string>
#include <vector>

class CScriptArray;

namespace thrive {

struct BiomeCompoundData {
public:
    float amount = 0;
    double density = 1.0f;
    double dissolved = 0.0f;

    BiomeCompoundData() {}

    BiomeCompoundData(float amount, double density, double dissolved) :
        amount(amount), density(density), dissolved(dissolved)
    {}
};

struct ChunkCompoundData {
public:
    double amount = 0.0f;
    std::string name;
    ChunkCompoundData() {}

    // Move constructor
    ChunkCompoundData(ChunkCompoundData&& other) noexcept :
        amount(other.amount), name(std::move(other.name))
    {}

    // Copy constructor
    ChunkCompoundData(const ChunkCompoundData& other) :
        amount(other.amount), name(other.name)
    {}

    ChunkCompoundData(double amount, std::string name) :
        amount(amount), name(name)
    {}
};

struct ChunkMeshData {
public:
    ChunkMeshData(const std::string& mesh, const std::string& texture) :
        mesh(mesh), texture(texture)
    {}

    std::string mesh;
    std::string texture;
};

struct ChunkData {
public:
    std::string name = "";
    double density = 1.0f;
    bool dissolves = true;
    unsigned int radius = 0;
    double chunkScale = 1.0f;
    unsigned int mass = 0;
    unsigned int size = 0;
    double ventAmount = 3.0f;
    double damages = 0.0f;
    bool deleteOnTouch = false;

    std::vector<ChunkMeshData> meshes;
    std::map<size_t, ChunkCompoundData> chunkCompounds;

    // Move constructor
    ChunkData(ChunkData&& other) noexcept :
        name(std::move(other.name)), density(other.density),
        dissolves(other.dissolves), radius(other.radius),
        chunkScale(other.chunkScale), mass(other.mass), size(other.size),
        ventAmount(other.ventAmount), damages(other.damages),
        deleteOnTouch(other.deleteOnTouch), meshes(std::move(other.meshes)),
        chunkCompounds(std::move(other.chunkCompounds))
    {}

    // Copy constructor
    ChunkData(const ChunkData& other) :
        name(other.name), density(other.density), dissolves(other.dissolves),
        radius(other.radius), chunkScale(other.chunkScale), mass(other.mass),
        size(other.size), ventAmount(other.ventAmount), damages(other.damages),
        deleteOnTouch(other.deleteOnTouch), meshes(other.meshes),
        chunkCompounds(other.chunkCompounds)
    {}

    ChunkData() {}

    ChunkData(std::string name, double density, bool dissolves) :
        name(name), density(density), dissolves(dissolves)
    {}

    ChunkCompoundData*
        getCompound(size_t type);

    CScriptArray*
        getCompoundKeys() const;

    size_t
        getMeshListSize();

    std::string
        getMesh(size_t index) const;

    std::string
        getTexture(size_t index) const;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(ChunkData);
};

class SimulationParameters;

class Biome : public RegistryType {
public:
    Biome();

    Biome(Json::Value value);

    BiomeCompoundData*
        getCompound(size_t type);
    CScriptArray*
        getCompoundKeys() const;

    CScriptArray*
        getChunkKeys() const;

    ChunkData*
        getChunk(size_t type);

    //! \brief Makes a JSON object representing this biome
    //! \todo Full mode and compounds and chunks
    Json::Value
        toJSON(bool full = false) const;

    //! The key is the compound id
    std::map<size_t, BiomeCompoundData> compounds;
    // No clue why this is a maps
    std::map<size_t, ChunkData> chunks;
    std::string background = "error";

    // skybox
    std::string skybox = "error";
    float skyboxLightIntensity;

    Float3 sunlightColor;
    float sunlightIntensity;
    Float3 sunlightDirection;
    float sunlightSourceRadius;
    float averageTemperature;
    double smallIronChunk;
    double bigIronChunk;

    // Eye adaptation / auto exposure settings
    //! Min eye adaptation for camera in this patch, valid range: [0.0f, 10.0f].
    float minEyeAdaptation = 0.003f;

    //! Max eye adaptation for camera in this patch, valid range: [0.0f, 10.0f].
    float maxEyeAdaptation = 2.0f;
};

} // namespace thrive
