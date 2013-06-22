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
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1AxisAlignedBox.html">AxisAlignedBox</a>
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1ColourValue.html">ColourValue</a>
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1Matrix3.html">Matrix3</a>
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1Plane.html">Plane</a>
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1Quaternion.html">Quaternion</a>
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1Radian.html">Radian</a>
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1Sphere.html">Sphere</a>
    *   - <a href="http://www.ogre3d.org/docs/api/html/classOgre_1_1Vector3.html">Vector3</a>
    */
    static luabind::scope
    luaBindings();

};

}
