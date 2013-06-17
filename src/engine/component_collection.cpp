#include "engine/component_collection.h"

#include "util/contains.h"

#include <unordered_set>

#include <iostream>

using namespace thrive;

using ComponentPtr = std::shared_ptr<Component>;

struct ComponentCollection::Implementation {

    Implementation(
        Component::TypeId type
    ) : m_type(type)
    {
    }

    std::unordered_map<
        unsigned int, 
        std::pair<ChangeCallback, ChangeCallback>
    > m_changeCallbacks;

    std::unordered_map<EntityId, ComponentPtr> m_components;

    unsigned int m_nextChangeCallbackId = 0;

    Component::TypeId m_type;

};


ComponentCollection::ComponentCollection(
    Component::TypeId type
) : m_impl(new Implementation(type))
{
}


ComponentCollection::~ComponentCollection() {}


Component*
ComponentCollection::operator[] (
    EntityId entityId
) const {
    return this->get(entityId);
}


bool
ComponentCollection::addComponent(
    EntityId entityId,
    std::shared_ptr<Component> component
) {
    bool isNew = true;
    // Check if we are overwriting an old component
    if (m_impl->m_components.erase(entityId) > 0) {
        isNew = false;
        for (auto& value : m_impl->m_changeCallbacks) {
            value.second.second(entityId, *component);
        }
    }
    // Insert new component
    m_impl->m_components.insert(std::make_pair(
        entityId, 
        component
    ));
    for (auto& value : m_impl->m_changeCallbacks) {
        value.second.first(entityId, *component);
    }
    std::cout << "Component count: " << m_impl->m_components.size() << std::endl;
    return isNew;
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


bool
ComponentCollection::removeComponent(
    EntityId entityId
) {
    auto iter = m_impl->m_components.find(entityId);
    if (iter != m_impl->m_components.end()) {
        for (auto& value : m_impl->m_changeCallbacks) {
            value.second.second(entityId, *iter->second);
        }
        m_impl->m_components.erase(iter);
        return true;
    }
    return false;
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

