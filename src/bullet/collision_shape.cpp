#include "bullet/collision_shape.h"

#include "bullet/bullet_ogre_conversion.h"
#include "engine/serialization.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// CollisionShape
////////////////////////////////////////////////////////////////////////////////

#define SHAPE_TYPE_CASE(cls) \
    case cls::SHAPE_TYPE: \
        return cls::load(storage);


std::unique_ptr<CollisionShape>
CollisionShape::load(
    const StorageContainer& storage
) {
    ShapeType type = static_cast<ShapeType>(
        storage.get<uint8_t>("shapeType", EMPTY_SHAPE)
    );
    switch (type) {
        SHAPE_TYPE_CASE(EmptyShape)
        SHAPE_TYPE_CASE(BoxShape)
        SHAPE_TYPE_CASE(CapsuleShape)
        SHAPE_TYPE_CASE(CompoundShape)
        SHAPE_TYPE_CASE(ConeShape)
        SHAPE_TYPE_CASE(CylinderShape)
        SHAPE_TYPE_CASE(SphereShape)
        default:
            return make_unique<EmptyShape>();
    }
}


luabind::scope
CollisionShape::luaBindings() {
    using namespace luabind;
    return class_<CollisionShape, std::shared_ptr<CollisionShape>>("CollisionShape")
        .enum_("Axis") [
            value("AXIS_X", CollisionShape::AXIS_X),
            value("AXIS_Y", CollisionShape::AXIS_Y),
            value("AXIS_Z", CollisionShape::AXIS_Z)
        ]
    ;
}


CollisionShape::~CollisionShape() {}


