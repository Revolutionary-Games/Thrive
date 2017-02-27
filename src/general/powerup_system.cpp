#include "general/powerup_system.h"


#include "bullet/collision_filter.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/game_state.h"
#include "game.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "scripting/luajit.h"

#include <iostream>

using namespace thrive;

void PowerupComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<PowerupComponent>("PowerupComponent",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<Component>(),

        "TYPE_ID", sol::var(PowerupComponent::TYPE_ID), 
        "TYPE_NAME", &PowerupComponent::TYPE_NAME,

        "setEffect", static_cast<void (PowerupComponent::*)(
            const std::string&)>(&PowerupComponent::setEffect)
    );
}

void
PowerupComponent::setEffect(
    const std::string& funcName
){
    this->effectName = funcName;
    this->setEffect(new std::function<bool(EntityId)>(
        [funcName](EntityId entityId) -> bool
        {
            lua_State* L = Game::instance().engine().luaState();
            luaL_openlibs(L);
            luaL_loadfile(L, "config.lua");

            lua_getglobal(L, funcName.c_str());
            lua_pushnumber(L, entityId);
            if (lua_pcall(L, 1, 1, 0) != 0)
            {
                std::cerr << "error: cannot call the function" << std::endl;
                return false;
            }

            if (!lua_isboolean(L, -1))
            {
                std::cerr << "function '" + funcName + "' must return a boolean" << std::endl;
                return false;
            }
            bool result = lua_toboolean(L, -1);
            lua_pop(L, 1);

            return result;
        }
    ));
}

void
PowerupComponent::setEffect(
    std::function<bool(EntityId)>* effect
){
    m_effect = effect;
}



void
PowerupComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);

    this->effectName = storage.get<std::string>("effect");
    setEffect(this->effectName);
}


StorageContainer
PowerupComponent::storage() const {
    StorageContainer storage = Component::storage();

    storage.set("effect", effectName);

    return storage;
}





REGISTER_COMPONENT(PowerupComponent)

////////////////////////////////////////////////////////////////////////////////
// PowerupSystem
////////////////////////////////////////////////////////////////////////////////

void PowerupSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<PowerupSystem>("PowerupSystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>()
    );
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
    GameStateData* gameState
) {
    System::initNamed("PowerupSystem", gameState);
    m_impl->m_entities.setEntityManager(gameState->entityManager());
    m_impl->m_powerupCollisions.init(gameState);
    m_impl->m_entityManager = gameState->entityManager();
}


void
PowerupSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_powerupCollisions.shutdown();
    System::shutdown();
}


void
PowerupSystem::update(int, int) {
    std::vector<Collision*> collisionsToRemove = std::vector<Collision*>();
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
        else if (m_impl->m_entities.containsEntity(entityB)){
            powerupEntity = entityB;
            powerupableEntity = entityA;
        }
        else {
            collisionsToRemove.push_back(&collision);
            break;
        }
        PowerupComponent* powerupComponent = m_impl->m_entityManager->getComponent<PowerupComponent>(powerupEntity);
        if (powerupComponent->m_effect->operator()(powerupableEntity)){
            m_impl->m_entityManager->removeEntity(powerupEntity);
            collisionsToRemove.push_back(&collision);
        }
    }
    for (auto* collision : collisionsToRemove){
        m_impl->m_powerupCollisions.removeCollision(*collision); //Invalid collision
    }
}
