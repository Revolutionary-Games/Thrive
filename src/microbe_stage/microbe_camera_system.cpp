#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/game_state.h"
#include "engine/player_data.h"
#include "microbe_stage/microbe_camera_system.h"
#include "ogre/camera_system.h"
#include "ogre/scene_node_system.h"
#include "scripting/luajit.h"

#include <string>

using namespace thrive;

void MicrobeCameraSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<MicrobeCameraSystem>( "MicrobeCameraSystem",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<System>(),

        "activate", &MicrobeCameraSystem::activate,

        "init", &MicrobeCameraSystem::init
    );
}

struct MicrobeCameraSystem::Implementation {
    OgreCameraComponent* camera = nullptr;
    OgreSceneNodeComponent* cameraSceneNode = nullptr;
};

MicrobeCameraSystem::MicrobeCameraSystem()
  : m_impl(new Implementation())
{
}

void
MicrobeCameraSystem::init(
    GameStateData* gameState
) {
    System::initNamed("MicrobeCameraSystem", gameState);
}

void
MicrobeCameraSystem::activate() {
    std::unique_ptr<Entity> cameraEntity(new Entity(MICROBE_CAMERA_NAME, gameState()));
    m_impl->camera = dynamic_cast<OgreCameraComponent*>(cameraEntity->getComponent(OgreCameraComponent::TYPE_ID));
    m_impl->cameraSceneNode = dynamic_cast<OgreSceneNodeComponent*>(cameraEntity->getComponent(OgreSceneNodeComponent::TYPE_ID));
    m_impl->camera->m_properties.offset = Ogre::Vector3(0, 0, INITIAL_CAMERA_HEIGHT);
    m_impl->camera->m_properties.touch();
}

void
MicrobeCameraSystem::update(int, int) {
    std::string playerName = gameState()->engine()->playerData().playerName();
    std::unique_ptr<Entity> playerEntity(new Entity(playerName, gameState()));
    OgreSceneNodeComponent* playerSceneNode = dynamic_cast<OgreSceneNodeComponent*>(playerEntity->getComponent(OgreSceneNodeComponent::TYPE_ID));
    m_impl->cameraSceneNode->m_transform.position = playerSceneNode->m_transform.position + m_impl->camera->m_properties.offset;
    m_impl->cameraSceneNode->m_transform.touch();
}
