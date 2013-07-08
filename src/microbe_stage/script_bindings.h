#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for script bindings
*/
struct MicrobeBindings {

    /**
    * @brief Exports relevant microbe bindings to Lua
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

};

}


