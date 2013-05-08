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
            EntityId entityId = iter->first;
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

    std::unordered_map<EntityId, ComponentPtr> m_components;

    std::unordered_map<EntityId, ComponentPtr> m_componentsToAdd;

    std::unordered_set<EntityId> m_componentsToRemove;

    mutable boost::recursive_mutex m_queueMutex;

    Component::TypeId m_type;

};


/**
* @brief Constructor
*
* @param type The type id of the components held by this collection.
*/
ComponentCollection::ComponentCollection(
    Component::TypeId type
) : m_impl(new Implementation(*this, type))
{
}


/**
* @brief Destructor
*/
ComponentCollection::~ComponentCollection() {}


/**
* @see
*   ComponentCollection::get
*/
Component*
ComponentCollection::operator[] (
    EntityId entityId
) const {
    return this->get(entityId);
}


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


/**
* @brief Processes the queues for added and removed components
*/
void
ComponentCollection::processQueue() {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->removeQueuedComponents();
    m_impl->addQueuedComponents();
}


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
ComponentCollection::queueComponentAddition(
    EntityId entityId,
    ComponentPtr component
) {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->m_componentsToAdd[entityId] = std::move(component);
}


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
ComponentCollection::queueComponentRemoval(
    EntityId entityId
) {
    boost::lock_guard<boost::recursive_mutex> lock(m_impl->m_queueMutex);
    m_impl->m_componentsToRemove.insert(entityId);
}


/**
* @brief The type id of the collection's components
*/
Component::TypeId
ComponentCollection::type() const {
    return m_impl->m_type;
}

