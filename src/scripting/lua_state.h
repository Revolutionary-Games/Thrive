#pragma once

#include <string>

class lua_State;

namespace thrive {

/**
* @brief RAII class for lua_State data structures
*/
class LuaState {

public:

    /**
    * @brief Constructor
    *
    * Calls \c luaL_newstate and \c luaL_openlibs.
    */
    LuaState();

    /**
    * @brief Non-copyable
    *
    */
    LuaState(const LuaState&) = delete;

    /**
    * @brief Move constructor
    *
    * @param other
    */
    LuaState(LuaState&& other);

    /**
    * @brief Destructor
    */
    ~LuaState();

    /**
    * @brief Non-copyable
    *
    */
    LuaState&
    operator= (const LuaState&) = delete;

    /**
    * @brief Move-assign operator
    *
    * @param other
    *
    * @return 
    */
    LuaState&
    operator= (LuaState&& other);

    /**
    * @brief Implicit cast to lua_State*
    */
    operator lua_State* ();

    /**
    * @brief Runs the file in the Lua state
    *
    * Calls \c luaL_dofile
    *
    * @param filename
    *   The file to load and run
    *
    * @return \c true if successful, \c false if there were errors
    */
    bool
    doFile(
        const std::string& filename
    );

    /**
    * @brief Runs the chunk in the Lua state
    *
    * Calls \c luaL_dostring
    *
    * @param string
    *   The script chunk to run
    *
    * @return \c true if successful, \c false if there were errors
    */
    bool
    doString(
        const std::string& string
    );

private:

    lua_State* m_state;
};

}
