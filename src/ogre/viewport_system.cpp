#include "ogre/viewport_system.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
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


luabind::scope
OgreViewportSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreViewportSystem, System>("OgreViewportSystem")
        .def(constructor<>())
    ;
}


struct OgreViewportSystem::Implementation {


    Implementation(
        OgreViewportSystem& system
    ) : m_system(system)
    {
    }


    void
    removeAllViewports() {
        for (const auto& item : m_entities) {
            OgreViewportComponent* viewportComponent = std::get<0>(item.second);
            viewportComponent->m_viewport = nullptr;
        }
        for (const auto& pair : m_viewports) {
            this->removeViewport(pair.second);
        }
        m_viewports.clear();
    }

    void
    removeViewport(
        Ogre::Viewport* viewport
    ) {
        m_renderWindow->removeViewport(
            viewport->getZOrder()
        );
    }


    void
    restoreAllViewports() {
        for (const auto& item : m_entities) {
            EntityId entityId = item.first;
            OgreViewportComponent* component = std::get<0>(item.second);
            this->restoreViewport(entityId, component);
        }
    }

    void
    restoreViewport(
        EntityId entityId,
        OgreViewportComponent* component
    ) {
        if (component->m_viewport) {
            // No need to restore
            return;
        }
        // Find camera (if any)
        Ogre::Camera* camera = nullptr;
        auto cameraComponent = m_system.entityManager()->getComponent<OgreCameraComponent>(
            component->m_properties.cameraEntity
        );
        if (cameraComponent) {
            camera = cameraComponent->m_camera;
        }
        // Create viewport
        Ogre::Viewport* viewport = m_renderWindow->addViewport(
            camera,
            component->zOrder()
        );
        component->m_viewport = viewport;
        m_viewports.emplace(
            entityId,
            viewport
        );
    }

    EntityFilter<OgreViewportComponent> m_entities = {true};

    Ogre::RenderWindow* m_renderWindow = nullptr;

    OgreViewportSystem& m_system;

    std::unordered_map<EntityId, Ogre::Viewport*> m_viewports;

};


OgreViewportSystem::OgreViewportSystem()
  : m_impl(new Implementation(*this))
{
}


OgreViewportSystem::~OgreViewportSystem() {}


void
OgreViewportSystem::activate() {
    m_impl->restoreAllViewports();
    m_impl->m_entities.clearChanges();
}


void
OgreViewportSystem::deactivate() {
    m_impl->removeAllViewports();
}


void
OgreViewportSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_renderWindow = this->engine()->renderWindow();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}




void
OgreViewportSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_renderWindow = nullptr;
    System::shutdown();
}


void
OgreViewportSystem::update(int, int) {
    for (EntityId id : m_impl->m_entities.removedEntities()) {
        Ogre::Viewport* viewport = m_impl->m_viewports[id];
        m_impl->removeViewport(viewport);
    }
    for (const auto& item : m_impl->m_entities.addedEntities()) {
        EntityId entityId = item.first;
        OgreViewportComponent* component = std::get<0>(item.second);
        m_impl->restoreViewport(entityId, component);
    }
    m_impl->m_entities.clearChanges();
    for (const auto& item : m_impl->m_entities) {
        OgreViewportComponent* viewportComponent = std::get<0>(item.second);
        auto& properties = viewportComponent->m_properties;
        if (properties.hasChanges()) {
            Ogre::Viewport* viewport = viewportComponent->m_viewport;
            auto cameraComponent = this->entityManager()->getComponent<OgreCameraComponent>(
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


