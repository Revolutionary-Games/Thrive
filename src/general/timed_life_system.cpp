#include "general/timed_life_system.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"
#include "scripting/luajit.h"


using namespace thrive;

REGISTER_COMPONENT(TimedLifeComponent)

void TimedLifeComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<TimedLifeComponent>("TimedLifeComponent",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<Component>(),

        "ID", sol::var(lua.create_table_with("TYPE_ID",
                sol::var(TimedLifeComponent::TYPE_ID))),
        "TYPE_NAME", &TimedLifeComponent::TYPE_NAME,

        "timeToLive", &TimedLifeComponent::m_timeToLive
    );
}

void
TimedLifeComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_timeToLive = storage.get<Milliseconds>("timeToLive");
}


StorageContainer
TimedLifeComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Milliseconds>("timeToLive", m_timeToLive);
    return storage;
}


////////////////////////////////////////////////////////////////////////////////
// TimedLifeSystem
////////////////////////////////////////////////////////////////////////////////

void TimedLifeSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<TimedLifeSystem>("TimedLifeSystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>()
    );
}

struct TimedLifeSystem::Implementation {

    EntityFilter<
        TimedLifeComponent
    > m_entities;
};


TimedLifeSystem::TimedLifeSystem()
  : m_impl(new Implementation())
{
}


TimedLifeSystem::~TimedLifeSystem() {}


void
TimedLifeSystem::init(
    GameStateData* gameState
) {
    System::initNamed("TimedLifeSystem", gameState);
    m_impl->m_entities.setEntityManager(gameState->entityManager());
}


void
TimedLifeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
TimedLifeSystem::update(int, int logicTime) {
    for (auto& value : m_impl->m_entities) {
        TimedLifeComponent* timedLifeComponent = std::get<0>(value.second);
        timedLifeComponent->m_timeToLive -= logicTime;
        if (timedLifeComponent->m_timeToLive <= 0) {
            this->entityManager()->removeEntity(value.first);
        }
    }
}
