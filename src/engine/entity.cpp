#include "engine/entity.h"

#include "engine/engine.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "game.h"
#include "scripting/luabind.h"

#include <luabind/operator.hpp>
#include <luabind/adopt_policy.hpp>

using namespace thrive;


struct Entity::Implementation {

    Implementation(
        EntityId id,
        EntityManager* manager
    ) : m_id(id),
        m_entityManager(manager)
    {
    }

    EntityId m_id = NULL_ENTITY;

    EntityManager* m_entityManager = nullptr;

};


static void
Entity_addComponent(
    Entity* self,
    Component* nakedComponent
) {
    self->addComponent(
        std::unique_ptr<Component>(nakedComponent)
    );
}


luabind::scope
Entity::luaBindings() {
    using namespace luabind;
    return class_<Entity>("Entity")
        .def(constructor<>())
        .def(constructor<GameState*>())
        .def(constructor<EntityId>())
        .def(constructor<EntityId, GameState*>())
        .def(constructor<const std::string&>())
        .def(constructor<const std::string&, GameState*>())

        .def(const_self == other<Entity>())
        .def("addComponent", &Entity_addComponent, adopt(_2))
        .def("destroy", &Entity::destroy)
        .def("exists", &Entity::exists)
        .def("getComponent", &Entity::getComponent)
        .def("isVolatile", &Entity::isVolatile)
        .def("removeComponent", &Entity::removeComponent)
        .def("setVolatile", &Entity::setVolatile)
        .def("stealName", &Entity::stealName)
        .property("id", &Entity::id)
    ;
}


static EntityManager&
getEntityManager(
    GameState* gameState
) {
    if (gameState) {
        return gameState->entityManager();
    }
    else {
        return Game::instance().engine().currentGameState()->entityManager();
    }
}


Entity::Entity(
    GameState* gameState
) : Entity(getEntityManager(gameState).generateNewId(), gameState)
{

}


Entity::Entity(
    EntityId id,
    GameState* gameState
) : m_impl(new Implementation(id, &getEntityManager(gameState)))
{
}


Entity::Entity(
    const std::string& name,
    GameState* gameState
) : Entity(getEntityManager(gameState).getNamedId(name), gameState)
{
}


Entity::Entity(
    const Entity& other
) : m_impl(new Implementation(other.m_impl->m_id, other.m_impl->m_entityManager))
{
}


Entity::~Entity() {}


bool
Entity::operator == (
    const Entity& other
) const {
    return
        (m_impl->m_entityManager == other.m_impl->m_entityManager) and
        (m_impl->m_id == other.m_impl->m_id)
    ;
}


Entity&
Entity::operator = (
    const Entity& other
) {
    if (this != &other) {
        m_impl->m_id = other.m_impl->m_id;
        m_impl->m_entityManager = other.m_impl->m_entityManager;
    }
    return *this;
}


void
Entity::addComponent(
    std::unique_ptr<Component> component
) {
    m_impl->m_entityManager->addComponent(
        m_impl->m_id,
        std::move(component)
    );
}


void
Entity::destroy() {


    m_impl->m_entityManager->removeEntity(m_impl->m_id);
}


bool
Entity::exists() const {
    return m_impl->m_entityManager->exists(m_impl->m_id);
}


Component*
Entity::getComponent(
    ComponentTypeId typeId
) {
    return m_impl->m_entityManager->getComponent(
        m_impl->m_id,
        typeId
    );
}


bool
Entity::hasComponent(
    ComponentTypeId typeId
) {
    Component* component = m_impl->m_entityManager->getComponent(
        m_impl->m_id,
        typeId
    );
    return component != nullptr;
}


EntityId
Entity::id() const {
    return m_impl->m_id;
}


bool
Entity::isVolatile() const {
    return m_impl->m_entityManager->isVolatile(m_impl->m_id);
}


void
Entity::removeComponent(
    ComponentTypeId typeId
) {
    m_impl->m_entityManager->removeComponent(
        m_impl->m_id,
        typeId
    );
}


void
Entity::setVolatile(
    bool isVolatile
) {
    m_impl->m_entityManager->setVolatile(m_impl->m_id, isVolatile);
}


void
Entity::stealName(
    const std::string& name
) {
    m_impl->m_entityManager->stealName(m_impl->m_id, name);
}





