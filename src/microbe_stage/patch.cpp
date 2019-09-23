// ------------------------------------ //
#include "patch.h"

#include "simulation_parameters.h"

using namespace thrive;
// ------------------------------------ //
Patch::Patch(const std::string& name, int32_t id, const Biome& biomeTemplate) :
    patchId(id), name(name), biome(biomeTemplate), biomeTemplate(biomeTemplate)
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
void
    Patch::setScreenCoordinates(Float2 coordinates)
{
    screenCoordinates = coordinates;
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
bool
    Patch::addSpecies(const Species::pointer& species, int population)
{
    if(!species || searchSpeciesByName(species->name))
        return false;

    speciesInPatch.emplace_back(SpeciesInPatch{species, population});
    return true;
}

bool
    Patch::removeSpecies(const Species::pointer& species)
{

    for(auto iter = speciesInPatch.begin(); iter != speciesInPatch.end();
        ++iter) {
        if(iter->species == species) {
            speciesInPatch.erase(iter);
            return true;
        }
    }

    return false;
}

bool
    Patch::updateSpeciesPopulation(const Species::pointer& species,
        int newPopulation)
{
    for(auto& entry : speciesInPatch) {
        if(entry.species == species) {
            entry.population = newPopulation;
            return true;
        }
    }

    return false;
}
// ------------------------------------ //
int
    Patch::getSpeciesPopulation(const Species::pointer& species)
{
    for(auto& entry : speciesInPatch) {
        if(entry.species == species) {
            return entry.population;
        }
    }

    return 0;
}

uint64_t
    Patch::getSpeciesCount() const
{
    return speciesInPatch.size();
}

Species::pointer
    Patch::getSpecies(uint64_t index) const
{
    if(index >= speciesInPatch.size())
        return nullptr;

    return speciesInPatch[index].species;
}
// ------------------------------------ //
Json::Value
    Patch::toJSON() const
{
    Json::Value result;

    result["id"] = patchId;
    result["name"] = name;
    result["biome"] = biome.toJSON();

    Json::Value coordinates;
    coordinates["x"] = screenCoordinates.X;
    coordinates["y"] = screenCoordinates.Y;
    result["screenCoordinates"] = coordinates;

    Json::Value species(Json::ValueType::arrayValue);
    for(const auto& current : speciesInPatch) {

        Json::Value obj;
        obj["population"] = current.population;
        obj["species"] = current.species->toJSON();

        species.append(obj);
    }

    result["species"] = species;

    Json::Value adjacent(Json::ValueType::arrayValue);
    for(auto patch : adjacentPatches)
        adjacent.append(patch);

    result["adjacentPatches"] = adjacent;

    return result;
}
// ------------------------------------ //
// Patch map
bool
    PatchMap::addPatch(const Patch::pointer& patch)
{
    if(!patch)
        return false;

    if(patches.find(patch->getId()) != patches.end())
        return false;

    patches[patch->getId()] = patch;
    return false;
}
// ------------------------------------ //
bool
    PatchMap::verify()
{
    if(!getCurrentPatch())
        return false;

    bool result = true;

    // Link verification caches
    std::unordered_map<int32_t, bool> incomingLinks;
    std::set<std::tuple<int32_t, int32_t>> seenLinks;

    // Verify all adjacent patches are valid
    for(const auto& [id, patch] : patches) {
        if(!patch)
            return false;

        if(incomingLinks.find(id) == incomingLinks.end())
            incomingLinks[id] = false;

        for(auto neighbour : patch->getNeighbours()) {
            if(!getPatch(neighbour)) {
                LOG_ERROR("patch " + std::to_string(id) +
                          " links to non-existing patch: " +
                          std::to_string(neighbour));
                result = false;
            }

            incomingLinks[neighbour] = true;

            seenLinks.insert(std::make_tuple(id, neighbour));
        }
    }

    // All patches have an incoming link
    for(const auto& [id, incoming] : incomingLinks) {
        if(!incoming) {
            // Allow the initial patch to not have any incoming links as long as
            // it is the only one
            if(patches.size() == 1 && id == currentPatchId)
                continue;

            LOG_ERROR(
                "no incoming links found for patch id: " + std::to_string(id));
            result = false;
        }
    }

    // All links are two way
    // TODO: do we want always two way links?
    for(const auto [from, to] : seenLinks) {
        // Find the other way
        bool found = false;

        for(const auto [findFrom, findTo] : seenLinks) {

            if(findFrom == to && findTo == from) {
                found = true;
                break;
            }
        }

        if(!found) {
            LOG_ERROR(
                "link " + std::to_string(from) + " -> " + std::to_string(to) +
                " is one way. These types of links are currently not wanted");
            result = false;
        }
    }

    return result;
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

        const auto result = iter->second->searchSpeciesByName(name);

        if(result)
            return result;
    }

    return nullptr;
}
// ------------------------------------ //
void
    PatchMap::updateGlobalPopulations()
{
    std::unordered_map<Species*, int32_t> seenPopulations;

    for(const auto& [id, patch] : patches) {
        for(const auto& patchSpecies : patch->getSpecies()) {
            Species* ptr = patchSpecies.species.get();

            // Having these checks this way allows this code to set a population
            // to 0
            if(seenPopulations.find(ptr) == seenPopulations.end())
                seenPopulations[ptr] = 0;

            if(patchSpecies.population > 0)
                seenPopulations[ptr] += patchSpecies.population;
        }
    }

    // Apply the populations after calculating them
    for(const auto [species, population] : seenPopulations) {
        species->setPopulationFromPatches(population);
    }
}

void
    PatchMap::removeExtinctSpecies(bool playerCantGoExtinct /*= false*/)
{
    std::vector<Species::pointer> extinctSpecies;

    for(const auto& [id, patch] : patches) {

        extinctSpecies.clear();

        for(const auto& patchSpecies : patch->getSpecies()) {
            if(patchSpecies.population <= 0) {
                if(playerCantGoExtinct &&
                    patchSpecies.species->isPlayerSpecies())
                    continue;

                extinctSpecies.push_back(patchSpecies.species);
            }
        }

        for(const auto& species : extinctSpecies) {

            LOG_INFO("Species: " + species->name +
                     " has gone extinct in patch: " + patch->getName());
            patch->removeSpecies(species);
        }
    }
}
// ------------------------------------ //
Json::Value
    PatchMap::toJSON() const
{
    Json::Value result;
    result["currentPatchId"] = currentPatchId;
    Json::Value patchesObj(Json::ValueType::objectValue);

    for(const auto& [id, patch] : patches) {

        patchesObj[std::to_string(id)] = patch->toJSON();
    }

    result["patches"] = patchesObj;

    return result;
}

std::string
    PatchMap::toJSONString() const
{
    std::stringstream sstream;
    const Json::Value value = toJSON();

    Json::StreamWriterBuilder builder;
    builder["indentation"] = "";
    std::unique_ptr<Json::StreamWriter> writer(builder.newStreamWriter());

    writer->write(value, &sstream);

    return sstream.str();
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
// ------------------------------------ //
