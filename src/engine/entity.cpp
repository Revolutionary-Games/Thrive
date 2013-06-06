#include "engine/entity.h"

#include "engine/component_registry.h"
#include "engine/entity_manager.h"
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

    EntityId m_id;

    EntityManager* m_entityManager;
};


luabind::scope
Entity::luaBindings() {
    using namespace luabind;
    return class_<Entity>("Entity")
        .def(constructor<>())
        .def(constructor<EntityId>())
        .def(constructor<const std::string&>())
        .def(const_self == other<Entity>())
        .def("addComponent", &Entity::addComponent)
        .def("exists", &Entity::exists)
        .def("getComponent",
            static_cast<Component* (Entity::*) (Component::TypeId)>(&Entity::getComponent)
        )
        .def("getComponent",
            static_cast<Component* (Entity::*) (const std::string&)>(&Entity::getComponent)
        )
        .def("removeComponent",
            static_cast<void (Entity::*) (Component::TypeId)>(&Entity::removeComponent)
        )
        .def("removeComponent",
            static_cast<void (Entity::*) (const std::string&)>(&Entity::removeComponent)
        )
        .property("id", &Entity::id)
    ;
}


Entity::Entity()
  : Entity(Game::instance().entityManager())
{
}


Entity::Entity(
    EntityManager& manager
) : Entity(manager.generateNewId(), manager)
{
}


Entity::Entity(
    EntityId id
) : Entity(id, Game::instance().entityManager())
{
}


Entity::Entity(
    EntityId id,
    EntityManager& manager
) : m_impl(new Implementation(id, &manager))
{
}


Entity::Entity(
    const std::string& name
) : Entity(name, Game::instance().entityManager())
{
}


Entity::Entity(
    const std::string& name,
    EntityManager& manager
) : m_impl(new Implementation(manager.getNamedId(name), &manager))
{
}


Entity::Entity(
    const Entity& other
) : Entity(
        other.m_impl->m_id,
        *(other.m_impl->m_entityManager)
    )
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
    std::shared_ptr<Component> component
) {
    m_impl->m_entityManager->addComponent(
        m_impl->m_id,
        std::move(component)
    );
}


bool
Entity::exists() const {
    return m_impl->m_entityManager->exists(m_impl->m_id);
}


Component*
Entity::getComponent(
    Component::TypeId typeId
) {
    return m_impl->m_entityManager->getComponent(
        m_impl->m_id,
        typeId
    );
}


Component*
Entity::getComponent(
    const std::string& typeName
) {
    return this->getComponent(
        ComponentRegistry::instance().typeNameToId(typeName)
    );
}


bool
Entity::hasComponent(
    Component::TypeId typeId
) {
    Component* component = m_impl->m_entityManager->getComponent(
        m_impl->m_id,
        typeId
    );
    return component != nullptr;
}


bool
Entity::hasComponent(
    const std::string& typeName
) {
    return this->hasComponent(
        ComponentRegistry::instance().typeNameToId(typeName)
    );
}


EntityId
Entity::id() const {
    return m_impl->m_id;
}


void
Entity::removeComponent(
    Component::TypeId typeId
) {
    m_impl->m_entityManager->removeComponent(
        m_impl->m_id,
        typeId
    );
}


void
Entity::removeComponent(
    const std::string& typeName
) {
    this->removeComponent(
        ComponentRegistry::instance().typeNameToId(typeName)
    );
}
