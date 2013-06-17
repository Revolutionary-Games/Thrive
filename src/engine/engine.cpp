#include "engine/engine.h"

#include "engine/component_collection.h"
#include "engine/entity_manager.h"
#include "engine/system.h"
#include "game.h"
#include "scripting/luabind.h"
#include "util/contains.h"
#include "util/pair_hash.h"

#include <boost/thread.hpp>
#include <chrono>
#include <forward_list>
#include <set>
#include <unordered_map>

#include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// Engine
////////////////////////////////////////////////////////////////////////////////

struct Engine::Implementation {

    Implementation(
        Engine& engine,
        EntityManager& entityManager
    ) : m_engine(engine),
        m_entityManager(entityManager)
    {
    }

    Engine& m_engine;

    EntityManager& m_entityManager;

    std::list<std::shared_ptr<System>> m_systems;

};




Engine::Engine(
    EntityManager& entityManager
) 
  : m_impl(new Implementation(*this, entityManager))
{
}


Engine::~Engine() { }


void
Engine::addSystem(
    std::shared_ptr<System> system
) {
    m_impl->m_systems.push_back(system);
}


EntityManager&
Engine::entityManager() {
    return m_impl->m_entityManager;
}


void
Engine::init() {
    for (auto& system : m_impl->m_systems) {
        system->init(this);
    }
}


void
Engine::shutdown() {
    for (auto& system : m_impl->m_systems) {
        system->shutdown();
    }
}


void
Engine::update(
    int milliSeconds
) {
    for(auto& system : m_impl->m_systems) {
        system->update(milliSeconds);
    }
}

