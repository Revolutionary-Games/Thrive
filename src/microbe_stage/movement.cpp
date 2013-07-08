#include "microbe_stage/movement.h"

#include "bullet/rigid_body_system.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"

#include <iostream>


using namespace thrive;

luabind::scope
MicrobeMovementComponent::luaBindings() {
    using namespace luabind;
    return class_<MicrobeMovementComponent, Component, std::shared_ptr<Component>>("MicrobeMovementComponent")
        .scope [
            def("TYPE_NAME", &MicrobeMovementComponent::TYPE_NAME),
            def("TYPE_ID", &MicrobeMovementComponent::TYPE_ID)
        ]
        .def(constructor<>())
        .def_readwrite("direction", &MicrobeMovementComponent::m_direction)
        .def_readwrite("force", &MicrobeMovementComponent::m_force)
    ;
}

struct MicrobeMovementSystem::Implementation {

    EntityFilter<
        MicrobeMovementComponent,
        RigidBodyComponent
    > m_entities;
};


MicrobeMovementSystem::MicrobeMovementSystem()
  : m_impl(new Implementation())
{
}


MicrobeMovementSystem::~MicrobeMovementSystem() {}


void
MicrobeMovementSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
MicrobeMovementSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
MicrobeMovementSystem::update(
    int milliseconds
) {
    for (auto& value : m_impl->m_entities) {
        MicrobeMovementComponent* movementComponent = std::get<0>(value.second);
        RigidBodyComponent* rigidBodyComponent = std::get<1>(value.second);
        if (not movementComponent->m_direction.isZeroLength()) {
            float impulseMagnitude = milliseconds * movementComponent->m_force;
            Ogre::Vector3 impulse = impulseMagnitude * movementComponent->m_direction;
            impulse.z = 0;
            rigidBodyComponent->applyCentralImpulse(impulse);
        }
    }
}

