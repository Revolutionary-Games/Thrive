#pragma once

#include <boost/chrono.hpp>
#include <memory>

namespace thrive {

class Engine;
class EntityManager;

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
    * @brief Returns the game's engine
    */
    Engine&
    engine();

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
    * @brief The target frame duration
    */
    boost::chrono::microseconds
    targetFrameDuration() const;

    /**
    * @brief The target frame rate
    */
    unsigned short
    targetFrameRate() const;

private:

    Game();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
