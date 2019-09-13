#pragma once

#include "patch.h"
#include "spawn_system.h"

#include <Entities/PerWorldData.h>

namespace thrive {

class PatchManager : public Leviathan::PerWorldData {
    struct ExistingSpawn {
        ExistingSpawn(SpawnerTypeId id,
            const std::string& thing,
            float density) :
            id(id),
            thing(thing), setDensity(density)
        {}

        SpawnerTypeId id;
        //! Used to detect existing items based on the chunk type or species
        //! name
        std::string thing;
        float setDensity;

        //! Flag for deleting removed ones. True when this should not be deleted
        bool marked = true;
    };

public:
    PatchManager(GameWorld& world);

    //! \brief Makes sure that the current patch settings are correctly applied
    void
        applyPatchSettings();

    //! \brief Sets the new map. Doesn't apply any settings yet
    void
        setNewMap(PatchMap::pointer map)
    {
        LOG_INFO("Setting new patch map");
        currentMap = map;
    }

    PatchMap::pointer
        getCurrentMap()
    {
        return currentMap;
    }

    PatchMap*
        getCurrentMapWrapper()
    {
        if(currentMap)
            currentMap->AddRef();
        return currentMap.get();
    }

private:
    void
        handleChunkSpawns(const Biome& biome);

    void
        handleCloudSpawns(const Biome& biome);

    void
        handleCellSpawns(const Patch& patch);

    void
        updateLight(const Biome& biome);

    //! Fires an event with the biome environment data
    void
        updateBiomeStatsForGUI(const Biome& biome);

    void
        updateCurrentPatchInfoForGUI(const Patch& patch);

    void
        removeNonMarkedSpawners();

    void
        clearUnmarkedSingle(std::vector<ExistingSpawn>& spawners);

    void
        unmarkAllSpawners();

    void
        unmarkSingle(std::vector<ExistingSpawn>& spawners);

private:
    PatchMap::pointer currentMap;

    CellStageWorld& cellWorld;

    // Currently active spawns
    std::vector<ExistingSpawn> chunkSpawners;
    std::vector<ExistingSpawn> cloudSpawners;
    std::vector<ExistingSpawn> microbeSpawners;
};

} // namespace thrive
