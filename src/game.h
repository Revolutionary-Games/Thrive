#pragma once

#include <chrono>
#include <memory>

/**
 * @mainpage Thrive API documentation
 *
 * This is the API documentation for the Thrive source code, targetted
 * at new developers (both C++ and Lua) and old developers that tend to forget
 * what they have written a couple of weeks ago.
 *
 * When (not if) you find anything that is unclear or is missing, please post
 * a thread about it in the <a href="http://thrivegame.forum-free.ca/f20-programming">
 * Thrive development forums</a>.
 *
 * If you are still reading, chances are that you seek information on a
 * specific topic. Apart from the raw API, this documentation currently offers
 * advice on:
 * - @ref shared_data
 * - @ref entity_component
 * - @ref script_primer
 *
 */

namespace thrive {

class Engine;

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

    std::chrono::microseconds
    targetFrameDuration() const;

    unsigned short
    targetFrameRate() const;

private:

    Game();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
