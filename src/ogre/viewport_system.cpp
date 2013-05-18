#include "ogre/viewport_system.h"

#include "engine/entity.h"
#include "game.h"
#include "ogre/camera_system.h"
#include "ogre/ogre_engine.h"
#include "scripting/luabind.h"

#include <OgreRenderWindow.h>
#include <OgreViewport.h>

#include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// OgreViewport
////////////////////////////////////////////////////////////////////////////////

static void
OgreViewport_touch(
    OgreViewport* self
) {
    return self->m_properties.touch();
}


static OgreViewport::Properties&
OgreViewport_getWorkingCopy(
    OgreViewport* self
) {
    return self->m_properties.workingCopy();
}


static const OgreViewport::Properties&
OgreViewport_getLatest(
    OgreViewport* self
) {
    return self->m_properties.latest();
}


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
    return class_<OgreViewport, std::shared_ptr<OgreViewport>>("OgreViewport")
        .scope [
            class_<Properties>("Properties")
                .property("cameraEntity", Properties_getCameraEntity, Properties_setCameraEntity)
                .def_readwrite("left", &Properties::left)
                .def_readwrite("top", &Properties::top)
                .def_readwrite("width", &Properties::width)
                .def_readwrite("height", &Properties::height)
                .def_readwrite("backgroundColour", &Properties::backgroundColour)
        ]
        .def(constructor<int>())
        .property("latest", OgreViewport_getLatest)
        .property("workingCopy", OgreViewport_getWorkingCopy)
        .def_readonly("zOrder", &OgreViewport::m_zOrder)
        .def("touch", OgreViewport_touch)
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
    std::shared_ptr<OgreViewport> viewport
) {
    Game& game = Game::instance();
    OgreViewportSystem& viewportSystem = game.ogreEngine().viewportSystem();
    viewportSystem.addViewport(viewport);
}


static void
OgreViewportSystem_removeViewport(
    std::shared_ptr<OgreViewport> viewport
) {
    Game& game = Game::instance();
    OgreViewportSystem& viewportSystem = game.ogreEngine().viewportSystem();
    viewportSystem.removeViewport(viewport);
}


luabind::scope
OgreViewportSystem::luaBindings() {
    using namespace luabind;
    return 
        def("addViewport", OgreViewportSystem_addViewport),
        def("removeViewport", OgreViewportSystem_removeViewport)
    ;
}

struct OgreViewportSystem::Implementation {

    RenderQueue<std::shared_ptr<OgreViewport>> m_addedViewports;

    OgreEngine* m_ogreEngine = nullptr;

    RenderQueue<std::shared_ptr<OgreViewport>> m_removedViewports;

    Ogre::RenderWindow* m_renderWindow = nullptr;

    std::list<std::shared_ptr<OgreViewport>> m_viewports;

};


OgreViewportSystem::OgreViewportSystem()
  : m_impl(new Implementation())
{
}


OgreViewportSystem::~OgreViewportSystem() {}


void
OgreViewportSystem::addViewport(
    std::shared_ptr<OgreViewport> viewport
) {
    m_impl->m_addedViewports.push(std::move(viewport));
}


void
OgreViewportSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_ogreEngine == nullptr && "Double init of system");
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_ogreEngine = ogreEngine;
    m_impl->m_renderWindow = ogreEngine->window();
}


void
OgreViewportSystem::removeViewport(
    std::shared_ptr<OgreViewport> viewport
) {
    m_impl->m_removedViewports.push(std::move(viewport));
}


void
OgreViewportSystem::shutdown() {
    m_impl->m_renderWindow = nullptr;
    m_impl->m_ogreEngine = nullptr;
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
    for (auto& removedViewport : m_impl->m_removedViewports) {
        for (
            auto iter = m_impl->m_viewports.begin(); 
            iter != m_impl->m_viewports.end(); 
            ++iter
        ) {
            if (*iter == removedViewport) {
                m_impl->m_viewports.erase(iter);
                break;
            }
        }
    }
    for (const auto& viewport : m_impl->m_viewports) {
        if (not viewport->m_properties.hasChanges()) {
            continue;
        }
        const auto& properties = viewport->m_properties.stable();
        auto* cameraComponent = m_impl->m_ogreEngine->getComponent<OgreCameraComponent>(
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
        viewport->m_properties.untouch();
    }
}


