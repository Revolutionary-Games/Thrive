#include "bullet/bullet_lua_bindings.h"

#include "scripting/luabind.h"

#include <btBulletCollisionCommon.h>
#include <memory>
#include <OgreVector3.h>


using namespace luabind;


namespace {

class BoxShape : public btBoxShape {
public:

    static luabind::scope
    luaBindings() {
        return class_<BoxShape, btCollisionShape, std::shared_ptr<btCollisionShape>>("btBoxShape")
            .def(constructor<const Ogre::Vector3&>())
        ;
    }

    BoxShape(
        const Ogre::Vector3& v
    ) : btBoxShape(btVector3(v.x, v.y, v.z))
    {
    }
};

class CylinderShape : public btCylinderShape {
public:

    static luabind::scope
    luaBindings() {
        return class_<CylinderShape, btCollisionShape, std::shared_ptr<btCollisionShape>>("btCylinderShape")
            .def(constructor<const Ogre::Vector3&>())
        ;
    }

    CylinderShape(
        const Ogre::Vector3& v
    ) : btCylinderShape(btVector3(v.x, v.y, v.z))
    {
    }
};
}


static luabind::scope
btCollisionShapeBindings() {
    return class_<btCollisionShape, std::shared_ptr<btCollisionShape>>("btCollisionShape")
    ;
}


static luabind::scope
btSphereShapeBoxBindings() {
    return class_<btSphereShape, btCollisionShape, std::shared_ptr<btCollisionShape>>("btSphereShape")
        .def(constructor<btScalar>())
    ;
}


static luabind::scope
btCapsuleShapeBindings() {
    return class_<btCapsuleShape, btCollisionShape, std::shared_ptr<btCollisionShape>>("btCapsuleShape")
        .def(constructor<btScalar,btScalar>())
    ;
}


static luabind::scope
btConeShapeBoxBindings() {
    return class_<btConeShape, btCollisionShape, std::shared_ptr<btCollisionShape>>("btConeShape")
        .def(constructor<btScalar,btScalar>())
    ;
}


luabind::scope
thrive::BulletBindings::luaBindings() {
    return (
        btCollisionShapeBindings(),
        btSphereShapeBoxBindings(),
        BoxShape::luaBindings(),
        CylinderShape::luaBindings(),
        btCapsuleShapeBindings(),
        btConeShapeBoxBindings()
    );


}
