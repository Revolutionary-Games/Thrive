#include "engine/entity_manager.h"

#include "engine/engine.h"

#include <unordered_map>
#include <unordered_set>

#include <iostream>


using namespace thrive;

struct EntityManager::Implementation {

    using ComponentMap = std::unordered_map<Component::TypeId, std::shared_ptr<Component>>;

    std::unordered_map<Entity::Id, ComponentMap> m_components;

    std::unordered_set<Engine*> m_engines;

};

EntityManager&
EntityManager::instance() {
    static EntityManager instance;
    return instance;
}


EntityManager::EntityManager() 
  : m_impl(new Implementation())
{
}

EntityManager::~EntityManager() {}

void
EntityManager::addComponent(
    Entity::Id entityId,
    std::shared_ptr<Component> component
) {
    Component::TypeId typeId = component->typeId();
    auto& componentMap = m_impl->m_components[entityId];
    componentMap[typeId] = component;
    for(Engine* engine : m_impl->m_engines) {
        engine->addComponent(entityId, component);
    }
}


void
EntityManager::registerEngine(
    Engine* engine
) {
    m_impl->m_engines.insert(engine);
}


void
EntityManager::removeComponent(
    Entity::Id entityId,
    Component::TypeId typeId
) {
    auto& componentMap = m_impl->m_components[entityId];
    if (componentMap.erase(typeId) == 1) {
        for (Engine* engine : m_impl->m_engines) {
            engine->removeComponent(entityId, typeId);
        }
        if (componentMap.empty()) {
            m_impl->m_components.erase(entityId);
        }
    }
}

void
EntityManager::removeEntity(
    Entity::Id entityId
) {
    auto& componentMap = m_impl->m_components[entityId];
    for (auto iter = componentMap.begin(); iter != componentMap.end(); ++iter) {
        for (Engine* engine : m_impl->m_engines) {
            engine->removeComponent(entityId, iter->first);
        }
    }
    m_impl->m_components.erase(entityId);
}


void
EntityManager::unregisterEngine(
    Engine* engine
) {
    m_impl->m_engines.erase(engine);
}
