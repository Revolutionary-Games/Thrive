#include "engine/player_data.h"

//#include "engine/game_state.h"
#include "general/locked_map.h"
//#include "scripting/luajit.h"
//#include "engine/entity.h"

#include <Define.h>

#include <unordered_set>

using namespace thrive;

struct PlayerData::Implementation {

    Implementation(std::string playerName) : m_playerName(playerName) {}

    ObjectID m_activeCreature = NULL_OBJECT;

    std::string m_playerName;

    LockedMap m_lockedMap;

    std::unordered_set<std::string> m_boolSet;
};

PlayerData::PlayerData(std::string name) : m_impl(new Implementation(name)) {}

PlayerData::~PlayerData() {}

const std::string&
    PlayerData::playerName()
{
    return m_impl->m_playerName;
}

LockedMap&
    PlayerData::lockedMap()
{
    return m_impl->m_lockedMap;
}

ObjectID
    PlayerData::activeCreature()
{
    return m_impl->m_activeCreature;
}

void
    PlayerData::setActiveCreature(ObjectID creatureId)
{
    LOG_INFO("Active player creature is now: " + std::to_string(creatureId));
    m_impl->m_activeCreature = creatureId;
}

bool
    PlayerData::isBoolSet(const std::string& key) const
{
    return (m_impl->m_boolSet.find(key) != m_impl->m_boolSet.end());
}

void
    PlayerData::setBool(const std::string& key, bool value)
{
    if(value) {
        m_impl->m_boolSet.emplace(key);
    } else {
        m_impl->m_boolSet.erase(key);
    }
}

// void
// PlayerData::load(
//     const StorageContainer& storage
// ) {

//     if(!m_impl->m_activeCreatureGamestate)
//         throw std::runtime_error("PlayerData.activeCreatureGamestate is null
//         in 'load'");


//     m_impl->m_playerName = storage.get<std::string>("playerName");
//     StorageContainer lockedMapStorage =
//     storage.get<StorageContainer>("lockedMap");
//     //This isn't the prettiest way to do it, but we need to reobtain a
//     reference to the players creature DEBUG_BREAK;
//     // m_impl->m_activeCreature = Entity(m_impl->m_playerName,
//     //     m_impl->m_activeCreatureGamestate).id();
//     StorageList boolValues = storage.get<StorageList>("boolValues");
//     for (const StorageContainer& container : boolValues) {
//         std::string boolKey = container.get<std::string>("boolKey");
//         m_impl->m_boolSet.emplace(boolKey);
//     }
//     m_impl->m_lockedMap.load(lockedMapStorage);
// }

// StorageContainer
// PlayerData::storage() const {
//     StorageContainer storage;
//     storage.set("playerName", m_impl->m_playerName);
//     StorageList boolValues;
//     boolValues.reserve(m_impl->m_boolSet.size());
//     for(auto key : m_impl->m_boolSet) {
//         StorageContainer container;
//         container.set<std::string>("boolKey", key);
//         boolValues.append(container);
//     }
//     storage.set<StorageList>("boolValues", boolValues);
//     storage.set("lockedMap", m_impl->m_lockedMap.storage());
//     return storage;
// }
