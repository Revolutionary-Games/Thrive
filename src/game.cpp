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

    // lua.new_usertype<Implementation::Clock::time_point>("time_point"
    // );

    lua.new_usertype<Game>("Game",

        "new", sol::no_constructor,

        "shouldQuit", sol::property([](Game &us){
                return us.m_impl->m_quit;
            }),

        "now", [](){

            return Implementation::Clock::now();
        },

        "delta", [](Implementation::Clock::time_point now,
            Implementation::Clock::time_point lastUpdate)
        {
            return now - lastUpdate;
        },

        "asMS", [](Implementation::Clock::duration duration)
        {
            return boost::chrono::duration_cast<boost::chrono::milliseconds>(duration).count();
        },

        "sleepIfNeeded", [](Game &us,
            Implementation::Clock::duration frameDuration)
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
        unsigned int fpsCount = 0;
        int fpsTime = 0;
        auto lastUpdate = Implementation::Clock::now();
        m_impl->m_engine.init();
        // Start game loop
        m_impl->m_quit = false;
        while (not m_impl->m_quit) {
            auto now = Implementation::Clock::now();
            auto delta = now - lastUpdate;
            int milliSeconds = boost::chrono::duration_cast<boost::chrono::milliseconds>(delta).count();
            lastUpdate = now;
            m_impl->m_engine.update(milliSeconds);
            auto frameDuration = Implementation::Clock::now() - now;
            auto sleepDuration = m_impl->m_targetFrameDuration - frameDuration;
            if (sleepDuration.count() > 0) {
                boost::this_thread::sleep_for(sleepDuration);
            }
            fpsCount += 1;
            fpsTime += boost::chrono::duration_cast<boost::chrono::milliseconds>(frameDuration).count();
            if (fpsTime >= 1000) {
                float fps = 1000 * float(fpsCount) / float(fpsTime);
                std::cout << "FPS: " << fps << std::endl;
                fpsCount = 0;
                fpsTime = 0;
            }
        }
        m_impl->m_engine.shutdown();
    }
    catch (const sol::error& e) {

        std::cerr << e.what() << std::endl;
    }
}


boost::chrono::microseconds
Game::targetFrameDuration() const {
    return m_impl->m_targetFrameDuration;
}


unsigned short
Game::targetFrameRate() const {
    return m_impl->m_targetFrameRate;
}


