#pragma once

#include <memory>

namespace thrive {

class EntityManager;
class OgreEngine;
class ScriptEngine;

/**
* @brief The main entry point for the game
*
* The Game class handles instantiation of all the necessary engines.
*/
class Game {

public:

    /**
    * @brief Singleton instance
    *
    */
    static Game&
    instance();

    /**
    * @brief Destructor
    */
    ~Game();

    /**
    * @brief Returns the game's global entity manager
    */
    EntityManager&
    entityManager();

    /**
    * @brief Returns the game's graphics engine
    */
    OgreEngine&
    ogreEngine();

    /**
    * @brief Stops all engines and quits the application
    */
    void
    quit();

    /**
    * @brief Starts all engines
    */
    void
    run();

    /**
    * @brief Returns the game's script engine
    */
    ScriptEngine&
    scriptEngine();

private:

    Game();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
