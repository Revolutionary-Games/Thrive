#include "microbe_stage/microbe_camera_system.h"

#include "ThriveGame.h"

#include "engine/player_data.h"

#include <Entities/GameWorld.h>
#include <Entities/Components.h>

using namespace thrive;

void
MicrobeCameraSystem::setCameraEntity(
    ObjectID id
) {
    m_cameraEntity = id; 
}

void
MicrobeCameraSystem::setCameraHeight(
    float height
) {
    m_cameraHeight = height;
}
// ------------------------------------ //
void
MicrobeCameraSystem::Run(
    Leviathan::GameWorld &world
) {
    if(m_cameraEntity == 0)
        return;
    
    // Get the entity the camera should follow
    auto controlledEntity = ThriveGame::Get()->playerData().activeCreature();

    try{

        const auto& playerPos = world.GetComponent<Leviathan::Position>(controlledEntity);

        auto& cameraPos = world.GetComponent<Leviathan::Position>(m_cameraEntity);

        auto targetPos = playerPos.Members._Position + Float3(0, m_cameraHeight, 0);

        if(cameraPos.Members._Position != targetPos){

            cameraPos.Members._Position = targetPos;
            cameraPos.Marked = true;
        }
        
    } catch(const Leviathan::NotFound &e){

        LOG_WARNING("MicrobeCameraSystem: failed to Run (missing component?) "
            "due to exception:");
        e.PrintToLog();
        return;
    }
}

