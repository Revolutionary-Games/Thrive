#pragma once

#include <Entities/Component.h>
#include <Entities/Components.h>
#include <Entities/System.h>
#include <microbe_stage/biomes.h>
#include <unordered_map>

namespace thrive {

class CellStageWorld;

//! An object that represents a patch
class Patch {
public:
    std::string name;
    size_t patchId;

    Patch(std::string name);
    virtual ~Patch();

    std::string
        getName();
    void
        setName(std::string name);

    size_t
        getBiome();
    void
        setBiome(size_t patchBiome);

    size_t
        getId();

private:
    size_t patchBiome;
    std::vector<std::weak_ptr<Patch>> adjacentPatches;
};


class PatchManager {
public:
    PatchManager();
    virtual ~PatchManager();
    size_t
        PatchManager::generatePatchMap();

    Patch*
        getCurrentPatch();

    Patch*
        getPatchFromKey(size_t key);

protected:
private:
    std::unordered_map<size_t, std::shared_ptr<Patch>> patchMap;
    size_t currentPatchId = 0;
};

} // namespace thrive