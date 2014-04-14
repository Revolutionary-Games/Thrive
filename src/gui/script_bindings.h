#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for script bindings
*/
struct GuiBindings {

    /**
    * @brief Exports relevant gui bindings to lua
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

};

}


