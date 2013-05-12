#pragma once

#include "engine/engine.h"
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
*       System::init(engine);
*       m_entities.setEngine(engine);
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
*       m_entities.setEngine(nullptr);
*       System::shutdown();
*   }
* };
* \endcode
*/
template<typename... ComponentTypes>
class EntityFilter {

public:

    using ComponentGroup = std::tuple<
        typename detail::ExtractComponentType<ComponentTypes>::PointerType...
    >;

    using EntityMap = std::unordered_map<EntityId, ComponentGroup>;

    EntityFilter(
        bool recordChanges = false
    );

    ~EntityFilter() {}

    std::unordered_set<EntityId>&
    addedEntities();

    typename EntityMap::const_iterator
    begin() const;

    typename EntityMap::const_iterator
    end() const;

    const EntityMap&
    entities() const;

    std::unordered_set<EntityId>&
    removedEntities();

    void
    setEngine(
        Engine* engine
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
