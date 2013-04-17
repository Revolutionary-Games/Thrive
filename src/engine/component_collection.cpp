#include "engine/component_collection.h"

#include "util/contains.h"

#include <boost/thread/locks.hpp>
#include <boost/thread/recursive_mutex.hpp>
#include <unordered_set>

using namespace thrive;

using ComponentPtr = std::unique_ptr<Component>;

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
            Entity::Id entityId = iter->first;
            // Check if we are overwriting any old components
            auto foundIter = m_components.find(entityId);
            if (foundIter != m_components.end()) {
                m_collection.sig_componentRemoved(
                    entityId, 
                    *foundIter->second
                );
            }
            // Insert new component
            Component& componentRef = *iter->second;
            m_components[entityId] = std::move(iter->second);
            iter = m_componentsToAdd.erase(iter);
            m_collection.sig_componentAdded(entityId, componentRef);
        }
    }

    void
    removeQueuedComponents() {
        auto iter = m_components.begin();
        while (iter != m_components.end()) {
            Entity::Id entityId = iter->first;
            if (contains(m_componentsToRemove, entityId)) {
                m_collection.sig_componentRemoved(entityId, *iter->second);
                iter = m_components.erase(iter);
            }
            else {
                ++iter;
            }
        }
        m_componentsToRemove.clear();
    }

    ComponentCollection& m_collection;

    std::unordered_map<Entity::Id, ComponentPtr> m_components;

    std::unordered_map<Entity::Id, ComponentPtr> m_componentsToAdd;

    std::unordered_set<Entity::Id> m_componentsToRemove;

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
    Entity::Id entityId
) const {
    return this->get(entityId);
}


Component*
ComponentCollection::get(
    Entity::Id entityId
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
    Entity::Id entityId,
    ComponentPtr component
) {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->m_componentsToAdd[entityId] = std::move(component);
}


void
ComponentCollection::queueComponentRemoval(
    Entity::Id entityId
) {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->m_componentsToRemove.insert(entityId);
}


Component::TypeId
ComponentCollection::type() const {
    return m_impl->m_type;
}

