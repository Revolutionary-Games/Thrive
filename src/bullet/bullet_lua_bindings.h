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
    * @brief Lua bindings for Bullet
    *
    * The exposed classes try to be as close to the C++ Bullet API as
    * possible.
    *
    * The currently exported classes are
    *   - Descendants of btCollisionShape:
    *       - btSphereShape
    *       - btBoxShape
    *       - btCylinderShape
    *       - btCapsuleShape
    *       - btConeShape
    */
    static luabind::scope
    luaBindings();

};

}
