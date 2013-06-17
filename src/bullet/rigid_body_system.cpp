#include "bullet/rigid_body_system.h"

#include "bullet/bullet_engine.h"
#include "bullet/bullet_ogre_conversion.h"
#include "engine/component_registry.h"
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
    this->position = position;
    this->rotation = rotation;
    this->linearVelocity = linearVelocity;
    this->angularVelocity = angularVelocity;
    m_dynamicPropertiesChanged = true;
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
            def("TYPE_ID", &RigidBodyComponent::TYPE_ID)
        ]
        .def(constructor<>())
        .def_readwrite("shape", &RigidBodyComponent::shape)
        .def_readwrite("restitution", &RigidBodyComponent::restitution)
        .def_readwrite("linearFactor", &RigidBodyComponent::linearFactor)
        .def_readwrite("angularFactor", &RigidBodyComponent::angularFactor)
        .def_readwrite("linearDamping", &RigidBodyComponent::linearDamping)
        .def_readwrite("angularDamping", &RigidBodyComponent::angularDamping)
        .def_readwrite("mass", &RigidBodyComponent::mass)
        .def_readwrite("friction", &RigidBodyComponent::friction)
        .def_readwrite("rollingFriction", &RigidBodyComponent::rollingFriction)
        .def_readwrite("forceApplied", &RigidBodyComponent::forceApplied)
        .def("setDynamicProperties", &RigidBodyComponent::setDynamicProperties)
        .def("applyImpulse", &RigidBodyComponent::applyImpulse)
        .def("applyCentralImpulse", &RigidBodyComponent::applyCentralImpulse)
    ;
}


void
RigidBodyComponent::getWorldTransform(
    btTransform& transform
) const {
    transform.setOrigin(
        ogreToBullet(this->position)
    );
    transform.setRotation(
        ogreToBullet(this->rotation)
    );

}


void
RigidBodyComponent::setWorldTransform(
    const btTransform& transform
) {
    this->position = bulletToOgre(transform.getOrigin());
    this->rotation = bulletToOgre(transform.getRotation());
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
    BulletEngine* bulletEngine = dynamic_cast<BulletEngine*>(engine);
    assert(bulletEngine != nullptr && "System requires a BulletEngine");
    m_impl->m_world = bulletEngine->world();
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
        btVector3 localInertia = rigidBodyComponent->localInertia;
        rigidBodyComponent->shape->calculateLocalInertia(
            rigidBodyComponent->mass,
            localInertia
        );
        btRigidBody::btRigidBodyConstructionInfo rigidBodyCI(
            rigidBodyComponent->mass,
            rigidBodyComponent,
            rigidBodyComponent->shape.get(),
            localInertia
        );
        std::unique_ptr<btRigidBody> rigidBody(new btRigidBody(rigidBodyCI));
        rigidBodyComponent->m_body = rigidBody.get();
        m_impl->m_world->addRigidBody(rigidBody.get());
        m_impl->m_bodies[entityId] = std::move(rigidBody);
    }
    for (const auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        btRigidBody* body = rigidBodyComponent->m_body;
        if (rigidBodyComponent->hasChanges()) {
            btVector3 localInertia = rigidBodyComponent->localInertia;
            rigidBodyComponent->shape->calculateLocalInertia(
                rigidBodyComponent->mass,
                localInertia
            );
            body->setMassProps(
                rigidBodyComponent->mass, 
                localInertia
            );
            body->setLinearFactor(ogreToBullet(rigidBodyComponent->linearFactor));
            body->setAngularFactor(ogreToBullet(rigidBodyComponent->angularFactor));
            body->setDamping(
                rigidBodyComponent->linearDamping, 
                rigidBodyComponent->angularDamping
            );
            body->setRestitution(rigidBodyComponent->restitution);
            body->setCollisionShape(rigidBodyComponent->shape.get());
            body->setFriction(rigidBodyComponent->friction);
            body->setRollingFriction(rigidBodyComponent->rollingFriction);
            rigidBodyComponent->untouch();
        }
        if (rigidBodyComponent->m_dynamicPropertiesChanged) {
            btTransform transform;
            rigidBodyComponent->getWorldTransform(transform);
            body->setWorldTransform(transform);
            body->setLinearVelocity(ogreToBullet(rigidBodyComponent->linearVelocity));
            body->setAngularVelocity(ogreToBullet(rigidBodyComponent->angularVelocity));
            rigidBodyComponent->m_dynamicPropertiesChanged = false;
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
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        btRigidBody* body = m_impl->m_bodies[entityId].get();
        m_impl->m_world->removeRigidBody(body);
        m_impl->m_bodies.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
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
        // Position and orientation are handled by RigidBodyComponent::setWorldTransform
        if (rigidBody->isActive()) {
            rigidBodyComponent->linearVelocity = bulletToOgre(rigidBody->getLinearVelocity());
            rigidBodyComponent->angularVelocity = bulletToOgre(rigidBody->getAngularVelocity());
        }
        else if (
            not rigidBodyComponent->linearVelocity.isZeroLength()
            or not rigidBodyComponent->angularVelocity.isZeroLength()
        ) {
            rigidBodyComponent->linearVelocity = Ogre::Vector3::ZERO;
            rigidBodyComponent->angularVelocity = Ogre::Vector3::ZERO;
        }
    }
}
