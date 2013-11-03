#include "bullet/rigid_body_system.h"

#include "bullet/bullet_ogre_conversion.h"
#include "engine/component_factory.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"
#include "engine/serialization.h"

#include <iostream>

using namespace thrive;

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

luabind::scope
RigidBodyComponent::luaBindings() {
    using namespace luabind;
    return class_<RigidBodyComponent, Component>("RigidBodyComponent")
        .enum_("ID") [
            value("TYPE_ID", RigidBodyComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &RigidBodyComponent::TYPE_NAME),
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
        .def_readonly("properties", &RigidBodyComponent::m_properties)
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


void
RigidBodyInputSystem::update(int milliseconds) {
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        btRigidBody* body = m_impl->m_bodies[entityId].get();
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
        }
        body->applyDamping(milliseconds / 1000.0f);
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
RigidBodyOutputSystem::update(int) {
    for (auto& value : m_impl->m_entities.entities()) {
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
