#pragma once

#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/component_collection.h"

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


/**
* @brief Extracts the Component class from a filter argument
*
* @tparam ComponentType A bare Component class
*/
template<typename ComponentType>
struct ExtractComponentType {

    using PointerType = ComponentType*;

    using Type = ComponentType;

};


/**
* @brief Extracts the Component class from a filter argument
* wrapped by the Optional template
*
* @tparam ComponentType A Component class wrapped by Op
*/
template<typename ComponentType>
struct ExtractComponentType<Optional<ComponentType>> {

    using PointerType = ComponentType*;

    using Type = ComponentType;

};


template<typename ComponentType>
struct IsRequired {
    
    static const bool value = true;

};


template<typename ComponentType>
struct IsRequired<Optional<ComponentType>> {

    static const bool value = false;

};


template<size_t index, typename... ComponentTypes>
struct ComponentGroupBuilder {

    using ComponentGroup = std::tuple<
        typename ExtractComponentType<ComponentTypes>::PointerType...
    >;

    static bool 
    build(
        Engine* engine,
        Entity::Id entityId,
        ComponentGroup& group
    ) {
        using ComponentType = typename std::tuple_element<index, std::tuple<ComponentTypes...>>::type;
        using RawType = typename ExtractComponentType<ComponentType>::Type;
        bool isRequired = IsRequired<ComponentType>::value;
        RawType* component = engine->getComponent<RawType>(entityId);
        if (isRequired and not component) {
            return false;
        }
        std::get<index>(group) = component;
        return ComponentGroupBuilder<index-1, ComponentTypes...>::build(engine, entityId, group);
    }
        
};


template<typename... ComponentTypes>
struct ComponentGroupBuilder<0, ComponentTypes...> {

    static bool 
    build(
        Engine* engine,
        Entity::Id entityId,
        std::tuple<typename ExtractComponentType<ComponentTypes>::PointerType...>& group
    ) {
        using ComponentType = typename std::tuple_element<0, std::tuple<ComponentTypes...>>::type;
        using RawType = typename ExtractComponentType<ComponentType>::Type;
        bool isRequired = IsRequired<ComponentType>::value;
        RawType* component = engine->getComponent<RawType>(entityId);
        if (isRequired and not component) {
            return false;
        }
        std::get<0>(group) = component;
        return true;
    }
        
};

template<size_t tupleIndex>
struct RegisterForNextSignal {
    
    template<typename Filter>
    static void registerForNextSignal(
        Filter& filter
    ) {
        // May the programming gods have mercy for the poor souls
        // who will have to read this.
        filter.template registerForSignal<tupleIndex-1>();
    }
};

template<>
struct RegisterForNextSignal<0> {

    template<typename Filter>
    static void registerForNextSignal(Filter&) {}
};


template<typename... ComponentTypes>
class EntityFilter {

public:

    using ComponentGroup = std::tuple<
        typename ExtractComponentType<ComponentTypes>::PointerType...
    >;

    using EntityMap = std::unordered_map<Entity::Id, ComponentGroup>;

    EntityMap
    entities() const {
        return m_entities;
    };

    void
    setEngine(
        Engine* engine
    ) {
        this->unregisterForSignals();
        m_entities.clear();
        m_engine = engine;
        if (engine) {
            this->initEntities();
            this->registerForSignal<sizeof...(ComponentTypes)-1>();
        }
    }

private:

    template<size_t> friend class RegisterForNextSignal;

    void
    initEntities() {
        for (Entity::Id id : m_engine->entities()) {
            this->initEntity(id);
        }
    }

    void
    initEntity(
        Entity::Id id
    ) {
        ComponentGroup group;
        bool isComplete = ComponentGroupBuilder<sizeof...(ComponentTypes) - 1, ComponentTypes...>::build(
            m_engine,
            id,
            group
        );
        if (isComplete) {
            m_entities[id] = group;
        }
    }

    void
    onComponentAdded(
        Entity::Id entityId
    ) {
        this->initEntity(entityId);
    }

    template<int tupleIndex>
    void
    onOptionalComponentRemoved(
        Entity::Id entityId
    ) {
        auto iter = m_entities.find(entityId);
        if (iter != m_entities.end()) {
            std::get<tupleIndex>(iter->second) = nullptr;
        }
    }

    void
    onRequiredComponentRemoved(
        Entity::Id entityId
    ) {
        m_entities.erase(entityId);
    }

    template<int tupleIndex>
    void
    registerForSignal() {
        using ComponentType = typename std::tuple_element<
            tupleIndex, 
            std::tuple<ComponentTypes...>
        >::type;
        using RawType = typename ExtractComponentType<ComponentType>::Type;
        using namespace std::placeholders;
        bool isRequired = IsRequired<ComponentType>::value;
        Connection::Ptr connection;
        const auto& collection = m_engine->getComponentCollection(
            RawType::TYPE_ID
        );
        // Added
        connection = collection.sig_componentAdded.connect(std::bind(
            &EntityFilter<ComponentTypes...>::onComponentAdded,
            this, _1
        ));
        m_connections.push_front(connection);
        // Removed
        if (isRequired) {
            connection = collection.sig_componentRemoved.connect(std::bind(
                &EntityFilter<ComponentTypes...>::onRequiredComponentRemoved,
                this, _1
            ));
        }
        else {
            connection = collection.sig_componentRemoved.connect(std::bind(
                &EntityFilter<ComponentTypes...>::onOptionalComponentRemoved<tupleIndex>,
                this, _1
            ));
        }
        m_connections.push_front(connection);
        RegisterForNextSignal<tupleIndex>::registerForNextSignal(*this);
    }

    void
    unregisterForSignals() {
        for(auto& connection : m_connections) {
            connection->disconnect();
        }
        m_connections.clear();
    }

    std::forward_list<Connection::Ptr> m_connections;

    Engine* m_engine = nullptr;

    EntityMap m_entities;

};

template<>
class EntityFilter<> {
};

}
