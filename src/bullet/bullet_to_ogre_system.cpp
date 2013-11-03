#include "bullet/bullet_to_ogre_system.h"

#include "bullet/rigid_body_system.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

using namespace thrive;


luabind::scope
BulletToOgreSystem::luaBindings() {
    using namespace luabind;
    return class_<BulletToOgreSystem, System>("BulletToOgreSystem")
        .def(constructor<>())
    ;
}


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
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
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
        auto& sceneNodeTransform = sceneNodeComponent->m_transform;
        auto& rigidBodyProperties = rigidBodyComponent->m_dynamicProperties;
        sceneNodeTransform.orientation = rigidBodyProperties.rotation;
        sceneNodeTransform.position = rigidBodyProperties.position;
        sceneNodeTransform.touch();
    }
}


