#include "ogre/script_bindings.h"

#include "scripting/luabind.h"

#include <luabind/operator.hpp>
#include <luabind/out_value_policy.hpp>

#include <btBulletCollisionCommon.h>

using namespace luabind;


static luabind::scope
btSphereShapeBoxBindings() {
    return class_<btSphereShape>("btSphereShape")
        .def(constructor<btScalar>())
    ;
}


static luabind::scope
btBoxShapeBindings() {
    return class_<btBoxShape>("btBoxShape")
        .def(constructor<btVector3>())
    ;
}


static luabind::scope
btCylinderShapeBindings() {
    return class_<btCylinderShape>("btCylinderShape")
        .def(constructor<btVector3>())
    ;
}


static luabind::scope
btCapsuleShapeBindings() {
    return class_<btCapsuleShape>("btCapsuleShape")
        .def(constructor<btScalar,btScalar>())
    ;
}


static luabind::scope
btConeShapeBoxBindings() {
    return class_<btConeShape>("btConeShape")
        .def(constructor<btScalar,btScalar>())
    ;
}


luabind::scope
thrive::BulletBindings::luaBindings() {
    return (
        btSphereShapeBoxBindings(),
        btBoxShapeBindings(),
        btCylinderShapeBindings(),
        btCapsuleShapeBindings(),
        btConeShapeBoxBindings()
    );


}
