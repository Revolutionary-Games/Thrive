#include "bullet/rigid_body_system.h"

#include "bullet/bullet_ogre_conversion.h"
#include "engine/component_factory.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"
#include "engine/serialization.h"

#include <cmath>
#include <iostream>

using namespace thrive;

const int PI = 3.1416f;
const float PUSHBACK_DIST = 2.2f; //Used for incrementally pushing emissions out of the emitters body

////////////////////////////////////////////////////////////////////////////////
// RigidBodyComponent
////////////////////////////////////////////////////////////////////////////////


void
RigidBodyComponent::setDynamicProperties(
    const Ogre::Vector3& position,
    const Ogre::Quaternion& rotation,
    const Ogre::Vector3& linearVelocity,
    const Ogre::Vector3& angularVelocity
) {
    m_dynamicProperties.position = position;
    m_dynamicProperties.rotation = rotation;
    m_dynamicProperties.linearVelocity = linearVelocity;
    m_dynamicProperties.angularVelocity = angularVelocity;
    m_dynamicProperties.touch();
}

void
RigidBodyComponent::applyCentralImpulse(
    const Ogre::Vector3& impulse
) {
    this->applyImpulse(impulse, Ogre::Vector3::ZERO);
}

void
RigidBodyComponent::applyImpulse(
    const Ogre::Vector3& impulse,
    const Ogre::Vector3& relativePosition
) {
    m_impulseQueue.push_back(
        std::make_pair(impulse, relativePosition)
    );
}


void
RigidBodyComponent::applyTorque(
    const Ogre::Vector3& torque
) {
    m_torque += torque;
}


void
RigidBodyComponent::clearForces(){
    m_toClearForces = true;
}


void
RigidBodyComponent::disableCollisionsWith(
    EntityId other
) {
    m_entityToNoCollide = other;
}

void
RigidBodyComponent::reenableAllCollisions() {
    m_shouldReenableAllCollisions = true;
}

luabind::scope
RigidBodyComponent::luaBindings() {
    using namespace luabind;
    return class_<RigidBodyComponent, Component>("RigidBodyComponent")
        .enum_("ID") [
            value("TYPE_ID", RigidBodyComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &RigidBodyComponent::TYPE_NAME),
            class_<DynamicProperties, Touchable>("DynamicProperties")
                .def_readwrite("position", &DynamicProperties::position)
                .def_readwrite("rotation", &DynamicProperties::rotation)
                .def_readwrite("linearVelocity", &DynamicProperties::linearVelocity)
                .def_readwrite("angularVelocity", &DynamicProperties::angularVelocity),
            class_<Properties, Touchable>("Properties")
                .def_readwrite("shape", &Properties::shape)
                .def_readwrite("restitution", &Properties::restitution)
                .def_readwrite("linearFactor", &Properties::linearFactor)
                .def_readwrite("angularFactor", &Properties::angularFactor)
                .def_readwrite("linearDamping", &Properties::linearDamping)
                .def_readwrite("angularDamping", &Properties::angularDamping)
                .def_readwrite("mass", &Properties::mass)
                .def_readwrite("friction", &Properties::friction)
                .def_readwrite("rollingFriction", &Properties::rollingFriction)
                .def_readwrite("hasContactResponse", &Properties::hasContactResponse)
                .def_readwrite("kinematic", &Properties::kinematic)
        ]
        .def(constructor<>())
        .def("setDynamicProperties", &RigidBodyComponent::setDynamicProperties)
        .def("applyImpulse", &RigidBodyComponent::applyImpulse)
        .def("applyCentralImpulse", &RigidBodyComponent::applyCentralImpulse)
        .def("applyTorque", &RigidBodyComponent::applyTorque)
        .def("clearForces", &RigidBodyComponent::clearForces)
        .def("disableCollisionsWith", &RigidBodyComponent::disableCollisionsWith)
        .def("reenableAllCollisions", &RigidBodyComponent::reenableAllCollisions)
        .def_readonly("properties", &RigidBodyComponent::m_properties)
        .def_readonly("dynamicProperties", &RigidBodyComponent::m_dynamicProperties)
        .def_readwrite("pushbackEntity", &RigidBodyComponent::m_pushbackEntity)
        .def_readwrite("m_pushbackAngle", &RigidBodyComponent::m_pushbackAngle)
    ;
}


