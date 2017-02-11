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

}
