#include "engine/entity_manager.h"

#include "engine/component_collection.h"
#include "engine/component_factory.h"
#include "engine/serialization.h"

#include <atomic>
#include <boost/thread.hpp>
#include <unordered_map>
#include <unordered_set>

#include <iostream>


using namespace thrive;

struct EntityManager::Implementation {

    ComponentCollection&
    getComponentCollection(
        ComponentTypeId typeId
    ) {
        std::unique_ptr<ComponentCollection>& collection = m_collections[typeId];
        if (not collection) {
            collection.reset(new ComponentCollection(typeId));
        }
        return *collection;
    }

    std::unordered_map<
        ComponentTypeId, 
        std::unique_ptr<ComponentCollection>
    > m_collections;

    std::list<std::pair<EntityId, ComponentTypeId>> m_componentsToRemove;

    EntityId m_currentId = NULL_ENTITY + 1;

    std::unordered_map<EntityId, uint16_t> m_entities;

    std::list<EntityId> m_entitiesToRemove;

    std::unordered_map<std::string, EntityId> m_namedIds;

};


EntityManager::EntityManager() 
  : m_impl(new Implementation())
{
}

EntityManager::~EntityManager() {
}

Component*
EntityManager::addComponent(
    EntityId entityId,
    std::unique_ptr<Component> component
) {
    assert(entityId != NULL_ENTITY);
    ComponentTypeId typeId = component->typeId();
    auto& componentCollection = m_impl->getComponentCollection(typeId);
    Component* rawComponent = component.get();
    bool isNew = componentCollection.addComponent(
        entityId, 
        std::move(component)
    );
    if (isNew) {
        m_impl->m_entities[entityId] += 1;
    }
    return rawComponent;
}


void
EntityManager::clear() {
    for (auto& pair : m_impl->m_collections) {
        pair.second->clear();
    }
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
    return m_impl->m_currentId++;
}


Component*
EntityManager::getComponent(
    EntityId entityId,
    ComponentTypeId typeId
) {
    auto& componentCollection = m_impl->getComponentCollection(typeId);
    return componentCollection[entityId];
}


ComponentCollection&
EntityManager::getComponentCollection(
    ComponentTypeId typeId
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


std::unordered_set<ComponentTypeId>
EntityManager::nonEmptyCollections() const {
    std::unordered_set<ComponentTypeId> collections;
    for (const auto& pair : m_impl->m_collections) {
        if (not pair.second->empty()) {
            collections.insert(pair.first);
        }
    }
    return collections;
}


void
EntityManager::processRemovals() {
    for (const auto& pair : m_impl->m_componentsToRemove) {
        EntityId entityId = pair.first;
        ComponentTypeId typeId = pair.second;
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
        for (const auto& pair : m_impl->m_collections) {
            pair.second->removeComponent(entityId);
        }
    }
    m_impl->m_entitiesToRemove.clear();
}


void
EntityManager::removeComponent(
    EntityId entityId,
    ComponentTypeId typeId
) {
    m_impl->m_componentsToRemove.emplace_back(entityId, typeId);
}

void
EntityManager::removeEntity(
    EntityId entityId
) {
    m_impl->m_entitiesToRemove.push_back(entityId);
}


void
EntityManager::restore(
    const StorageContainer& storage,
    const ComponentFactory& factory
) {
    this->clear();
    StorageContainer collections = storage.get<StorageContainer>("collections");
    auto typeNames = collections.keys();
    for (const std::string& typeName : typeNames) {
        StorageList componentList = collections.get<StorageList>(typeName);
        for (const StorageContainer& componentStorage : componentList) {
            auto component = factory.load(typeName, componentStorage);
            EntityId owner = component->owner();
            this->addComponent(owner, std::move(component));
        }
    }
}


StorageContainer
EntityManager::storage() const {
    StorageContainer storage;
    // Collections
    StorageContainer collections;
    for (const auto& item : m_impl->m_collections) {
        const auto& components = item.second->components();
        StorageList componentList;
        componentList.reserve(components.size());
        std::string typeName = "";
        for (const auto& pair : components) {
            if (typeName.empty()) {
                typeName = pair.second->typeName();
            }
            componentList.append(pair.second->storage());
        }
        if (not typeName.empty()) {
            collections.set(typeName, std::move(componentList));
        }
    }
    storage.set("collections", std::move(collections));
    // Components to remove
    StorageList componentsToRemove;
    componentsToRemove.reserve(m_impl->m_componentsToRemove.size());
    for (const auto& pair : m_impl->m_componentsToRemove) {
        StorageContainer pairStorage;
        pairStorage.set("entityId", pair.first);
        pairStorage.set("componentTypeId", pair.second);
        componentsToRemove.append(std::move(pairStorage));
    }
    storage.set("componentsToRemove", std::move(componentsToRemove));
    // Current Id
    storage.set("currentId", m_impl->m_currentId);
    // Entities
    StorageList entities;
    entities.reserve(m_impl->m_entities.size());
    for (const auto& item : m_impl->m_entities) {
        StorageContainer itemStorage;
        itemStorage.set("entityId", item.first);
        itemStorage.set("componentCount", item.second);
        entities.append(std::move(itemStorage));
    }
    storage.set("entities", std::move(entities));
    // Entities to remove
    StorageList entitiesToRemove;
    entitiesToRemove.reserve(m_impl->m_entitiesToRemove.size());
    for (EntityId entityId : m_impl->m_entitiesToRemove) {
        StorageContainer idStorage;
        idStorage.set("id", entityId);
        entitiesToRemove.append(std::move(idStorage));
    }
    storage.set("entitiesToRemove", std::move(entitiesToRemove));
    // Named entities
    StorageList namedIds;
    namedIds.reserve(m_impl->m_namedIds.size());
    for (const auto& item : m_impl->m_namedIds) {
        StorageContainer itemStorage;
        itemStorage.set("name", item.first);
        itemStorage.set("entityId", item.second);
        namedIds.append(std::move(itemStorage));
    }
    storage.set("namedIds", std::move(namedIds));
    return storage;
}


