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
 * - @ref entity_component
 * - @ref script_primer
 *
 */


/**
 * @page entity_component Entities, Components and Systems
 *
 * Introductions to the entity / component approach can be found here:
 *  - <a href="http://piemaster.net/2011/07/entity-component-primer/">Entity / Component Game Design: A Primer</a>
 *  - <a href="http://www.gamasutra.com/blogs/MeganFox/20101208/88590/Game_Engines_101_The_EntityComponent_Model.php">Game Engines 101: The Entity / Component Model</a>
 *  - <a href="http://www.richardlord.net/blog/what-is-an-entity-framework">What is an entity system framework?</a>
 *
 * The following gives an overview of the implementation of entities, 
 * components and systems in Thrive.
 *
 * @section entity_manager Entity Manager
 *
 * Entities and their components are managed by an EntityManager. The
 * EntityManager identifies each entity by its unique id. You can use
 * either EntityManager::generateNewId() or EntityManager::getNamedId()
 * to obtain an id.
 *
 * An entity can have at most one component of each type. Component types are
 * distinguished primarily by their <em>component id</em> (see also 
 * Component::generateTypeId()). This component id is generated dynamically
 * at application startup and may change between executions. To identify a
 * component type across executions, use the <em>component name</em>, which
 * should be constant between executions. To convert between component id
 * and component name, use ComponentFactory::typeNameToId() and 
 * ComponentFactory::typeIdToName().
 *
 * For convenience, there's an Entity class that wraps the API of 
 * EntityManager in an entity-centric way.
 *
 * @section system Systems
 *
 * The absolute minimum a system has to implement is the System::update()
 * function. You can also override System::init() and System::shutdown() for
 * setup and teardown procedures.
 *
 * Usually, a system operates on entities that have a specific combination of
 * components. The EntityFilter template can filter out entities that have
 * such a component makeup.
 *
 * @section engine Engine
 *
 * All systems are managed by the engine. The engine provides the entity 
 * manager, initializes the systems, updates them during the game and 
 * finally, shuts them down.
 *
 */

/**
 * @page script_primer Script Primer
 * 
 * Thrive is scriptable with the Lua script language. If you are not familiar 
 * with Lua, you can get an overview at the following links:
 *  - <a href="http://awesome.naquadah.org/wiki/The_briefest_introduction_to_Lua">The briefest introduction to Lua</a>
 *  - <a href="http://www.lua.org/manual/5.2/">Lua 5.2 Reference Manual</a>
 *  - <a href="http://lua-users.org/wiki/TutorialDirectory">Lua Wiki Tutorials</a>
 *
 * At application startup, Thrive parses the file \c scripts/manifest.txt. Each
 * line of this file should be either a filename or a directory name. If it's
 * a file name, the file is executed with Lua. For directories, Thrive goes
 * down into this directory and looks for another manifest.txt, applying the
 * same procedure recursively. You can make Thrive ignore a line in a manifest
 * by starting the line with two forward-slashes: "//".
 *
 * There is currently no way to reparse the scripts at runtime and it is 
 * doubtful there ever will be. To apply changes in the scripts, you have to
 * restart the application.
 *
 * Within the Lua scripts, you have access to all classes exposed by Thrive.
 * The most important one is probably Entity. Then, there are the subclasses of
 * Component. If you want to know more about the script API of these classes,
 * look for the function \c luaBindings() (e.g. Entity::luaBindings()), it 
 * will explain how to use the class from a script.
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

    /**
    * @brief The target frame duration
    */
    std::chrono::microseconds
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
