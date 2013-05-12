#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"

#include <string>

namespace luabind {
class scope;
}

namespace thrive {

class EntityManager;

/**
* @brief Convenience class to handle an entity
*
* Mostly for script purposes, but if you prefer, you can use this class
* instead of directly calling the EntityManager functions.
*
* @note
*   Generally, the type id overloads of this class are to be preferred
*   for optimum performance.
*/
class Entity {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes the following \b constructors:
    * - \c Entity(): Entity()
    * - \c Entity(number): Entity(EntityId)
    * - \c Entity(string): Entity(const std::string&)
    *
    * Exposes the following \b functions:
    * - \c addComponent(Component): addComponent(std::shared_ptr<Component>)
    * - \c getComponent(number): getComponent(Component::TypeId)
    * - \c getComponent(string): getComponent(const std::string&)
    * - \c removeComponent(number): removeComponent(Component::TypeId)
    * - \c removeComponent(string): removeComponent(const std::string&)
    *
    * Exposes the following \b operators:
    * - \c ==: operator==(const Entity&)
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    *
    * Creates a new unnamed entity
    *
    * Uses the Game's global entity manager
    */
    Entity();

    /**
    * @brief Constructor
    *
    * Creates a new unnamed entity
    *
    * @param entityManager
    *   The entity manager to use
    */
    Entity(
        EntityManager& entityManager
    );

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
    Entity(
        EntityId id
    );

    /**
    * @brief Constructor
    *
    * Interfaces to an existing entity
    *
    * @param id
    *   The entity id to interface to
    * @param entityManager
    *   The entity manager to use
    */
    Entity(
        EntityId id,
        EntityManager& entityManager
    );

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
    Entity(
        const std::string& name
    );

    /**
    * @brief Constructor
    *
    * Interfaces to a named entity
    *
    * @param name
    *   The name of the entity to interface to
    * @param entityManager
    *   The entity manager to use
    */
    Entity(
        const std::string& name,
        EntityManager& entityManager
    );

    /**
    * @brief Copy constructor
    *
    * @param other
    */
    Entity(
        const Entity& other
    );

    /**
    * @brief Destructor
    */
    ~Entity();

    /**
    * @brief Copy assignment
    */
    Entity&
    operator = (
        const Entity& other
    );

    /**
    * @brief Compares two entities
    *
    * Entities compare to \c true if their entity ids are equal
    * and they use the same entity manager.
    *
    */
    bool
    operator == (
        const Entity& other
    ) const;

    /**
    * @brief Adds a component to this entity
    *
    * @param component
    */
    void
    addComponent(
        std::shared_ptr<Component> component
    );

    /**
    * @brief Checks if the entity has any components
    *
    * @return \c true if the entity has at least one component, \c false otherwise
    */
    bool
    exists() const;

    /**
    * @brief Retrieves a component by type id
    *
    * @param typeId
    *   The component's type id
    *
    * @return 
    *   A non-owning pointer to the component or \c nullptr if no such 
    *   component was found
    */
    Component*
    getComponent(
        Component::TypeId typeId
    );

    /**
    * @brief Retrieves a component by type name
    *
    * @param typeName
    *   The component's type name
    *
    * @return 
    *   A non-owning pointer to the component or \c nullptr if no such 
    *   component was found
    */
    Component*
    getComponent(
        const std::string& typeName
    );

    /**
    * @brief Checks whether this entity has a component
    *
    * Equivalent to 
    * \code
    * entity->getComponent(typeId) != nullptr;
    * \endcode
    *
    * @param typeId
    *   The component's type id
    *
    * @return 
    *   \c true if such a component was found, false otherwise
    */
    bool
    hasComponent(
        Component::TypeId typeId
    );

    /**
    * @brief Checks whether this entity has a component
    *
    * Equivalent to 
    * \code
    * entity->getComponent(typeName) != nullptr;
    * \endcode
    *
    * @param typeName
    *   The component's type name
    *
    * @return 
    *   \c true if such a component was found, false otherwise
    */
    bool
    hasComponent(
        const std::string& typeName
    );

    /**
    * @brief Removes a component by type id
    *
    * If no such component was found, does nothing.
    *
    * @param typeId
    *   The component's type id
    */
    void
    removeComponent(
        Component::TypeId typeId
    );

    /**
    * @brief Removes a component by type name
    *
    * If no such component was found, does nothing.
    *
    * @param name
    *   The component's type name
    */
    void
    removeComponent(
        const std::string& name
    );

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


}
