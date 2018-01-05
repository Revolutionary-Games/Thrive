#error replaced by Leviathan::RenderNode

#pragma once

// #include "engine/component.h"
// #include "engine/system.h"
// #include "engine/touchable.h"

#include <memory>
#include <OgreVector3.h>
#include <OgreQuaternion.h>
#include <vector>
#include <map>

#include <Entities/Component.h>
#include <Entities/System.h>

namespace Ogre {
class SceneNode;
}

namespace thrive {
    class SoundManager;
}

namespace thrive {

/**
* @brief A component for a Ogre scene nodes
*
*/
class OgreSceneNodeComponent : public Leviathan::Component {
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
    * - @link m_transform transform @endlink
    * - Transform
    *   - Transform::orientation
    *   - Transform::position
    *   - Transform::scale
    * - OgreSceneNodeComponent::entity
    * - OgreSceneNodeComponent::meshName
    * - OgreSceneNodeComponent::visible
    * - OgreSceneNodeComponent::playAnimation
    * - OgreSceneNodeComponent::attachObject
    * - OgreSceneNodeComponent::attachSoundListener
    * - OgreSceneNodeComponent::detachObject (unimplemented)
    * - OgreSceneNodeComponent::m_parentId (as "parent")
    * - OgreSceneNodeComponent::planeTexture
    *
    * @return
    */
    static void luaBindings(sol::state &lua);
    

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    /**
    * @brief Sets the current animation to be played
    *
    * @param name
    *  Name of the animation to play
    *
    * @param loop
    *  If true the animation will loop indefinitely
    *
    */
    void
    playAnimation(
        std::string name,
        bool loop
    );

    /**
    * @brief Stops a specific animation
    *
    * @param name
    *  Name of the animation to stop
    *
    */
    void
    stopAnimation(
        std::string name
    );

    /**
    * @brief Stops all animations playing on this sceneNode
    */
    void
    stopAllAnimations();

    /**
    * @brief Sets the speed for all animations for this scenenode
    *
    * @param factor
    *  Factor to multiply the speed by. 1.0 is normal speed.
    *
    */
    void
    setAnimationSpeed(
        float factor
    );

    /**
    * @brief Attaches a MovableObject to the underlying scenenode
    *
    *  Note that this will not transfer across serialization so should prefer to be used in initialization
    *
    * @param obj
    *  Object to attach
    */
    void
    attachObject(
        Ogre::MovableObject* obj
    );

    /**
    * @brief Attaches the sound-listener to the underlying scenenode, having the sound perspective move with it
    */
    void
    attachSoundListener();

    /**
    * @brief The name of the mesh to attach to this scene node
    */
    TouchableValue<Ogre::String> m_meshName;

    /**
    * @brief The entity id of the parent scene node
    */
    TouchableValue<EntityId> m_parentId = NULL_ENTITY;

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

    /**
    * @brief Pointer to the underlying Ogre::Entity
    */
    Ogre::Entity* m_entity = nullptr;


    /**
    * @brief Whether the scenenode is visible
    */
    TouchableValue<bool> m_visible = true;

    /**
    * @brief Whether the scenenode is a simple plane
    */
    TouchableValue<std::string> m_planeTexture;

private:

    friend class OgreUpdateSceneNodeSystem;
    TouchableValue<std::vector<Ogre::MovableObject*>> m_objectsToAttach;
    TouchableValue<bool> m_attachToListener = false;

    void
    _attachObject(
        Ogre::MovableObject* obj
    );

    std::vector<std::pair<std::string, bool>> m_animationsToStart;
    std::vector<std::string> m_animationsToHalt;
    bool m_animationChange = false;
    bool m_fullAnimationHalt = false;
    float m_animationSpeedFactor = 1.0f;

    static bool s_soundListenerAttached;



};

/**
* @brief Creates scene nodes for new OgreSceneNodeComponents
*/
class OgreAddSceneNodeSystem : public System {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreAddSceneNodeSystem()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

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
    */
    void init(GameStateData* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Adds new scene nodes
    */
    void update(int, int) override;

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
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreRemoveSceneNodeSystem()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

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
    */
    void init(GameStateData* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Removes stale scene nodes
    */
    void update(int, int) override;

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
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreUpdateSceneNodeSystem()
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

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
    */
    void init(GameStateData* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the scene nodes
    */
    void update(int, int) override;


private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}


