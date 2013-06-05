#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for sound script bindings
*/
struct SoundBindings {

    /**
    * @brief Lua bindings for sound systems
    *
    */
    static luabind::scope
    luaBindings();

};

}

