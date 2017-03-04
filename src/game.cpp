#include "game.h"

#include "engine/engine.h"
#include "engine/typedefs.h"
#include "util/make_unique.h"

#include "scripting/luajit.h"

#include <boost/thread.hpp>
#include <type_traits>
#include <unordered_map>


using namespace thrive;

struct Game::Implementation {

    using Clock = boost::chrono::high_resolution_clock;

    Implementation()
    {
        m_targetFrameDuration = boost::chrono::microseconds(1000000 / m_targetFrameRate);
    }

    Engine m_engine;

    boost::chrono::microseconds m_targetFrameDuration;

    unsigned short m_targetFrameRate = 60;

    bool m_quit = false;

};

void Game::luaBindings(sol::state &lua){

    // These probably don't need to be exposed
    // lua.new_usertype<Implementation::Clock::time_point>("boost.time_point"
    // );

    // lua.new_usertype<Implementation::Clock::duration>("boost.duration"
    // );

    

    lua.new_usertype<Game>("Game",

        "new", sol::no_constructor,

        "shouldQuit", sol::property([](Game &us){
                return us.m_impl->m_quit;
            }),

        // Static functions. Access with Game.func
        "now", []() -> Implementation::Clock::time_point{

            return Implementation::Clock::now();
        },

        "delta", [](const Implementation::Clock::time_point &now,
            const Implementation::Clock::time_point &lastUpdate) ->
        Implementation::Clock::duration
        {
            return now - lastUpdate;
        },

        "asMS", [](const Implementation::Clock::duration &duration) -> int32_t
        {
            return boost::chrono::duration_cast<boost::chrono::milliseconds>(duration).count();
        },

        "sleepIfNeeded", [](Game &us,
            const Implementation::Clock::duration &frameDuration)
        {
            auto sleepDuration = us.m_impl->m_targetFrameDuration - frameDuration;
            if (sleepDuration.count() > 0) {
                boost::this_thread::sleep_for(sleepDuration);
            }
        }
    );
}



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


Engine&
Game::engine() {
    return m_impl->m_engine;
}


void
Game::quit() {
    m_impl->m_quit = true;
}


void
Game::run() {
    try {
        
        m_impl->m_engine.init();
        
        // Start game loop
        m_impl->m_quit = false;
        m_impl->m_engine.enterLuaMain(this);
    }
    catch (const sol::error& e) {

        std::cerr << "Main loop/init failed with error: " <<
            e.what() << std::endl;
    }

    // Shutdown needs to be called even if init/main loop fails
    m_impl->m_engine.shutdown();
}


boost::chrono::microseconds
Game::targetFrameDuration() const {
    return m_impl->m_targetFrameDuration;
}


unsigned short
Game::targetFrameRate() const {
    return m_impl->m_targetFrameRate;
}


