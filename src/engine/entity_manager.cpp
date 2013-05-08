#include "engine/entity_manager.h"

#include "engine/engine.h"

#include <atomic>
#include <boost/thread.hpp>
#include <unordered_map>
#include <unordered_set>

#include <iostream>


using namespace thrive;

const EntityId EntityManager::NULL_ID = 0;

struct EntityManager::Implementation {

    using ComponentMap = std::unordered_map<Component::TypeId, std::shared_ptr<Component>>;

    static std::atomic<EntityId> currentId;

    std::unordered_map<EntityId, ComponentMap> m_entities;

    std::unordered_set<Engine*> m_engines;

    std::unordered_map<std::string, EntityId> m_namedIds;

    boost::mutex m_mutex;

};

std::atomic<EntityId> EntityManager::Implementation::currentId(EntityManager::NULL_ID + 1);

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
    boost::lock_guard<boost::mutex> lock(m_impl->m_mutex);
    assert(entityId != NULL_ID);
    Component::TypeId typeId = component->typeId();
    auto& componentMap = m_impl->m_entities[entityId];
    componentMap[typeId] = component;
    for(Engine* engine : m_impl->m_engines) {
        engine->addComponent(entityId, component);
    }
}


void
EntityManager::clear() {
    m_impl->m_entities.clear();
}


bool
EntityManager::exists(
    EntityId id
) const {
    return m_impl->m_entities.find(id) != m_impl->m_entities.cend();
}


EntityId
EntityManager::generateNewId() {
    return Implementation::currentId.fetch_add(1);
}


Component*
EntityManager::getComponent(
    EntityId entityId,
    Component::TypeId typeId
) {
    boost::lock_guard<boost::mutex> lock(m_impl->m_mutex);
    auto entityIter = m_impl->m_entities.find(entityId);
    if (entityIter == m_impl->m_entities.cend()) {
        return nullptr;
    }
    auto componentIter = entityIter->second.find(typeId);
    if (componentIter == entityIter->second.cend()) {
        return nullptr;
    }
    return componentIter->second.get();
}


EntityId
EntityManager::getNamedId(
    const std::string& name
) {
    boost::lock_guard<boost::mutex> lock(m_impl->m_mutex);
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
EntityManager::registerEngine(
    Engine* engine
) {
    boost::lock_guard<boost::mutex> lock(m_impl->m_mutex);
    m_impl->m_engines.insert(engine);
    for (auto& entity : m_impl->m_entities) {
        EntityId entityId = entity.first;
        for (auto& component : entity.second) {
            engine->addComponent(entityId, component.second);
        }
    }
}


void
EntityManager::removeComponent(
    EntityId entityId,
    Component::TypeId typeId
) {
    boost::lock_guard<boost::mutex> lock(m_impl->m_mutex);
    auto& componentMap = m_impl->m_entities[entityId];
    if (componentMap.erase(typeId) == 1) {
        for (Engine* engine : m_impl->m_engines) {
            engine->removeComponent(entityId, typeId);
        }
        if (componentMap.empty()) {
            m_impl->m_entities.erase(entityId);
        }
    }
}

void
EntityManager::removeEntity(
    EntityId entityId
) {
    auto& componentMap = m_impl->m_entities[entityId];
    for (auto iter = componentMap.begin(); iter != componentMap.end(); ++iter) {
        for (Engine* engine : m_impl->m_engines) {
            engine->removeComponent(entityId, iter->first);
        }
    }
    m_impl->m_entities.erase(entityId);
}


void
EntityManager::unregisterEngine(
    Engine* engine
) {
    boost::lock_guard<boost::mutex> lock(m_impl->m_mutex);
    m_impl->m_engines.erase(engine);
}
