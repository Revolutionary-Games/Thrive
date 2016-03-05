#include "ogre/light_system.h"

#include "engine/component_factory.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "engine/serialization.h"
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
    return class_<OgreLightComponent, Component>("OgreLightComponent")
        .enum_("ID") [
            value("TYPE_ID", OgreLightComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &OgreLightComponent::TYPE_NAME),
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


void
OgreLightComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_properties.attenuationConstant = storage.get<Ogre::Real>("attenuationConstant", 1.0f);
    m_properties.attenuationLinear = storage.get<Ogre::Real>("attenuationLinear", 0.5f);
    m_properties.attenuationQuadratic = storage.get<Ogre::Real>("attenuationQuadratic", 0.75f);
    m_properties.attenuationRange = storage.get<Ogre::Real>("attenuationRange", 10.0f);
    m_properties.diffuseColour = storage.get<Ogre::ColourValue>("diffuseColour", Ogre::ColourValue::White);
    m_properties.specularColour = storage.get<Ogre::ColourValue>("specularColour", Ogre::ColourValue::White);
    m_properties.spotlightFalloff = storage.get<Ogre::Real>("spotlightFalloff", 1.0f);
    m_properties.spotlightInnerAngle = storage.get<Ogre::Degree>("spotlightInnerAngle", Ogre::Degree(45));
    m_properties.spotlightNearClipDistance = storage.get<Ogre::Real>("spotlightNearDistance", 10.0f);
    m_properties.spotlightOuterAngle = storage.get<Ogre::Degree>("spotlightOuterAngle", Ogre::Degree(45));
    m_properties.type = static_cast<Ogre::Light::LightTypes>(
        storage.get<int16_t>("lightType", Ogre::Light::LT_POINT)
    );
}


StorageContainer
OgreLightComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Ogre::Real>("attenuationConstant", m_properties.attenuationConstant);
    storage.set<Ogre::Real>("attenuationLinear", m_properties.attenuationLinear);
    storage.set<Ogre::Real>("attenuationQuadratic", m_properties.attenuationQuadratic);
    storage.set<Ogre::Real>("attenuationRange", m_properties.attenuationRange);
    storage.set<Ogre::ColourValue>("diffuseColour", m_properties.diffuseColour);
    storage.set<Ogre::ColourValue>("specularColour", m_properties.specularColour);
    storage.set<Ogre::Real>("spotlightFalloff", m_properties.spotlightFalloff);
    storage.set<Ogre::Degree>("spotlightInnerAngle", m_properties.spotlightInnerAngle);
    storage.set<Ogre::Real>("spotlightNearClipDistance", m_properties.spotlightNearClipDistance);
    storage.set<Ogre::Degree>("spotlightOuterAngle", m_properties.spotlightOuterAngle);
    storage.set<int16_t>("lightType", m_properties.type);
    return storage;
}

REGISTER_COMPONENT(OgreLightComponent)


////////////////////////////////////////////////////////////////////////////////
// OgreLightSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreLightSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreLightSystem, System>("OgreLightSystem")
        .def(constructor<>())
    ;
}


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
    GameState* gameState
) {
    System::initNamed("OgreLightSystem", gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreLightSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreLightSystem::update(int, int) {
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Light* light = m_impl->m_lights[entityId];
        if (light) {
            m_impl->m_sceneManager->destroyLight(light);
        }
        m_impl->m_lights.erase(entityId);
    }
    for (const auto& added : m_impl->m_entities.addedEntities()) {
        EntityId entityId = added.first;
        OgreLightComponent* lightComponent = std::get<0>(added.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(added.second);
        Ogre::Light* light = m_impl->m_sceneManager->createLight();
        lightComponent->m_light = light;
        m_impl->m_lights[entityId] = light;
        sceneNodeComponent->m_sceneNode->attachObject(light);
    }
    m_impl->m_entities.clearChanges();
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
}


