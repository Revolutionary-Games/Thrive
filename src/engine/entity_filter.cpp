namespace thrive {

namespace detail {
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
        EntityManager* entityManager,
        EntityId entityId,
        ComponentGroup& group
    ) {
        using ComponentType = typename std::tuple_element<index, std::tuple<ComponentTypes...>>::type;
        using RawType = typename ExtractComponentType<ComponentType>::Type;
        bool isRequired = IsRequired<ComponentType>::value;
        RawType* component = entityManager->getComponent<RawType>(entityId);
        if (isRequired and not component) {
            return false;
        }
        std::get<index>(group) = component;
        return ComponentGroupBuilder<index-1, ComponentTypes...>::build(entityManager, entityId, group);
    }
        
};


template<typename... ComponentTypes>
struct ComponentGroupBuilder<0, ComponentTypes...> {

    static bool 
    build(
        EntityManager* entityManager,
        EntityId entityId,
        std::tuple<typename ExtractComponentType<ComponentTypes>::PointerType...>& group
    ) {
        using ComponentType = typename std::tuple_element<0, std::tuple<ComponentTypes...>>::type;
        using RawType = typename ExtractComponentType<ComponentType>::Type;
        bool isRequired = IsRequired<ComponentType>::value;
        RawType* component = entityManager->getComponent<RawType>(entityId);
        if (isRequired and not component) {
            return false;
        }
        std::get<0>(group) = component;
        return true;
    }
        
};

template<size_t tupleIndex>
struct RegisterNextCallback {
    
    template<typename Filter>
    static void registerNextCallback(
        Filter& filter
    ) {
        // May the programming gods have mercy for the poor souls
        // who will have to read this.
        //
        // This calls a template function called registerCallback
        // on the filter object.
        filter.template registerCallback<tupleIndex-1>();
    }
};

template<>
struct RegisterNextCallback<0> {

    template<typename Filter>
    static void registerNextCallback(Filter&) {}
};

} // namespace detail


template<typename... ComponentTypes>
struct EntityFilter<ComponentTypes...>::Implementation {

    Implementation(
        bool recordChanges
    ) : m_recordChanges(recordChanges)
    {
    }

    void
    initEntities() {
        for (EntityId id : m_entityManager->entities()) {
            this->initEntity(id);
        }
    }

    void
    initEntity(
        EntityId id
    ) {
        ComponentGroup group;
        bool isComplete = detail::ComponentGroupBuilder<sizeof...(ComponentTypes) - 1, ComponentTypes...>::build(
            m_entityManager,
            id,
            group
        );
        if (isComplete) {
            m_entities[id] = group;
            m_entities.insert(std::make_pair(id, group));
            if (m_recordChanges) {
                m_addedEntities[id] = group;
            }
        }
    }

    void
    onComponentAdded(
        EntityId entityId
    ) {
        this->initEntity(entityId);
    }

    template<int tupleIndex>
    void
    onOptionalComponentRemoved(
        EntityId entityId
    ) {
        auto iter = m_entities.find(entityId);
        if (iter != m_entities.end()) {
            std::get<tupleIndex>(iter->second) = nullptr;
            if (iter->second == ComponentGroup()) {
                m_entities.erase(entityId);
                if (m_recordChanges) {
                    m_removedEntities.insert(entityId);
                }
            }
        }
    }

    void
    onRequiredComponentRemoved(
        EntityId entityId
    ) {
        
        if (m_entities.erase(entityId) > 0 and m_recordChanges) {
            if (m_addedEntities.erase(entityId) == 0) {
                // If entityId already was in addedEntities, the entity
                // was added, then removed in the same frame.
                m_removedEntities.insert(entityId);
            }
        }
    }