void
RigidBodyComponent::getWorldTransform(
    btTransform& transform
) const {
    transform.setOrigin(
        ogreToBullet(m_dynamicProperties.position)
    );
    transform.setRotation(
        ogreToBullet(m_dynamicProperties.rotation)
    );

}


void
RigidBodyComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    // Static
    m_properties.shape = CollisionShape::load(storage.get<StorageContainer>("shape", StorageContainer()));
    m_properties.restitution = storage.get<btScalar>("restitution", 0.0f);
    m_properties.linearFactor = storage.get<Ogre::Vector3>("linearFactor", Ogre::Vector3(1,1,1));
    m_properties.angularFactor = storage.get<Ogre::Vector3>("angularFactor", Ogre::Vector3(1,1,1));
    m_properties.mass = storage.get<btScalar>("mass", 1.0f);
    m_properties.friction = storage.get<btScalar>("friction", 0.0f);
    m_properties.linearDamping = storage.get<btScalar>("linearDamping", 0.0f);
    m_properties.angularDamping = storage.get<btScalar>("angularDamping", 0.0f);
    m_properties.rollingFriction = storage.get<btScalar>("rollingFriction", 0.0f);
    m_properties.hasContactResponse = storage.get<bool>("hasContactResponse", true);
    m_properties.kinematic = storage.get<bool>("kinematic", false);
    m_properties.touch();
    // Dynamic
    m_dynamicProperties.position = storage.get<Ogre::Vector3>("position", Ogre::Vector3::ZERO);
    m_dynamicProperties.rotation = storage.get<Ogre::Quaternion>("rotation", Ogre::Quaternion::IDENTITY);
    m_dynamicProperties.linearVelocity = storage.get<Ogre::Vector3>("linearVelocity", Ogre::Vector3::ZERO);
    m_dynamicProperties.angularVelocity = storage.get<Ogre::Vector3>("angularVelocity", Ogre::Vector3::ZERO);
}


void
RigidBodyComponent::setWorldTransform(
    const btTransform& transform
) {
    m_dynamicProperties.position = bulletToOgre(transform.getOrigin());
    m_dynamicProperties.rotation = bulletToOgre(transform.getRotation());
}


StorageContainer
RigidBodyComponent::storage() const {
    StorageContainer storage = Component::storage();
    // Static
    storage.set<StorageContainer>("shape", m_properties.shape->storage());
    storage.set<Ogre::Vector3>("linearFactor", m_properties.linearFactor);
    storage.set<Ogre::Vector3>("angularFactor", m_properties.angularFactor);
    storage.set<btScalar>("mass", m_properties.mass);
    storage.set<btScalar>("friction", m_properties.friction);
    storage.set<btScalar>("linearDamping", m_properties.linearDamping);
    storage.set<btScalar>("angularDamping", m_properties.angularDamping);
    storage.set<btScalar>("rollingFriction", m_properties.rollingFriction);
    storage.set<bool>("hasContactResponse", m_properties.hasContactResponse);
    storage.set<bool>("kinematic", m_properties.kinematic);
    // Dynamic
    storage.set<Ogre::Vector3>("position", m_dynamicProperties.position);
    storage.set<Ogre::Quaternion>("rotation", m_dynamicProperties.rotation);
    storage.set<Ogre::Vector3>("linearVelocity", m_dynamicProperties.linearVelocity);
    storage.set<Ogre::Vector3>("angularVelocity", m_dynamicProperties.angularVelocity);
    return storage;
}

