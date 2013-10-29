#include "ogre/viewport_system.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/entity_manager.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/camera_system.h"
#include "scripting/luabind.h"

#include <luabind/adopt_policy.hpp>
#include <OgreRenderWindow.h>
#include <OgreViewport.h>

#include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// OgreViewport
////////////////////////////////////////////////////////////////////////////////

static Entity
Properties_getCameraEntity(
    const OgreViewportComponent::Properties* self
) {
    return Entity(self->cameraEntity);
}


static void
Properties_setCameraEntity(
    OgreViewportComponent::Properties* self,
    const Entity& entity
) {
    self->cameraEntity = entity.id();
}


luabind::scope
OgreViewportComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreViewportComponent, Component>("OgreViewportComponent")
        .scope [
            class_<Properties, Touchable>("Properties")
                .def_readwrite("backgroundColour", &Properties::backgroundColour)
                .property("cameraEntity", Properties_getCameraEntity, Properties_setCameraEntity)
                .def_readwrite("height", &Properties::height)
                .def_readwrite("left", &Properties::left)
                .def_readwrite("top", &Properties::top)
                .def_readwrite("width", &Properties::width)
        ]
        .def(constructor<int>())
        .def_readonly("properties", &OgreViewportComponent::m_properties)
        .property("zOrder", &OgreViewportComponent::m_zOrder)
    ;
}

OgreViewportComponent::OgreViewportComponent(
    int zOrder
) : m_zOrder(zOrder)
{
}


void
OgreViewportComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_properties.backgroundColour = storage.get<Ogre::ColourValue>("backgroundColour");
    m_properties.cameraEntity = storage.get<EntityId>("cameraEntity");
    m_properties.height = storage.get<Ogre::Real>("height");
    m_properties.left = storage.get<Ogre::Real>("left");
    m_properties.top = storage.get<Ogre::Real>("top");
    m_properties.width = storage.get<Ogre::Real>("width");
    m_zOrder = storage.get<int32_t>("zOrder");
}


StorageContainer
OgreViewportComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set("backgroundColour", m_properties.backgroundColour);
    storage.set("cameraEntity", m_properties.cameraEntity);
    storage.set("height", m_properties.height);
    storage.set("left", m_properties.left);
    storage.set("top", m_properties.top);
    storage.set("width", m_properties.width);
    storage.set("zOrder", m_zOrder);
    return storage;
}


int
OgreViewportComponent::zOrder() const {
    return m_zOrder;
}

REGISTER_COMPONENT(OgreViewportComponent)
////////////////////////////////////////////////////////////////////////////////
// OgreViewportSystem
////////////////////////////////////////////////////////////////////////////////


struct OgreViewportSystem::Implementation {

    EntityFilter<OgreViewportComponent> m_entities = {true};

    Ogre::RenderWindow* m_renderWindow = nullptr;

    std::unordered_map<EntityId, Ogre::Viewport*> m_viewports;

};


OgreViewportSystem::OgreViewportSystem()
  : m_impl(new Implementation())
{
}


OgreViewportSystem::~OgreViewportSystem() {}


void
OgreViewportSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_renderWindow = engine->renderWindow();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
OgreViewportSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_renderWindow = nullptr;
    System::shutdown();
}


void
OgreViewportSystem::update(int) {
    for (EntityId id : m_impl->m_entities.removedEntities()) {
        Ogre::Viewport* viewport = m_impl->m_viewports[id];
        this->engine()->renderWindow()->removeViewport(
            viewport->getZOrder()
        );
    }
    for (const auto& item : m_impl->m_entities.addedEntities()) {
        EntityId id = item.first;
        OgreViewportComponent* viewportComponent = std::get<0>(item.second);
        Ogre::Viewport* viewport = m_impl->m_renderWindow->addViewport(
            nullptr, // No camera
            viewportComponent->zOrder()
        );
        viewportComponent->m_viewport = viewport;
        m_impl->m_viewports.emplace(
            id,
            viewport
        );
    }
    m_impl->m_entities.clearChanges();
    for (const auto& item : m_impl->m_entities) {
        OgreViewportComponent* viewportComponent = std::get<0>(item.second);
        auto& properties = viewportComponent->m_properties;
        if (properties.hasChanges()) {
            Ogre::Viewport* viewport = viewportComponent->m_viewport;
            auto cameraComponent = this->engine()->entityManager().getComponent<OgreCameraComponent>(
                properties.cameraEntity
            );
            if (cameraComponent) {
                viewport->setCamera(cameraComponent->m_camera);
            }
            else {
                viewport->setCamera(nullptr);
            }
            viewport->setDimensions(
                properties.left,
                properties.top,
                properties.width,
                properties.height
            );
            viewport->setBackgroundColour(
                properties.backgroundColour
            );
            properties.untouch();
        }
    }
}


