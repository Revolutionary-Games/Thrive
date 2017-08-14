#include "compound_registry.h"

#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/scene_node_system.h"
#include "scripting/luajit.h"
#include "util/make_unique.h"
#include "microbe_stage/compound.h"

#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <stdexcept>

using namespace thrive;

void CompoundRegistry::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CompoundRegistry>("CompoundRegistry",

        "new", sol::no_constructor,

        "registerCompoundType", &CompoundRegistry::registerCompoundType,
        "registerAgentType",
            static_cast<CompoundId (*)(
                const std::string&,
                const std::string&,
                const std::string&,
                double,
                bool,
                float,
                sol::object
            )>(&CompoundRegistry::registerAgentType),

        "loadFromLua", &CompoundRegistry::loadFromLua,
        "loadAgentFromLua", &CompoundRegistry::loadAgentFromLua,
        "getCompoundDisplayName", &CompoundRegistry::getCompoundDisplayName,
        "getCompoundInternalName", &CompoundRegistry::getCompoundInternalName,
        "getCompoundMeshName", &CompoundRegistry::getCompoundMeshName,
        "getCompoundUnitVolume", &CompoundRegistry::getCompoundUnitVolume,
        "getCompoundId", &CompoundRegistry::getCompoundId,

        // sol:: doesn't like boost wrapped iterators
        "getCompoundList", [](sol::this_state s){

            THRIVE_BIND_ITERATOR_TO_TABLE(CompoundRegistry::getCompoundList());
        },

        "getCompoundMeshScale", &CompoundRegistry::getCompoundMeshScale,
        "getAgentEffect", &CompoundRegistry::getAgentEffect
    );
}


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

void
CompoundRegistry::loadFromLua(
    sol::table compoundTable,
    sol::table agentTable
) {

    for(const auto& pair : compoundTable){

        const auto key = pair.first.as<std::string>();
        auto data = pair.second.as<sol::table>();

        const auto name = data.get<std::string>("name");
        const auto weight = data.get<float>("weight");
        const auto meshname = data.get<std::string>("mesh");
        const auto size = data.get<float>("size");
        bool isUseful = data.get<bool>("isUseful");

        registerCompoundType(
                key,
                name,
                meshname,
                size,
                isUseful,
                weight
            );
    }

    for(const auto& pair : agentTable){

        const auto key = pair.first.as<std::string>();
        auto data = pair.second.as<sol::table>();

        const auto name = data.get<std::string>("name");
        const auto weight = data.get<float>("weight");
        const auto meshname = data.get<std::string>("mesh");
        const auto size = data.get<float>("size");

        sol::object effect = data["effect"];

        registerAgentType(
                key,
                name,
                meshname,
                size,
                true,
                weight,
                effect
            );
    }
}

void
CompoundRegistry::loadAgentFromLua(
    sol::object internalName,
    sol::table data
) {

    const auto internal_name = internalName.as<std::string>();

    const auto name = data.get<std::string>("name");
    const auto weight = data.get<float>("weight");
    const auto meshname = data.get<std::string>("mesh");
    const auto size = data.get<float>("size");

    // std::cerr << "before casting effect" << std::endl;
    sol::object effect = data["effect"];
    registerAgentType(
            internal_name,
            name,
            meshname,
            size,
            true,
            weight,
            effect
        );
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


//Luabind version
CompoundId
CompoundRegistry::registerAgentType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    bool isUseful,
    float unitVolume,
    sol::object effect
) {
    auto effectLambda = new std::function<bool(EntityId, double)>(
        [effect](EntityId entityId, double potency) -> bool
        {
            effect.as<sol::protected_function>()(entityId, potency);
            return true;
        });
    //Call overload
    return registerAgentType(
        internalName,
         displayName,
         meshName,
         meshScale,
         isUseful,
         unitVolume,
         effectLambda);
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
const BoostCompoundMapIterator
CompoundRegistry::getCompoundList(
) {
    return compoundRegistryMap() | boost::adaptors::map_values;
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
