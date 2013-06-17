#include "game.h"

#include "engine/engine.h"
#include "engine/entity_manager.h"
#include "engine/shared_data.h"
#include "engine/typedefs.h"
#include "ogre/ogre_engine.h"
#include "scripting/lua_state.h"
#include "scripting/script_engine.h"
#include "bullet/bullet_engine.h"
#include "util/make_unique.h"

#include <boost/thread.hpp>
#include <type_traits>
#include <unordered_map>

#include <AL/al.h>
#include <AL/alc.h>
#include <AL/alext.h>


using namespace thrive;

struct Game::Implementation {

    using Clock = std::chrono::high_resolution_clock;

    Implementation()
      : m_bulletEngine(m_entityManager),
        m_ogreEngine(m_entityManager),
        m_scriptEngine(m_entityManager, m_luaState)
    {
        m_targetFrameDuration = std::chrono::microseconds(1000000 / m_targetFrameRate);
    }
    // Lua state must be one of the last to be destroyed,
    // so keep it at top.
    LuaState m_luaState;

    // EntityManager is required by the engine 
    // constructors, so keep it at second place
    EntityManager m_entityManager;

    BulletEngine m_bulletEngine;

    OgreEngine m_ogreEngine;

    ScriptEngine m_scriptEngine;

    std::chrono::microseconds m_targetFrameDuration;

    unsigned short m_targetFrameRate = 60;

    bool m_quit;

};


Game&
Game::instance() {
    static Game instance;
    return instance;
}


Game::Game()
  : m_impl(new Implementation())
{
}


Game::~Game() { }


EntityManager&
Game::entityManager() {
    return m_impl->m_entityManager;
}


OgreEngine&
Game::ogreEngine() {
    return m_impl->m_ogreEngine;
}


BulletEngine&
Game::bulletEngine() {
    return m_impl->m_bulletEngine;
}


void
Game::quit() {
    m_impl->m_quit = true;
}


void
Game::run() {
    unsigned int fpsCount = 0;
    int fpsTime = 0;
    auto lastUpdate = Implementation::Clock::now();
    // Initialize engines
    m_impl->m_ogreEngine.init();
    m_impl->m_bulletEngine.init();
    m_impl->m_scriptEngine.init();
    // Start game loop
    m_impl->m_quit = false;
    while (not m_impl->m_quit) {
        auto now = Implementation::Clock::now();
        auto delta = now - lastUpdate;
        int milliSeconds = std::chrono::duration_cast<std::chrono::milliseconds>(delta).count();
        lastUpdate = now;
        m_impl->m_bulletEngine.update(milliSeconds);
        m_impl->m_scriptEngine.update(milliSeconds);
        m_impl->m_ogreEngine.update(milliSeconds);
        m_impl->m_entityManager.update();
        auto frameDuration = Implementation::Clock::now() - now;
        auto sleepDuration = m_impl->m_targetFrameDuration - frameDuration;
        if (sleepDuration.count() > 0) {
            auto microseconds = std::chrono::duration_cast<std::chrono::microseconds>(sleepDuration).count();
            boost::chrono::microseconds boostDuration = boost::chrono::microseconds(microseconds);
            boost::this_thread::sleep_for(boostDuration);
        }
        fpsCount += 1;
        fpsTime += std::chrono::duration_cast<std::chrono::milliseconds>(frameDuration).count();
        if (fpsTime >= 1000) {
            float fps = 1000 * float(fpsCount) / float(fpsTime);
            std::cout << "FPS: " << fps << std::endl;
            fpsCount = 0;
            fpsTime = 0;
        }
    }
    m_impl->m_scriptEngine.shutdown();
    m_impl->m_bulletEngine.shutdown();
    m_impl->m_ogreEngine.shutdown();
}


ScriptEngine&
Game::scriptEngine() {
    return m_impl->m_scriptEngine;
}

std::chrono::microseconds
Game::targetFrameDuration() const {
    return m_impl->m_targetFrameDuration;
}


unsigned short
Game::targetFrameRate() const {
    return m_impl->m_targetFrameRate;
}


