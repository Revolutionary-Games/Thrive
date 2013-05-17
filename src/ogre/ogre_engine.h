#pragma once

#include "engine/engine.h"

#include <memory>

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

class KeyboardSystem;

/**
* @brief Graphics engine
*/
class OgreEngine : public Engine {

public:

    /**
    * @brief Constructor
    */
    OgreEngine();

    /**
    * @brief Destructor
    */
    ~OgreEngine();

    /**
    * @brief Initializes the engine
    *
    * 1. Loads the resources
    * 2. Loads the configuration
    * 3. Creates a render window
    * 4. Sets up basic lighting
    * 5. Sets up user input
    * 6. Creates essential systems
    *
    * @param entityManager
    *   The entity manager to use
    */
    void init(
        EntityManager* entityManager
    ) override;

    /**
    * @brief The engine's input manager
    */
    OIS::InputManager*
    inputManager() const;

    /**
    * @brief The keyboard system
    */
    std::shared_ptr<KeyboardSystem>
    keyboardSystem() const;

    /**
    * @brief The OGRE root object
    */
    Ogre::Root*
    root() const;

    /**
    * @brief The scene manager
    */
    Ogre::SceneManager*
    sceneManager() const;

    /**
    * @brief Shuts the engine down
    */
    void 
    shutdown() override;

    /**
    * @brief Renders a frame
    */
    void
    update() override;

    // Test code for scriptable cameras
    // TODO: Remove this when scriptable viewports are available (#24)
    Ogre::Viewport*
    viewport() const;

    /**
    * @brief The render window
    */
    Ogre::RenderWindow*
    window() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
