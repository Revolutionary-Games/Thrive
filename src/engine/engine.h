#pragma once

#include "engine/game_state.h"
#include "engine/typedefs.h"

#include <memory>
#include <vector>
#include <set>

#include <luabind/object.hpp>

class btDiscreteDynamicsWorld;
class lua_State;

namespace luabind {
    class scope;
}

namespace Ogre {
    class RenderWindow;
    class Root;
    class SceneManager;
    class Viewport;
}

namespace OgreOggSound {
    class OgreOggSoundManager;
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
    * - Engine::quit()
    * - Engine::pauseGame()
    * - Engine::resumeGame()
    * - Engine::timedSystemShutdown()
    * - Engine::isSystemTimedShutdown()
    * - Engine::componentFactory() (as property)
    * - Engine::keyboard() (as property)
    * - Engine::mouse() (as property)
    * - Engine::thriveVersion()
    * - Engine::registerConsoleObject()
    *
    * @return
    */
    static luabind::scope
    luaBindings();

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
    * @brief Creates a new game state
    *
    * @param name
    *   The game state's name
    *
    * @param systems
    *   The systems active in the game state
    *
    * @param initializer
    *   The initialization function for the game state
    *
    * @return
    *   The new game state. Will never be \c null. It is returned as a pointer
    *   as a convenience for Lua bindings, which don't handle references well.
    */
    GameState*
    createGameState(
        std::string name,
        std::vector<std::unique_ptr<System>> systems,
        GameState::Initializer initializer,
        std::string guiLayoutName
    );

    /**
    * @brief Returns the currently active game state
    *
    * If no game state has been set yet, returns \c nullptr
    *
    */
    GameState*
    currentGameState() const;

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
    * @brief Retrieves a game state
    *
    * @param name
    *   The game state's name
    *
    * @return
    *   The game state with \a name or \c nullptr if no game state with
    *   this name exists.
    */
    GameState*
    getGameState(
        const std::string& name
    ) const;

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
    * @brief Sets the current game state
    *
    * The game state will be activated at the beginning of the next frame.
    *
    * \a gameState must not be \c null. It's passed by pointer as a
    * convenience for the Lua bindings (which can't handle references well).
    *
    * @param gameState
    *   The new game state
    */
    void
    setCurrentGameState(
        GameState* gameState
    );

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

    OgreOggSound::OgreOggSoundManager*
    soundManager() const;

    /**
    * @brief Renders a single frame
    *
    * Before calling update() the first time, you need to call Engine::init().
    *
    * @param milliseconds
    *   The number of milliseconds to advance. For real-time, this is the
    *   number of milliseconds since the last frame.
    */
    void
    update(
        int milliseconds
    );

    /**
    * @brief Keeps a system alive after being shut down for a specified amount of  time
    *
    * Note that this causes update to be called for the specified duration so be careful
    * to ensure that the system is not enabled or it will get update calls twice.
    *
    * @param system
    *   The system to keep updated
    *
    * @param milliseconds
    *   The number of milliseconds to keep the system updated for
    */
    void
    timedSystemShutdown(
        System& system,
        int milliseconds
    );

    /**
    * @brief Returns whether the specified system has already been set for a timed shutdown
    *
    * @param system
    *   The system to check for
    *
    * @return
    */
    bool
    isSystemTimedShutdown(
        System& system
    ) const;

    /**
    * @brief Transfers an entity from one gamestate to another
    *
    * @param oldEntityId
    *  The id of the entity to transfer in the old entitymanager
    *
    * @param oldEntityManager
    *  The old entitymanager which is currently handling the entity
    *
    * @param newGameState
    *  The new gamestate to transfer the entity to
    */
    EntityId
    transferEntityGameState(
        EntityId oldEntityId,
        EntityManager* oldEntityManager,
        GameState* newGameState
    );

    /**
    * @brief The render window
    */
    Ogre::RenderWindow*
    renderWindow() const;

    /**
    * @brief Registers the console object
    */
    void
    registerConsoleObject(luabind::object consoleObject);


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
