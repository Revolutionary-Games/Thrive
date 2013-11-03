#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"

#include <string>

namespace luabind {
class scope;
}

namespace thrive {

class GameState;

/**
* @brief Convenience class to handle an entity
*
* This is a thin wrapper around the EntityManager API. Mostly for script 
* purposes, but if you prefer, you can use this class instead of directly 
* calling the EntityManager functions.
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
    * - \c addComponent(Component): addComponent(std::unique_ptr<Component>)
    * - \c getComponent(number): getComponent(ComponentTypeId)
    * - \c removeComponent(number): removeComponent(ComponentTypeId)
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
    * @param gameState
    *   The game state the new entity belongs to. If \c null, the current game
    *   state of the global engine object is used.
    *
    */
    Entity(
        GameState* gameState = nullptr
    );

    /**
    * @brief Constructor
    *
    * Interfaces to an existing entity
    *
    * @param id
    *   The entity id to interface to
    *
    * @param gameState
    *   The game state the entity belongs to. If \c null, the current game
    *   state of the global engine object is used.
    *
    */
    Entity(
        EntityId id,
        GameState* gameState = nullptr
    );

    /**
    * @brief Constructor
    *
    * Interfaces to a named entity
    *
    * Uses the EntityManager of Game::engine()
    *
    * @param name
    *   The name of the entity to interface to
    *
    * @param gameState
    *   The game state the entity belongs to. If \c null, the current game
    *   state of the global engine object is used.
    *
    */
    Entity(
        const std::string& name,
        GameState* gameState = nullptr
    );

    /**
    * @brief Copy constructor
    *
    * This does *not* copy the entity's components, it just creates another
    * wrapper.
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
    *
    * This does *not* copy the entity's components, it just changes the 
    * entity id this wrapper points to.
    *
    */
    Entity&
    operator = (
        const Entity& other
    );

    /**
    * @brief Compares two entities
    *
    * Entities compare to \c true if their entity ids are equal
    * and they are part of the same game state.
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
        std::unique_ptr<Component> component
    );

    /**
    * @brief Removes all components of this entity
    */
    void
    destroy();

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
        ComponentTypeId typeId
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
        ComponentTypeId typeId
    );

    /**
    * @brief The entity's id
    */
    EntityId
    id() const;

    /**
    * @brief Returns the volatile flag
    *
    * Volatile entities are not serialized into a savegame
    *
    * @return 
    */
    bool
    isVolatile() const;

    /**
    * @brief Removes a component by type id
    *
    * If no such component was found, does nothing.
    *
    * @note
    *   The component is only actually removed after the entity manager's
    *   EntityManager::processRemovals() function is called.
    *
    * @param typeId
    *   The component's type id
    */
    void
    removeComponent(
        ComponentTypeId typeId
    );

    /**
    * @brief Sets the volatile flag
    *
    * @param isVolatile
    *
    * @see isVolatile()
    */
    void
    setVolatile(
        bool isVolatile
    );

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


}
