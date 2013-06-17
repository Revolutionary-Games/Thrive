#pragma once

#include "engine/component.h"
#include "engine/system.h"

#include <memory>
#include <OgrePlane.h>
#include <OgreResourceGroupManager.h>

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief A component for a sky plane
*
* Usually, only one entity should have a sky plane component
*/
class SkyPlaneComponent : public Component {
    COMPONENT(SkyPlane)

public:

    /**
    * @brief Whether this sky plane is enabled
    */
    bool enabled = true;
    /**
    * @brief The sky plane's plane (normal and distance from camera)
    */
    Ogre::Plane plane = {1, 1, 1, 1};
    /**
    * @brief The material path
    */
    Ogre::String materialName = "Background/Blue1";
    /**
    * @brief The bigger the scale, the bigger the sky plane
    */
    Ogre::Real scale = 1000;
    /**
    * @brief How often to repeat the material across the plane
    */
    Ogre::Real tiling = 10;
    /**
    * @brief Whether to draw the plane before everything else
    */
    bool drawFirst = true;
    /**
    * @brief If zero, the plane will be flat. Above zero, the plane will
    * be slightly curved
    */
    Ogre::Real bow = 0;
    /**
    * @brief Segments for bowed plane
    */
    int xsegments = 1;
    /**
    * @brief Segments for bowed plane
    */
    int ysegments = 1;
    /**
    * @brief The resource group to which to assign the plane mesh
    */
    Ogre::String groupName = Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME;

    /**
    * @brief Lua bindings
    *
    * Exposes the following \ref shared_data_lua "shared properties":
    * - \c SkyPlaneComponent::enabled
    * - \c SkyPlaneComponent::plane
    * - \c SkyPlaneComponent::materialName
    * - \c SkyPlaneComponent::scale
    * - \c SkyPlaneComponent::tiling
    * - \c SkyPlaneComponent::drawFirst
    * - \c SkyPlaneComponent::bow
    * - \c SkyPlaneComponent::xsegments
    * - \c SkyPlaneComponent::ysegments
    * - \c SkyPlaneComponent::groupName
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

};


/**
* @brief Handles sky planes, boxes and domes
*/
class SkySystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    SkySystem();

    /**
    * @brief Destructor
    */
    ~SkySystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    *   Must be an OgreEngine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the sky components
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
