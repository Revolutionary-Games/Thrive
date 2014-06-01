#include "engine/player_data.h"

#include "engine/serialization.h"
#include "general/locked_map.h"
#include "scripting/luabind.h"

#include <unordered_set>

using namespace thrive;

struct PlayerData::Implementation {

    Implementation(
        std::string playerName
    ) : m_playerName(playerName)
    {
    }

    EntityId m_activeCreature = NULL_ENTITY;

    std::string m_playerName;

    LockedMap m_lockedMap;

    std::unordered_set<std::string> m_boolSet;

};

luabind::scope
PlayerData::luaBindings() {
    using namespace luabind;
    return class_<PlayerData>("PlayerData")
        .def(constructor<std::string>())
        .def("playerName", &PlayerData::playerName)
        .def("lockedMap", &PlayerData::lockedMap)
        .def("activeCreature", &PlayerData::activeCreature)
        .def("setActiveCreature", &PlayerData::setActiveCreature)
        .def("isBoolSet", &PlayerData::isBoolSet)
        .def("setBool", &PlayerData::setBool)
    ;
}

PlayerData::PlayerData(
    std::string name
) : m_impl(new Implementation(name)) {

}

PlayerData::~PlayerData(){}

const std::string&
PlayerData::playerName(){
    return m_impl->m_playerName;
}

LockedMap&
PlayerData::lockedMap(){
    return m_impl->m_lockedMap;
}

EntityId
PlayerData::activeCreature(){
    return m_impl->m_activeCreature;
}

void
PlayerData::setActiveCreature(
    EntityId creatureId
){
    m_impl->m_activeCreature = creatureId;
}

bool
PlayerData::isBoolSet(
    std::string key
) const {
    return (m_impl->m_boolSet.find(key) != m_impl->m_boolSet.end());
}

void
PlayerData::setBool(
    std::string key,
    bool value
) {
    if (value){
        m_impl->m_boolSet.emplace(key);
    }
    else {
        m_impl->m_boolSet.erase(key);
    }
}

void
PlayerData::load(
    const StorageContainer& storage
) {
    m_impl->m_playerName = storage.get<std::string>("playerName");
    StorageContainer lockedMapStorage = storage.get<StorageContainer>("lockedMap");
    m_impl->m_lockedMap.load(lockedMapStorage);
}

StorageContainer
PlayerData::storage() const {
    StorageContainer storage;
    storage.set("playerName", m_impl->m_playerName);
    storage.set("lockedMap", m_impl->m_lockedMap.storage());
    return storage;
}
