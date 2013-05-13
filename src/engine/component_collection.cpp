#include "engine/component_collection.h"

#include "util/contains.h"

#include <boost/thread/locks.hpp>
#include <boost/thread/recursive_mutex.hpp>
#include <unordered_set>

using namespace thrive;

using ComponentPtr = std::shared_ptr<Component>;

struct ComponentCollection::Implementation {

    Implementation(
        ComponentCollection& collection,
        Component::TypeId type
    ) : m_collection(collection),
        m_components(),
        m_componentsToAdd(),
        m_componentsToRemove(),
        m_type(type)
    {
    }

    void
    addQueuedComponents() {
        m_components.reserve(m_components.size() + m_componentsToAdd.size());
        auto iter = m_componentsToAdd.begin();
        while (iter != m_componentsToAdd.end()) {
            EntityId entityId = iter->first;
            // Check if we are overwriting any old components
            auto foundIter = m_components.find(entityId);
            if (foundIter != m_components.end()) {
                for (auto& value : m_changeCallbacks) {
                    value.second.second(entityId, *iter->second);
                }
            }
            // Insert new component
            Component& componentRef = *iter->second; // We move the pointer, making it invalid
            m_components.insert(std::make_pair(
                entityId, 
                std::move(iter->second)
            ));
            iter = m_componentsToAdd.erase(iter);
            for (auto& value : m_changeCallbacks) {
                value.second.first(entityId, componentRef);
            }
        }
    }

    void
    removeQueuedComponents() {
        auto iter = m_components.begin();
        while (iter != m_components.end()) {
            EntityId entityId = iter->first;
            if (contains(m_componentsToRemove, entityId)) {
                for (auto& value : m_changeCallbacks) {
                    value.second.second(entityId, *iter->second);
                }
                iter = m_components.erase(iter);
            }
            else {
                ++iter;
            }
        }
        m_componentsToRemove.clear();
    }

    std::unordered_map<
        unsigned int, 
        std::pair<ChangeCallback, ChangeCallback>
    > m_changeCallbacks;

    ComponentCollection& m_collection;

    std::unordered_map<EntityId, ComponentPtr> m_components;

    std::unordered_map<EntityId, ComponentPtr> m_componentsToAdd;

    std::unordered_set<EntityId> m_componentsToRemove;

    unsigned int m_nextChangeCallbackId = 0;

    mutable boost::recursive_mutex m_queueMutex;

    Component::TypeId m_type;

};


ComponentCollection::ComponentCollection(
    Component::TypeId type
) : m_impl(new Implementation(*this, type))
{
}


ComponentCollection::~ComponentCollection() {}


Component*
ComponentCollection::operator[] (
    EntityId entityId
) const {
    return this->get(entityId);
}


Component*
ComponentCollection::get(
    EntityId entityId
) const {
    auto iter = m_impl->m_components.find(entityId);
    if (iter != m_impl->m_components.end()) {
        return iter->second.get();
    }
    else {
        return nullptr;
    }
}


void
ComponentCollection::processQueue() {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->removeQueuedComponents();
    m_impl->addQueuedComponents();
}


void
ComponentCollection::queueComponentAddition(
    EntityId entityId,
    ComponentPtr component
) {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->m_componentsToAdd[entityId] = std::move(component);
}


void
ComponentCollection::queueComponentRemoval(
    EntityId entityId
) {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->m_componentsToRemove.insert(entityId);
}


unsigned int
ComponentCollection::registerChangeCallbacks(
    ChangeCallback onComponentAdded,
    ChangeCallback onComponentRemoved
) {
    unsigned int id = m_impl->m_nextChangeCallbackId++;
    m_impl->m_changeCallbacks.insert(std::make_pair(
        id,
        std::make_pair(onComponentAdded, onComponentRemoved)
    ));
    return id;
}


Component::TypeId
ComponentCollection::type() const {
    return m_impl->m_type;
}


void
ComponentCollection::unregisterChangeCallbacks(
    unsigned int id
) {
    m_impl->m_changeCallbacks.erase(id);
}

