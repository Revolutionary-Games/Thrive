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

    bool m_freeBuilding = false;

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
// ------------------------------------ //
bool
    PlayerData::isFreeBuilding() const
{
    return m_impl->m_freeBuilding;
}

void
    PlayerData::enterFreeBuild()
{
    LOG_INFO("Marking player as having used freebuild");
    m_impl->m_freeBuilding = true;
}
// ------------------------------------ //
void
    PlayerData::newGame()
{
    LOG_INFO("Clearing PlayerData for new game");
    // Only name needs to be stored
    const auto playerName = m_impl->m_playerName;
    m_impl = std::make_unique<Implementation>(playerName);
}
