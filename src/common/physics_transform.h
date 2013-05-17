#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"

#include <btBulletDynamicsCommon.h>

namespace thrive {

/**
* @brief Holds position, scale and orientation
*/
class PhysicsTransformComponent : public Component {
    COMPONENT(PhysicsTransform)

public:

    /**
    * @brief Properties that are shared across threads
    */
    struct Properties {
        /**
        * @brief Orientation
        */
        btQuaternion orientation {0,0,0,1};

        /**
        * @brief Position
        *
        * Defaults to origin (0,0,0).
        */
        btVector3 position = {0, 0, 0};

        /**
        * @brief Velocity
        *
        * Defaults to 0 (0,0,0).
        */
        btVector3 velocity = {0, 0, 0};
    };

    /**
    * @brief Lua bindings
    *
    * This component exposes the following \ref shared_data_lua shared properties:
    * \arg \c orientation (btQuaternion): The component's orientation
    * \arg \c position (btVector3): The component's position
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Shared properties
    */
    PhysicsOutputData<Properties>
    m_properties;


};

}
