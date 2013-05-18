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
        * @brief Rotation
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

        /**
        * @brief Velocity
        *
        * Defaults to (0,0,0).
        * Used only for doppler effect, render blur or similars
        */
        Ogre::Vector3 velocity = {0,0,0};
    };

    /**
    * @brief Lua bindings
    *
    * This component exposes the following \ref shared_data_lua shared properties:
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
        Ogre::Quaternion rotation {0,0,0,1};

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
