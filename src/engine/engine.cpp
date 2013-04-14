#include "engine/engine.h"

#include "engine/component_collection.h"
#include "engine/system.h"
#include "util/contains.h"
#include "util/pair_hash.h"

#include <chrono>
#include <forward_list>
#include <set>
#include <unordered_map>

using namespace thrive;

using ComponentPtr = std::unique_ptr<Component>;

using ComponentCollectionPtr = std::unique_ptr<ComponentCollection>;

struct SystemCompare {

    bool
    operator() (
        std::shared_ptr<System> lhs,
        std::shared_ptr<System> rhs
    ) const {
        return lhs->order() < rhs->order();
    }
};

struct Engine::Implementation {

    using Clock = std::chrono::high_resolution_clock;

    Implementation(
        Engine& engine
    ) : m_engine(engine)
    {
    }

    ComponentCollection&
    getComponentCollection(
        Component::TypeId typeId
    ) {
        ComponentCollectionPtr& collection = m_components[typeId];
        if (not collection) {
            collection.reset(new ComponentCollection(typeId));
            collection->sig_componentAdded.connect(
                [this] (Entity::Id entityId, Component&) {
                    m_entities[entityId] += 1;
                    if (m_entities[entityId] == 1) {
                        m_engine.sig_entityAdded(entityId);
                    }
                }
            );
            collection->sig_componentRemoved.connect(
                [this] (Entity::Id entityId, Component&) {
                    m_entities[entityId] -= 1;
                    if (m_entities[entityId] == 0) {
                        m_engine.sig_entityRemoved(entityId);
                    }
                    else {
                        assert(m_entities[entityId] > 0 && "Removed component from non-existent entity");
                    }
                }
            );
        }
        return *collection;
    }

    void
    processSystemsQueue() {
        // Remove systems
        while (not m_systemsToRemove.empty()) {
            std::string name = m_systemsToRemove.front();
            System::Ptr system = m_systems[name];
            system->shutdown();
            m_activeSystems.erase(system);
            m_systems.erase(name);
            m_systemsToRemove.pop_front();
        }
        // Activate systems
        while (not m_systemsToActivate.empty()) {
            std::string name = m_systemsToActivate.front();
            System::Ptr system = m_systems[name];
            m_activeSystems.insert(system);
            system->init(&m_engine);
            m_systemsToActivate.pop_front();
        }
    }

    std::unordered_map<Component::TypeId, ComponentCollectionPtr> m_components;

    Engine& m_engine;

    std::unordered_map<Entity::Id, int> m_entities;

    std::set<System::Ptr, SystemCompare> m_activeSystems;

    Clock::time_point m_lastUpdate;

    std::unordered_map<std::string, System::Ptr> m_systems;

    std::forward_list<std::string> m_systemsToActivate;

    std::forward_list<std::string> m_systemsToRemove;

};


Engine::Engine()
  : m_impl(new Implementation(*this))
{
}


Engine::~Engine() {}


void
Engine::addComponent(
    Entity::Id entityId,
    std::unique_ptr<Component> component
) {
    Component::TypeId typeId = component->typeId();
    ComponentCollection& collection = m_impl->getComponentCollection(typeId);
    collection.queueComponentAddition(
        entityId, 
        std::move(component)
    );
}


void
Engine::addSystem(
    std::string name,
    System::Ptr system
) {
    m_impl->m_systems[name] = system;
    m_impl->m_systemsToActivate.push_front(name);
}


std::unordered_set<Entity::Id>
Engine::entities() const {
    std::unordered_set<Entity::Id> entities;
    for(auto& pair : m_impl->m_entities) {
        entities.insert(pair.first);
    }
    return entities;
}


Component*
Engine::getComponent(
    Entity::Id entityId,
    Component::TypeId typeId
) const {
    ComponentCollection& collection = m_impl->getComponentCollection(typeId);
    return collection.get(entityId);
}


const ComponentCollection&
Engine::getComponentCollection(
    Component::TypeId typeId
) const {
    return m_impl->getComponentCollection(typeId);
}


System::Ptr
Engine::getSystem(
    std::string name
) const {
    auto iter = m_impl->m_systems.find(name);
    if (iter != m_impl->m_systems.end()) {
        return iter->second;
    }
    else {
        return nullptr;
    }
}


void
Engine::init() {
    m_impl->m_lastUpdate = Implementation::Clock::now();
}


void
Engine::removeComponent(
    Entity::Id entityId,
    Component::TypeId typeId
) {
    ComponentCollection& collection = m_impl->getComponentCollection(typeId);
    collection.queueComponentRemoval(entityId);
}


void
Engine::removeEntity(
    Entity::Id entityId
) {
    for (auto& pair : m_impl->m_components) {
        pair.second->queueComponentRemoval(entityId);
    }
}


void
Engine::removeSystem(
    std::string name
) {
    m_impl->m_systemsToRemove.push_front(name);
}


void
Engine::shutdown() {

}


void
Engine::update() {
    for (auto& pair : m_impl->m_components) {
        pair.second->processQueue();
    }
    m_impl->processSystemsQueue();
    auto now = Implementation::Clock::now();
    auto delta = (now - m_impl->m_lastUpdate);
    int milliSeconds = std::chrono::duration_cast<std::chrono::milliseconds>(delta).count();
    m_impl->m_lastUpdate = now;
    for(auto& system : m_impl->m_activeSystems) {
        system->update(milliSeconds);
    }
}


