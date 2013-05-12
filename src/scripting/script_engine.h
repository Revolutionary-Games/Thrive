#pragma once

#include "engine/engine.h"

#include <memory>

class lua_State;

namespace thrive {

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


/**
* @brief Handles the initialization and execution of Lua scripts
*/
class ScriptEngine : public Engine {

public:

    /**
    * @brief Constructor
    *
    * @param L
    *   The Lua state to use
    */
    ScriptEngine(
        lua_State* L
    );

    /**
    * @brief Destructor
    */
    ~ScriptEngine();

    /**
    * @brief Initializes the engine
    *
    * This adds essential systems and loads all scripts
    *
    * @param entityManager
    *   The entity manager to use
    */
    void 
    init(
        EntityManager* entityManager
    ) override;

    /**
    * @brief The script engine's Lua state
    */
    lua_State*
    luaState();

    /**
    * @brief Shuts down the engine
    */
    void 
    shutdown() override;

    /**
    * @brief Renders a frame
    */
    void
    update() override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

