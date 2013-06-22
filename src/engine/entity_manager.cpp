#include "engine/entity_manager.h"

#include "engine/component_collection.h"

#include <atomic>
#include <boost/thread.hpp>
#include <unordered_map>
#include <unordered_set>

#include <iostream>


using namespace thrive;

const EntityId EntityManager::NULL_ID = 0;

struct EntityManager::Implementation {

    ComponentCollection&
    getComponentCollection(
        Component::TypeId typeId
    ) {
        std::unique_ptr<ComponentCollection>& collection = m_components[typeId];
        if (not collection) {
            collection.reset(new ComponentCollection(typeId));
        }
        return *collection;
    }

    static EntityId currentId;

    std::unordered_map<
        Component::TypeId, 
        std::unique_ptr<ComponentCollection>
    > m_components;

    std::list<std::pair<EntityId, Component::TypeId>> m_componentsToRemove;

    std::unordered_map<EntityId, int> m_entities;

    std::list<EntityId> m_entitiesToRemove;

    std::unordered_map<std::string, EntityId> m_namedIds;

};

EntityId EntityManager::Implementation::currentId = NULL_ID + 1;


EntityManager::EntityManager() 
  : m_impl(new Implementation())
{
}

EntityManager::~EntityManager() {
}

void
EntityManager::addComponent(
    EntityId entityId,
    std::shared_ptr<Component> component
) {
    assert(entityId != NULL_ID);
    Component::TypeId typeId = component->typeId();
    auto& componentCollection = m_impl->getComponentCollection(typeId);
    bool isNew = componentCollection.addComponent(entityId, component);
    if (isNew) {
        m_impl->m_entities[entityId] += 1;
    }
}


void
EntityManager::clear() {
    m_impl->m_components.clear();
}


std::unordered_set<EntityId>
EntityManager::entities() {
    std::unordered_set<EntityId> entities;
    for (const auto& pair : m_impl->m_entities) {
        entities.insert(pair.first);
    }
    return entities;
}


bool
EntityManager::exists(
    EntityId entityId
) const {
    return m_impl->m_entities.find(entityId) != m_impl->m_entities.end();
}


EntityId
EntityManager::generateNewId() {
    return Implementation::currentId++;
}


Component*
EntityManager::getComponent(
    EntityId entityId,
    Component::TypeId typeId
) {
    auto& componentCollection = m_impl->getComponentCollection(typeId);
    return componentCollection[entityId];
}


ComponentCollection&
EntityManager::getComponentCollection(
    Component::TypeId typeId
) {
    return m_impl->getComponentCollection(typeId);
}


EntityId
EntityManager::getNamedId(
    const std::string& name
) {
    auto iter = m_impl->m_namedIds.find(name);
    if (iter != m_impl->m_namedIds.end()) {
        return iter->second;
    }
    else {
        EntityId newId = this->generateNewId();
        m_impl->m_namedIds.insert(iter, std::make_pair(name, newId));
        return newId;
    }
}


void
EntityManager::processRemovals() {
    for (const auto& pair : m_impl->m_componentsToRemove) {
        EntityId entityId = pair.first;
        Component::TypeId typeId = pair.second;
        auto& componentCollection = m_impl->getComponentCollection(typeId);
        bool removed = componentCollection.removeComponent(entityId);
        if (removed) {
            auto iter = m_impl->m_entities.find(entityId);
            iter->second -= 1;
            if (iter->second == 0) {
                m_impl->m_entities.erase(iter);
            }
            else {
                assert(iter->second > 0 && "Removed component from non-existent entity");
            }
        }
    }
    m_impl->m_componentsToRemove.clear();
    for (EntityId entityId : m_impl->m_entitiesToRemove) {
        for (const auto& pair : m_impl->m_components) {
            pair.second->removeComponent(entityId);
        }
    }
    m_impl->m_entitiesToRemove.clear();
}


void
EntityManager::removeComponent(
    EntityId entityId,
    Component::TypeId typeId
) {
    m_impl->m_componentsToRemove.emplace_back(entityId, typeId);
}

void
EntityManager::removeEntity(
    EntityId entityId
) {
    m_impl->m_entitiesToRemove.push_back(entityId);
}



