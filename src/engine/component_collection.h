#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"

#include <memory>

namespace thrive {

class Engine;

/**
* @brief A collection of components of one particular type
*
* Component collections are used by Engine objects to keep
* track of the components attached to an entity. Components
* are 
*/
class ComponentCollection {

public:

    /**
    * @brief Callback for when a component has been added or removed
    */
    using ChangeCallback = std::function<void(EntityId, Component&)>;

    /**
    * @brief Destructor
    */
    ~ComponentCollection();

    /**
    * @see ComponentCollection::get
    */
    Component*
    operator[] (
        EntityId entityId
    ) const;

    /**
    * @brief Retrieves a component from the collection
    *
    * @param entityId The entity the component belongs to
    *
    * @return 
    *   A non-owning pointer to the component or \c nullptr if no such 
    *   component exists
    *
    */
    Component*
    get(
        EntityId entityId
    ) const;

    /**
    * @brief Registers callbacks for when components are added or removed
    *
    * @param onComponentAdded
    *   Called when a component has been added
    * @param 
    *   Called when a component has been removed
    *
    * @return 
    *   An identifier with which you can remove the callbacks.
    *
    * @see unregisterChangeCallbacks
    */
    unsigned int
    registerChangeCallbacks(
        ChangeCallback onComponentAdded,
        ChangeCallback onComponentRemoved
    );

    /**
    * @brief The type id of the collection's components
    */
    Component::TypeId
    type() const;

    /**
    * @brief Unregisters change callbacks
    *
    * If the id could not be found, does nothing.
    *
    * @param id
    *   The id returned by registerChangeCallbacks
    */
    void
    unregisterChangeCallbacks(
        unsigned int id
    );

private:

    friend class Engine;

    /**
    * @brief Constructor
    *
    * @param type The type id of the components held by this collection.
    */
    ComponentCollection(
        Component::TypeId type
    );

    /**
    * @brief Processes the queues for added and removed components
    */
    void
    processQueue();

    /**
    * @brief Queues a component for addition
    *
    * The component will be available after the next call to 
    * \c ComponentCollection::processQueue.
    *
    * Any existing component of the same type will be overwritten.
    *
    * This method is thread-safe.
    *
    * @param entityId
    *   The entity the component belongs to
    *
    * @param component
    *   The component to add
    */
    void
    queueComponentAddition(
        EntityId entityId,
        std::shared_ptr<Component> component
    );

    /**
    * @brief Queues a component for removal
    *
    * The component will be removed after the next call to 
    * \c ComponentCollection::processQueue.
    *
    * If no such component exists, does nothing.
    *
    * This method is thread-safe.
    *
    * @param entityId
    *   The entity the component belongs to
    */
    void
    queueComponentRemoval(
        EntityId entityId
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    
};

}

