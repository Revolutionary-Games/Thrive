#include "common/bullet_to_ogre_system.h"

#include "bullet/rigid_body_system.h"
#include "engine/entity_filter.h"
#include "ogre/scene_node_system.h"

using namespace thrive;

struct BulletToOgreSystem::Implementation {

    EntityFilter<
        RigidBodyComponent,
        OgreSceneNodeComponent
    > m_entities;
};


BulletToOgreSystem::BulletToOgreSystem()
  : m_impl(new Implementation())
{
}


BulletToOgreSystem::~BulletToOgreSystem() {}


void
BulletToOgreSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEngine(engine);
}


void
BulletToOgreSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    System::shutdown();
}


void
BulletToOgreSystem::update(int) {
    for (auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        if (not rigidBodyComponent->m_dynamicOutputProperties.hasChanges()) {
            continue;
        }
        const auto& physicsProperties = rigidBodyComponent->m_dynamicOutputProperties.stable();
        auto& ogreProperties = sceneNodeComponent->m_properties.workingCopy();
        ogreProperties.orientation = physicsProperties.rotation;
        ogreProperties.position = physicsProperties.position;
        sceneNodeComponent->m_properties.touch();
    }
}


