#include "bullet/rigidBody_bindings.h"

#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "ogre/ogre_engine.h"
#include "ogre/scene_node_system.h"
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
    return self->m_properties.touch();
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
            class_<Properties>("Properties")
                .def_readwrite("linearVelocity", &Properties::linearVelocity)
                .def_readwrite("angularVelocity", &Properties::angularVelocity)
                .def_readwrite("restitution", &Properties::restitution)
                .def_readwrite("linearFactor", &Properties::linearFactor)
                .def_readwrite("angularFactor", &Properties::angularFactor)
                .def_readwrite("mass", &Properties::mass)
                .def_readwrite("friction", &Properties::friction)
                .def_readwrite("rollingFriction", &Properties::rollingFriction)
        ]
        .def(constructor<>())
        .property("latest", RigidBodyComponent_getLatest)
        .property("workingCopy", RigidBodyComponent_getWorkingCopy)
        .def("touch", RigidBodyComponent_touch)
    ;
}

REGISTER_COMPONENT(RigidBodyComponent)


////////////////////////////////////////////////////////////////////////////////
// RigidBodySystem
////////////////////////////////////////////////////////////////////////////////

struct RigidBodySystem::Implementation {

    EntityFilter<
        RigidBodyComponent
    > m_entities = {true};

    std::unordered_map<EntityId, btRigidBody*> m_bodies;

    btDiscreteDynamicsWorld* m_world = nullptr;

};


RigidBodySystem::RigidBodySystem()
  : m_impl(new Implementation())
{
}


RigidBodySystem::~RigidBodySystem() {}


void
RigidBodySystem::init(
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
RigidBodySystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
RigidBodySystem::update(int) {
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        RigidBodyComponent* rigidBodyComponent = std::get<0>(added.second);
        btRigidBodyConstructionInfo* rigidBodyCI = btRigidBody::btRigidBodyConstructionInfo::btRigidBodyConstructionInfo(
            rigidBodyComponent->m_properties.mass, btMotionState, rigidBodyComponent->m_properties.shape);
        btRigidBody* rigidBody = btRigidBody(rigidBodyCI);
        lightComponent->m_body = rigidBody;
        m_impl->m_bodies[entityId] = body;
        m_impl->m_world->addRigidBody(body);
    }
    for (const auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBody = std::get<0>(value.second);
        btRigidBody* body = rigidBodyComponent->m_body;
        const auto& properties = rigidBodyComponent->m_properties.stable();
        body->setMassProps(properties.mass, properties.inertia);
        body->setLinearVelocity(properties.linearVelocity);
        body->setAngularVelocity(properties.angularVelocity);
        body->setLinearFactor(properties.linearFactor);
        body->setAngularFactor(properties.angularFactor);
        body->setRestitution(properties.restitution);
        body->setColisionShape(properties.shape);
        body->setFriction(properties.friction);
        body->setRollingFriction(properties.friction);
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Light* light = m_impl->m_lights[entityId];
        m_impl->m_world->removeRigidBody(body);
        m_impl->m_bodies.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
}