StorageContainer
CollisionShape::storage() const {
    StorageContainer storage;
    storage.set<uint8_t>("shapeType", this->shapeType());
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// BoxShape
////////////////////////////////////////////////////////////////////////////////

std::unique_ptr<BoxShape>
BoxShape::load(
    const StorageContainer& storage
) {
    Ogre::Vector3 extents = storage.get<Ogre::Vector3>(
        "extents",
        Ogre::Vector3(1,1,1)
    );
    return make_unique<BoxShape>(extents);
}


luabind::scope
BoxShape::luaBindings() {
    using namespace luabind;
    return class_<BoxShape, CollisionShape, std::shared_ptr<CollisionShape>>("BoxShape")
        .def(constructor<Ogre::Vector3>())
    ;
}


BoxShape::BoxShape(
    const Ogre::Vector3& extents
) : m_bulletShape(new btBoxShape(ogreToBullet(extents))),
    m_extents(extents)
{
}


StorageContainer
BoxShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    storage.set<Ogre::Vector3>("extents", m_extents);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// CapsuleShape
////////////////////////////////////////////////////////////////////////////////

std::unique_ptr<CapsuleShape>
CapsuleShape::load(
    const StorageContainer& storage
) {
    CollisionShape::Axis axis = static_cast<CollisionShape::Axis>(
        storage.get<uint8_t>(
            "axis", 
            CollisionShape::AXIS_X
        )
    );
    btScalar radius = storage.get<btScalar>("radius", 1.0f);
    btScalar height = storage.get<btScalar>("height", 1.0f);
    return make_unique<CapsuleShape>(axis, radius, height);
}


luabind::scope
CapsuleShape::luaBindings() {
    using namespace luabind;
    return class_<CapsuleShape, CollisionShape, std::shared_ptr<CollisionShape>>("CapsuleShape")
        .def(constructor<CollisionShape::Axis, btScalar, btScalar>())
    ;
}


CapsuleShape::CapsuleShape(
    CollisionShape::Axis axis,
    btScalar radius,
    btScalar height
) : m_axis(axis),
    m_height(height),
    m_radius(radius)
{
    switch(axis) {
        case CollisionShape::AXIS_X:
            m_bulletShape.reset(new btCapsuleShapeX(radius, height));
            break;
        default:
        case CollisionShape::AXIS_Y:
            m_bulletShape.reset(new btCapsuleShape(radius, height));
            break;
        case CollisionShape::AXIS_Z:
            m_bulletShape.reset(new btCapsuleShapeZ(radius, height));
            break;
    }
}


StorageContainer
CapsuleShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    storage.set<uint8_t>("axis", m_axis);
    storage.set<btScalar>("height", m_height);
    storage.set<btScalar>("radius", m_radius);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// CompoundShape
////////////////////////////////////////////////////////////////////////////////

std::unique_ptr<CompoundShape>
CompoundShape::load(
    const StorageContainer& storage
) {
    auto shape = make_unique<CompoundShape>();
    StorageList childShapes = storage.get<StorageList>("childShapes", StorageList());
    for (const StorageContainer& childStorage : childShapes) {
        Ogre::Vector3 translation = childStorage.get<Ogre::Vector3>(
            "compoundTranslation",
            Ogre::Vector3::ZERO
        );
        Ogre::Quaternion rotation = childStorage.get<Ogre::Quaternion>(
            "compoundRotation",
            Ogre::Quaternion::IDENTITY
        );
        shape->addChildShape(
            translation,
            rotation,
            CollisionShape::load(childStorage)
        );
    }
    return shape;
}


luabind::scope
CompoundShape::luaBindings() {
    using namespace luabind;
    return class_<CompoundShape, CollisionShape, std::shared_ptr<CollisionShape>>("CompoundShape")
        .def(constructor<>())
        .def("addChildShape", &CompoundShape::addChildShape)
        .def("clear", &CompoundShape::clear)
        .def("removeChildShape", &CompoundShape::removeChildShape)
    ;
}


CompoundShape::CompoundShape() 
  : m_bulletShape(new btCompoundShape())
{
}


void
CompoundShape::addChildShape(
    const Ogre::Vector3& translation,
    const Ogre::Quaternion& rotation,
    std::shared_ptr<CollisionShape> shape
) {
    btTransform transform(
        ogreToBullet(rotation),
        ogreToBullet(translation)
    );
    m_bulletShape->addChildShape(transform, shape->bulletShape());
    m_childShapes.emplace_back(ChildShape{
        translation,
        rotation,
        shape
    });
}


void
CompoundShape::clear() {
    for (const auto& childShape : m_childShapes) {
        m_bulletShape->removeChildShape(
            childShape.shape->bulletShape()
        );
    }
    m_childShapes.clear();
}


void
CompoundShape::removeChildShape(
    const CollisionShape::Ptr& shape
) {
    m_bulletShape->removeChildShape(shape->bulletShape());
    auto iter = m_childShapes.begin();
    while (iter != m_childShapes.end()) {
        if (iter->shape == shape) {
            iter = m_childShapes.erase(iter);
        }
        else {
            ++iter;
        }
    }

}


StorageContainer
CompoundShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    StorageList childShapes;
    childShapes.reserve(m_childShapes.size());
    for (const auto& childShape : m_childShapes) {
        StorageContainer childStorage = childShape.shape->storage();
        childStorage.set<Ogre::Vector3>(
            "compoundTranslation", 
            childShape.translation
        );
        childStorage.set<Ogre::Quaternion>(
            "compoundRotation", 
            childShape.rotation
        );
        childShapes.push_back(childStorage);
    }
    storage.set<StorageList>("childShapes", childShapes);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// ConeShape
////////////////////////////////////////////////////////////////////////////////

std::unique_ptr<ConeShape>
ConeShape::load(
    const StorageContainer& storage
) {
    CollisionShape::Axis axis = static_cast<CollisionShape::Axis>(
        storage.get<uint8_t>(
            "axis", 
            CollisionShape::AXIS_X
        )
    );
    btScalar radius = storage.get<btScalar>("radius", 1.0f);
    btScalar height = storage.get<btScalar>("height", 1.0f);
    return make_unique<ConeShape>(axis, radius, height);
}


luabind::scope
ConeShape::luaBindings() {
    using namespace luabind;
    return class_<ConeShape, CollisionShape, std::shared_ptr<CollisionShape>>("ConeShape")
        .def(constructor<CollisionShape::Axis, btScalar, btScalar>())
    ;
}


ConeShape::ConeShape(
    CollisionShape::Axis axis,
    btScalar radius,
    btScalar height
) : m_axis(axis),
    m_height(height),
    m_radius(radius)
{
    switch(axis) {
        case CollisionShape::AXIS_X:
            m_bulletShape.reset(new btConeShapeX(radius, height));
            break;
        default:
        case CollisionShape::AXIS_Y:
            m_bulletShape.reset(new btConeShape(radius, height));
            break;
        case CollisionShape::AXIS_Z:
            m_bulletShape.reset(new btConeShapeZ(radius, height));
            break;
    }
}


StorageContainer
ConeShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    storage.set<uint8_t>("axis", m_axis);
    storage.set<btScalar>("height", m_height);
    storage.set<btScalar>("radius", m_radius);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// CylinderShape
////////////////////////////////////////////////////////////////////////////////

std::unique_ptr<CylinderShape>
CylinderShape::load(
    const StorageContainer& storage
) {
    CollisionShape::Axis axis = static_cast<CollisionShape::Axis>(
        storage.get<uint8_t>(
            "axis", 
            CollisionShape::AXIS_X
        )
    );
    btScalar radius = storage.get<btScalar>("radius", 1.0f);
    btScalar height = storage.get<btScalar>("height", 1.0f);
    return make_unique<CylinderShape>(axis, radius, height);
}


luabind::scope
CylinderShape::luaBindings() {
    using namespace luabind;
    return class_<CylinderShape, CollisionShape, std::shared_ptr<CollisionShape>>("CylinderShape")
        .def(constructor<CollisionShape::Axis, btScalar, btScalar>())
    ;
}


CylinderShape::CylinderShape(
    CollisionShape::Axis axis,
    btScalar radius,
    btScalar height
) : m_axis(axis),
    m_height(height),
    m_radius(radius)
{
    switch(axis) {
        case CollisionShape::AXIS_X:
            m_bulletShape.reset(
                new btCylinderShapeX(btVector3(height*0.5, radius, radius))
            );
            break;
        default:
        case CollisionShape::AXIS_Y:
            m_bulletShape.reset(
                new btCylinderShape(btVector3(radius, height*0.5, radius))
            );
            break;
        case CollisionShape::AXIS_Z:
            m_bulletShape.reset(
                new btCylinderShapeZ(btVector3(radius, radius, height*0.5))
            );
            break;
    }
}


StorageContainer
CylinderShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    storage.set<uint8_t>("axis", m_axis);
    storage.set<btScalar>("height", m_height);
    storage.set<btScalar>("radius", m_radius);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// EmptyShape
////////////////////////////////////////////////////////////////////////////////

std::unique_ptr<EmptyShape>
EmptyShape::load(
    const StorageContainer&
) {
    return make_unique<EmptyShape>();
}


luabind::scope
EmptyShape::luaBindings() {
    using namespace luabind;
    return class_<EmptyShape, CollisionShape, std::shared_ptr<CollisionShape>>("EmptyShape")
        .def(constructor<>())
    ;
}


EmptyShape::EmptyShape() 
  : m_bulletShape(new btEmptyShape())
{
}


StorageContainer
EmptyShape::storage() const {
    return CollisionShape::storage();
}



////////////////////////////////////////////////////////////////////////////////
// SphereShape
////////////////////////////////////////////////////////////////////////////////

std::unique_ptr<SphereShape>
SphereShape::load(
    const StorageContainer& storage
) {
    btScalar radius = storage.get<btScalar>("radius", 1.0f);
    return make_unique<SphereShape>(radius);
}


luabind::scope
SphereShape::luaBindings() {
    using namespace luabind;
    return class_<SphereShape, CollisionShape, std::shared_ptr<CollisionShape>>("SphereShape")
        .def(constructor<btScalar>())
    ;
}


SphereShape::SphereShape(
    btScalar radius
) : m_bulletShape(new btSphereShape(radius)),
    m_radius(radius)
{
}


StorageContainer
SphereShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    storage.set<btScalar>("radius", m_radius);
    return storage;
}


