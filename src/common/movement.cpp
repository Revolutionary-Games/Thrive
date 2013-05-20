#include "common/movement.h"

#include "common/transform.h"
#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// TransformUpdateSystem
////////////////////////////////////////////////////////////////////////////////

struct TransformUpdateSystem::Implementation {

    EntityFilter<
        OgreSceneNodeComponent,
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
        PhysicsTransformComponent* physicsTransform = std::get<1>(value.second);
        if (not physicsTransform->m_properties.hasChanges()) {
            continue;
        }
        OgreSceneNodeComponent* ogreSceneNodeComponent = std::get<0>(value.second);
        const auto& physicsProperties = physicsTransform->m_properties.stable();
        auto& graphicsProperties = ogreSceneNodeComponent->m_properties.workingCopy();
        graphicsProperties.position = physicsProperties.position;
        //graphicsProperties.orientation = physicsProperties.orientation;
        graphicsProperties.velocity = physicsProperties.velocity;
        ogreSceneNodeComponent->m_properties.touch();
    }
}

