#include "engine/component_collection.h"

#include "util/contains.h"

#include <iostream>
#include <unordered_set>
#include <stdexcept>
#include <assert.h>

using namespace thrive;

struct ComponentCollection::Implementation {

    Implementation(
        ComponentTypeId type
    ) : m_type(type)
    {
    }

    std::unordered_map<
        unsigned int,
        std::pair<ChangeCallback, ChangeCallback>
    > m_changeCallbacks;

    std::unordered_map<EntityId, std::unique_ptr<Component>> m_components;

    unsigned int m_nextChangeCallbackId = 0;

    ComponentTypeId m_type = NULL_COMPONENT_TYPE;

};


ComponentCollection::ComponentCollection(
    ComponentTypeId type
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
    std::unique_ptr<Component> component
) {
    bool isNew = true;
    // Check if we are overwriting an old component
    auto iter = m_impl->m_components.find(entityId);
    if (iter != m_impl->m_components.end()) {
        assert(iter->second.get() != component.get() && "Replacing component with the same component. Did you add the same component twice?");
        m_impl->m_components.erase(entityId);
        isNew = false;
        for (auto& value : m_impl->m_changeCallbacks) {
            value.second.second(entityId, *component);
        }
    }
    // Insert new component
    Component* rawComponent = component.get();
    m_impl->m_components.insert(std::make_pair(
        entityId,
        std::move(component)
    ));
    for (auto& value : m_impl->m_changeCallbacks) {
        value.second.first(entityId, *rawComponent);
    }
    rawComponent->setOwner(entityId);
    return isNew;
}


void
ComponentCollection::clear() {
    auto iter = m_impl->m_components.begin();
    while (iter != m_impl->m_components.end()) {
        EntityId entityId = iter->first;
        std::unique_ptr<Component>& component = iter->second;
        for (auto& value : m_impl->m_changeCallbacks) {
            value.second.second(entityId, *component);
        }
        component->setOwner(NULL_ENTITY);
        iter = m_impl->m_components.erase(iter);
    }
}


const std::unordered_map<EntityId, std::unique_ptr<Component>>&
ComponentCollection::components() const {
    return m_impl->m_components;
}


bool
ComponentCollection::empty() const {
    return m_impl->m_components.empty();
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
        iter->second->setOwner(NULL_ENTITY);
        m_impl->m_components.erase(iter);
        return true;
    }
    return false;
}


ComponentTypeId
ComponentCollection::type() const {
    return m_impl->m_type;
}


void
ComponentCollection::unregisterChangeCallbacks(
    unsigned int id
) {
    m_impl->m_changeCallbacks.erase(id);
}

