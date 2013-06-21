#pragma once

#include "engine/component.h"
#include "engine/entity_manager.h"
#include "engine/system.h"

#include <memory>
#include <OgreCommon.h>
#include <OgreColourValue.h>

namespace luabind {
class scope;
}

namespace Ogre {
class Viewport;
}

namespace thrive {

/**
* @brief A proxy for Ogre::Viewport
*
*/
class OgreViewport {

public:

    /**
    * @brief The camera entity to use
    *
    * If the given entity has no OgreCameraComponent, the viewport
    * will stay black
    */
    EntityId cameraEntity = EntityManager::NULL_ID;

    /**
    * @brief Left edge of the viewport within the window
    *
    * The coordinate system is relative, i.e. 0.0 is leftmost,
    * 1.0 is rightmost.
    */
    Ogre::Real left = 0.0f;

    /**
    * @brief Top edge of the viewport within the window
    *
    * The coordinate system is relative, i.e. 0.0 is topmost,
    * 1.0 is bottommost.
    */
    Ogre::Real top = 0.0f;

    /**
    * @brief The viewport's width relative to the window
    *
    * The coordinate system is relative, i.e. 0.5 is half width,
    * 1.0 is full width.
    */
    Ogre::Real width = 1.0f;

    /**
    * @brief The viewport's height relative to the window
    *
    * The coordinate system is relative, i.e. 0.5 is half height,
    * 1.0 is full height.
    */
    Ogre::Real height = 1.0f;

    /**
    * @brief The viewport's background colour
    */
    Ogre::ColourValue backgroundColour;

    /**
    * @brief Lua bindings
    *
    * Exposes the following \ref shared_data_lua shared properties:
    * - \c OgreViewport::cameraEntity
    * - \c OgreViewport::left
    * - \c OgreViewport::top
    * - \c OgreViewport::width
    * - \c OgreViewport::height
    * - \c OgreViewport::backgroundColour
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    *
    * @param zOrder
    *   The lower, the further to the front
    */
    OgreViewport(
        int zOrder = 0
    );

    bool
    hasChanges() const;

    void
    touch();

    void untouch();

    /**
    * @brief Pointer to internal Ogre::Viewport
    */
    Ogre::Viewport* m_viewport = nullptr;

    /**
    * @brief The viewport's zOrder
    */
    const int m_zOrder;

private:
    
    bool m_hasChanges;

};


/**
* @brief Creates, updates and removes viewports
*/
class OgreViewportSystem : public System {
    
public:

    /**
    * @brief Lua bindings
    *
    * Exposes the following free functions:
    * - \c addViewport(OgreViewport): Adds the viewport to the window
    * - \c removeViewport(OgreViewport): Removes the viewport from the window
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    OgreViewportSystem();

    /**
    * @brief Destructor
    */
    ~OgreViewportSystem();

    /**
    * @brief Adds a viewport
    *
    * @param viewport
    *   The viewport to add
    */
    void
    addViewport(
        std::shared_ptr<OgreViewport> viewport
    );

    /**
    * @brief Initializes the system
    *
    * @param engine
    */
    void init(Engine* engine) override;

    /**
    * @brief Removes a viewport
    *
    * @param viewport
    *   The viewport to remove
    */
    void
    removeViewport(
        std::shared_ptr<OgreViewport> viewport
    );

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


