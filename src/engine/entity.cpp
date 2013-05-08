#include "engine/entity.h"

#include "engine/component_factory.h"
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


/**
* @brief Returns Lua bindings for the Entity class
*/
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
    ;
}


/**
* @brief Constructor
*
* Creates a new unnamed entity
*
* Uses the Game's global entity manager
*/
Entity::Entity() 
  : Entity(Game::instance().entityManager())
{
}


/**
* @brief Constructor
*
* Creates a new unnamed entity
*
* @param manager
*   The entity manager to use
*/
Entity::Entity(
    EntityManager& manager
) : Entity(manager.generateNewId(), manager)
{
}


/**
* @brief Constructor
*
* Interfaces to an existing entity
*
* Uses the Game's global entity manager
*
* @param id
*   The entity id to interface to
*/
Entity::Entity(
    EntityId id
) : Entity(id, Game::instance().entityManager())
{
}


/**
* @brief Constructor
*
* Interfaces to an existing entity
*
* @param id
*   The entity id to interface to
* @param manager
*   The entity manager to use
*/
Entity::Entity(
    EntityId id,
    EntityManager& manager
) : m_impl(new Implementation(id, &manager))
{
}


/**
* @brief Constructor
*
* Interfaces to a named entity
*
* Uses the Game's global entity manager
*
* @param name
*   The name of the entity to interface to
*/
Entity::Entity(
    const std::string& name
) : Entity(name, Game::instance().entityManager())
{
}


/**
* @brief Constructor
*
* Interfaces to a named entity
*
* @param name
*   The name of the entity to interface to
* @param manager
*   The entity manager to use
*/
Entity::Entity(
    const std::string& name,
    EntityManager& manager
) : m_impl(new Implementation(manager.getNamedId(name), &manager))
{
}


/**
* @brief Copy constructor
*
* @param other
*/
Entity::Entity(
    const Entity& other
) : Entity(
        other.m_impl->m_id, 
        *(other.m_impl->m_entityManager)
    )
{
}


/**
* @brief Destructor
*/
Entity::~Entity() {}


/**
* @brief Compares two entities
*
* Entities compare to \c true if their entity ids are equal
* and they use the same entity manager.
*
*/
bool
Entity::operator == (
    const Entity& other
) const {
    return 
        (m_impl->m_entityManager == other.m_impl->m_entityManager) and
        (m_impl->m_id == other.m_impl->m_id)
    ;
}


/**
* @brief Copy assignment
*/
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


/**
* @brief Adds a component to this entity
*
* @param component
*/
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
        ComponentFactory::instance().typeNameToId(typeName)
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
        ComponentFactory::instance().typeNameToId(typeName)
    );
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
        ComponentFactory::instance().typeNameToId(typeName)
    );
}
