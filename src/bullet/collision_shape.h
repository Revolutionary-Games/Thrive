#pragma once

#include <btBulletCollisionCommon.h>
#include <cstdint>
#include <memory>
#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace sol {
class state;
}

namespace thrive {

class StorageContainer;

/**
* @brief Wrapper for Bullet's collision shapes
*
* Wrapping the native Bullet shape classes makes it more convenient to 
* serialize them and bind them to scripts. It also allows us to offer a 
* more convenient interface.
*/
class CollisionShape {

public:

    /**
    * @brief Convenience typedef
    */
    using Ptr = std::shared_ptr<CollisionShape>;

    /**
    * @brief Enumeration for the 3 axes
    *
    * Some shapes require an axis for orientation, such as the cylinder shape.
    */
    enum Axis : uint8_t {
        AXIS_X = 0,
        AXIS_Y = 1,
        AXIS_Z = 2
    };

    /**
    * @brief Enumeration for the currently supported shape types.
    *
    * Used for deserializing a shape
    */
    enum ShapeType : uint8_t {
        EMPTY_SHAPE = 0,
        BOX_SHAPE = 1,
        CAPSULE_SHAPE = 2,
        COMPOUND_SHAPE = 3,
        CONE_SHAPE = 4,
        CYLINDER_SHAPE = 5,
        SPHERE_SHAPE = 6
    };

    /**
    * @brief Loads a shape from a storage container
    *
    * @param storage
    *   The storage of the shape
    *
    * @return 
    *   A new shape or an EmptyShape if the type is unknown
    */
    static std::unique_ptr<CollisionShape>
    load(
        const StorageContainer& storage
    );

    /**
    * @brief Lua bindings
    *
    * - CollisionShape::Axis
    *
    * @return 
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Destructor
    */
    virtual ~CollisionShape() = 0;

    /**
    * @brief Returns a pointer to the internal Bullet shape object
    *
    * @return 
    */
    virtual btCollisionShape*
    bulletShape() const = 0;

    /**
    * @brief The shape's type
    *
    */
    virtual ShapeType
    shapeType() const = 0;

    /**
    * @brief Serializes the shape into a storage container
    *
    * @return 
    */
    virtual StorageContainer
    storage() const = 0;

};


/**
* @brief Macro for defining and declaring the basic content of a shape class
*
* @param cls
*   The name of the shape class
* @param type
*   The ShapeType
* @param bulletShapeClass
*   The name of the wrapped Bullet shape class
*
*/
#define SHAPE_CLASS(cls, type, bulletShapeClass)    \
public:                                             \
                                                    \
 static const ShapeType SHAPE_TYPE = type;          \
                                                    \
 static std::unique_ptr<cls>                        \
 load(                                              \
     const StorageContainer& storage                \
 );                                                 \
                                                    \
 static void luaBindings(sol::state &lua);          \
                                                    \
 btCollisionShape*                                  \
 bulletShape() const override {                     \
     return m_bulletShape.get();                    \
 }                                                  \
                                                    \
 ShapeType                                          \
 shapeType() const override {                       \
     return SHAPE_TYPE;                             \
 }                                                  \
                                                    \
 StorageContainer                                   \
 storage() const override;                          \
                                                    \
private:                                            \
                                                    \
 std::unique_ptr<bulletShapeClass> m_bulletShape;


////////////////////////////////////////////////////////////////////////////////
// BoxShape
////////////////////////////////////////////////////////////////////////////////

/**
* @brief A simple box shape
*/
class BoxShape : public CollisionShape {

    SHAPE_CLASS(BoxShape, BOX_SHAPE, btBoxShape)

public:

    /**
    * @brief Constructor
    *
    * @param extents
    *   The side lengths of the box
    */
    BoxShape(
        const Ogre::Vector3& extents
    );

private:

    const Ogre::Vector3 m_extents;

};


////////////////////////////////////////////////////////////////////////////////
// CapsuleShape
////////////////////////////////////////////////////////////////////////////////

/**
* @brief A capsule shape. Think cylinder with rounded caps.
*/
class CapsuleShape : public CollisionShape {

    SHAPE_CLASS(CapsuleShape, CAPSULE_SHAPE, btCapsuleShape)

public:

    /**
    * @brief Constructor
    *
    * @param axis
    *   The long axis of the capsule
    * @param radius
    *   The radius of the capsule
    * @param height
    *   The height of the capsule
    */
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

/**
* @brief A shape compounded of multiple other shapes
*/
class CompoundShape : public CollisionShape {

    SHAPE_CLASS(CompoundShape, COMPOUND_SHAPE, btCompoundShape)

public:

    /**
    * @brief Constructor
    */
    CompoundShape();

    /**
    * @brief Adds a new child shape
    *
    * @param translation
    *   The child shape's local translation relative to this shape's origin
    * @param rotation
    *   The child shape's local rotation
    * @param shape
    *   The child shape
    */
    void
    addChildShape(
        const Ogre::Vector3& translation,
        const Ogre::Quaternion& rotation,
        CollisionShape::Ptr shape
    );

    /**
    * @brief Removes all child shapes
    */
    void
    clear();

    /**
    * @brief Removes a child shape
    *
    * @param shape
    *   The shape to remove
    */
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

/**
* @brief A cone shape
*/
class ConeShape : public CollisionShape {

    SHAPE_CLASS(ConeShape, CONE_SHAPE, btConeShape)

public:

    /**
    * @brief Constructor
    *
    * @param axis
    *   The long axis of the cone
    * @param radius
    *   The cone's radius
    * @param height
    *   The cone's height
    */
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

/**
* @brief A cylinder shape
*/
class CylinderShape : public CollisionShape {

    SHAPE_CLASS(CylinderShape, CYLINDER_SHAPE, btCylinderShape)

public:

    /**
    * @brief Constructor
    *
    * @param axis
    *   The long axis of the cylinder
    * @param radius
    *   The cylinder's radius
    * @param height
    *   The cylinder's height
    */
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

/**
* @brief An empty shape
*
* Mostly as a placeholder
*/
class EmptyShape : public CollisionShape {

    SHAPE_CLASS(EmptyShape, EMPTY_SHAPE, btEmptyShape)

public:

    /**
    * @brief Constructor
    */
    EmptyShape();

};


////////////////////////////////////////////////////////////////////////////////
// SphereShape
////////////////////////////////////////////////////////////////////////////////

/**
* @brief A sphere shape
*/
class SphereShape : public CollisionShape {

    SHAPE_CLASS(SphereShape, SPHERE_SHAPE, btSphereShape)

public:

    /**
    * @brief Constructor
    *
    * @param radius
    *   The sphere's radius
    */
    SphereShape(
        btScalar radius
    );

private:

    const btScalar m_radius;

};


}


