#pragma once

#include "engine/typedefs.h"

#include <memory>

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

namespace OIS {
    class InputManager;
}

namespace thrive {

class ComponentFactory;
class EntityManager;
class KeyboardSystem;
class MouseSystem;
class OgreViewportSystem;
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
    * - Engine::addScriptSystem()
    * - Engine::load()
    * - Engine::save()
    * - Engine::setPhysicsDebugDrawingEnabled()
    * - Engine::componentFactory() (as property)
    * - Engine::keyboard() (as property)
    * - Engine::mouse() (as property)
    * - Engine::sceneManager() (as property)
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
    * @brief Adds a system to the ScriptSystemUpdater
    *
    * @param system
    *   The system to add
    */
    void
    addScriptSystem(
        std::shared_ptr<System> system
    );

    /**
    * @brief Returns the internal component factory
    *
    * @return
    */
    ComponentFactory&
    componentFactory();

    /**
    * @brief The engine's entity manager
    *
    */
    EntityManager&
    entityManager();


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
    * @brief The engine's input manager
    */
    OIS::InputManager*
    inputManager() const;

    /**
    * @brief The keyboard system
    */
    KeyboardSystem&
    keyboardSystem() const;

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
    * @brief The script engine's Lua state
    */
    lua_State*
    luaState();

    /**
    * @brief The mouse system
    */
    MouseSystem&
    mouseSystem() const;

    /**
    * @brief The Ogre root object
    */
    Ogre::Root*
    ogreRoot() const;

    /**
    * @brief The physics world
    */
    btDiscreteDynamicsWorld*
    physicsWorld() const;

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
    * @brief The Ogre scene manager
    */
    Ogre::SceneManager*
    sceneManager() const;

    /**
    * @brief Enables or disables physics debug drawing
    *
    * @param enabled
    */
    void
    setPhysicsDebugDrawingEnabled(
        bool enabled
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
    * @brief The viewport system
    */
    OgreViewportSystem&
    viewportSystem();

    /**
    * @brief The render window
    */
    Ogre::RenderWindow*
    renderWindow() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
