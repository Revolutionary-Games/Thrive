#pragma once

#include <Entities/Component.h>
#include <Entities/Components.h>
#include <Entities/System.h>
#include <microbe_stage/biomes.h>
#include <unordered_map>

namespace thrive {

class CellStageWorld;

//!An object that represents a patch
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

    Biome*
        getBiome();
    void
        setBiome(Biome* biome);

	size_t
        getId();

	private:
    Biome* patchBiome = nullptr;
    std::vector<std::weak_ptr<Patch>> adjacentPatches;


};

}