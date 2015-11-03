#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

#include <Ogre.h>
#include <OgreTextAreaOverlayElement.h>

namespace luabind {
    class scope;
}

namespace thrive {

/**
* @brief A component for a text overlay
*/
class TextOverlayComponent : public Component {
    COMPONENT(TextOverlay)

public:
    
    /**
    * @brief Properties
    */
    struct Properties : public Touchable {

        /**
        * @brief The character height in pixels
        */
        Ogre::Real charHeight = 16.0f;

        /**
        * @brief Text colour
        */
        Ogre::ColourValue colour = Ogre::ColourValue::White;

        /**
        * @brief Font name
        */
        Ogre::String fontName = "Thrive";

        /**
        * @brief Textbox height in pixels
        */
        Ogre::Real height = 100.0f;

        /**
        * @brief Horizontal alignment relative to screen
        */
        Ogre::GuiHorizontalAlignment horizontalAlignment = Ogre::GHA_LEFT;

        /**
        * @brief Offset relative to screen anchor in pixels. 
        *
        * Positive is to the right.
        */
        Ogre::Real left = 0.0;

        /**
        * @brief Text to display
        */
        Ogre::String text = "";

        /**
        * @brief Offset relative to screen anchor in pixels.
        *
        * Positive is downwards
        */
        Ogre::Real top = 0.0;

        /**
        * @brief Vertical alignment relative to screen
        */
        Ogre::GuiVerticalAlignment verticalAlignment = Ogre::GVA_TOP;

        /**
        * @brief Textbox width in pixels
        */
        Ogre::Real width = 100.0f;
    };

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - TextOverlayComponent(std::string)
    * - Properties
    *   - Properties::charHeight
    *   - Properties::colour
    *   - Properties::fontName
    *   - Properties::height
    *   - Properties::horizontalAlignment
    *   - Properties::left
    *   - Properties::text
    *   - Properties::top
    *   - Properties::verticalAlignment
    *   - Properties::width
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    *
    * @param name
    *   The overlay's name, must be unique
    */
    TextOverlayComponent(
        Ogre::String name
    );

    TextOverlayComponent();

    void
    load(
        const StorageContainer& storage
    ) override;

    /**
    * @brief The overlay's name
    *
    */
    Ogre::String
    name() const {
        return m_name;
    }

    StorageContainer
    storage() const override;

    /**
    * @brief Pointer to internal overlay element
    */
    Ogre::TextAreaOverlayElement* m_overlayElement = nullptr;

    /**
    * @brief Properties
    */
    Properties m_properties;

private:
    
    Ogre::String m_name;
};


/**
* @brief Creates, updates and removes text overlays
*/
class TextOverlaySystem : public System {
    
public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - TextOverlaySystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    TextOverlaySystem();

    /**
    * @brief Destructor
    */
    ~TextOverlaySystem();

    void activate() override;

    void deactivate() override;

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
    void update(int, int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
