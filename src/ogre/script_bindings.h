#pragma once

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Helper for OGRE script bindings
*/
struct OgreBindings {

    /**
    * @brief Lua bindings for OGRE
    *
    * The exposed classes try to be as close to the C++ OGRE API as
    * possible.
    *
    * The currently exported classes are
    *   - AxisAlignedBox
    *   - ColourValue
    *   - Matrix3
    *   - Plane
    *   - Quaternion
    *   - Radian
    *   - Sphere
    *   - Vector3
    */
    static luabind::scope
    luaBindings();

};

}
