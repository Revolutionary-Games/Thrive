#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"
#include "util/make_unique.h"

#include <memory>
#include <unordered_set>

namespace thrive {

class Component;
class ComponentCollection;

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
        Component::TypeId typeId
    );

    /**
    * @brief Convenience template overload
    *
    * This is the same as EntityManager::getComponent(EntityId, Component::TypeId),
    * but includes a cast to the expected type. The cast is a static cast for
    * performance reasons. Unless there is a serious error in the way 
    * component type ids are generated or components are stored in the entity
    * manager, the cast should always be correct.
    *
    * The component id is read from \a ComponentType::TYPE_ID()
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
    * returns the same entity id for the same name during the same application 
    * instance.
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
    * @brief Removes all components queued for removal
    */
    void
    processRemovals();

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
        Component::TypeId typeId
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

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
