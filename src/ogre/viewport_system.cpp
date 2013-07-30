#include "ogre/viewport_system.h"

#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_manager.h"
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
    const OgreViewport::Properties* self
) {
    return Entity(self->cameraEntity);
}


static void
Properties_setCameraEntity(
    OgreViewport::Properties* self,
    const Entity& entity
) {
    self->cameraEntity = entity.id();
}


luabind::scope
OgreViewport::luaBindings() {
    using namespace luabind;
    return class_<OgreViewport>("OgreViewport")
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
        .def_readonly("properties", &OgreViewport::m_properties)
        .def_readonly("zOrder", &OgreViewport::m_zOrder)
    ;
}

OgreViewport::OgreViewport(
    int zOrder
) : m_zOrder(zOrder)
{
}

////////////////////////////////////////////////////////////////////////////////
// OgreViewportSystem
////////////////////////////////////////////////////////////////////////////////


static void
OgreViewportSystem_addViewport(
    OgreViewport* nakedViewport
) {
    std::unique_ptr<OgreViewport> viewport(nakedViewport);
    Game& game = Game::instance();
    OgreViewportSystem& viewportSystem = game.engine().viewportSystem();
    viewportSystem.addViewport(std::move(viewport));
}


static void
OgreViewportSystem_removeViewport(
    OgreViewport* viewport
) {
    Game& game = Game::instance();
    OgreViewportSystem& viewportSystem = game.engine().viewportSystem();
    viewportSystem.removeViewport(viewport);
}


luabind::scope
OgreViewportSystem::luaBindings() {
    using namespace luabind;
    return 
        def("addViewport", OgreViewportSystem_addViewport, adopt(_1)),
        def("removeViewport", OgreViewportSystem_removeViewport)
    ;
}

struct OgreViewportSystem::Implementation {

    std::list<std::unique_ptr<OgreViewport>> m_addedViewports;

    Engine* m_engine = nullptr;

    std::list<OgreViewport*> m_removedViewports;

    Ogre::RenderWindow* m_renderWindow = nullptr;

    std::list<std::unique_ptr<OgreViewport>> m_viewports;

};


OgreViewportSystem::OgreViewportSystem()
  : m_impl(new Implementation())
{
}


OgreViewportSystem::~OgreViewportSystem() {}


void
OgreViewportSystem::addViewport(
    std::unique_ptr<OgreViewport> viewport
) {
    m_impl->m_addedViewports.push_back(std::move(viewport));
}


void
OgreViewportSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_engine == nullptr && "Double init of system");
    m_impl->m_engine = engine;
    m_impl->m_renderWindow = engine->renderWindow();
}


void
OgreViewportSystem::removeViewport(
    OgreViewport* viewport
) {
    m_impl->m_removedViewports.push_back(viewport);
}


void
OgreViewportSystem::shutdown() {
    m_impl->m_renderWindow = nullptr;
    m_impl->m_engine = nullptr;
    System::shutdown();
}


void
OgreViewportSystem::update(int) {
    for (auto& addedViewport : m_impl->m_addedViewports) {
        addedViewport->m_viewport = m_impl->m_renderWindow->addViewport(
            nullptr, // No camera
            addedViewport->m_zOrder
        );
        m_impl->m_viewports.emplace_back(
            std::move(addedViewport)
        );
    }
    m_impl->m_addedViewports.clear();
    for (OgreViewport* removedViewport : m_impl->m_removedViewports) {
        for (
            auto iter = m_impl->m_viewports.begin(); 
            iter != m_impl->m_viewports.end(); 
            ++iter
        ) {
            if (iter->get() == removedViewport) {
                m_impl->m_viewports.erase(iter);
                break;
            }
        }
    }
    m_impl->m_removedViewports.clear();
    for (const auto& viewport : m_impl->m_viewports) {
        auto& properties = viewport->m_properties;
        if (not properties.hasChanges()) {
            continue;
        }
        auto* cameraComponent = m_impl->m_engine->entityManager().getComponent<OgreCameraComponent>(
            properties.cameraEntity
        );
        if (cameraComponent) {
            viewport->m_viewport->setCamera(cameraComponent->m_camera);
        }
        else {
            viewport->m_viewport->setCamera(nullptr);
        }
        viewport->m_viewport->setDimensions(
            properties.left,
            properties.top,
            properties.width,
            properties.height
        );
        viewport->m_viewport->setBackgroundColour(
            properties.backgroundColour
        );
        properties.untouch();
    }
}