REGISTER_COMPONENT(RigidBodyComponent)


////////////////////////////////////////////////////////////////////////////////
// RigidBodyInputSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
RigidBodyInputSystem::luaBindings() {
    using namespace luabind;
    return class_<RigidBodyInputSystem, System>("RigidBodyInputSystem")
        .def(constructor<>())
    ;
}


struct RigidBodyInputSystem::Implementation {

    EntityFilter<
        RigidBodyComponent
    > m_entities = {true};

    std::unordered_map<EntityId, std::unique_ptr<btRigidBody>> m_bodies;

    btDiscreteDynamicsWorld* m_world = nullptr;

    //Map links the dominating entity to a shared constraint
    std::map<EntityId, std::unique_ptr<btTypedConstraint>> m_activeConstraints;
    std::map<EntityId, EntityId> m_activeConstraintOtherEntity;
};


RigidBodyInputSystem::RigidBodyInputSystem()
  : m_impl(new Implementation())
{
}


RigidBodyInputSystem::~RigidBodyInputSystem() {}


void
RigidBodyInputSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    assert(m_impl->m_world == nullptr && "Double init of system");
    m_impl->m_world = gameState->physicsWorld();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
RigidBodyInputSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_world = nullptr;
    System::shutdown();
}



struct _ContactResultCallback  : public btCollisionWorld::ContactResultCallback
{
    bool collisionDetected = false;

    btScalar addSingleResult(
        btManifoldPoint&,
        const btCollisionObjectWrapper*,
        int ,
        int ,
        const btCollisionObjectWrapper*,
        int ,
        int)
    {
        collisionDetected = true;
        return 0.0f;
    }
};

struct DummyConstraint : public btTypedConstraint {

public:
    DummyConstraint(btRigidBody* body1, btRigidBody* body2)
        : btTypedConstraint(btTypedConstraintType::D6_CONSTRAINT_TYPE, *body1, *body2) //static_cast<btTypedConstraintType>(0xFF), *body1, *body2)
    {}

	void getInfo1 (btConstraintInfo1* info ){info->m_numConstraintRows = 0; info->nub = 0;}
	void getInfo2 (btConstraintInfo2* ){}
	void	setParam(int , btScalar , int) {}
    btScalar getParam(int, int ) const {return 1.0;}

};

