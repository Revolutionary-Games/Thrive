#include "microbe_stage/patch.h"

using namespace thrive;

Patch::Patch(std::string name)
{
    this->name = name;
}

Patch::~Patch()
{
    delete patchBiome;
}

std::string
    Patch::getName()
{
    return this->name;
}

void
    Patch::setName(std::string name)
{
    this->name = name;
}

Biome*
    Patch::getBiome()
{
    return this->patchBiome;
}

void
    Patch::setBiome(Biome* biome)
{
    if(patchBiome)
        delete patchBiome;
    this->patchBiome = biome;
}

size_t
    Patch::getId()
{
    return this->patchId;
}

//Patch manager
PatchManager::PatchManager()
{
    this->currentPatchId = generatePatchMap();
}

///Generate patch map and return the id of the starting patch
size_t
    PatchManager::generatePatchMap()
{
	//TODO: add map generator
    return 0;
}
Patch*
    PatchManager::getCurrentPatch(){
    return patchMap[this->currentPatchId].get();
}

Patch*
    PatchManager::getPatchFromKey(size_t key)
{
    return patchMap[key].get();
}
