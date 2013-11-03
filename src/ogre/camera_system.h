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
        * @brief Far clip distance
        */
        Ogre::Real farClipDistance = 10000.0f;

        /**
        * @brief The y-dimension field of view
        */
        Ogre::Degree fovY = Ogre::Degree{45.0f};

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
    * @brief Default constructor
    *
    * Should only be used for loading
    */
    OgreCameraComponent();

    /**
    * @brief Loads a camera
    *
    * @param storage
    */
    void
    load(
        const StorageContainer& storage
    ) override;

    /**
    * @brief Returns the camera's name
    *
    */
    std::string
    name() const;

    /**
    * @brief Serializes the camera
    *
    * @return 
    */
    StorageContainer
    storage() const override;

    /**
    * @brief Pointer to internal camera
    */
    Ogre::Camera* m_camera = nullptr;

    /**
    * @brief Properties
    */
    Properties
    m_properties;

private:

    /**
    * @brief The camera's name
    */
    std::string m_name;

};


/**
* @brief Creates, updates and removes cameras
*/
class OgreCameraSystem : public System {
    
public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreCameraSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

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
    */
    void init(GameState* gameState) override;

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

