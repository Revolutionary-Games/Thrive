#include "bullet/collision_shape.h"

#include "bullet/bullet_ogre_conversion.h"
#include "engine/serialization.h"
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

void CollisionShape::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CollisionShape>("CollisionShape"

    );

    lua.new_enum("SHAPE_AXIS",
        "X", CollisionShape::AXIS_X,
        "Y", CollisionShape::AXIS_Y,
        "Z", CollisionShape::AXIS_Z
    );
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

/**
* @brief Loads the box shape
*
* @param storage
*
* @return 
*/
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


/**
* @brief Lua bindings
*
* - BoxShape::BoxShape()
*
* @return 
*/
void BoxShape::luaBindings(
    sol::state &lua
){
    lua.new_usertype<BoxShape>("BoxShape",

        sol::constructors<sol::types<Ogre::Vector3>>(),
        
        sol::base_classes, sol::bases<CollisionShape>()
    );
}

BoxShape::BoxShape(
    const Ogre::Vector3& extents
) : m_bulletShape(new btBoxShape(ogreToBullet(extents))),
    m_extents(extents)
{
}


/**
* @brief Serializes this box shape
*
* @return 
*/
StorageContainer
BoxShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    storage.set<Ogre::Vector3>("extents", m_extents);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// CapsuleShape
////////////////////////////////////////////////////////////////////////////////

/**
* @brief Loads a capsule shape
*
* @param storage
*
* @return 
*/
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


/**
* @brief Lua bindings
*
* - CapsuleShape::CapsuleShape()
*
* @return 
*/
void CapsuleShape::luaBindings(
    sol::state &lua
){    
    lua.new_usertype<CapsuleShape>("CapsuleShape",

        sol::constructors<sol::types<CollisionShape::Axis, btScalar, btScalar>>(),
        
        sol::base_classes, sol::bases<CollisionShape>()
    );
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


/**
* @brief Serializes this capsule shape
*
* @return 
*/
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

/**
* @brief Loads a compound shape
*
* @param storage
*
* @return 
*/
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


/**
* @brief Lua bindings
*
* - CompoundShape::CompoundShape()
* - CompoundShape::addChildShape()
* - CompoundShape::clear()
* - CompoundShape::removeChildShape()
*
* @return 
*/
void CompoundShape::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CompoundShape>("CompoundShape",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<CollisionShape>(),

        "addChildShape", &CompoundShape::addChildShape,
        "clear", &CompoundShape::clear,
        "removeChildShape", &CompoundShape::removeChildShape
    );
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


/**
* @brief Serializes this compound shape
*
* @return 
*/
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

/**
* @brief Loads a cone shape
*
* @param storage
*
* @return 
*/
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


/**
* @brief Lua bindings
*
* - ConeShape::ConeShape()
*
* @return 
*/
void ConeShape::luaBindings(
    sol::state &lua
){
    lua.new_usertype<ConeShape>("ConeShape",

        sol::constructors<sol::types<CollisionShape::Axis, btScalar, btScalar>>(),
        
        sol::base_classes, sol::bases<CollisionShape>()
    );
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


/**
* @brief Serializes this cone shape
*
* @return 
*/
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

/**
* @brief Loads a cylinder shape
*
* @param storage
*
* @return 
*/
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


/**
* @brief Lua bindings
*
* - CylinderShape::CylinderShape()
*
* @return 
*/
void CylinderShape::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CylinderShape>("CylinderShape",

        sol::constructors<sol::types<CollisionShape::Axis, btScalar, btScalar>>(),
        
        sol::base_classes, sol::bases<CollisionShape>()
    );
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


/**
* @brief Serializes this cylinder shape
*
* @return 
*/
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

/**
* @brief Loads an empty shape
*
* @return 
*/
std::unique_ptr<EmptyShape>
EmptyShape::load(
    const StorageContainer&
) {
    return make_unique<EmptyShape>();
}


/**
* @brief Lua bindings
*
* EmptyShape::EmptyShape()
*
* @return 
*/
void EmptyShape::luaBindings(
    sol::state &lua
){
    lua.new_usertype<EmptyShape>("EmptyShape",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<CollisionShape>()
    );
}

EmptyShape::EmptyShape() 
  : m_bulletShape(new btEmptyShape())
{
}


/**
* @brief Serializes this empty shape
*
* @return 
*/
StorageContainer
EmptyShape::storage() const {
    return CollisionShape::storage();
}



////////////////////////////////////////////////////////////////////////////////
// SphereShape
////////////////////////////////////////////////////////////////////////////////

/**
* @brief Loads a sphere shape
*
* @param storage
*
* @return 
*/
std::unique_ptr<SphereShape>
SphereShape::load(
    const StorageContainer& storage
) {
    btScalar radius = storage.get<btScalar>("radius", 1.0f);
    return make_unique<SphereShape>(radius);
}


/**
* @brief Lua bindings
*
* - SphereShape::SphereShape()
*
* @return 
*/
void SphereShape::luaBindings(
    sol::state &lua
){
    lua.new_usertype<SphereShape>("SphereShape",

        sol::constructors<sol::types<btScalar>>(),
        
        sol::base_classes, sol::bases<CollisionShape>()
    );
}

SphereShape::SphereShape(
    btScalar radius
) : m_bulletShape(new btSphereShape(radius)),
    m_radius(radius)
{
}


/**
* @brief Serializes this sphere shape
*
* @return 
*/
StorageContainer
SphereShape::storage() const {
    StorageContainer storage = CollisionShape::storage();
    storage.set<btScalar>("radius", m_radius);
    return storage;
}


