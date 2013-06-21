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
    this->attenuationRange = range;
    this->attenuationConstant = 1.0f;
    this->attenuationLinear = 4.5f / range;
    this->attenuationQuadratic = 75.0f / (range * range);
}


luabind::scope
OgreLightComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreLightComponent, Component, std::shared_ptr<Component>>("OgreLightComponent")
        .scope [
            def("TYPE_NAME", &OgreLightComponent::TYPE_NAME),
            def("TYPE_ID", &OgreLightComponent::TYPE_ID)
        ]
        .enum_("LightTypes") [
            value("LT_POINT", Ogre::Light::LT_POINT),
            value("LT_DIRECTIONAL", Ogre::Light::LT_DIRECTIONAL),
            value("LT_SPOTLIGHT", Ogre::Light::LT_SPOTLIGHT)
        ]
        .def(constructor<>())
        .def_readwrite("type", &OgreLightComponent::type)
        .def_readwrite("diffuseColour", &OgreLightComponent::diffuseColour)
        .def_readwrite("specularColour", &OgreLightComponent::specularColour)
        .def_readwrite("attenuationRange", &OgreLightComponent::attenuationRange)
        .def_readwrite("attenuationConstant", &OgreLightComponent::attenuationConstant)
        .def_readwrite("attenuationLinear", &OgreLightComponent::attenuationLinear)
        .def_readwrite("attenuationQuadratic", &OgreLightComponent::attenuationQuadratic)
        .def_readwrite("spotlightInnerAngle", &OgreLightComponent::spotlightInnerAngle)
        .def_readwrite("spotlightOuterAngle", &OgreLightComponent::spotlightOuterAngle)
        .def_readwrite("spotlightFalloff", &OgreLightComponent::spotlightFalloff)
        .def_readwrite("spotlightNearClipDistance", &OgreLightComponent::spotlightNearClipDistance)
        .def("setRange", &OgreLightComponent::setRange)
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
        if (not lightComponent->hasChanges()) {
            continue;
        }
        Ogre::Light* light = lightComponent->m_light;
        light->setType(lightComponent->type);
        light->setDiffuseColour(lightComponent->diffuseColour);
        light->setSpecularColour(lightComponent->specularColour);
        light->setAttenuation(
            lightComponent->attenuationRange,
            lightComponent->attenuationConstant,
            lightComponent->attenuationLinear,
            lightComponent->attenuationQuadratic
        );
        light->setSpotlightRange(
            lightComponent->spotlightInnerAngle,
            lightComponent->spotlightOuterAngle,
            lightComponent->spotlightFalloff
        );
        light->setSpotlightNearClipDistance(lightComponent->spotlightNearClipDistance);
        lightComponent->untouch();
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Light* light = m_impl->m_lights[entityId];
        m_impl->m_sceneManager->destroyLight(light);
        m_impl->m_lights.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
}


