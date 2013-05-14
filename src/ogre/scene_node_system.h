#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"
#include "engine/system.h"

#include <memory>


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
    * @brief Lua bindings
    *
    * Doesn't expose anything special, but this component is needed for many
    * systems.
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

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
    * @brief Updates the sky components
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
    * @brief Updates the sky components
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

