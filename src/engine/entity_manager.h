#pragma once

#include "engine/typedefs.h"
#include "util/make_unique.h"

#include <memory>
#include <unordered_set>

namespace thrive {

class Component;
class ComponentCollection;
class ComponentFactory;
class GameState;
class StorageContainer;

/**
* @brief Manages entities and their components
*
* The entity manager holds a collection of Component objects, sorted by type
* and entity.
*/
class EntityManager {

public:

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
    * @param entityId
    *   The entity to add to
    * @param component
    *   The component to add
    *
    * @return
    *   The component as a non-owning pointer
    *
    * @note:
    *   Use the templated version to receive the proper type back
    */
    Component*
    addComponent(
        EntityId entityId,
        std::unique_ptr<Component> component
    );

    /**
    * @brief Adds a component
    *
    * @tparam C
    *   The component's class
    *
    * @param entityId
    *   The entity to add to
    *
    * @param component
    *   The component to add
    *
    * @return
    *   The component as a non-owning pointer
    */
    template<typename C>
    C*
    addComponent(
        EntityId entityId,
        std::unique_ptr<C> component
    ) {
        return static_cast<C*>(
            this->addComponent(
                entityId,
                std::unique_ptr<Component>(std::move(component))
            )
        );
    }

    /**
    * @brief Removes all components
    *
    * Usually only used in testing.
    */
    void
    clear();

    /**
    * @brief Returns a set of entity ids that have at least one components
    */
    std::unordered_set<EntityId>
    entities();

    /**
    * @brief Generates a new, unique entity id
    *
    * This function is safe.
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
        ComponentTypeId typeId
    );

    /**
    * @brief Convenience template overload
    *
    * This is the same as EntityManager::getComponent(EntityId, ComponentTypeId),
    * but includes a cast to the expected type. The cast is a static cast for
    * performance reasons. Unless there is a serious error in the way
    * component type ids are generated or components are stored in the entity
    * manager, the cast should always be correct.
    *
    * The component id is read from \a ComponentType::TYPE_ID
    *
    * @tparam ComponentType
    *   The component subclass to retrieve
    *
    * @param entityId
    *   The component's owner
    *
    * @return
    */
    template<class ComponentType>
    ComponentType*
    getComponent(
        EntityId entityId
    ) {
        Component* component = this->getComponent(
            entityId,
            ComponentType::TYPE_ID
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
        ComponentTypeId typeId
    );

    /**
    * @brief Returns the id of a named entity
    *
    * If the name is unknown, a new entity id is created. This function always
    * returns the same entity id for the same name during the same application
    * instance.
    *
    * @param name
    *   The entity's name
    *
    * @param forceNew
    *   Forces a new entity with the provided name to be created whether it exists or not
    *
    * @return
    *   The named entity's id
    */
    EntityId
    getNamedId(
        const std::string& name,
        bool forceNew = false
    );

    /**
    * @brief Retrieves a component, creating it if necessary
    *
    * @tparam C
    *   The component class
    *
    * @tparam Args
    *   Constructor arguments in case the component could not be found
    *
    * @param id
    *   The entity the component belongs to
    *
    * @param args
    *   Constructor arguments
    *
    * @return
    *   A non-owning pointer to the component
    */
    template<typename C, typename... Args>
    C*
    getOrCreateComponent(
        EntityId id,
        Args&&... args
    ) {
        auto component = this->getComponent<C>(id);
        if (not component) {
            auto newComponent = make_unique<C>(args...);
            component = newComponent.get();
            this->addComponent(id, std::move(newComponent));
        }
        return component;
    }

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
    * @brief Returns the set of non-empty collection ids
    *
    * @return
    */
    std::unordered_set<ComponentTypeId>
    nonEmptyCollections() const;

    /**
    * @brief Returns the volatile flag for an entity
    *
    * Volatile entities are not serialized into a savegame
    *
    * @param id
    *
    * @return
    */
    bool
    isVolatile(
        EntityId id
    ) const;

    /**
    * @brief Rebinds a name to this entity
    *
    * @param name
    *  name re rebind
    */
    void
    stealName(
        EntityId entityId,
        const std::string& name
    );

    /**
    * @brief Transfers an entity to a different gamestate removing it from the current one
    *
    * @param entityId
    *   The entity to transfer
    *
    * @param gameState
    *  The new gamestate to own the entity
    *
    * @return
    *  The new entity id in the new gamestate
    */
    EntityId
    transferEntity(
        EntityId entityId,
        GameState* gameState
    );

    /**
    * @brief Stores a single entity
    *
    * @param entityId
    *   The entity to store
    *
    * @return
    *  A container holding the entity.
    */
    StorageContainer
    storeEntity(
        EntityId entityId
    ) const;

    /**
    * @brief Loads an entity into this entity manager
    *
    * @param storage
    *   The storage to load the entity from
    *
    * @param componentFactory
    *  Factory used for loading entity components
    *
    * @return
    *  The new entity id in the new gamestate
    */
    EntityId
    loadEntity(
        StorageContainer storage,
        const ComponentFactory& componentFactory
    );

    /**
    * @brief Removes all components queued for removal
    */
    void
    processRemovals();

    /**
    * @brief Transfers all components queued for transfer
    */
    void
    processTransfers();

    /**
    * @brief Removes a component
    *
    * If the component doesn't exist, this function does nothing.
    *
    * To allow self-removing components such as script handles, the component
    * is only removed with the next call to EntityManager::processRemovals().
    *
    * @param entityId
    *   The component's owner
    * @param typeId
    *   The component's type id
    */
    void
    removeComponent(
        EntityId entityId,
        ComponentTypeId typeId
    );

    /**
    * @brief Removes all components of an entity
    *
    * If the entity has no components, this function does nothing.
    *
    * To allow self-removing components such as script handles, the component
    * is only removed with the next call to EntityManager::processRemovals().
    *
    * @param entityId
    *   The entity to remove
    */
    void
    removeEntity(
        EntityId entityId
    );

    /**
    * @brief Gets the name for a given entityId if it exists
    *
    * @param entityId
    *  The entity to find a name mapping for
    *
    * @return
    *   \c valid pointer to string if such a namemapping was found, nullptr otherwise
    */
    const std::string*
    getNameMappingFor(
        EntityId entityId
    );

    /**
    * @brief Restores the entity manager from a storage container
    *
    * @param storage
    *   The storage container to restore from
    * @param factory
    *   The component factory to use
    */
    void
    restore(
        const StorageContainer& storage,
        const ComponentFactory& factory
    );

    /**
    * @brief Sets the volatile flag for an entity
    *
    * @param id
    * @param isVolatile
    */
    void
    setVolatile(
        EntityId id,
        bool isVolatile
    );

    /**
    * @brief Serializes the current non-volatile components into a storage container
    *
    * @param factory
    *   The component factory to use for type name lookup
    *
    * @return
    */
    StorageContainer
    storage(
        const ComponentFactory& factory
    ) const;

private:

    friend class Engine;

    /**
    * @brief Transfers an entity to a different gamestate removing it from the current one
    *  This is called by engine when it processes transfers
    *
    * @param oldEntityId
    *   The old entity to transfer
    *
    * @param newEntityId
    *   The new entity to transfer components to
    *
    * @param newEntityManager
    *  The new entityManager owning the newEntity
    *
    * @param componentFactory
    *  Factory used for loading new copies of components
    */
    void
    transferEntity(
        EntityId oldEntityId,
        EntityId newEntityId,
        EntityManager& newEntityManager,
        const ComponentFactory& componentFactory
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
