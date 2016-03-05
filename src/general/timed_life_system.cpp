#include "general/timed_life_system.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"
#include "scripting/luabind.h"


using namespace thrive;

REGISTER_COMPONENT(TimedLifeComponent)


luabind::scope
TimedLifeComponent::luaBindings() {
    using namespace luabind;
    return class_<TimedLifeComponent, Component>("TimedLifeComponent")
        .enum_("ID") [
            value("TYPE_ID", TimedLifeComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &TimedLifeComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def_readwrite("timeToLive", &TimedLifeComponent::m_timeToLive)
    ;
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

luabind::scope
TimedLifeSystem::luaBindings() {
    using namespace luabind;
    return class_<TimedLifeSystem, System>("TimedLifeSystem")
        .def(constructor<>())
    ;
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
    GameState* gameState
) {
    System::initNamed("TimedLifeSystem", gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
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
