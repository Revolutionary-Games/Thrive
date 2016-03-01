#include "microbe_stage/compound.h"

#include "bullet/collision_filter.h"
#include "bullet/collision_system.h"
#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/rng.h"
#include "engine/serialization.h"
#include "game.h"
#include "general/timed_life_system.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"

#include "tinyxml.h"

#include <luabind/iterator_policy.hpp>
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <stdexcept>


using namespace thrive;

REGISTER_COMPONENT(CompoundComponent)


luabind::scope
CompoundComponent::luaBindings() {
    using namespace luabind;
    return class_<CompoundComponent, Component>("CompoundComponent")
        .enum_("ID") [
            value("TYPE_ID", CompoundComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &CompoundComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def_readwrite("compoundId", &CompoundComponent::m_compoundId)
        .def_readwrite("potency", &CompoundComponent::m_potency)
        .def_readwrite("velocity", &CompoundComponent::m_velocity)
    ;
}


void
CompoundComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    //m_compoundId = storage.get<CompoundId>("compoundId", NULL_COMPOUND);
    m_potency = storage.get<float>("potency");
    m_velocity = storage.get<Ogre::Vector3>("velocity");
}


StorageContainer
CompoundComponent::storage() const {
    StorageContainer storage = Component::storage();
    //storage.set<CompoundId>("compoundId", m_compoundId);
    storage.set<float>("potency", m_potency);
    storage.set<Ogre::Vector3>("velocity", m_velocity);
    return storage;
}


////////////////////////////////////////////////////////////////////////////////
// CompoundMovementSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
CompoundMovementSystem::luaBindings() {
    using namespace luabind;
    return class_<CompoundMovementSystem, System>("CompoundMovementSystem")
        .def(constructor<>())
    ;
}


struct CompoundMovementSystem::Implementation {

    EntityFilter<
        CompoundComponent,
        RigidBodyComponent
    > m_entities;
};


CompoundMovementSystem::CompoundMovementSystem()
  : m_impl(new Implementation())
{
}


CompoundMovementSystem::~CompoundMovementSystem() {}


void
CompoundMovementSystem::init(
    GameState* gameState
) {
    System::initNamed("CompoundMovementSystem", gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
CompoundMovementSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
CompoundMovementSystem::update(int, int logicTime) {
    for (auto& value : m_impl->m_entities) {
        CompoundComponent* compoundComponent = std::get<0>(value.second);
        RigidBodyComponent* rigidBodyComponent = std::get<1>(value.second);
        Ogre::Vector3 delta = compoundComponent->m_velocity * float(logicTime) / 1000.0f;
        rigidBodyComponent->m_dynamicProperties.position += delta;
    }
}
