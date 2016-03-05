#pragma once

#include "engine/entity_manager.h"
#include "engine/component_collection.h"

#include <assert.h>
#include <forward_list>
#include <functional>
#include <tuple>
#include <unordered_map>

#include <iostream>

namespace thrive {

/**
* @brief Marker for optional components in a filter
*
* @tparam ComponentType The option component type
*/
template<typename ComponentType>
struct Optional {};

namespace detail {

template<typename ComponentType>
struct ExtractComponentType {

    using PointerType = ComponentType*;

    using Type = ComponentType;

};

template<typename ComponentType>
struct ExtractComponentType<Optional<ComponentType>> {

    using PointerType = ComponentType*;

    using Type = ComponentType;

};

} // namespace detail


/**
* @brief Filters for entities that contain specific components
*
* An entity filter helps a system in finding the entities that have exactly
* the right components to be relevant for the system.
*
* @tparam ComponentTypes
*   The component classes to watch for. You can wrap a class with the
*   Optional template if you want to know if it's there, but it's not
*   required.
*
* Usage example:
* \code
* class MySystem : public System {
*
* private:
*
*   EntityFilter<
*       MyComponent,
*       Optional<SomeOtherComponent>
*   >
*   m_entities;
*
* public:
*
*   void init(Engine* engine) override {
*       System::initNamed("MySystem", "engine);
*       m_entities.setEntityFilter(&engine->entityManager());
*   }
*
*   void update(int milliseconds) override {
*       for (auto& value : m_entities) {
*           EntityId entity = value.first;
*           MyComponent* myComponent = std::get<0>(value.second);
*           // Do something with myComponent
*           SomeOtherComponent* someOtherComponent = std::get<1>(value.second);
*           if (someOtherComponent) {
*               // Do something with someOtherComponent
*           }
*       }
*   }
*
*   void shutdown() overrde {
*       m_entities.setEntityManager(nullptr);
*       System::shutdown();
*   }
* };
* \endcode
*/
template<typename... ComponentTypes>
class EntityFilter {

public:

    /**
    * @brief Typedef for the filter's component tuple
    *
    * All elements of the tuple are pointers. An optional component may
    * be \c nullptr if not present.
    */
    using ComponentGroup = std::tuple<
        typename detail::ExtractComponentType<ComponentTypes>::PointerType...
    >;

    /**
    * @brief Typedef for the filter's list of relevant entities
    */
    using EntityMap = std::unordered_map<EntityId, ComponentGroup>;

    /**
    * @brief Constructor
    *
    * @param recordChanges
    *   If \c true, you can query the added and removed entities through
    *   this filter.
    *
    * @warning
    *   If \a recordChanges is true, you are responsible for clearing the
    *   collections returned by addedEntities() and removedEntities().
    *   If you don't clear them regularly, it's a memory leak.
    *   You can use EntityFilter::clearChanges() to clear both collections.
    */
    EntityFilter(
        bool recordChanges = false
    );

    /**
    * @brief Destructor
    */
    ~EntityFilter() {}

    /**
    * @brief Returns the entities added to this filter
    *
    * When you have processed the collection, please call clear() on
    * it.
    *
    */
    EntityMap&
    addedEntities();

    /**
    * @brief Iterator
    *
    * Equivalent to
    * \code
    * entities().cbegin()
    * \endcode
    *
    * @return An iterator to the first relevant entity
    */
    typename EntityMap::const_iterator
    begin() const;

    /**
    * @brief Clears the lists for added and removed entities
    */
    void
    clearChanges();

    /**
    * @brief Checks whether an entity id is contained in this filter
    *
    * @param id
    *   The entity to check for
    *
    * @return
    *   \c true if this id can be found in this filter, \c false otherwise
    */
    bool
    containsEntity(
        EntityId id
    ) const;

    /**
    * @brief Iterator
    *
    * Equivalent to
    * \code
    * entities().cend()
    * \endcode
    *
    * @return An iterator to the end of the relevant entities
    */
    typename EntityMap::const_iterator
    end() const;

    /**
    * @brief The relevant entities
    *
    * The returned collection maps the entity id to the components required
    * by this filter. Optional components may be \c nullptr.
    *
    */
    const EntityMap&
    entities() const;

    /**
    * @brief Returns the entities removed from this filter
    *
    * When you have processed the collection, please call clear() on
    * it.
    *
    */
    std::unordered_set<EntityId>&
    removedEntities();

    /**
    * @brief Gets the current entiyManager
    *
    * @return
    */
    EntityManager*
    entityManager();

    /**
    * @brief Sets the entity manager this filter applies to
    *
    * @param entityManager
    *   The new entity manager to listen to. If \c nullptr, the filter stays
    *   empty.
    */
    void
    setEntityManager(
        EntityManager* entityManager
    );

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

template<>
class EntityFilter<> {
};

}

#include "engine/entity_filter.cpp"
