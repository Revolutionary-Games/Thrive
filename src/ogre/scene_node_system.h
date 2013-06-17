#pragma once

#include "engine/component.h"
#include "engine/system.h"

#include <memory>
#include <OgreVector3.h>
#include <OgreQuaternion.h>


#include <iostream>

namespace luabind {
class scope;
}

namespace Ogre {
class SceneNode;
}

namespace thrive {

/**
* @brief A component for a Ogre scene nodes
*
*/
class OgreSceneNodeComponent : public Component {
    COMPONENT(OgreSceneNode)

public:

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
    */
    Ogre::Vector3 velocity = {0,0,0};

    /**
    * @brief Lua bindings
    *
    * This component exposes the following \ref shared_data_lua "shared properties":
    * - Properties::orientation
    * - Properties::posiiton
    * - Properties::scale
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Pointer to the underlying Ogre::SceneNode
    *
    */
    Ogre::SceneNode* m_sceneNode = nullptr;


};


/**
* @brief Creates scene nodes for new OgreSceneNodeComponents
*/
class OgreAddSceneNodeSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    OgreAddSceneNodeSystem();

    /**
    * @brief Destructor
    */
    ~OgreAddSceneNodeSystem();

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
    * @brief Adds new scene nodes
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};


/**
* @brief Removes scene nodes for removed OgreSceneNodeComponents
*/
class OgreRemoveSceneNodeSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    OgreRemoveSceneNodeSystem();

    /**
    * @brief Destructor
    */
    ~OgreRemoveSceneNodeSystem();

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
    * @brief Removes stale scene nodes
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

/**
* @brief Updates scene node transformations
*/
class OgreUpdateSceneNodeSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    OgreUpdateSceneNodeSystem();

    /**
    * @brief Destructor
    */
    ~OgreUpdateSceneNodeSystem();

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
    * @brief Updates the scene nodes
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}


