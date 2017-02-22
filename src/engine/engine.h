#pragma once

#include "engine/game_state.h"
#include "engine/typedefs.h"

#include <memory>
#include <vector>
#include <set>

class btDiscreteDynamicsWorld;
class lua_State;

namespace sol {
class state;
}

namespace Ogre {
    class RenderWindow;
    class Root;
    class SceneManager;
    class Viewport;
}

namespace thrive{
    class SoundManager;
    class Game;
}

namespace OIS {
    class InputManager;
}

namespace thrive {

class ComponentFactory;
class EntityManager;
class Entity;
class PlayerData;
class Keyboard;
class Mouse;
class OgreViewportSystem;
class CollisionSystem;
class System;
class RNG;


/**
* @brief The heart of the game
*
* The engine keeps an ordered list of System objects and updates them each
* frame. It handles initialization and shutdown of graphics, physics, scripts
* and more.
*/
class Engine {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - Engine::createGameState()
    * - Engine::currentGameState()
    * - Engine::getGameState()
    * - Engine::setCurrentGameState()
    * - Engine::playerData()
    * - Engine::load()
    * - Engine::fileExists()
    * - Engine::save()
    * - Engine::saveCreation()
    * - Engine::loadCreation()
    * - Engine::screenShot()
    * - Engine::quit()
    * - Engine::pauseGame()
    * - Engine::resumeGame()
    * - Engine::componentFactory() (as property)
    * - Engine::keyboard() (as property)
    * - Engine::mouse() (as property)
    * - Engine::thriveVersion()
    * - Engine::registerConsoleObject()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    Engine();

    /**
    * @brief Non-copyable
    *
    */
    Engine(const Engine& other) = delete;

    /**
    * @brief Destructor
    */
    ~Engine();

    /**
    * @brief Returns the internal component factory
    *
    * @return
    */
    ComponentFactory&
    componentFactory();


    /**
    * @brief Object holding generic player data
    */
    PlayerData&
    playerData();

    /**
    * @brief The engine's RNG
    *
    */
    RNG&
    rng();


    /**
    * @brief Initializes the engine
    *
    * This sets up basic data structures for the different engine parts
    * (input, graphics, physics, etc.) and then calls System::init() on
    * all systems.
    */
    void
    init();

    /**
    * @brief Enters the main loop in lua
    */
    void
    enterLuaMain(
        Game* gameObj
    );
        

    /**
    * @brief The engine's input manager
    */
    OIS::InputManager*
    inputManager() const;

    /**
    * @brief Returns the keyboard interface
    *
    */
    const Keyboard&
    keyboard() const;

    /**
    * @brief Loads a savegame
    *
    * @param filename
    *   The file to load
    */
    void
    load(
        std::string filename
    );
    /**
    * @brief checks if a file exists
    *
    * @param filename
    *   The file to check for
    */
    bool
    fileExists(
        std::string filePath
    );

    /**
    * @brief The script engine's Lua state
    */
    lua_State*
    luaState();

    /**
    * @brief Returns the mouse interface
    *
    */
    const Mouse&
    mouse() const;

    /**
    * @brief The Ogre root object
    */
    Ogre::Root*
    ogreRoot() const;

    /**
    * @brief Creates a savegame
    *
    * @param filename
    *   The file to save
    */
    void
    save(
        std::string filename
    );

    /**
    * @brief Saves a creation to file
    *
    * @param entityId
    *   The entity which represents the creation to save
    *
    * @param entityManager
    *   The entity manager that manages the entity
    *
    * @param name
    *   The name of the file
    *
    * @param type
    *   The type of creation. This also becomes the file extension.
    */
    void
    saveCreation(
        EntityId entityId,
        const EntityManager& entityManager,
        std::string name,
        std::string type
    ) const;

    /**
    * @brief Overload of above
    */
    void
    saveCreation(
        EntityId entityId,
        std::string name,
        std::string type
    ) const;

    /**
    * @brief Loads a creation from file
    *
    * @param file
    *   The file to load from
    *
    * @param entityManager
    *   The entity manager that will manage the loaded entity
    *
    * @return entityId
    */
    EntityId
    loadCreation(
        std::string file,
        EntityManager& entityManager
    );

    /**
    * @brief Overload of above
    */
    EntityId
    loadCreation(
        std::string file
    );

    /**
    * @brief Takes a screenshot
    *
    * @param path
    *   The path and filename relative to exe
    */
    void
    screenShot(
       std::string path
    );

    /**
    * @brief Obtains a list of filenames for saved creations that match the provided type
    *
    * @param stage
    *   The game stage to filter creations on
    *
    * @return
    *  A string of concatenated paths separated by spaces.
    *  Optimally a vector of strings would be returned but luabind causes occasional crashes with that.
    */
    std::string
    getCreationFileList(
        std::string stage
    ) const;


    /**
    * @brief Shuts the engine down
    *
    * This calls System::shutdown() on all systems and then destroys the data
    * structures created in Engine::init().
    */
    void
    shutdown();

    /**
    * @brief Request the game to close
    */
    void
    quit();

    /**
    * @brief Pauses system updates of the game
    */
    void
    pauseGame();

    /**
    * @brief Resumes system updates of the game
    */
    void
    resumeGame();

    SoundManager*
    soundManager() const;

    /**
    * @brief Updates C++ side things. Called from lua
    */
    void
    update(
        int milliseconds
    );

    /**
    * @brief Returns the width of the games window
    *
    * @return
    */
    int
    getResolutionWidth() const;

    /**
    * @brief Returns the width of the games window
    *
    * @return
    */
    int
    getResolutionHeight() const;


    /**
    * @brief Calls the LuaEngine transfer entity method
    */
    EntityId
    transferEntityGameState(
        EntityId id,
        EntityManager* entityManager,
        GameStateData* targetState
    );

    /**
    * @brief The render window
    */
    Ogre::RenderWindow*
    renderWindow() const;

    /**
    * @brief Gets the current version of thrive as a string.
    *
    * The version is loaded from thriveversion.ver file.
    * It returns as "unknown" if that file was not found
    *
    * @return versionString
    */
    const std::string&
    thriveVersion() const;

private:



    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
