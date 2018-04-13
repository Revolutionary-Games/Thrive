#pragma once

#include <Entities/EntityCommon.h>

#include <unordered_map>

#define MICROBE_CAMERA_NAME "camera"

#define INITIAL_CAMERA_HEIGHT 40

namespace Leviathan {
class GameWorld;
}

namespace thrive {

/**
* @brief Moves the camera entity to match position of the player microbe
*
* This is a frame render time system
*/
class MicrobeCameraSystem {
public:

    //! \brief Sets the entity that is the camera (must have a Camera component and a Position)
    //!
    //! Set to 0 to disable this system and stop moving the camera
    void
    setCameraEntity(
        ObjectID id
    );

    //! \brief Sets the zoom level of the camera
    void
    setCameraHeight(
        float height
    );

    //! \brief Automatically finds the player entity from the game
    //! state and moves m_cameraEntity there
    //! \todo Should this directly use the CellStageWorld or be templated to have more
    //! performant component lookups
    void Run(
        Leviathan::GameWorld &world
    );

private:

    ObjectID m_cameraEntity = 0;
    float m_cameraHeight = INITIAL_CAMERA_HEIGHT;
};
}
