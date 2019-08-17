#include "microbe_stage/patch.h"
#include "simulation_parameters.h"

using namespace thrive;

Patch::Patch(std::string name)
{
    this->name = name;
}

Patch::~Patch() {}

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

size_t
    Patch::getBiome()
{
    return this->patchBiome;
}

void
    Patch::setBiome(size_t patchBiome)
{
    this->patchBiome = patchBiome;
}

size_t
    Patch::getId()
{
    return this->patchId;
}

// Patch manager
PatchManager::PatchManager()
{
    this->currentPatchId = generatePatchMap();
}

PatchManager::~PatchManager()
{
    patchMap.clear();
}
/// Generate patch map and return the id of the starting patch
size_t
    PatchManager::generatePatchMap()
{
    // TODO: add proper map generator
    std::shared_ptr<Patch> p = std::make_shared<Patch>("Pangonian vents");
    p.get()->setBiome(0);
    p.get()->patchId = 0;

    patchMap[0] = p;
    return 0;
}
Patch*
    PatchManager::getCurrentPatch()
{
    return patchMap[this->currentPatchId].get();
}

Patch*
    PatchManager::getPatchFromKey(size_t key)
{
    return patchMap[key].get();
}
