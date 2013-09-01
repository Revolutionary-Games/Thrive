#include "bullet/script_bindings.h"

#include "bullet/bullet_ogre_conversion.h"
#include "bullet/rigid_body_system.h"
#include "scripting/luabind.h"

#include <btBulletCollisionCommon.h>
#include <memory>
#include <OgreVector3.h>

using namespace luabind;
using namespace thrive;

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


class CompoundShape : public btCompoundShape {
public:

    static luabind::scope
    luaBindings() {
        return class_<CompoundShape, btCollisionShape, std::shared_ptr<btCollisionShape>>("btCompoundShape")
            .def(constructor<>())
            .def("addChildShape", &CompoundShape::addChildShape)
            .def("clear", &CompoundShape::clear)
            .def("removeChildShape", &CompoundShape::removeChildShape)
            .def("removeChildShapeByIndex", &CompoundShape::removeChildShapeByIndex)
        ;
    }

    void
    addChildShape(
        const Ogre::Quaternion& rotation,
        const Ogre::Vector3& translation,
        btCollisionShape* shape
    ) {
        btCompoundShape::addChildShape(
            btTransform(
                ogreToBullet(rotation),
                ogreToBullet(translation)
            ),
            shape
        );
    }


    void
    clear() {
        while (this->getNumChildShapes() > 0) {
            this->removeChildShapeByIndex(0);
        }
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

class CylinderShapeX : public btCylinderShapeX {
public:

    static luabind::scope
    luaBindings() {
        return class_<CylinderShapeX, btCollisionShape, std::shared_ptr<btCollisionShape>>("btCylinderShapeX")
            .def(constructor<const Ogre::Vector3&>())
        ;
    }

    CylinderShapeX(
        const Ogre::Vector3& v
    ) : btCylinderShapeX(btVector3(v.x, v.y, v.z))
    {
    }
};


class CylinderShapeZ : public btCylinderShapeZ {
public:

    static luabind::scope
    luaBindings() {
        return class_<CylinderShapeZ, btCollisionShape, std::shared_ptr<btCollisionShape>>("btCylinderShapeZ")
            .def(constructor<const Ogre::Vector3&>())
        ;
    }

    CylinderShapeZ(
        const Ogre::Vector3& v
    ) : btCylinderShapeZ(btVector3(v.x, v.y, v.z))
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
        CompoundShape::luaBindings(),
        CylinderShape::luaBindings(),
        CylinderShapeX::luaBindings(),
        CylinderShapeZ::luaBindings(),
        btCapsuleShapeBindings(),
        btConeShapeBoxBindings(),
        RigidBodyComponent::luaBindings(),
    );
}

