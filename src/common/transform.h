#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"

#include <OgreVector3.h>
#include <OgreQuaternion.h>

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
        Ogre::Quaternion rotation = Ogre::Quaternion::IDENTITY;

        /**
        * @brief Position
        *
        * Defaults to origin (0,0,0).
        */
        Ogre::Vector3 position = {0, 0, 0};

        /**
        * @brief Velocity
        *
        * Defaults to 0 (0,0,0).
        */
        Ogre::Vector3 velocity = {0, 0, 0};
    };

    /**
    * @brief Lua bindings
    *
    * This component exposes the following \ref shared_data_lua shared properties:
    * \arg \c rotation (Ogre::Quaternion): The component's rotation
    * \arg \c position (Ogre::Vector3): The component's position
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
