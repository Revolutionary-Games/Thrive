#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

#include <memory>

namespace sol {
class state;
}

namespace Ogre {
    class CompositorWorkspace;
    class SceneManager;
    class RenderTarget;
}

namespace thrive {

class OgreWorkspaceSystem;
    
/**
 * @brief A proxy for Ogre::Workspace
 *
 */
class OgreWorkspaceComponent : public Component {

    friend OgreWorkspaceSystem;
    
    COMPONENT(OgreWorkspace)

public:

    /**
     * @brief Properties
     */
    struct Properties : public Touchable {

        /**
         * @brief The camera entity to use
         *
         * If the given entity has no OgreCameraComponent, the workspace
         * will stay black or assert
         */
        EntityId cameraEntity = NULL_ENTITY;

        /**
           @brief Defines the order in which workspaces are rendered

           0 Is rendered first 1 second and so on. -1 is rendered last but
           it is reserved for GUI (CEGUI)
        */
        int position = 0;
    };

    /**
     * @brief Lua bindings
     *
     * Exposes:
     * - OgreViewport(int)
     * - @link m_properties properties @endlink
     * - Properties
     *   - Properties::cameraEntity
     *   - Properties::position
     *
     * @return 
     */
    static void luaBindings(sol::state &lua);

    /**
       @brief Constructor


       @note The definitionName CompositorWorkspaceDef has to be created
       before this call
       
    */
    OgreWorkspaceComponent(
        std::string definitionName
    );

    //! @brief dummy constructor for REGISTER_COMPONENT to not error
    OgreWorkspaceComponent();

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    /**
      @brief The workspace's position

      Lower values are rendered first, -1 is rendered last
    */
    int
    position() const;

    /**
     * @brief Properties
     */
    Properties
    m_properties;


private:

    /**
       @brief The internal Ogre::CompositorWorkspace
    */
    Ogre::CompositorWorkspace* m_workspace = nullptr;

    //! @brief Name of the workspace definition
    std::string m_name;
};


/**
 * @brief Creates, updates and removes workspaces
 */
class OgreWorkspaceSystem : public System {
public:

    /**
     * @brief Lua bindings
     *
     * Exposes:
     * - OgreViewportSystem()
     *
     * @return 
     */
    static void luaBindings(sol::state &lua);

    /**
     * @brief Constructor
     */
    OgreWorkspaceSystem();

    /**
     * @brief Destructor
     */
    ~OgreWorkspaceSystem();

    void
    activate() override;

    void
    deactivate() override;

    /**
     * @brief Initializes the system
     *
     * @param gameState
     */
    void init(GameStateData* gameState) override;

    /**
     * @brief Shuts the system down
     */
    void shutdown() override;

    /**
     * @brief Updates the system
     */
    void update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
