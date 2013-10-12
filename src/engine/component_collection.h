#pragma once

#include "engine/component.h"
#include "engine/typedefs.h"

#include <memory>

namespace thrive {

class EntityManager;

/**
* @brief Utility class for the EntityManager
*
* A component collection handles components of one specific type. It offers
* functions to retrieve the component (if any) of a specific entity and
* callbacks for when a component has been added or removed (mainly used by
* the EntityFilter).
*
* Component collections are pretty much read-only for anything but the 
* EntityManager. Use the manager to actually add or remove components.
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
    * @brief Empties the component collection
    */
    void
    clear();

    /**
    * @brief Returns a reference to the internal component map
    *
    */
    const std::unordered_map<EntityId, std::unique_ptr<Component>>&
    components() const;

    /**
    * @brief Checks whether this collection is empty
    *
    * @return \c true if the collection is empty, \c false otherwise
    */
    bool
    empty() const;

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
    * @param onComponentRemoved
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
    ComponentTypeId
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

    /**
    * @brief Only the EntityManager should be able to add / remove components.
    */
    friend class EntityManager;

    /**
    * @brief Constructor
    *
    * @param type The type id of the components held by this collection.
    */
    ComponentCollection(
        ComponentTypeId type
    );

    /**
    * @brief Adds a component
    *
    * Also calls any callbacks registered for added components.
    *
    * @param entityId
    *   The entity the component belongs to
    * @param component
    *   The component to add
    *
    * @return 
    *   \c true if the component is new, i.e. does not overwrite an existing 
    *   one, \c false otherwise
    */
    bool
    addComponent(
        EntityId entityId,
        std::unique_ptr<Component> component
    );

    /**
    * @brief Removes a component
    *
    * Also calls any callbacks registered for removed components.
    *
    * @param entityId
    *   The entity the component belongs to
    *
    * @return 
    *   \c true if a component was removed, \c false if no component for
    *   \a entityId was found.
    */
    bool
    removeComponent(
        EntityId entityId
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
    
};

}

