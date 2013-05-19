#include "bullet/rigid_body_system.h"

#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "bullet/bullet_engine.h"
#include "scripting/luabind.h"
#include "common/transform.h"

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
RigidBodyComponent_setDynamicProperties(
    RigidBodyComponent* self,
    Ogre::Vector3 position,
    Ogre::Quaternion rotation,
    Ogre::Vector3 linearVelocity,
    Ogre::Vector3 angularVelocity
) {
    self->m_dynamicProperties.workingCopy().position = position;
    self->m_dynamicProperties.workingCopy().rotation = rotation;
    self->m_dynamicProperties.workingCopy().linearVelocity = linearVelocity;
    self->m_dynamicProperties.workingCopy().angularVelocity = angularVelocity;
    return self->m_dynamicProperties.touch();
}

static void
RigidBodyComponent_printPosition(
    RigidBodyComponent* self
) {
    btTransform t = btTransform::getIdentity();
    self->m_body->getMotionState()->getWorldTransform(t);
    Ogre::Vector3 p = btToOgVector3(t.getOrigin());
    std::printf("Position: x:%f y:%f z:%f\n",p.x,p.y,p.z);
}

static void
RigidBodyComponent_printVelocity(
    RigidBodyComponent* self
) {
    Ogre::Vector3 p = btToOgVector3(self->m_body->getLinearVelocity());
    std::printf("Velocity: x:%f y:%f z:%f\n",p.x,p.y,p.z);
}

static void
RigidBodyComponent_printForce(
    RigidBodyComponent* self
) {
    Ogre::Vector3 p = self->m_staticProperties.workingCopy().forceApplied;
    std::printf("Force: x:%f y:%f z:%f\n",p.x,p.y,p.z);
}

static void
RigidBodyComponent_addToForce(
    RigidBodyComponent* self,
    Ogre::Vector3 add
) {
    self->m_staticProperties.workingCopy().forceApplied+=add;
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
                .def_readwrite("forceApplied", &StaticProperties::forceApplied)
        ]
        .def(constructor<>())
        .property("latest", RigidBodyComponent_getLatest)
        .property("workingCopy", RigidBodyComponent_getWorkingCopy)
        .def("touch", RigidBodyComponent_touch)
        .def("setDynamicProperties", RigidBodyComponent_setDynamicProperties)
        .def("printPosition",RigidBodyComponent_printPosition)
        .def("printVelocity",RigidBodyComponent_printVelocity)
        .def("addToForce",RigidBodyComponent_addToForce)
        .def("printForce",RigidBodyComponent_printForce)
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
RigidBodyInputSystem::update(int milliseconds) {
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        RigidBodyComponent* rigidBodyComponent = std::get<0>(added.second);
        const auto& dynamicProperties = rigidBodyComponent->m_dynamicProperties.stable();
        const auto& staticProperties = rigidBodyComponent->m_staticProperties.stable();
        btDefaultMotionState* motionState = new btDefaultMotionState(
            btTransform(
                ogToBtQuaternion(dynamicProperties.rotation),
                ogToBtVector3(dynamicProperties.position)
            ),
            staticProperties.comOffset
        );
        btVector3 localInertia = staticProperties.localInertia;
        staticProperties.shape->calculateLocalInertia(staticProperties.mass,localInertia);
        //staticProperties.localInertia = btToOgVector3(localInertia);
        btRigidBody::btRigidBodyConstructionInfo rigidBodyCI(
            staticProperties.mass,
            motionState,
            staticProperties.shape.get(),
            localInertia
        );
        btRigidBody* rigidBody = new btRigidBody(rigidBodyCI);
        rigidBodyComponent->m_body = rigidBody;
        m_impl->m_bodies[entityId] = rigidBody;
        m_impl->m_world->addRigidBody(rigidBody);
    }
    for (const auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        btRigidBody* body = rigidBodyComponent->m_body;
        if (rigidBodyComponent->m_staticProperties.hasChanges()) {
            const auto& properties = rigidBodyComponent->m_staticProperties.stable();
            btVector3 localInertia = properties.localInertia;
            properties.shape->calculateLocalInertia(properties.mass,localInertia);
            body->setMassProps(properties.mass, localInertia);
            //staticProperties.localInertia = btToOgVector3(localInertia);
            body->setLinearFactor(ogToBtVector3(properties.linearFactor));
            body->setAngularFactor(ogToBtVector3(properties.angularFactor));
            body->setDamping(properties.linearDamping,properties.angularDamping);
            body->setRestitution(properties.restitution);
            body->setCollisionShape(properties.shape.get());
            body->setFriction(properties.friction);
            body->setRollingFriction(properties.rollingFriction);
            //body->clearForces();

            if(!body->isActive()){
               body->activate();
            }
            rigidBodyComponent->m_staticProperties.untouch();
        }
        if (rigidBodyComponent->m_dynamicProperties.hasChanges()) {

            const auto& properties = rigidBodyComponent->m_dynamicProperties.stable();
            btTransform transform;
            transform.setIdentity();
            transform.setOrigin(ogToBtVector3(properties.position));
            transform.setRotation(ogToBtQuaternion(properties.rotation));
            body->setWorldTransform(transform);
            body->setLinearVelocity(ogToBtVector3(properties.linearVelocity));
            body->setAngularVelocity(ogToBtVector3(properties.angularVelocity));
            if(!body->isActive()){
               body->activate();
            }
            rigidBodyComponent->m_dynamicProperties.untouch();
        }
        if(!body->isActive()){
               body->activate();
            }
        body->applyCentralForce(ogToBtVector3(rigidBodyComponent->m_staticProperties.stable().forceApplied));
        body->applyDamping(milliseconds/1000);
        }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        btRigidBody* body = m_impl->m_bodies[entityId];
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
        RigidBodyComponent,
        PhysicsTransformComponent
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
        PhysicsTransformComponent* transform = std::get<1>(value.second);
        btRigidBody* rigidBody = rigidBodyComponent->m_body;
        btTransform trans = rigidBody->getWorldTransform();
        btVector3 position = trans.getOrigin();
        btQuaternion rotation = trans.getRotation();
        btVector3 velocity = rigidBody->getLinearVelocity();
        transform->m_properties.workingCopy().position = Ogre::Vector3(position.x(),position.y(),position.z());
        transform->m_properties.workingCopy().rotation = Ogre::Quaternion(rotation.w(),rotation.x(),rotation.y(),rotation.z());
        transform->m_properties.workingCopy().velocity = Ogre::Vector3(velocity.x(),velocity.y(),velocity.z());
        transform->m_properties.touch();
    }
}
