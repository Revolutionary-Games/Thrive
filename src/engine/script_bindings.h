#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for Engine script bindings
*/
struct EngineBindings {

    /**
    * @brief Bindings for basic Engine classes
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

};

}
