#include "general/powerup_system.h"


#include "bullet/collision_filter.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "scripting/luabind.h"


using namespace thrive;

luabind::scope
PowerupComponent::luaBindings() {
    using namespace luabind;
    return class_<PowerupComponent, Component>("PowerupComponent")
        .enum_("ID") [
            value("TYPE_ID", PowerupComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &PowerupComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("setEffect",
             static_cast<void (PowerupComponent::*)(const luabind::object&)>(&PowerupComponent::setEffect)
         )
    ;
}

void
PowerupComponent::setEffect(
    std::function<bool(EntityId)> effect
){
    m_effect = effect;
}

void
PowerupComponent::setEffect(
    const luabind::object& effect
){
    auto effectLambda = [effect](EntityId entityId) -> bool
        {
            return luabind::call_function<bool>(effect, entityId);;
        };
    this->setEffect(std::function<bool(EntityId)>(effectLambda));
}

void
PowerupComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
}


StorageContainer
PowerupComponent::storage() const {
    StorageContainer storage = Component::storage();
    return storage;
}

REGISTER_COMPONENT(PowerupComponent)

////////////////////////////////////////////////////////////////////////////////
// PowerupSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
PowerupSystem::luaBindings() {
    using namespace luabind;
    return class_<PowerupSystem, System>("PowerupSystem")
        .def(constructor<>())
    ;
}


struct PowerupSystem::Implementation {

    Implementation()
      : m_powerupCollisions("powerupable", "powerup")
    {
    }

    EntityFilter<
        PowerupComponent
    > m_entities;

    CollisionFilter m_powerupCollisions;

    EntityManager* m_entityManager = nullptr;
};


PowerupSystem::PowerupSystem()
  : m_impl(new Implementation())
{
}


PowerupSystem::~PowerupSystem() {}


void
PowerupSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
    m_impl->m_powerupCollisions.init(gameState);
    m_impl->m_entityManager = &gameState->entityManager();
}


void
PowerupSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_powerupCollisions.shutdown();
    System::shutdown();
}


void
PowerupSystem::update(int /*milliseconds*/) {
    for (Collision collision : m_impl->m_powerupCollisions)
    {
        EntityId entityA = collision.entityId1;
        EntityId entityB = collision.entityId2;
        EntityId powerupableEntity = NULL_ENTITY;
        EntityId powerupEntity = NULL_ENTITY;
        if (
            m_impl->m_entities.containsEntity(entityA)
        ) {
            powerupEntity = entityA;
            powerupableEntity = entityB;
        }
        else {
            powerupEntity = entityB;
            powerupableEntity = entityA;
        }
        PowerupComponent* powerupComponent = m_impl->m_entityManager->getComponent<PowerupComponent>(powerupEntity);
        if (powerupComponent->m_effect.operator()(powerupableEntity)){
            m_impl->m_powerupCollisions.removeCollision(collision);
            m_impl->m_entityManager->removeEntity(powerupEntity);
        }
    }
}
