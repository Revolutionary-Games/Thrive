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


static void
RigidBodyComponent_touch(
    RigidBodyComponent* self
) {
    return self->m_staticProperties.touch();
}

static void
RigidBodyComponent_setDynamicProperties(
    RigidBodyComponent* self,
    Ogre::Vector3 position,
    Ogre::Quaternion rotation,
    Ogre::Vector3 linearVelocity,
    Ogre::Vector3 angularVelocity
) {
    auto& properties = self->m_dynamicInputProperties.workingCopy();
    properties.position = position;
    properties.rotation = rotation;
    properties.linearVelocity = linearVelocity;
    properties.angularVelocity = angularVelocity;
    self->m_dynamicInputProperties.touch();
}

static void
RigidBodyComponent_applyCentralImpulse(
    RigidBodyComponent* self,
    const Ogre::Vector3& impulse
) {
    self->m_impulseQueue.push(
        std::make_pair(impulse, Ogre::Vector3::ZERO)
    );
}

static void
RigidBodyComponent_applyImpulse(
    RigidBodyComponent* self,
    const Ogre::Vector3& impulse,
    const Ogre::Vector3& relativePosition
) {
    self->m_impulseQueue.push(
        std::make_pair(impulse, relativePosition)
    );
}

static void
RigidBodyComponent_setShape(
    RigidBodyComponent* self,
    btCollisionShape* shape
) {
    self->m_staticProperties.workingCopy().shape.reset(shape);
}

static RigidBodyComponent::StaticProperties&
RigidBodyComponent_getWorkingCopy(
    RigidBodyComponent* self
) {
    return self->m_staticProperties.workingCopy();
}

static const RigidBodyComponent::StaticProperties&
RigidBodyComponent_getLatest(
    RigidBodyComponent* self
) {
    return self->m_staticProperties.latest();
}


luabind::scope
RigidBodyComponent::luaBindings() {
    using namespace luabind;
    return class_<RigidBodyComponent, Component, std::shared_ptr<Component>>("RigidBodyComponent")
        .scope [
            def("TYPE_NAME", &RigidBodyComponent::TYPE_NAME),
            def("TYPE_ID", &RigidBodyComponent::TYPE_ID),
            class_<StaticProperties>("StaticProperties")
                //.def_readwrite("shape", &StaticProperties::shape)
                .def_readwrite("restitution", &StaticProperties::restitution)
                .def_readwrite("linearFactor", &StaticProperties::linearFactor)
                .def_readwrite("angularFactor", &StaticProperties::angularFactor)
                .def_readwrite("linearDamping", &StaticProperties::linearDamping)
                .def_readwrite("angularDamping", &StaticProperties::angularDamping)
                .def_readwrite("mass", &StaticProperties::mass)
                .def_readwrite("friction", &StaticProperties::friction)
                .def_readwrite("rollingFriction", &StaticProperties::rollingFriction)
                .def_readwrite("forceApplied", &StaticProperties::forceApplied)
        ]
        .def(constructor<>())
        .property("latest", RigidBodyComponent_getLatest)
        .property("workingCopy", RigidBodyComponent_getWorkingCopy)
        .def("touch", RigidBodyComponent_touch)
        .def("setDynamicProperties", RigidBodyComponent_setDynamicProperties)
        .def("applyImpulse",RigidBodyComponent_applyImpulse)
        .def("applyCentralImpulse",RigidBodyComponent_applyCentralImpulse)
        .def("setShape",RigidBodyComponent_setShape)
    ;
}


void
RigidBodyComponent::getWorldTransform(
    btTransform& transform
) const {
    const auto& properties = m_dynamicInputProperties.stable();
    transform.setOrigin(
        ogreToBullet(properties.position)
    );
    transform.setRotation(
        ogreToBullet(properties.rotation)
    );

}


void
RigidBodyComponent::setWorldTransform(
    const btTransform& transform
) {
    auto& properties = m_dynamicOutputProperties.workingCopy();
    properties.position = bulletToOgre(transform.getOrigin());
    properties.rotation = bulletToOgre(transform.getRotation());
    m_dynamicOutputProperties.touch();
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
    m_impl->m_entities.setEngine(engine);
}


void
RigidBodyInputSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    m_impl->m_world = nullptr;
    System::shutdown();
}


void
RigidBodyInputSystem::update(int milliseconds) {
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        RigidBodyComponent* rigidBodyComponent = std::get<0>(added.second);
        const auto& staticProperties = rigidBodyComponent->m_staticProperties.stable();
        btVector3 localInertia = staticProperties.localInertia;
        staticProperties.shape->calculateLocalInertia(staticProperties.mass,localInertia);
        btRigidBody::btRigidBodyConstructionInfo rigidBodyCI(
            staticProperties.mass,
            rigidBodyComponent,
            staticProperties.shape.get(),
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
        if (rigidBodyComponent->m_staticProperties.hasChanges()) {
            const auto& properties = rigidBodyComponent->m_staticProperties.stable();
            btVector3 localInertia = properties.localInertia;
            properties.shape->calculateLocalInertia(properties.mass,localInertia);
            body->setMassProps(properties.mass, localInertia);
            body->setLinearFactor(ogreToBullet(properties.linearFactor));
            body->setAngularFactor(ogreToBullet(properties.angularFactor));
            body->setDamping(properties.linearDamping, properties.angularDamping);
            body->setRestitution(properties.restitution);
            body->setCollisionShape(properties.shape.get());
            body->setFriction(properties.friction);
            body->setRollingFriction(properties.rollingFriction);
            rigidBodyComponent->m_staticProperties.untouch();
        }
        if (rigidBodyComponent->m_dynamicInputProperties.hasChanges()) {
            const auto& properties = rigidBodyComponent->m_dynamicInputProperties.stable();
            btTransform transform;
            transform.setIdentity();
            transform.setOrigin(ogreToBullet(properties.position));
            transform.setRotation(ogreToBullet(properties.rotation));
            body->setWorldTransform(transform);
            body->setLinearVelocity(ogreToBullet(properties.linearVelocity));
            body->setAngularVelocity(ogreToBullet(properties.angularVelocity));
            rigidBodyComponent->m_dynamicInputProperties.untouch();
        }
        for (const auto& impulsePair : rigidBodyComponent->m_impulseQueue) {
            body->applyImpulse(
                ogreToBullet(impulsePair.first),
                ogreToBullet(impulsePair.second)
            );
            body->activate();
        }
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
    m_impl->m_entities.setEngine(engine);
}


void
RigidBodyOutputSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    System::shutdown();
}


void
RigidBodyOutputSystem::update(int) {
    for (auto& value : m_impl->m_entities.entities()) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        btRigidBody* rigidBody = rigidBodyComponent->m_body;
        auto& properties = rigidBodyComponent->m_dynamicOutputProperties.workingCopy();
        if (rigidBody->isActive()) {
            properties.linearVelocity = bulletToOgre(rigidBody->getLinearVelocity());
            properties.angularVelocity = bulletToOgre(rigidBody->getAngularVelocity());
            rigidBodyComponent->m_dynamicOutputProperties.touch();
        }
        else if (
            not properties.linearVelocity.isZeroLength()
            or not properties.angularVelocity.isZeroLength()
        ) {
            properties.linearVelocity = Ogre::Vector3::ZERO;
            properties.angularVelocity = Ogre::Vector3::ZERO;
            rigidBodyComponent->m_dynamicOutputProperties.touch();
        }
    }
}
