#include "ogre/light_system.h"

#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include <iostream>
#include <OgreSceneManager.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// OgreLightComponent
////////////////////////////////////////////////////////////////////////////////


void
OgreLightComponent::setRange(
    Ogre::Real range
) {
    m_properties.attenuationRange = range;
    m_properties.attenuationConstant = 1.0f;
    m_properties.attenuationLinear = 4.5f / range;
    m_properties.attenuationQuadratic = 75.0f / (range * range);
    m_properties.touch();
}


luabind::scope
OgreLightComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreLightComponent, Component, std::shared_ptr<Component>>("OgreLightComponent")
        .scope [
            def("TYPE_NAME", &OgreLightComponent::TYPE_NAME),
            def("TYPE_ID", &OgreLightComponent::TYPE_ID),
            class_<Properties, Touchable>("Properties")
                .def_readwrite("attenuationConstant", &Properties::attenuationConstant)
                .def_readwrite("attenuationLinear", &Properties::attenuationLinear)
                .def_readwrite("attenuationRange", &Properties::attenuationRange)
                .def_readwrite("attenuationQuadratic", &Properties::attenuationQuadratic)
                .def_readwrite("diffuseColour", &Properties::diffuseColour)
                .def_readwrite("specularColour", &Properties::specularColour)
                .def_readwrite("spotlightFalloff", &Properties::spotlightFalloff)
                .def_readwrite("spotlightInnerAngle", &Properties::spotlightInnerAngle)
                .def_readwrite("spotlightNearClipDistance", &Properties::spotlightNearClipDistance)
                .def_readwrite("spotlightOuterAngle", &Properties::spotlightOuterAngle)
                .def_readwrite("type", &Properties::type)
        ]
        .enum_("LightTypes") [
            value("LT_POINT", Ogre::Light::LT_POINT),
            value("LT_DIRECTIONAL", Ogre::Light::LT_DIRECTIONAL),
            value("LT_SPOTLIGHT", Ogre::Light::LT_SPOTLIGHT)
        ]
        .def(constructor<>())
        .def("setRange", &OgreLightComponent::setRange)
        .def_readonly("properties", &OgreLightComponent::m_properties)
    ;
}

REGISTER_COMPONENT(OgreLightComponent)


////////////////////////////////////////////////////////////////////////////////
// OgreLightSystem
////////////////////////////////////////////////////////////////////////////////

struct OgreLightSystem::Implementation {

    EntityFilter<
        OgreLightComponent,
        OgreSceneNodeComponent
    > m_entities = {true};

    std::unordered_map<EntityId, Ogre::Light*> m_lights;

    Ogre::SceneManager* m_sceneManager = nullptr;

};


OgreLightSystem::OgreLightSystem()
  : m_impl(new Implementation())
{
}


OgreLightSystem::~OgreLightSystem() {}


void
OgreLightSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = engine->sceneManager();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
OgreLightSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreLightSystem::update(int) {
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        OgreLightComponent* lightComponent = std::get<0>(added.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(added.second);
        Ogre::Light* light = m_impl->m_sceneManager->createLight();
        lightComponent->m_light = light;
        m_impl->m_lights[entityId] = light;
        sceneNodeComponent->m_sceneNode->attachObject(light);
    }
    for (const auto& value : m_impl->m_entities) {
        OgreLightComponent* lightComponent = std::get<0>(value.second);
        auto& properties = lightComponent->m_properties;
        if (not properties.hasChanges()) {
            continue;
        }
        Ogre::Light* light = lightComponent->m_light;
        light->setType(properties.type);
        light->setDiffuseColour(properties.diffuseColour);
        light->setSpecularColour(properties.specularColour);
        light->setAttenuation(
            properties.attenuationRange,
            properties.attenuationConstant,
            properties.attenuationLinear,
            properties.attenuationQuadratic
        );
        light->setSpotlightRange(
            properties.spotlightInnerAngle,
            properties.spotlightOuterAngle,
            properties.spotlightFalloff
        );
        light->setSpotlightNearClipDistance(properties.spotlightNearClipDistance);
        properties.untouch();
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Light* light = m_impl->m_lights[entityId];
        m_impl->m_sceneManager->destroyLight(light);
        m_impl->m_lights.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
}


