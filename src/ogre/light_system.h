#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"

#include <memory>
#include <OgreLight.h>

#include <iostream>

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief A component for a light
*/
class OgreLightComponent : public Component {
    COMPONENT(OgreLight)

public:

    /**
    * @brief Properties
    */
    struct Properties : public Touchable {
        /**
        * @brief Attenuation constant factor
        */
        Ogre::Real attenuationConstant = 1.0f;

        /**
        * @brief Attenuation linear factor
        */
        Ogre::Real attenuationLinear = 0.5f;

        /**
        * @brief Attenuation quadratic factor
        */
        Ogre::Real attenuationQuadratic = 0.75f;

        /**
        * @brief Attenuation range
        */
        Ogre::Real attenuationRange = 10.0f;

        /**
        * @brief The diffuse colour
        *
        * Defaults to white.
        */
        Ogre::ColourValue diffuseColour = Ogre::ColourValue::White;

        /**
        * @brief The specular colour
        *
        * Defaults to white.
        */
        Ogre::ColourValue specularColour = Ogre::ColourValue::White;

        /**
        * @brief Spotlight falloff
        */
        Ogre::Real spotlightFalloff = 1.0f;

        /**
        * @brief Spotlight inner angle
        */
        Ogre::Degree spotlightInnerAngle = Ogre::Degree(30);

        /**
        * @brief Spotlight near clip distance
        */
        Ogre::Real spotlightNearClipDistance = 10.0f;

        /**
        * @brief Spotlight outer angle
        */
        Ogre::Degree spotlightOuterAngle = Ogre::Degree(60);

        /**
        * @brief The light's type
        *
        * Can be
        *   - \c Ogre::Light::LT_POINT (the default)
        *   - \c Ogre::Light::LT_DIRECTIONAL
        *   - \c Ogre::Light::LT_SPOTLIGHT
        */
        Ogre::Light::LightTypes type = Ogre::Light::LT_POINT;

    };

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreLightComponent.LightTypes
    *   - LT_POINT
    *   - LT_DIRECTIONAL
    *   - LT_SPOTLIGHT
    * - OgreLightComponent()
    * - setRange()
    * - @link m_properties properties @endlink
    * - Properties
    *   - Properties::attenuationConstant
    *   - Properties::attenuationLinear
    *   - Properties::attenuationRange
    *   - Properties::attenuationQuadratic
    *   - Properties::diffuseColour
    *   - Properties::specularColour
    *   - Properties::spotlightFalloff
    *   - Properties::spotlightInnerAngle
    *   - Properties::spotlightNearClipDistance
    *   - Properties::spotlightOuterAngle
    *   - Properties::type
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    void
    load(
        const StorageContainer& storage
    ) override;

    /**
    * @brief Convenience function for setting sensible attenuation values
    *
    * This function sets the attenuation range and attenuation coefficients
    * to sensible values, as desribed <a href="http://www.ogre3d.org/tikiwiki/tiki-index.php?page=Light+Attenuation+Shortcut">
    * here</a>.
    *
    * @param range
    *   The light's range
    */
    void setRange(
        Ogre::Real range
    );

    StorageContainer
    storage() const override;

    /**
    * @brief Internal light, don't use this directly
    */
    Ogre::Light* m_light = nullptr;

    /**
    * @brief Properties
    */
    Properties
    m_properties;

};


/**
* @brief Creates lights and updates their properties
*/
class OgreLightSystem : public System {
    
public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - OgreLightSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor
    */
    OgreLightSystem();

    /**
    * @brief Destructor
    */
    ~OgreLightSystem();

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

