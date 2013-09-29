#pragma once

#include <btBulletCollisionCommon.h>
#include <cstdint>
#include <memory>
#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace luabind {
    class scope;
}

namespace thrive {

class StorageContainer;

class CollisionShape {

public:

    using Ptr = std::shared_ptr<CollisionShape>;

    enum Axis : uint8_t {
        AXIS_X = 0,
        AXIS_Y = 1,
        AXIS_Z = 2
    };

    enum ShapeType : uint8_t {
        EMPTY_SHAPE = 0,
        BOX_SHAPE = 1,
        CAPSULE_SHAPE = 2,
        COMPOUND_SHAPE = 3,
        CONE_SHAPE = 4,
        CYLINDER_SHAPE = 5,
        SPHERE_SHAPE = 6
    };

    static std::unique_ptr<CollisionShape>
    load(
        const StorageContainer& storage
    );

    static luabind::scope
    luaBindings();

    virtual ~CollisionShape() = 0;

    virtual btCollisionShape*
    bulletShape() const = 0;

    virtual ShapeType
    shapeType() const = 0;

    virtual StorageContainer
    storage() const = 0;

};


#define SHAPE_CLASS(cls, type, bulletShapeClass) \
    public: \
        \
        static const ShapeType SHAPE_TYPE = type; \
        \
        static std::unique_ptr<cls> \
        load( \
            const StorageContainer& storage \
        ); \
        \
        static luabind::scope \
        luaBindings(); \
        \
        btCollisionShape* \
        bulletShape() const override { \
            return m_bulletShape.get(); \
        } \
        \
        ShapeType \
        shapeType() const override { \
            return SHAPE_TYPE; \
        } \
        \
        StorageContainer \
        storage() const override; \
        \
    private: \
        \
        std::unique_ptr<bulletShapeClass> m_bulletShape;


////////////////////////////////////////////////////////////////////////////////
// BoxShape
////////////////////////////////////////////////////////////////////////////////

class BoxShape : public CollisionShape {

    SHAPE_CLASS(BoxShape, BOX_SHAPE, btBoxShape)

public:

    BoxShape(
        const Ogre::Vector3& extents
    );

private:

    const Ogre::Vector3 m_extents;

};


////////////////////////////////////////////////////////////////////////////////
// CapsuleShape
////////////////////////////////////////////////////////////////////////////////

class CapsuleShape : public CollisionShape {

    SHAPE_CLASS(CapsuleShape, CAPSULE_SHAPE, btCapsuleShape)

public:

    CapsuleShape(
        CollisionShape::Axis axis,
        btScalar radius,
        btScalar height
    );

private:

    const CollisionShape::Axis m_axis;

    const btScalar m_height;

    const btScalar m_radius;

};


////////////////////////////////////////////////////////////////////////////////
// CompoundShape
////////////////////////////////////////////////////////////////////////////////

class CompoundShape : public CollisionShape {

    SHAPE_CLASS(CompoundShape, COMPOUND_SHAPE, btCompoundShape)

public:

    CompoundShape();

    void
    addChildShape(
        const Ogre::Vector3& translation,
        const Ogre::Quaternion& rotation,
        CollisionShape::Ptr shape
    );

    void
    clear();

    void
    removeChildShape(
        const CollisionShape::Ptr& shape
    );

private:

    struct ChildShape {
        Ogre::Vector3 translation;
        Ogre::Quaternion rotation;
        CollisionShape::Ptr shape;
    };

    std::vector<ChildShape> m_childShapes;

};


////////////////////////////////////////////////////////////////////////////////
// ConeShape
////////////////////////////////////////////////////////////////////////////////

class ConeShape : public CollisionShape {

    SHAPE_CLASS(ConeShape, CONE_SHAPE, btConeShape)

public:

    ConeShape(
        CollisionShape::Axis axis,
        btScalar radius,
        btScalar height
    );

private:

    const CollisionShape::Axis m_axis;

    const btScalar m_height;

    const btScalar m_radius;

};


////////////////////////////////////////////////////////////////////////////////
// CylinderShape
////////////////////////////////////////////////////////////////////////////////

class CylinderShape : public CollisionShape {

    SHAPE_CLASS(CylinderShape, CYLINDER_SHAPE, btCylinderShape)

public:

    CylinderShape(
        CollisionShape::Axis axis,
        btScalar radius,
        btScalar height
    );


private:

    const CollisionShape::Axis m_axis;

    const btScalar m_height;

    const btScalar m_radius;

};


////////////////////////////////////////////////////////////////////////////////
// EmptyShape
////////////////////////////////////////////////////////////////////////////////

class EmptyShape : public CollisionShape {

    SHAPE_CLASS(EmptyShape, EMPTY_SHAPE, btEmptyShape)

public:

    EmptyShape();

};


////////////////////////////////////////////////////////////////////////////////
// SphereShape
////////////////////////////////////////////////////////////////////////////////

class SphereShape : public CollisionShape {

    SHAPE_CLASS(SphereShape, SPHERE_SHAPE, btSphereShape)

public:

    SphereShape(
        btScalar radius
    );

private:

    const btScalar m_radius;

};


}
