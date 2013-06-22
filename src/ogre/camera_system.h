#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

#include <memory>
#include <OgreCommon.h>
#include <OgreMath.h>

namespace luabind {
class scope;
}

namespace Ogre {
class Camera;
}

namespace thrive {

/**
* @brief A component for a camera
*
*/
class OgreCameraComponent : public Component {
    COMPONENT(OgreCamera)

public:


    /**
    * @brief Properties
    */
    struct Properties : public Touchable {

        /**
        * @brief Aspect ratio of the frustum viewport
        */
        Ogre::Real aspectRatio = 1.3333f;

        /**
        * @brief Far clip distance
        */
        Ogre::Real farClipDistance = 10000.0f;

        /**
        * @brief The y-dimension field of view
        */
        Ogre::Radian fovY = Ogre::Radian{45.0f};

        /**
        * @brief Near clip distance
        */
        Ogre::Real nearClipDistance = 100.0f;

        /**
        * @brief The level of rendering detail
        */
        Ogre::PolygonMode polygonMode = Ogre::PM_SOLID;
    };

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreCameraComponent(std::string)
    * - Properties
    *   - Properties::aspectRatio
    *   - Properties::farClipDistance
    *   - Properties::fovY
    *   - Properties::nearClipDistance
    *   - Properties::polygonMode
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    *
    * @param name
    *   The camera's name
    */
    OgreCameraComponent(
        std::string name
    );

    /**
    * @brief Pointer to internal camera
    */
    Ogre::Camera* m_camera = nullptr;

    /**
    * @brief The camera's name
    */
    const std::string m_name;

    /**
    * @brief Properties
    */
    Properties
    m_properties;

};


/**
* @brief Creates, updates and removes cameras
*/
class OgreCameraSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    OgreCameraSystem();

    /**
    * @brief Destructor
    */
    ~OgreCameraSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