    template<int tupleIndex>
    void
    registerCallback() {
        using ComponentType = typename std::tuple_element<
            tupleIndex, 
            std::tuple<ComponentTypes...>
        >::type;
        using RawType = typename detail::ExtractComponentType<ComponentType>::Type;
        bool isRequired = detail::IsRequired<ComponentType>::value;
        auto& collection = m_entityManager->getComponentCollection(
            RawType::TYPE_ID
        );
        // Callbacks
        auto onAdded = [this] (EntityId id, Component&) {
            this->onComponentAdded(id);
        };
        ComponentCollection::ChangeCallback onRemoved;
        if (isRequired) {
            onRemoved = [this] (EntityId id, Component&) {
                this->onRequiredComponentRemoved(id);
            };
        }
        else {
            onRemoved = [this] (EntityId id, Component&) {
                this->onOptionalComponentRemoved<tupleIndex>(id);
            };
        }
        unsigned int id = collection.registerChangeCallbacks(
            onAdded,
            onRemoved
        );
        m_registeredCallbacks.push_front(
            std::make_pair(std::ref(collection), id)
        );
        detail::RegisterNextCallback<tupleIndex>::registerNextCallback(*this);
    }

    void
    unregisterCallbacks() {
        for(auto& pair : m_registeredCallbacks) {
            pair.first.get().unregisterChangeCallbacks(pair.second);
        }
        m_registeredCallbacks.clear();
    }

    EntityMap m_addedEntities;

    EntityMap m_entities;

    EntityManager* m_entityManager = nullptr;

    bool m_recordChanges;

    std::forward_list<std::pair<
        std::reference_wrapper<ComponentCollection>, 
        unsigned int
    >> m_registeredCallbacks;

    std::unordered_set<EntityId> m_removedEntities;

};

template<typename... ComponentTypes>
EntityFilter<ComponentTypes...>::EntityFilter(
    bool recordChanges
) : m_impl(new Implementation(recordChanges))
{
}


template<typename... ComponentTypes>
typename EntityFilter<ComponentTypes...>::EntityMap&
EntityFilter<ComponentTypes...>::addedEntities() {
    assert(m_impl->m_recordChanges && "Added entities are not recorded by this filter");
    return m_impl->m_addedEntities;
}


template<typename... ComponentTypes>
typename EntityFilter<ComponentTypes...>::EntityMap::const_iterator
EntityFilter<ComponentTypes...>::begin() const {
    return m_impl->m_entities.cbegin();
}


template<typename... ComponentTypes>
void
EntityFilter<ComponentTypes...>::clearChanges() {
    m_impl->m_addedEntities.clear();
    m_impl->m_removedEntities.clear();
}


template<typename... ComponentTypes>
bool
EntityFilter<ComponentTypes...>::containsEntity(
    EntityId id
) const {
    return m_impl->m_entities.find(id) != m_impl->m_entities.end();
}


template<typename... ComponentTypes>
typename EntityFilter<ComponentTypes...>::EntityMap::const_iterator
EntityFilter<ComponentTypes...>::end() const {
    return m_impl->m_entities.cend();
}


template<typename... ComponentTypes>
const typename EntityFilter<ComponentTypes...>::EntityMap&
EntityFilter<ComponentTypes...>::entities() const {
    return m_impl->m_entities;
}


template<typename... ComponentTypes>
std::unordered_set<EntityId>&
EntityFilter<ComponentTypes...>::removedEntities() {
    assert(m_impl->m_recordChanges && "Removed entities are not recorded by this filter");
    return m_impl->m_removedEntities;
}

template<typename... ComponentTypes>
EntityManager*
EntityFilter<ComponentTypes...>::entityManager() {
    return m_impl->m_entityManager;
}

template<typename... ComponentTypes>
void
EntityFilter<ComponentTypes...>::setEntityManager(
    EntityManager* entityManager
) {
    m_impl->unregisterCallbacks();
    m_impl->m_entities.clear();
    m_impl->m_addedEntities.clear();
    m_impl->m_removedEntities.clear();
    m_impl->m_entityManager = entityManager;
    if (entityManager) {
        detail::RegisterNextCallback<sizeof...(ComponentTypes)>::registerNextCallback(*m_impl);
        m_impl->initEntities();
    }
}

} // namespace thrive
