#pragma once

namespace sol {
class state;
}

namespace thrive {

/**
* @brief Initializes a Lua state for use with the script engine
*
* This will register the known classes in the Lua state
*/
void initializeLua(sol::state &lua);

// This isn't currently used, because maybe the default handler is
// good enough, and this has broken with newer sol versions
//std::string thriveLuaOnError(sol::this_state lua, std::string err);

}
