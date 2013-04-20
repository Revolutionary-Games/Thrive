#pragma once

#include "engine/engine.h"

#include <memory>

namespace Ogre {
    class RenderWindow;
    class Root;
    class SceneManager;
}

namespace OIS {
    class Keyboard;
    class Mouse;
}

namespace thrive {

class OgreEngine : public Engine {

public:

    OgreEngine();

    ~OgreEngine();

    void init() override;

    OIS::Keyboard*
    keyboard() const;

    Ogre::Root*
    root() const;

    Ogre::SceneManager*
    sceneManager() const;

    void shutdown() override;

    Ogre::RenderWindow*
    window() const;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
