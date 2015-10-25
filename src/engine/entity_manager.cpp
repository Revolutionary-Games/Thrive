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

    std::unordered_set<EntityId> m_volatileEntities;

    std::unordered_map<EntityId, std::vector<EntityId>*> m_entityChildren;

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
    m_impl->m_componentsToRemove.clear();
    m_impl->m_entities.clear();
    m_impl->m_entitiesToRemove.clear();
    m_impl->m_namedIds.clear();
    m_impl->m_volatileEntities.clear();
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
    const std::string& name,
    bool forceNew
) {
    if (forceNew){
        m_impl->m_namedIds.erase(name);

    }
    auto iter = m_impl->m_namedIds.find(name);
    if (iter != m_impl->m_namedIds.end()) {
        if (forceNew){
            iter->second = this->generateNewId();
        }
        return iter->second;
    }
    else {
        EntityId newId = this->generateNewId();
        m_impl->m_namedIds.insert(iter, std::make_pair(name, newId));
        return newId;
    }
}


bool
EntityManager::isVolatile(
    EntityId id
) const {
    return m_impl->m_volatileEntities.count(id) > 0;
}

void
EntityManager::stealName(
    EntityId entityId,
    const std::string& name
){
    m_impl->m_namedIds[name] = entityId;
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
                auto childIter = m_impl->m_entityChildren.find(entityId);
                if (childIter != m_impl->m_entityChildren.end()){
                     m_impl->m_entitiesToRemove.push_back(childIter->first);
                }
                m_impl->m_entityChildren.erase(childIter);
            }
            else {
                assert(iter->second > 0 && "Removed component from non-existent entity");
            }
        }
    }
    m_impl->m_componentsToRemove.clear();
    for (EntityId entityId : m_impl->m_entitiesToRemove) {
        if (m_impl->m_entityChildren.find(entityId) != m_impl->m_entityChildren.end()){
            for(EntityId childId : *m_impl->m_entityChildren.at(entityId)){
                m_impl->m_entitiesToRemove.push_back(childId);
            }
            m_impl->m_entityChildren.erase(entityId);
        }
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
EntityManager::transferEntity(
    EntityId oldEntityId,
    EntityId newEntityId,
    EntityManager& newEntityManager,
    const ComponentFactory& componentFactory
){
    for (const auto& pair : m_impl->m_collections) {
        Component* component = pair.second->get(oldEntityId);
        if (component != nullptr){
            if (not component->isVolatile() and not
                m_impl->m_volatileEntities.count(oldEntityId) > 0
            ) {
                auto newComponent = componentFactory.load(componentFactory.getTypeName(pair.first), component->storage());
                newComponent->setOwner(newEntityId);

                newEntityManager.addComponent(newEntityId, std::move(newComponent));
            }
        }
        m_impl->m_entitiesToRemove.push_back(oldEntityId);
    }
}

StorageContainer
EntityManager::storeEntity(
    EntityId entityId
) const {
    StorageContainer entityStorage;
    StorageList componentList;

    for (const auto& pair : m_impl->m_collections) {
        Component* component = pair.second->get(entityId);
        if (component != nullptr){
            StorageContainer componentStorage = component->storage();
            componentStorage.set("typename", component->typeName());
            componentList.append(componentStorage);
        }
    }
    entityStorage.set("components", std::move(componentList));
    return entityStorage;
}

EntityId
EntityManager::loadEntity(
    StorageContainer storage,
    const ComponentFactory& componentFactory
) {
    EntityId entityId = this->generateNewId();
    StorageList componentList = storage.get<StorageList>("components");
    for (const StorageContainer& entry : componentList) {
        auto component = componentFactory.load(entry.get<std::string>("typename"), entry);
        this->addComponent(entityId, std::move(component));
    }
    return entityId;
}

void
EntityManager::restore(
    const StorageContainer& storage,
    const ComponentFactory& factory
) {
    this->clear();
    // Current Id
    m_impl->m_currentId = storage.get<EntityId>("currentId");
    // Named entities
    StorageList namedIds = storage.get<StorageList>("namedIds");
    for (const auto& entry : namedIds) {
        std::string name = entry.get<std::string>("name");
        EntityId id = entry.get<EntityId>("entityId");
        m_impl->m_namedIds[name] = id;
    }
    // Collections
    StorageContainer collections = storage.get<StorageContainer>("collections");
    auto typeNames = collections.keys();
    for (const std::string& typeName : typeNames) {
        StorageList componentList = collections.get<StorageList>(typeName);
        for (const StorageContainer& componentStorage : componentList) {
            auto component = factory.load(typeName, componentStorage);
            EntityId owner = component->owner();
            if (owner == NULL_ENTITY) {
                std::cerr << "Component with no entity: " << typeName << std::endl;
            }
            this->addComponent(owner, std::move(component));
        }
    }
    // Components to remove
    StorageList componentsToRemove = storage.get<StorageList>("componentsToRemove");
    for (const StorageContainer& entry : componentsToRemove) {
        EntityId entityId = entry.get<EntityId>("entityId");
        std::string typeName = entry.get<std::string>("componentTypeName");
        ComponentTypeId typeId = factory.getTypeId(typeName);
        this->removeComponent(entityId, typeId);
    }
    // Entities to remove
    StorageList entitiesToRemove = storage.get<StorageList>("entitiesToRemove");
    for (const auto& entry : entitiesToRemove) {
        EntityId entityId = entry.get<EntityId>("id");
        this->removeEntity(entityId);
    }
}

void
EntityManager::addChild(
    EntityId child,
    EntityId parent
) {
    if (m_impl->m_entityChildren.find(parent) == m_impl->m_entityChildren.end()){
        m_impl->m_entityChildren.insert(std::make_pair(parent, new std::vector<EntityId>()));
    }
    m_impl->m_entityChildren.at(parent)->push_back(child);
}

bool
EntityManager::hasChildren(
   EntityId entityId
) const {
    auto iter = m_impl->m_entityChildren.find(entityId);
    return iter != m_impl->m_entityChildren.end();
}

const std::string*
EntityManager::getNameMappingFor(
    EntityId entityId
){
    // Rough way of checking for a name mapping. But performance should not matter much here.
    for (auto& pair : m_impl->m_namedIds){
        if (pair.second == entityId){
            return &pair.first;
        }
    }
    return nullptr;
}

void
EntityManager::setVolatile(
    EntityId id,
    bool isVolatile
) {
    if (isVolatile) {
        m_impl->m_volatileEntities.insert(id);
    }
    else {
        m_impl->m_volatileEntities.erase(id);
    }
}

StorageContainer
EntityManager::storage(
    const ComponentFactory& factory
) const {
    StorageContainer storage;
    // Current Id
    storage.set("currentId", m_impl->m_currentId);
    // Collections
    StorageContainer collections;
    for (const auto& item : m_impl->m_collections) {
        const auto& components = item.second->components();
        StorageList componentList;
        componentList.reserve(components.size());
        for (const auto& pair : components) {
            EntityId entityId = pair.first;
            const std::unique_ptr<Component>& component = pair.second;
            if (component->isVolatile() or
                m_impl->m_volatileEntities.count(entityId) > 0
            ) {
                continue;
            }
            componentList.append(component->storage());
        }
        if (not componentList.empty()) {
            std::string typeName = factory.getTypeName(item.first);
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
        std::string typeName = factory.getTypeName(pair.second);
        pairStorage.set("componentTypeName", typeName);
        componentsToRemove.append(std::move(pairStorage));
    }
    storage.set("componentsToRemove", std::move(componentsToRemove));
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


