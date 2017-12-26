#include "compound_registry.h"

// #include "bullet/rigid_body_system.h"
// #include "engine/component_factory.h"
// #include "engine/engine.h"
// #include "engine/entity_filter.h"
// #include "engine/game_state.h"
// #include "engine/serialization.h"
// #include "game.h"
// #include "ogre/scene_node_system.h"
// #include "scripting/luajit.h"
// #include "util/make_unique.h"
// #include "microbe_stage/compound.h"

// #include <OgreEntity.h>
// #include <OgreSceneManager.h>
// #include <stdexcept>

#include <vector>

#include <unordered_map>

using namespace thrive;

namespace {
    struct CompoundRegistryEntry
    {
        std::string internalName;
        std::string displayName;
        float unitVolume;
		std::string meshName;
        double meshScale;
        bool isAgent;
        bool isUseful;
        std::function<bool(EntityId, double)>* effect;
    };
}

static std::vector<CompoundRegistryEntry>&
compoundRegistry() {
    static std::vector<CompoundRegistryEntry> compoundRegistry;
    return compoundRegistry;
}
static std::unordered_map<std::string, CompoundId>&
compoundRegistryMap() {
    static std::unordered_map<std::string, CompoundId> compoundRegistryMap;
    return compoundRegistryMap;
}

CompoundId
CompoundRegistry::registerCompoundType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    bool isUseful,
    float unitVolume

) {
    return registerAgentType(
        internalName,
        displayName,
        meshName,
        meshScale,
        isUseful,
        unitVolume,
        static_cast<std::function<bool(EntityId, double)>*>(nullptr)
    );
}

CompoundId
CompoundRegistry::registerAgentType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    bool isUseful,
    float unitVolume,
    std::function<bool(EntityId, double)>* effect
) {
    if (compoundRegistryMap().count(internalName) == 0)
    {
        CompoundRegistryEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
		entry.meshName = meshName;
        entry.meshScale = meshScale;
        entry.unitVolume = unitVolume;
        entry.effect = effect;
        entry.isAgent = (effect != nullptr);
        entry.isUseful = entry.isAgent || isUseful;
        compoundRegistry().push_back(entry);
        compoundRegistryMap().emplace(std::string(internalName), compoundRegistry().size());
        return compoundRegistry().size();
    }
    else
    {
        throw std::invalid_argument("Duplicate internalName not allowed.");
    }
}

std::string
CompoundRegistry::getCompoundDisplayName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].displayName;
}

std::string
CompoundRegistry::getCompoundInternalName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].internalName;
}

int
CompoundRegistry::getCompoundUnitVolume(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].unitVolume;
}

CompoundId
CompoundRegistry::getCompoundId(
    const std::string& internalName
) {
    CompoundId compoundId;
    try
    {
        compoundId = compoundRegistryMap().at(internalName);
    }
    catch(std::out_of_range&)
    {
        throw std::out_of_range("Internal name of compound does not exist.");
    }
    return compoundId;
}

std::string
CompoundRegistry::getCompoundMeshName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].meshName;
}

double
CompoundRegistry::getCompoundMeshScale(
    CompoundId compoundId


) {
    if (static_cast<std::size_t>(compoundId) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[compoundId-1].meshScale;
}

std::function<bool(EntityId, double)>*
CompoundRegistry::getAgentEffect(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].effect;
}


bool
CompoundRegistry::isAgentType(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].isAgent;
}

bool
CompoundRegistry::isUseful(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].isUseful;
}
