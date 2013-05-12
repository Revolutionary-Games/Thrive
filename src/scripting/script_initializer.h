#pragma once

class lua_State;

namespace thrive {

/**
* @brief Initializes a Lua state for use with the script engine
*
* This will register the known classes in the Lua state
*
* @param L
*   The state to initialize
*/
void
initializeLua(
    lua_State* L
);

}