void
RigidBodyInputSystem::update(int, int logicTime) {
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        btRigidBody* body = m_impl->m_bodies[entityId].get();
        auto it = m_impl->m_activeConstraints.find(entityId);
        if (it != m_impl->m_activeConstraints.end()) {
            btRigidBody* otherbody =  m_impl->m_bodies[m_impl->m_activeConstraintOtherEntity[entityId]].get();
            body->removeConstraintRef(it->second.get());
            otherbody->removeConstraintRef(it->second.get());
            m_impl->m_activeConstraints.erase(it);
            m_impl->m_activeConstraintOtherEntity.erase(entityId);
        }
        if (body) {
            m_impl->m_world->removeRigidBody(body);
        }
        m_impl->m_bodies.erase(entityId);
    }
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        RigidBodyComponent* rigidBodyComponent = std::get<0>(added.second);
        auto& properties = rigidBodyComponent->m_properties;
        btVector3 localInertia;
        properties.shape->bulletShape()->calculateLocalInertia(
            properties.mass,
            localInertia
        );
        btRigidBody::btRigidBodyConstructionInfo rigidBodyCI(
            properties.mass,
            rigidBodyComponent,
            properties.shape->bulletShape(),
            localInertia
        );
        std::unique_ptr<btRigidBody> rigidBody(new btRigidBody(rigidBodyCI));
        rigidBody->setUserPointer(reinterpret_cast<void*>(entityId));
        rigidBodyComponent->m_body = rigidBody.get();
        m_impl->m_world->addRigidBody(
            rigidBody.get(),
            rigidBodyComponent->m_collisionFilterGroup,
            rigidBodyComponent->m_collisionFilterMask
        );
        m_impl->m_bodies[entityId] = std::move(rigidBody);
    }
    m_impl->m_entities.clearChanges();
    for (const auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        btRigidBody* body = rigidBodyComponent->m_body;
        auto& properties = rigidBodyComponent->m_properties;
        if (properties.hasChanges()) {
            btVector3 localInertia;
            properties.shape->bulletShape()->calculateLocalInertia(
                properties.mass,
                localInertia
            );
            body->setMassProps(
                properties.mass,
                localInertia
            );
            body->setLinearFactor(ogreToBullet(properties.linearFactor));
            body->setAngularFactor(ogreToBullet(properties.angularFactor));
            body->setDamping(
                properties.linearDamping,
                properties.angularDamping
            );
            body->setRestitution(properties.restitution);
            body->setCollisionShape(properties.shape->bulletShape());
            body->setFriction(properties.friction);
            body->setRollingFriction(properties.rollingFriction);
            if (properties.hasContactResponse) {
                body->setCollisionFlags(
                    body->getCollisionFlags() & not btCollisionObject::CF_NO_CONTACT_RESPONSE
                );
            }
            else {
                body->setCollisionFlags(
                    body->getCollisionFlags() | btCollisionObject::CF_NO_CONTACT_RESPONSE
                );
            }
            if (properties.kinematic) {
                body->setCollisionFlags(
                    body->getCollisionFlags() | btCollisionObject::CF_KINEMATIC_OBJECT
                );
            }
            else {
                body->setCollisionFlags(
                    body->getCollisionFlags() & not btCollisionObject::CF_KINEMATIC_OBJECT
                );
            }
            properties.untouch();
        }
        auto& dynamicProperties = rigidBodyComponent->m_dynamicProperties;
        if (dynamicProperties.hasChanges()) {
            btTransform transform;
            rigidBodyComponent->getWorldTransform(transform);
            body->setWorldTransform(transform);
            body->setLinearVelocity(ogreToBullet(dynamicProperties.linearVelocity));
            body->setAngularVelocity(ogreToBullet(dynamicProperties.angularVelocity));
            dynamicProperties.untouch();
            body->activate();
        }
        for (const auto& impulsePair : rigidBodyComponent->m_impulseQueue) {
            body->applyImpulse(
                ogreToBullet(impulsePair.first),
                ogreToBullet(impulsePair.second)
            );
            body->activate();
        }
        rigidBodyComponent->m_impulseQueue.clear();

        if (not rigidBodyComponent->m_torque.isZeroLength()) {
            body->applyTorque(
                ogreToBullet(rigidBodyComponent->m_torque)
            );
            rigidBodyComponent->m_torque = Ogre::Vector3::ZERO;
            body->activate();
        }
        if(rigidBodyComponent->m_toClearForces == true){
            body->clearForces();
            body->setLinearVelocity(btVector3(0,0,0));
            body->setAngularVelocity(btVector3(0,0,0));
            rigidBodyComponent->m_toClearForces = false;
        }
        if(rigidBodyComponent->m_pushbackEntity != NULL_ENTITY) {
            //To debug this in the future, in the context of emitter components
            // set emission speeds on the emitter components to 0
            RigidBodyComponent* otherEntityBody =  m_impl->m_entities.entityManager()->getComponent<RigidBodyComponent>(rigidBodyComponent->m_pushbackEntity);
            auto callback = _ContactResultCallback();
            if (otherEntityBody) {
                m_impl->m_world->contactPairTest(body, otherEntityBody->m_body, callback);
                auto v = Ogre::Vector3(PUSHBACK_DIST*sin(rigidBodyComponent->m_pushbackAngle*(PI/180.0)),PUSHBACK_DIST*cos(rigidBodyComponent->m_pushbackAngle*(PI/180.0)),0);
                while(callback.collisionDetected)
                {
                    rigidBodyComponent->m_dynamicProperties.position = rigidBodyComponent->m_dynamicProperties.position + v;
                    btTransform transform;
                    rigidBodyComponent->getWorldTransform(transform);
                    body->setWorldTransform(transform);

                    callback.collisionDetected = false;
                    m_impl->m_world->contactPairTest(body, otherEntityBody->m_body, callback);
                }
            }
            rigidBodyComponent->m_pushbackEntity = NULL_ENTITY;
        }
        if (rigidBodyComponent->m_shouldReenableAllCollisions) {
            //As implemented right now there can be only one nocollideentity
            if (m_impl->m_activeConstraints.find(value.first) != m_impl->m_activeConstraints.end()) {
                btRigidBody* otherbody =  m_impl->m_bodies[m_impl->m_activeConstraintOtherEntity[value.first]].get();
                body->removeConstraintRef(m_impl->m_activeConstraints[value.first].get());

                if (otherbody) {
                    otherbody->removeConstraintRef(m_impl->m_activeConstraints[value.first].get());
                }
                m_impl->m_activeConstraints.erase(value.first);
                m_impl->m_activeConstraintOtherEntity.erase(value.first);
            }
            rigidBodyComponent->m_shouldReenableAllCollisions = false;
        }
        if (rigidBodyComponent->m_entityToNoCollide != NULL_ENTITY)
        {
            btRigidBody* otherbody =  m_impl->m_bodies[rigidBodyComponent->m_entityToNoCollide].get();
            //If both entities exist and we aren't activating a collision with the same entity again
            if (otherbody ) {
                std::unique_ptr<btTypedConstraint> constraint = make_unique<DummyConstraint>(otherbody, body);
                otherbody->addConstraintRef(constraint.get());
                body->addConstraintRef(constraint.get());
                m_impl->m_activeConstraints.insert( std::pair<EntityId, std::unique_ptr<btTypedConstraint>>(value.first, std::move(constraint)));
                m_impl->m_activeConstraintOtherEntity.insert( std::pair<EntityId,EntityId>(value.first, rigidBodyComponent->m_entityToNoCollide));
            }
            rigidBodyComponent->m_entityToNoCollide  = NULL_ENTITY;
        }

        body->applyDamping(logicTime / 1000.0f);
    }
}

