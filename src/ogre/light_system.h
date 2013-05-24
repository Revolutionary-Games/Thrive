#pragma once

#include "engine/component.h"
#include "engine/shared_data.h"
#include "engine/system.h"

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
    struct Properties {
        /**
        * @brief The light's type
        *
        * Can be
        *   - \c Ogre::Light::LT_POINT (the default)
        *   - \c Ogre::Light::LT_DIRECTIONAL
        *   - \c Ogre::Light::LT_SPOTLIGHT
        */
        Ogre::Light::LightTypes type = Ogre::Light::LT_POINT;

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
        * @brief Attenuation range
        */
        Ogre::Real attenuationRange = 10.0f;

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
        Ogre::Real attenuationQuadratic = 0.75;

        /**
        * @brief Spotlight inner angle
        */
        Ogre::Radian spotlightInnerAngle = Ogre::Radian(0.5);

        /**
        * @brief Spotlight outer angle
        */
        Ogre::Radian spotlightOuterAngle = Ogre::Radian(1.0);

        /**
        * @brief Spotlight falloff
        */
        Ogre::Real spotlightFalloff = 1.0f;

        /**
        * @brief Spotlight near clip distance
        */
        Ogre::Real spotlightNearClipDistance = 10.0f;

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

    };

    /**
    * @brief Lua bindings
    *
    * Exposes the following \ref shared_data_lua "shared properties":
    * - \c Properties::type
    * - \c Properties::diffuseColor
    * - \c Properties::specularColor
    * - \c Properties::attenuationRange
    * - \c Properties::attenuationConstant
    * - \c Properties::attenuationLinear
    * - \c Properties::attenuationQuadratic
    * - \c Properties::spotlightInnerAngle
    * - \c Properties::spotlightOuterAngle
    * - \c Properties::spotlightFalloff
    * - \c Properties::spotlightNearClipDistance
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Internal light, don't use this directly
    */
    Ogre::Light* m_light = nullptr;

    /**
    * @brief Shared properties
    */
    RenderData<Properties>
    m_properties;

};


/**
* @brief Creates lights and updates their properties
*/
class OgreLightSystem : public System {
    
public:

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

