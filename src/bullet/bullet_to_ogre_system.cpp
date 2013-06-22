#include "bullet/bullet_to_ogre_system.h"

#include "bullet/rigid_body_system.h"
#include "engine/engine.h"
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
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
BulletToOgreSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
BulletToOgreSystem::update(int) {
    for (auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        auto& sceneNodeProperties = sceneNodeComponent->m_properties;
        auto& rigidBodyProperties = rigidBodyComponent->m_dynamicProperties;
        sceneNodeProperties.orientation = rigidBodyProperties.rotation;
        sceneNodeProperties.position = rigidBodyProperties.position;
        sceneNodeProperties.touch();
    }
}


