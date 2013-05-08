#pragma once

#include <memory>

namespace thrive {

class EntityManager;
class OgreEngine;
class ScriptEngine;

class Game {

public:

    static Game&
    instance();

    ~Game();

    EntityManager&
    entityManager();

    OgreEngine&
    ogreEngine();

    void
    quit();

    void
    run();

    ScriptEngine&
    scriptEngine();

private:

    Game();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
