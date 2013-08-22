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

class TextOverlayComponent : public Component {
    COMPONENT(TextOverlay)

public:
    
    static luabind::scope
    luaBindings();

    struct Properties : public Touchable {

        Ogre::Real charHeight = 16.0;

        Ogre::ColourValue colour = Ogre::ColourValue::White;

        Ogre::String fontName = "Thrive";

        Ogre::Real height = 100.0;

        Ogre::GuiHorizontalAlignment horizontalAlignment = Ogre::GHA_LEFT;

        Ogre::Real left = 0.0;

        Ogre::String text = "";

        Ogre::Real top = 0.0;

        Ogre::GuiVerticalAlignment verticalAlignment = Ogre::GVA_TOP;

        Ogre::Real width = 100.0;
    };

    TextOverlayComponent(
        Ogre::String name
    );

    const Ogre::String m_name;

    Ogre::TextAreaOverlayElement* m_overlayElement = nullptr;

    Properties m_properties;
};


class TextOverlaySystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    TextOverlaySystem();

    /**
    * @brief Destructor
    */
    ~TextOverlaySystem();

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