////////////////////////////////////////////////////////////////////////////////
// RigidBodyOutputSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
RigidBodyOutputSystem::luaBindings() {
    using namespace luabind;
    return class_<RigidBodyOutputSystem, System>("RigidBodyOutputSystem")
        .def(constructor<>())
    ;
}


struct RigidBodyOutputSystem::Implementation {

    EntityFilter<
        RigidBodyComponent
    > m_entities;
};


RigidBodyOutputSystem::RigidBodyOutputSystem()
  : m_impl(new Implementation())
{
}


RigidBodyOutputSystem::~RigidBodyOutputSystem() {}


void
RigidBodyOutputSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
RigidBodyOutputSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
RigidBodyOutputSystem::update(int, int logicTime) {
    for (auto& value : m_impl->m_entities.entities()) {
        if (logicTime > 0) {
            RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
            btRigidBody* rigidBody = rigidBodyComponent->m_body;
            auto& dynamicProperties = rigidBodyComponent->m_dynamicProperties;
            // Position and orientation are handled by RigidBodyComponent::setWorldTransform
            if (rigidBody->isActive()) {
                dynamicProperties.linearVelocity = bulletToOgre(rigidBody->getLinearVelocity());
                dynamicProperties.angularVelocity = bulletToOgre(rigidBody->getAngularVelocity());
            }
            else if (
                not dynamicProperties.linearVelocity.isZeroLength()
                or not dynamicProperties.angularVelocity.isZeroLength()
            ) {
                dynamicProperties.linearVelocity = Ogre::Vector3::ZERO;
                dynamicProperties.angularVelocity = Ogre::Vector3::ZERO;
            }
        }
    }
}
