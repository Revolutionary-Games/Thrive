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

class EntityManager;
class KeyboardSystem;
class OgreViewportSystem;
class System;

class Engine {

public:

    /**
    * @brief Constructor
    */
    Engine(
        EntityManager& entityManager,
        lua_State* L
    );

    /**
    * @brief Non-copyable
    *
    */
    Engine(const Engine& other) = delete;

    ~Engine();

    EntityManager&
    entityManager();

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
    * @brief The script engine's Lua state
    */
    lua_State*
    luaState();

    /**
    * @brief The physics world
    */
    btDiscreteDynamicsWorld*
    physicsWorld() const;

    /**
    * @brief The Ogre root object
    */
    Ogre::Root*
    ogreRoot() const;

    /**
    * @brief The Ogre scene manager
    */
    Ogre::SceneManager*
    sceneManager() const;

    void 
    shutdown();

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
