#include "scripting/script_engine.h"

#include "bullet/bullet_engine.h"
#include "bullet/debug_drawing.h"
#include "common/bullet_to_ogre_system.h"
#include "engine/shared_data.h"
#include "game.h"
#include "ogre/ogre_engine.h"
#include "ogre/on_key.h"
#include "ogre/keyboard_system.h"
#include "scripting/on_update.h"
#include "scripting/script_initializer.h"

#include <boost/algorithm/string.hpp>
#include <boost/filesystem.hpp>
#include <fstream>
#include <iostream>

using namespace thrive;

struct ScriptEngine::Implementation {

    Implementation(
        lua_State* luaState
    ) : m_luaState(luaState)
    {
    }

    void
    loadScripts(
        const boost::filesystem::path& directory
    ) {
        namespace fs = boost::filesystem;
        fs::path manifestPath = directory / "manifest.txt";
        if (not fs::exists(manifestPath)) {
            return;
        }
        std::ifstream manifest(manifestPath.string());
        if (not manifest.is_open()) {
            throw std::runtime_error("Could not open manifest file: " + manifestPath.string());
        }
        std::string line;
        while(not manifest.eof()) {
            std::getline(manifest, line);
            boost::algorithm::trim(line);
            if (line.empty() or line.find("//") == 0) {
                continue;
            }
            fs::path manifestEntryPath = directory / line;
            if (not fs::exists(manifestEntryPath)) {
                std::cerr << "Warning: Could not find file " << manifestEntryPath.string() << std::endl;
                continue;
            }
            else if (fs::is_directory(manifestEntryPath)) {
                this->loadScripts(manifestEntryPath);
            }
            else if(luaL_dofile(m_luaState, manifestEntryPath.string().c_str())) {
                const char* errorMessage = lua_tostring(m_luaState, -1);
                std::cerr << "Error while parsing " << manifestEntryPath.string() << ": " << errorMessage << std::endl;
            }
        }
    }

    bool
    quitRequested() {
        OgreEngine& ogreEngine = Game::instance().ogreEngine();
        if (not ogreEngine.keyboardSystem()) {
            return false;
        }
        return ogreEngine.keyboardSystem()->isKeyDown(
            OIS::KeyCode::KC_ESCAPE
        );
    }

    lua_State* m_luaState;
};


ScriptEngine::ScriptEngine(
    EntityManager& entityManager,
    lua_State* luaState
) : Engine(entityManager),
    m_impl(new Implementation(luaState))
{
}

ScriptEngine::~ScriptEngine() {}


void
ScriptEngine::init() {
    initializeLua(m_impl->m_luaState);
    this->addSystem(
        std::make_shared<OnUpdateSystem>()
    );
    this->addSystem(
        std::make_shared<OnKeySystem>()
    );
    m_impl->loadScripts("../scripts/");
    Engine::init();
}


lua_State*
ScriptEngine::luaState() {
    return m_impl->m_luaState;
}


void
ScriptEngine::shutdown() {
    Engine::shutdown();
}


void
ScriptEngine::update(
    int milliseconds
) {
    if (m_impl->quitRequested()) {
        Game::instance().quit();
        return;
    }
    Engine::update(milliseconds);
}
