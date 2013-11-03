#include "ogre/script_bindings.h"

#include "scripting/luabind.h"
#include "ogre/camera_system.h"
#include "ogre/colour_material.h"
#include "ogre/keyboard.h"
#include "ogre/light_system.h"
#include "ogre/mouse.h"
#include "ogre/render_system.h"
#include "ogre/scene_node_system.h"
#include "ogre/script_bindings.h"
#include "ogre/sky_system.h"
#include "ogre/text_overlay.h"
#include "ogre/viewport_system.h"
#include "scripting/luabind.h"

#include <luabind/operator.hpp>
#include <luabind/out_value_policy.hpp>

#include <OgreAxisAlignedBox.h>
#include <OgreColourValue.h>
#include <OgreMath.h>
#include <OgreMatrix3.h>
#include <OgreRay.h>
#include <OgreSceneManager.h>
#include <OgreSphere.h>
#include <OgreVector3.h>

using namespace luabind;
using namespace Ogre;


static luabind::scope
axisAlignedBoxBindings() {
    return class_<AxisAlignedBox>("AxisAlignedBox")
        .enum_("Extent") [
            value("EXTENT_NULL", AxisAlignedBox::EXTENT_NULL),
            value("EXTENT_FINITE", AxisAlignedBox::EXTENT_FINITE),
            value("EXTENT_INFINITE", AxisAlignedBox::EXTENT_INFINITE)
        ]
        .enum_("CornerEnum") [
            value("FAR_LEFT_BOTTOM", AxisAlignedBox::FAR_LEFT_BOTTOM),
            value("FAR_LEFT_TOP", AxisAlignedBox::FAR_LEFT_TOP),
            value("FAR_RIGHT_TOP", AxisAlignedBox::FAR_RIGHT_TOP),
            value("FAR_RIGHT_BOTTOM", AxisAlignedBox::FAR_RIGHT_BOTTOM),
            value("NEAR_RIGHT_BOTTOM", AxisAlignedBox::NEAR_RIGHT_BOTTOM),
            value("NEAR_LEFT_BOTTOM", AxisAlignedBox::NEAR_LEFT_BOTTOM),
            value("NEAR_LEFT_TOP", AxisAlignedBox::NEAR_LEFT_TOP),
            value("NEAR_RIGHT_TOP", AxisAlignedBox::NEAR_RIGHT_TOP)
        ]
        .def(constructor<>())
        .def(constructor<AxisAlignedBox::Extent>())
        .def(constructor<const Vector3&, const Vector3&>())
        .def(constructor<
            Real, Real, Real,
            Real, Real, Real >()
        )
        .def(const_self == other<AxisAlignedBox>())
        .def("getMinimum", 
            static_cast<const Vector3& (AxisAlignedBox::*) () const>(&AxisAlignedBox::getMinimum)
        )
        .def("getMaximum", 
            static_cast<const Vector3& (AxisAlignedBox::*) () const>(&AxisAlignedBox::getMaximum)
        )
        .def("setMinimum", 
            static_cast<void (AxisAlignedBox::*) (const Vector3&)>(&AxisAlignedBox::setMinimum)
        )
        .def("setMinimum", 
            static_cast<void (AxisAlignedBox::*) (Real, Real, Real)>(&AxisAlignedBox::setMinimum)
        )
        .def("setMinimumX", &AxisAlignedBox::setMinimumX)
        .def("setMinimumY", &AxisAlignedBox::setMinimumY)
        .def("setMinimumZ", &AxisAlignedBox::setMinimumZ)
        .def("setMaximum", 
            static_cast<void (AxisAlignedBox::*) (const Vector3&)>(&AxisAlignedBox::setMaximum)
        )
        .def("setMaximum", 
            static_cast<void (AxisAlignedBox::*) (Real, Real, Real)>(&AxisAlignedBox::setMaximum)
        )
        .def("setMaximumX", &AxisAlignedBox::setMaximumX)
        .def("setMaximumY", &AxisAlignedBox::setMaximumY)
        .def("setMaximumZ", &AxisAlignedBox::setMaximumZ)
        .def("setExtents", 
            static_cast<void (AxisAlignedBox::*) (const Vector3&, const Vector3&)>(&AxisAlignedBox::setExtents)
        )
        .def("setExtents", 
            static_cast<void (AxisAlignedBox::*) (Real, Real, Real, Real, Real, Real)>(&AxisAlignedBox::setExtents)
        )
        .def("getCorner", &AxisAlignedBox::getCorner)
        .def("merge", 
            static_cast<void (AxisAlignedBox::*) (const AxisAlignedBox&)>(&AxisAlignedBox::merge)
        )
        .def("merge", 
            static_cast<void (AxisAlignedBox::*) (const Vector3&)>(&AxisAlignedBox::merge)
        )
        .def("setNull", &AxisAlignedBox::setNull)
        .def("isNull", &AxisAlignedBox::isNull)
        .def("isFinite", &AxisAlignedBox::isFinite)
        .def("setInfinite", &AxisAlignedBox::setInfinite)
        .def("isInfinite", &AxisAlignedBox::isInfinite)
        .def("intersects", 
            static_cast<bool (AxisAlignedBox::*) (const AxisAlignedBox&) const>(&AxisAlignedBox::intersects)
        )
        .def("intersection", &AxisAlignedBox::intersection)
        .def("volume", &AxisAlignedBox::volume)
        .def("scale", &AxisAlignedBox::scale)
        .def("intersects", 
            static_cast<bool (AxisAlignedBox::*) (const Sphere&) const>(&AxisAlignedBox::intersects)
        )
        .def("intersects", 
            static_cast<bool (AxisAlignedBox::*) (const Plane&) const>(&AxisAlignedBox::intersects)
        )
        .def("intersects", 
            static_cast<bool (AxisAlignedBox::*) (const Vector3&) const>(&AxisAlignedBox::intersects)
        )
        .def("getCenter", &AxisAlignedBox::getCenter)
        .def("getSize", &AxisAlignedBox::getSize)
        .def("getHalfSize", &AxisAlignedBox::getHalfSize)
        .def("contains", 
            static_cast<bool (AxisAlignedBox::*) (const Vector3&) const>(&AxisAlignedBox::contains)
        )
        .def("distance", &AxisAlignedBox::distance)
        .def("contains", 
            static_cast<bool (AxisAlignedBox::*) (const AxisAlignedBox&) const>(&AxisAlignedBox::contains)
        )
    ;
}


