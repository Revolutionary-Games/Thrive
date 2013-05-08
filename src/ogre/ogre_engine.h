#pragma once

#include "engine/engine.h"

#include <memory>

namespace Ogre {
    class RenderWindow;
    class Root;
    class SceneManager;
}

namespace OIS {
    class InputManager;
}

namespace thrive {

class KeyboardSystem;

class OgreEngine : public Engine {

public:

    OgreEngine();

    ~OgreEngine();

    void init(
        EntityManager* entityManager
    ) override;

    OIS::InputManager*
    inputManager() const;

    std::shared_ptr<KeyboardSystem>
    keyboardSystem() const;

    Ogre::Root*
    root() const;

    Ogre::SceneManager*
    sceneManager() const;

    void 
    shutdown() override;

    void
    update() override;

    Ogre::RenderWindow*
    window() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
