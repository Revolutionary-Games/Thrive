#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

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
    * @brief Properties
    */
    struct Transform : public Touchable {

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

    };

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreSceneNodeComponent()
    * - @link m_properties properties @endlink
    * - Properties
    *   - Properties::orientation
    *   - Properties::position
    *   - Properties::scale
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Transform
    */
    Transform
    m_transform;

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


