#include "microbe_stage/microbe_camera_system.h"

#include "ThriveGame.h"

#include "engine/player_data.h"

#include <Entities/Components.h>
#include <Entities/GameWorld.h>

using namespace thrive;

void
    MicrobeCameraSystem::setCameraEntity(ObjectID id)
{
    m_cameraEntity = id;
}

void
    MicrobeCameraSystem::setCameraHeight(float height)
{
    m_cameraHeight = height;
}

void
    MicrobeCameraSystem::changeCameraOffset(float amount)
{
    m_cameraHeight += amount;

    m_cameraHeight =
        std::clamp(m_cameraHeight, MIN_CAMERA_HEIGHT, MAX_CAMERA_HEIGHT);
}
// ------------------------------------ //
void
    MicrobeCameraSystem::Run(Leviathan::GameWorld& world)
{
    if(m_cameraEntity == 0)
        return;

    // Get the entity the camera should follow
    auto controlledEntity = ThriveGame::Get()->playerData().activeCreature();

    try {

        const auto& playerPos =
            world.GetComponent<Leviathan::Position>(controlledEntity);

        auto& cameraPos =
            world.GetComponent<Leviathan::Position>(m_cameraEntity);

        auto targetPos =
            playerPos.Members._Position + Float3(0, m_cameraHeight, 0);

        if(cameraPos.Members._Position != targetPos) {

            cameraPos.Members._Position =
                lerp(cameraPos.Members._Position, targetPos, CAMERA_FLOW);
            cameraPos.Marked = true;
        }

    } catch(const Leviathan::NotFound& e) {

        LOG_WARNING("MicrobeCameraSystem: failed to Run (missing component?) "
                    "due to exception:");
        e.PrintToLog();
        return;
    }
}
