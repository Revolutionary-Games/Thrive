#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for script bindings
*/
struct ScriptBindings {

    /**
    * @brief Exports relevant script bindings to Lua
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

};

}

