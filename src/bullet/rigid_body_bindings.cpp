#include "bullet/rigid_body_bindings.h"

#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "bullet/bullet_engine.h"
#include "scripting/luabind.h"

#include <iostream>

#include <OgreVector3.h>
#include <OgreQuaternion.h>

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
RigidBodyComponent_setDinamicProperties(
    RigidBodyComponent* self,
    Ogre::Vector3 position,
    Ogre::Quaternion rotation,
    Ogre::Vector3 linearVelocity,
    Ogre::Vector3 angularVelocity
) {
    self->m_dinamicProperties.position = position;
    self->m_dinamicProperties.rotation = rotation;
    self->m_dinamicProperties.linearVelocity = linearVelocity;
    self->m_dinamicProperties.angularVelocity = angularVelocity;
    return self->m_dinamicProperties.touch();
}


static RigidBodyComponent::Properties&
RigidBodyComponent_getWorkingCopy(
    RigidBodyComponent* self
) {
    return self->m_properties.workingCopy();
}


static const RigidBodyComponent::Properties&
RigidBodyComponent_getLatest(
    RigidBodyComponent* self
) {
    return self->m_properties.latest();
}


luabind::scope
RigidBodyComponent::luaBindings() {
    using namespace luabind;
    return class_<RigidBodyComponent, Component, std::shared_ptr<Component>>("RigidBodyComponent")
        .scope [
            def("TYPE_NAME", &RigidBodyComponent::TYPE_NAME),
            def("TYPE_ID", &RigidBodyComponent::TYPE_ID),
            class_<StaticProperties>("StaticProperties")
                .def_readwrite("shape", &StaticProperties::shape)
                /*.def_readwrite("linearVelocity", &Properties::linearVelocity)
                .def_readwrite("position", &Properties::position)
                .def_readwrite("rotation", &Properties::rotation)
                .def_readwrite("angularVelocity", &Properties::angularVelocity)*/
                .def_readwrite("restitution", &StaticProperties::restitution)
                .def_readwrite("linearFactor", &StaticProperties::linearFactor)
                .def_readwrite("angularFactor", &StaticProperties::angularFactor)
                .def_readwrite("mass", &StaticProperties::mass)
                .def_readwrite("comOffset", &StaticProperties::comOffset)
                .def_readwrite("friction", &StaticProperties::friction)
                .def_readwrite("rollingFriction", &StaticProperties::rollingFriction)
        ]
        .def(constructor<>())
        .property("latest", RigidBodyComponent_getLatest)
        .property("workingCopy", RigidBodyComponent_getWorkingCopy)
        .def("touch", RigidBodyComponent_touch)
        .def("setDinamicProperties", RigidBodyComponent_setDinamicProperties)
    ;
}

REGISTER_COMPONENT(RigidBodyComponent)


////////////////////////////////////////////////////////////////////////////////
// RigidBodyInputSystem
////////////////////////////////////////////////////////////////////////////////

struct RigidBodyInputSystem::Implementation {

    EntityFilter<
        RigidBodyComponent
    > m_entities = {true};

    std::unordered_map<EntityId, btRigidBody*> m_bodies;

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
RigidBodyInputSystem::update(int) {
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        RigidBodyComponent* rigidBodyComponent = std::get<0>(added.second);
        btDefaultMotionState* motionState =
                new btDefaultMotionState(btTransform(rigidBodyComponent->m_properties.stable().rotation,rigidBodyComponent->m_properties.stable().position),rigidBodyComponent->m_properties.stable().comOffset);
        btRigidBody::btRigidBodyConstructionInfo rigidBodyCI = btRigidBody::btRigidBodyConstructionInfo(
            rigidBodyComponent->m_properties.stable().mass, motionState, rigidBodyComponent->m_properties.stable().shape.get(),rigidBodyComponent->m_properties.stable().inertia);
        btRigidBody* rigidBody = new btRigidBody(rigidBodyCI);
        rigidBodyComponent->m_body = rigidBody;
        m_impl->m_bodies[entityId] = rigidBody;
        m_impl->m_world->addRigidBody(rigidBody);
    }
    for (const auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        if (rigidBodyComponent->m_properties.hasChanges()) {
            btRigidBody* body = rigidBodyComponent->m_body;
            const auto& properties = rigidBodyComponent->m_properties.stable();
            body->setMassProps(properties.mass, properties.inertia);
            body->setLinearVelocity(properties.linearVelocity);
            body->setAngularVelocity(properties.angularVelocity);
            body->setLinearFactor(properties.linearFactor);
            body->setAngularFactor(properties.angularFactor);
            body->setRestitution(properties.restitution);
            body->setCollisionShape(properties.shape.get());
            body->setFriction(properties.friction);
            body->setRollingFriction(properties.friction);
        }
        rigidBodyComponent->m_properties.untouch();
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        btRigidBody* body = m_impl->m_bodies[entityId];
        m_impl->m_world->removeRigidBody(body);
        m_impl->m_bodies.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
}


