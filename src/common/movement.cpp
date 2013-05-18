#include "common/movement.h"

#include "common/transform.h"
#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"

#include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// MovableComponent
////////////////////////////////////////////////////////////////////////////////


luabind::scope
MovableComponent::luaBindings() {
    using namespace luabind;
    return class_<MovableComponent, Component, std::shared_ptr<Component>>("MovableComponent")
        .scope [
            def("TYPE_NAME", &MovableComponent::TYPE_NAME),
            def("TYPE_ID", &MovableComponent::TYPE_ID)
        ]
        .def(constructor<>())
        .def_readwrite("velocity", &MovableComponent::m_velocity)
    ;
}

REGISTER_COMPONENT(MovableComponent)


////////////////////////////////////////////////////////////////////////////////
// MovementSystem
////////////////////////////////////////////////////////////////////////////////

struct MovementSystem::Implementation {

    EntityFilter<
        MovableComponent,
        TransformComponent
    > m_entities;
};


MovementSystem::MovementSystem()
  : m_impl(new Implementation())
{
}


MovementSystem::~MovementSystem() {}


void
MovementSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEngine(engine);
}


void
MovementSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    System::shutdown();
}


void
MovementSystem::update(int milliseconds) {
    for (auto& value : m_impl->m_entities.entities()) {
        MovableComponent* movable = std::get<0>(value.second);
        TransformComponent* transform = std::get<1>(value.second);
        if (not movable->m_velocity.isZeroLength()) {
            Ogre::Vector3 delta = movable->m_velocity * milliseconds / 1000.0;
            transform->m_properties.workingCopy().position += delta;
            transform->m_properties.touch();
        }
    }
}

////////////////////////////////////////////////////////////////////////////////
// TransformUpdateSystem
////////////////////////////////////////////////////////////////////////////////

struct TransformUpdateSystem::Implementation {

    EntityFilter<
        TransformComponent,
        PhysicsTransformComponent
    > m_entities;
};


TransformUpdateSystem::TransformUpdateSystem()
  : m_impl(new Implementation())
{
}


TransformUpdateSystem::~TransformUpdateSystem() {}


void
TransformUpdateSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEngine(engine);
}


void
TransformUpdateSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    System::shutdown();
}


void
TransformUpdateSystem::update(int) {
    for (auto& value : m_impl->m_entities.entities()) {
        TransformComponent* transform = std::get<0>(value.second);
        PhysicsTransformComponent* physicsTransform = std::get<1>(value.second);
        transform->m_properties.workingCopy().position = physicsTransform->m_properties.stable().position;
        //transform->m_properties.workingCopy().orientation = physicsTransform->m_properties.stable().rotation;
        transform->m_properties.workingCopy().velocity = physicsTransform->m_properties.stable().velocity;
        transform->m_properties.touch();

    }
}