static luabind::scope
colourValueBindings() {
    return class_<ColourValue>("ColourValue")
        .def(constructor<float, float, float, float>())
        .def(const_self == other<ColourValue>())
        .def(const_self + other<ColourValue>())
        .def(const_self - other<ColourValue>())
        .def(const_self * other<ColourValue>())
        .def(const_self * other<ColourValue>())
        .def(const_self * float())
        .def("saturate", &ColourValue::saturate)
        .def("setHSB", &ColourValue::setHSB)
        .def("getHSB", &ColourValue::getHSB,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def_readwrite("r", &ColourValue::r)
        .def_readwrite("g", &ColourValue::g)
        .def_readwrite("b", &ColourValue::b)
        .def_readwrite("a", &ColourValue::a)
    ;
}


static luabind::scope
degreeBindings() {
    return class_<Degree>("Degree")
        .def(constructor<Real>())
        .def(constructor<const Radian&>())
        .def(const_self == other<Degree>())
        .def(const_self + other<Degree>())
        .def(const_self - other<Degree>())
        .def(const_self * other<Degree>())
        .def(const_self * Real())
        .def(const_self / Real())
        .def(const_self < other<Degree>())
    ;
}


static void
SubEntity_setColour(
    SubEntity* self,
    const Ogre::ColourValue& colour
) {
    auto material = thrive::getColourMaterial(colour);
    self->setMaterial(material);
}


static luabind::scope
entityBindings() {
    return (
        class_<SubEntity, MovableObject>("OgreSubEntity")
            .def("setColour", &SubEntity_setColour)
        ,
        class_<Ogre::Entity, MovableObject>("OgreEntity")
            .def("getSubEntity", static_cast<SubEntity*(Entity::*)(const String&) const>(&Entity::getSubEntity))
            .def("getNumSubEntities", &Entity::getNumSubEntities)
    );
}



static luabind::scope
matrix3Bindings() {
    return class_<Matrix3>("Matrix3")
        .def(constructor<>())
        .def(constructor<
            Real, Real, Real,
            Real, Real, Real,
            Real, Real, Real>())
        .def(const_self == other<Matrix3>())
        .def(const_self + other<Matrix3>())
        .def(const_self - other<Matrix3>())
        .def(const_self * other<Matrix3>())
        .def(const_self * other<Vector3>())
        .def(const_self * Real())
        .def("GetColumn", &Matrix3::GetColumn)
        .def("SetColumn", &Matrix3::SetColumn)
        .def("FromAxes", &Matrix3::FromAxes)
        .def("Transpose", &Matrix3::Transpose)
        .def("Inverse", 
            static_cast<bool(Matrix3::*)(Matrix3&, Real) const>(&Matrix3::Inverse), 
            pure_out_value(_2)
        )
        .def("Determinant", &Matrix3::Determinant)
        .def("SingularValueDecomposition", 
            &Matrix3::SingularValueDecomposition,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("SingularValueComposition", &Matrix3::SingularValueComposition)
        .def("Orthonormalize", &Matrix3::Orthonormalize)
        .def("QDUDecomposition", 
            &Matrix3::QDUDecomposition,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("SpectralNorm", &Matrix3::SpectralNorm)
        .def("ToAngleAxis", 
            static_cast<void(Matrix3::*)(Vector3&, Radian&) const>(&Matrix3::ToAngleAxis),
            (pure_out_value(_2), pure_out_value(_3))
        )
        .def("FromAngleAxis", &Matrix3::FromAngleAxis)
        .def("ToEulerAnglesXYZ", 
            &Matrix3::ToEulerAnglesXYZ,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("ToEulerAnglesXZY", 
            &Matrix3::ToEulerAnglesXZY,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("ToEulerAnglesYXZ", 
            &Matrix3::ToEulerAnglesYXZ,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("ToEulerAnglesYZX", 
            &Matrix3::ToEulerAnglesYZX,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("ToEulerAnglesZXY", 
            &Matrix3::ToEulerAnglesZXY,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("ToEulerAnglesZYX", 
            &Matrix3::ToEulerAnglesZYX,
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("FromEulerAnglesXYZ", &Matrix3::FromEulerAnglesXYZ)
        .def("FromEulerAnglesXZY", &Matrix3::FromEulerAnglesXZY)
        .def("FromEulerAnglesYXZ", &Matrix3::FromEulerAnglesYXZ)
        .def("FromEulerAnglesYZX", &Matrix3::FromEulerAnglesYZX)
        .def("FromEulerAnglesZXY", &Matrix3::FromEulerAnglesZXY)
        .def("FromEulerAnglesZYX", &Matrix3::FromEulerAnglesZYX)
        .def("hasScale", &Matrix3::hasScale)
    ;
}


static luabind::scope
movableObjectBindings() {
    return class_<MovableObject>("MovableObject");
}


static luabind::scope
planeBindings() {
    return class_<Plane>("Plane")
        .enum_("Side") [
            value("NO_SIDE", Plane::NO_SIDE),
            value("POSITIVE_SIDE", Plane::POSITIVE_SIDE),
            value("NEGATIVE_SIDE", Plane::NEGATIVE_SIDE),
            value("BOTH_SIDE", Plane::BOTH_SIDE)
        ]
        .def(constructor<>())
        .def(constructor<const Vector3&, Real>())
        .def(constructor<Real, Real, Real, Real>())
        .def(constructor<const Vector3&, const Vector3&>())
        .def(constructor<const Vector3&, const Vector3&, const Vector3&>())
        .def(const_self == other<Plane>())
        .def("getSide", 
            static_cast<Plane::Side (Plane::*) (const Vector3&) const>(&Plane::getSide)
        )
        .def("getSide", 
            static_cast<Plane::Side (Plane::*) (const AxisAlignedBox&) const>(&Plane::getSide)
        )
        .def("getSide", 
            static_cast<Plane::Side (Plane::*) (const Vector3&, const Vector3&) const>(&Plane::getSide)
        )
        .def("getDistance", &Plane::getDistance)
        .def("redefine", 
            static_cast<void (Plane::*) (const Vector3&, const Vector3&)>(&Plane::redefine)
        )
        .def("redefine", 
            static_cast<void (Plane::*) (const Vector3&, const Vector3&, const Vector3&)>(&Plane::redefine)
        )
        .def("projectVector", &Plane::projectVector)
        .def("normalise", &Plane::normalise)
        .def_readwrite("normal", &Plane::normal)
        .def_readwrite("d", &Plane::d)
    ;
}


static luabind::scope
quaternionBindings() {
    return class_<Quaternion>("Quaternion")
        .def(constructor<>())
        .def(constructor<Real, Real, Real, Real>())
        .def(constructor<const Matrix3&>())
        .def(constructor<const Radian&, const Vector3&>())
        .def(constructor<const Vector3&, const Vector3&, const Vector3&>())
        .def(const_self + other<Quaternion>())
        .def(const_self - other<Quaternion>())
        .def(const_self * other<Quaternion>())
        .def(const_self * Real())
        .def(const_self * other<Vector3>())
        .def(const_self == other<Quaternion>())
        .def("FromRotationMatrix", &Quaternion::FromRotationMatrix)
        .def("ToRotationMatrix", &Quaternion::ToRotationMatrix, pure_out_value(_2))
        .def("FromAngleAxis", &Quaternion::FromAngleAxis)
        .def("ToAngleAxis", 
            static_cast<void(Quaternion::*)(Radian&, Vector3&) const>(&Quaternion::ToAngleAxis), 
            (pure_out_value(_2), pure_out_value(_3))
        )
        .def("FromAxes", 
            static_cast<void(Quaternion::*)(const Vector3&, const Vector3&, const Vector3&)>(&Quaternion::FromAxes)
        )
        .def("ToAxes", 
            static_cast<void(Quaternion::*)(Vector3&, Vector3&, Vector3&) const>(&Quaternion::ToAxes),
            (pure_out_value(_2), pure_out_value(_3), pure_out_value(_4))
        )
        .def("xAxis", &Quaternion::xAxis)
        .def("yAxis", &Quaternion::yAxis)
        .def("zAxis", &Quaternion::zAxis)
        .def("Dot", &Quaternion::Dot)
        .def("Norm", &Quaternion::Norm)
        .def("normalise", &Quaternion::normalise)
        .def("Inverse", &Quaternion::Inverse)
        .def("UnitInverse", &Quaternion::UnitInverse)
        .def("Exp", &Quaternion::Exp)
        .def("Log", &Quaternion::Log)
        .def("getRoll", &Quaternion::getRoll)
        .def("getPitch", &Quaternion::getPitch)
        .def("getYaw", &Quaternion::getYaw)
        .def("equals", &Quaternion::equals)
        .def("isNaN", &Quaternion::isNaN)
    ;
}


static luabind::scope
radianBindings() {
    return class_<Radian>("Radian")
        .def(constructor<Real>())
        .def(constructor<const Degree&>())
        .def(const_self == other<Radian>())
        .def(const_self + other<Radian>())
        .def(const_self - other<Radian>())
        .def(const_self * other<Radian>())
        .def(const_self * Real())
        .def(const_self / Real())
        .def(const_self < other<Radian>())
        .def("valueDegrees", &Radian::valueDegrees)
        .def("valueRadians", &Radian::valueRadians)
        .def("valueAngleUnits", &Radian::valueAngleUnits)
    ;
}


static bool
Ray_intersects(
    const Ray* self,
    const Plane& plane,
    Real& t
) {
    bool intersects = false;
    std::tie(intersects, t) = self->intersects(plane);
    return intersects;
}

static luabind::scope
rayBindings() {
    return class_<Ray>("Ray")
        .def(constructor<>())
        .def(constructor<const Vector3&, const Vector3&>())
        .def(const_self * Real())
        .def("setOrigin", &Ray::setOrigin)
        .def("getOrigin", &Ray::getOrigin)
        .def("setDirection", &Ray::setDirection)
        .def("getDirection", &Ray::getDirection)
        .def("getPoint", &Ray::getPoint)
        .def("intersects", Ray_intersects, pure_out_value(_3))
    ;
}


static luabind::scope
sceneManagerBindings() {
    return class_<SceneManager>("SceneManager")
        .enum_("PrefabType") [
            value("PT_PLANE", SceneManager::PT_PLANE),
            value("PT_CUBE", SceneManager::PT_CUBE),
            value("PT_SPHERE", SceneManager::PT_SPHERE)
        ]
        .def("createEntity", 
            static_cast<Entity* (SceneManager::*)(const String&)>(&SceneManager::createEntity)
        )
        .def("createEntity", 
            static_cast<Entity* (SceneManager::*)(SceneManager::PrefabType)>(&SceneManager::createEntity)
        )
        .def("setAmbientLight", &SceneManager::setAmbientLight)
    ;
}


static luabind::scope
sphereBindings() {
    return class_<Sphere>("Sphere")
        .def(constructor<>())
        .def(constructor<const Vector3&, Real>())
        .def("getRadius", &Sphere::getRadius)
        .def("setRadius", &Sphere::setRadius)
        .def("getCenter", &Sphere::getCenter)
        .def("setCenter", &Sphere::setCenter)
        .def("intersects", 
            static_cast<bool (Sphere::*)(const Sphere&) const>(&Sphere::intersects)
        )
        .def("intersects", 
            static_cast<bool (Sphere::*)(const AxisAlignedBox&) const>(&Sphere::intersects)
        )
        .def("intersects", 
            static_cast<bool (Sphere::*)(const Plane&) const>(&Sphere::intersects)
        )
        .def("intersects", 
            static_cast<bool (Sphere::*)(const Vector3&) const>(&Sphere::intersects)
        )
        .def("merge", &Sphere::merge)
    ;
}


static luabind::scope
vector3Bindings() {
    return class_<Vector3>("Vector3")
        .def(constructor<>())
        .def(constructor<const Real, const Real, const Real>())
        .def(const_self == other<Vector3>())
        .def(const_self + other<Vector3>())
        .def(const_self - other<Vector3>())
        .def(const_self * Real())
        .def(Real() * const_self)
        .def(const_self * other<Vector3>())
        .def(const_self / Real())
        .def(const_self / other<Vector3>())
        .def(const_self < other<Vector3>())
        .def(tostring(self))
        .def_readwrite("x", &Vector3::x)
        .def_readwrite("y", &Vector3::y)
        .def_readwrite("z", &Vector3::z)
        .def("length", &Vector3::length)
        .def("squaredLength", &Vector3::squaredLength)
        .def("distance", &Vector3::distance)
        .def("squaredDistance", &Vector3::squaredDistance)
        .def("dotProduct", &Vector3::dotProduct)
        .def("absDotProduct", &Vector3::absDotProduct)
        .def("normalise", &Vector3::normalise)
        .def("crossProduct", &Vector3::crossProduct)
        .def("midPoint", &Vector3::midPoint)
        .def("makeFloor", &Vector3::makeFloor)
        .def("makeCeil", &Vector3::makeCeil)
        .def("perpendicular", &Vector3::perpendicular)
        .def("randomDeviant", &Vector3::randomDeviant)
        .def("angleBetween", &Vector3::angleBetween)
        .def("getRotationTo", &Vector3::getRotationTo)
        .def("isZeroLength", &Vector3::isZeroLength)
        .def("normalisedCopy", &Vector3::normalisedCopy)
        .def("reflect", &Vector3::reflect)
        .def("positionEquals", &Vector3::positionEquals)
        .def("positionCloses", &Vector3::positionCloses)
        .def("directionEquals", &Vector3::directionEquals)
        .def("isNaN", &Vector3::isNaN)
        .def("primaryAxis", &Vector3::primaryAxis)
    ;
}

luabind::scope
thrive::OgreBindings::luaBindings() {
    return (
        // Math
        axisAlignedBoxBindings(),
        colourValueBindings(),
        degreeBindings(),
        matrix3Bindings(),
        planeBindings(),
        quaternionBindings(),
        radianBindings(),
        rayBindings(),
        sphereBindings(),
        vector3Bindings(),
        // Scene Manager
        sceneManagerBindings(),
        movableObjectBindings(),
        entityBindings(),
        // Components
        OgreCameraComponent::luaBindings(),
        OgreLightComponent::luaBindings(),
        OgreSceneNodeComponent::luaBindings(),
        OgreViewportComponent::luaBindings(),
        SkyPlaneComponent::luaBindings(),
        TextOverlayComponent::luaBindings(),
        // Systems
        OgreAddSceneNodeSystem::luaBindings(),
        OgreCameraSystem::luaBindings(),
        OgreLightSystem::luaBindings(),
        OgreRemoveSceneNodeSystem::luaBindings(),
        OgreUpdateSceneNodeSystem::luaBindings(),
        OgreViewportSystem::luaBindings(),
        thrive::RenderSystem::luaBindings(), // Fully qualified because of Ogre::RenderSystem
        SkySystem::luaBindings(),
        TextOverlaySystem::luaBindings(),
        // Other
        Keyboard::luaBindings(),
        Mouse::luaBindings()
    );
}
