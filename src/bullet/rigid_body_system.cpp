#include "bullet/rigid_body_system.h"

#include "bullet/bullet_ogre_conversion.h"
#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"

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

luabind::scope
RigidBodyComponent::luaBindings() {
    using namespace luabind;
    return class_<RigidBodyComponent, Component, std::shared_ptr<Component>>("RigidBodyComponent")
        .scope [
            def("TYPE_NAME", &RigidBodyComponent::TYPE_NAME),
            def("TYPE_ID", &RigidBodyComponent::TYPE_ID),
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
                .def_readwrite("forceApplied", &Properties::forceApplied)
        ]
        .def(constructor<>())
        .def("setDynamicProperties", &RigidBodyComponent::setDynamicProperties)
        .def("applyImpulse", &RigidBodyComponent::applyImpulse)
        .def("applyCentralImpulse", &RigidBodyComponent::applyCentralImpulse)
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
RigidBodyComponent::setWorldTransform(
    const btTransform& transform
) {
    m_dynamicProperties.position = bulletToOgre(transform.getOrigin());
    m_dynamicProperties.rotation = bulletToOgre(transform.getRotation());
}

REGISTER_COMPONENT(RigidBodyComponent)


////////////////////////////////////////////////////////////////////////////////
// RigidBodyInputSystem
////////////////////////////////////////////////////////////////////////////////

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
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_world == nullptr && "Double init of system");
    m_impl->m_world = engine->physicsWorld();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
RigidBodyInputSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_world = nullptr;
    System::shutdown();
}


void
RigidBodyInputSystem::update(int milliseconds) {
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        RigidBodyComponent* rigidBodyComponent = std::get<0>(added.second);
        auto& properties = rigidBodyComponent->m_properties;
        btVector3 localInertia = properties.localInertia;
        properties.shape->calculateLocalInertia(
            properties.mass,
            localInertia
        );
        btRigidBody::btRigidBodyConstructionInfo rigidBodyCI(
            properties.mass,
            rigidBodyComponent,
            properties.shape.get(),
            localInertia
        );
        std::unique_ptr<btRigidBody> rigidBody(new btRigidBody(rigidBodyCI));
        rigidBodyComponent->m_body = rigidBody.get();
        m_impl->m_world->addRigidBody(rigidBody.get());
        m_impl->m_bodies[entityId] = std::move(rigidBody);
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        btRigidBody* body = m_impl->m_bodies[entityId].get();
        m_impl->m_world->removeRigidBody(body);
        m_impl->m_bodies.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
    for (const auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        btRigidBody* body = rigidBodyComponent->m_body;
        auto& properties = rigidBodyComponent->m_properties;
        if (properties.hasChanges()) {
            btVector3 localInertia = properties.localInertia;
            properties.shape->calculateLocalInertia(
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
            body->setCollisionShape(properties.shape.get());
            body->setFriction(properties.friction);
            body->setRollingFriction(properties.rollingFriction);
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
        body->applyDamping(milliseconds / 1000.0f);
    }
}

////////////////////////////////////////////////////////////////////////////////
// RigidBodyOutputSystem
////////////////////////////////////////////////////////////////////////////////

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
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
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
