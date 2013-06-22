#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for Bullet script bindings
*/
struct BulletBindings {

    /**
    * @brief Lua bindings for physics systems
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

};

}

