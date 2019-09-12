// ------------------------------------ //
#include "patch.h"

#include "simulation_parameters.h"

using namespace thrive;
// ------------------------------------ //
Patch::Patch(const std::string& name, int32_t id, const Biome& biomeTemplate) :
    patchId(id), name(name), biome(biomeTemplate)
{}
// ------------------------------------ //
bool
    Patch::addNeighbour(int32_t id)
{
    if(adjacentPatches.find(id) != adjacentPatches.end())
        return false;

    adjacentPatches.insert(id);
    return true;
}
// ------------------------------------ //
Species::pointer
    Patch::searchSpeciesByName(const std::string& name) const
{
    for(const auto& entry : speciesInPatch) {
        if(entry.species->name == name)
            return entry.species;
    }

    return nullptr;
}
// ------------------------------------ //
// Patch map
bool
    PatchMap::addPatch(Patch::pointer patch)
{
    if(!patch)
        return false;

    if(patches.find(patch->getId()) != patches.end())
        return false;

    patches[patch->getId()] = patch;
    return false;
}
// ------------------------------------ //
Species::pointer
    PatchMap::findSpeciesByName(const std::string& name)
{
    const auto current = patches.find(currentPatchId);

    if(current != patches.end()) {

        const auto result = current->second->searchSpeciesByName(name);

        if(result)
            return result;
    }

    for(auto iter = patches.begin(); iter != patches.end(); ++iter) {

        if(iter == current)
            continue;

        const auto result = current->second->searchSpeciesByName(name);

        if(result)
            return result;
    }

    return nullptr;
}
// ------------------------------------ //
Patch::pointer
    PatchMap::getCurrentPatch()
{
    return getPatch(currentPatchId);
}

bool
    PatchMap::setCurrentPatch(int32_t newId)
{
    if(patches.find(newId) == patches.end())
        return false;

    currentPatchId = newId;
    return true;
}
// ------------------------------------ //
Patch::pointer
    PatchMap::getPatch(int32_t id)
{
    if(patches.find(id) == patches.end()) {
        LOG_ERROR("PatchMap: has no patch with id: " + std::to_string(id));
        return nullptr;
    }

    return patches[id];
}
