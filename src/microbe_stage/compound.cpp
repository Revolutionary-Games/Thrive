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
#include "scripting/luajit.h"
#include "util/make_unique.h"

#include "tinyxml.h"

#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <stdexcept>


using namespace thrive;

REGISTER_COMPONENT(CompoundComponent)

void CompoundComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CompoundComponent>("CompoundComponent",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<Component>(),

        "ID", sol::var(lua.create_table_with("TYPE_ID", CompoundComponent::TYPE_ID)),
        "TYPE_NAME", &CompoundComponent::TYPE_NAME,

        "compoundId", &CompoundComponent::m_compoundId,
        "potency", &CompoundComponent::m_potency,
        "velocity", &CompoundComponent::m_velocity
    );
}

void
CompoundComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_compoundId = storage.get<CompoundId>("compoundId");
    m_potency = storage.get<float>("potency");
    m_velocity = storage.get<Ogre::Vector3>("velocity");
}


StorageContainer
CompoundComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<CompoundId>("compoundId", m_compoundId);
    storage.set<float>("potency", m_potency);
    storage.set<Ogre::Vector3>("velocity", m_velocity);
    return storage;
}


////////////////////////////////////////////////////////////////////////////////
// CompoundMovementSystem
////////////////////////////////////////////////////////////////////////////////

void CompoundMovementSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CompoundMovementSystem>("CompoundMovementSystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>()
    );
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
    GameStateData* gameState
) {
    System::initNamed("CompoundMovementSystem", gameState);
    m_impl->m_entities.setEntityManager(gameState->entityManager());
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
