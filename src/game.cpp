#include "game.h"

#include "engine/engine.h"
#include "engine/entity_manager.h"
#include "engine/shared_data.h"
#include "engine/typedefs.h"
#include "ogre/ogre_engine.h"
#include "scripting/lua_state.h"
#include "scripting/script_engine.h"
#include "util/make_unique.h"

#include <boost/thread.hpp>
#include <type_traits>
#include <unordered_map>

using namespace thrive;

struct Game::Implementation {

    Implementation()
      : m_scriptEngine(m_luaState)
    {
    }
    // Lua state must be one of the last to be destroyed,
    // so keep it at top.
    LuaState m_luaState;

    std::list< std::unique_ptr<EngineRunner> > m_engineRunners;

    EntityManager m_entityManager;

    OgreEngine m_ogreEngine;

    ScriptEngine m_scriptEngine;

    bool m_quit;

    boost::condition_variable m_quitCondition;

    boost::mutex m_quitMutex;

};


Game&
Game::instance() {
    // Make sure that shared states are instantiated first
    // to avoid problems with static destruction order
    RenderState::instance();
    InputState::instance();
    static Game instance;
    return instance;
}


Game::Game()
  : m_impl(new Implementation())
{
}


Game::~Game() {
    assert(m_impl->m_engineRunners.size() == 0 && "Game still running on destruction");
}


EntityManager&
Game::entityManager() {
    return m_impl->m_entityManager;
}


OgreEngine&
Game::ogreEngine() {
    return m_impl->m_ogreEngine;
}


void
Game::quit() {
    boost::lock_guard<boost::mutex> lock(m_impl->m_quitMutex);
    m_impl->m_quit = true;
    m_impl->m_quitCondition.notify_one();
}


void
Game::run() {
    irrengine = irrklang::createIrrKlangDevice();
    if (!irrengine)
        return;
    irrengine->play2D("/../media/music/Thrive_Main.mp3",false,false,true);

    // Make sure we're not running
    assert(m_impl->m_engineRunners.size() == 0 && "Can't start Game twice");
    // Initialize engine runners
    m_impl->m_engineRunners.push_back(
        make_unique<EngineRunner>(m_impl->m_ogreEngine)
    );
    m_impl->m_engineRunners.push_back(
        make_unique<EngineRunner>(m_impl->m_scriptEngine)
    );
    // Start runners
    boost::unique_lock<boost::mutex> lock(m_impl->m_quitMutex);
    m_impl->m_quit = false;
    for (auto& runner : m_impl->m_engineRunners) {
        runner->start(&m_impl->m_entityManager);
    }
    // Wait for quit
    while (not m_impl->m_quit) {
        m_impl->m_quitCondition.wait(lock);
    }
    // Stop all runners
    for (auto& runner : m_impl->m_engineRunners) {
        runner->stop();
    }
    m_impl->m_engineRunners.clear();
}


ScriptEngine&
Game::scriptEngine() {
    return m_impl->m_scriptEngine;
}


