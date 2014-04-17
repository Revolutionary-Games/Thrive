#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for script bindings
*/
struct GeneralBindings {

    /**
    * @brief Exports relevant general bindings to Lua
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

};

}


