#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive {

/**
* @brief Holds position, scale and orientation
*/
class TransformComponent : public Component {
    COMPONENT(Transform)

public:

    /**
    * @brief Properties that are shared across threads
    */
    struct Properties {
        /**
        * @brief Orientation
        *
        * Defaults to Ogre::Quaternion::IDENTITY.
        */
        Ogre::Quaternion orientation = Ogre::Quaternion::IDENTITY;

        /**
        * @brief Position
        *
        * Defaults to origin (0,0,0).
        */
        Ogre::Vector3 position = {0, 0, 0};

        /**
        * @brief Scale
        *
        * Defaults to (1, 1, 1).
        */
        Ogre::Vector3 scale = {1, 1, 1};
    };

    /**
    * @brief Lua bindings
    *
    * This component exposes the following \ref shared_data shared properties:
    * \arg \c orientation (Ogre.Quaternion): The component's orientation
    * \arg \c position (Ogre.Vector3): The component's position
    * \arg \c scale (Ogre.Vector3): The component's scale
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Shared properties
    */
    RenderData<Properties>
    m_properties;

};

}
