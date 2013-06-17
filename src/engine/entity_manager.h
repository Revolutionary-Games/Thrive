#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"

#include <memory>
#include <unordered_set>

namespace thrive {

class Component;
class ComponentCollection;

/**
* @brief Manages entities and their components
*
* The entity manager is the central place for adding and removing
* components from the game. It communicates with the active engines
* so that they get notified of new components in a thread-safe manner.
*/
class EntityManager {

public:

    /**
    * @brief Special entity id for "no entity"
    *
    * This entity id will never be returned by generateNewId()
    */
    static const EntityId NULL_ID;

    /**
    * @brief Constructor
    */
    EntityManager();

    /**
    * @brief Destructor
    */
    ~EntityManager();

    /**
    * @brief Adds a component
    *
    * All registered engines will be notified and will update their 
    * component collections at the beginning of their next frame.
    *
    * @param entityId
    *   The entity to add to
    * @param component
    *   The component to add
    */
    void
    addComponent(
        EntityId entityId,
        std::shared_ptr<Component> component
    );

    /**
    * @brief Removes all components
    *
    * Usually only used in testing.
    */
    void
    clear();

    std::unordered_set<EntityId>
    entities();

    /**
    * @brief Generates a new, unique entity id
    *
    * This function is thread safe.
    *
    * @return A new entity id
    */
    EntityId
    generateNewId();

    /**
    * @brief Retrieves a component
    *
    * @param entityId
    *   The component's owner
    * @param typeId
    *   The component's type id
    *
    * @return 
    *   A non-owning pointer to the component or \c nullptr if no such 
    *   component exists
    */
    Component*
    getComponent(
        EntityId entityId,
        Component::TypeId typeId
    );

    template<class ComponentType>
    ComponentType*
    getComponent(
        EntityId entityId
    ) {
        Component* component = this->getComponent(
            entityId,
            ComponentType::TYPE_ID()
        );
        return static_cast<ComponentType*>(component);
    }

    /**
    * @brief Returns a component collection
    *
    * @param typeId
    *   The component type the collection is holding
    *
    */
    ComponentCollection&
    getComponentCollection(
        Component::TypeId typeId
    );

    /**
    * @brief Returns the id of a named entity
    *
    * If the name is unknown, a new entity id is created. This function always
    * returns the same entity id for the same name during the same 
    * application instance.
    *
    * @param name
    *   The entity's name
    *
    * @return 
    *   The named entity's id
    */
    EntityId
    getNamedId(
        const std::string& name
    );

    /**
    * @brief Checks whether an entity exists
    *
    * @param entityId
    *   The id to check for
    *
    * @return \c true if the entity has at least one component, false otherwise
    */
    bool
    exists(
        EntityId entityId
    ) const;

    /**
    * @brief Removes a component
    *
    * If the component doesn't exist, this function does nothing.
    *
    * @param entityId
    *   The component's owner
    * @param typeId
    *   The component's type id
    */
    void
    removeComponent(
        EntityId entityId,
        Component::TypeId typeId
    );

    /**
    * @brief Removes all components of an entity
    *
    * If the entity has no components, this function does nothing.
    *
    * @param entityId
    *   The entity to remove
    */
    void
    removeEntity(
        EntityId entityId
    );

    void
    update();

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
