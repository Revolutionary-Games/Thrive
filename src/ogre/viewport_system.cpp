#include "ogre/viewport_system.h"

#include "engine/engine.h"
#include "engine/entity.h"
#include "game.h"
#include "ogre/camera_system.h"
#include "scripting/luabind.h"

#include <OgreRenderWindow.h>
#include <OgreViewport.h>

#include <iostream>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// OgreViewport
////////////////////////////////////////////////////////////////////////////////

static Entity
OgreViewport_getCameraEntity(
    const OgreViewport* self
) {
    return Entity(self->cameraEntity);
}


static void
OgreViewport_setCameraEntity(
    OgreViewport* self,
    const Entity& entity
) {
    self->cameraEntity = entity.id();
}


luabind::scope
OgreViewport::luaBindings() {
    using namespace luabind;
    return class_<OgreViewport, std::shared_ptr<OgreViewport>>("OgreViewport")
        .def(constructor<int>())
        .def("touch", &OgreViewport::touch)
        .property("cameraEntity", OgreViewport_getCameraEntity, OgreViewport_setCameraEntity)
        .def_readwrite("left", &OgreViewport::left)
        .def_readwrite("top", &OgreViewport::top)
        .def_readwrite("width", &OgreViewport::width)
        .def_readwrite("height", &OgreViewport::height)
        .def_readwrite("backgroundColour", &OgreViewport::backgroundColour)
        .def_readonly("zOrder", &OgreViewport::m_zOrder)
    ;
}

OgreViewport::OgreViewport(
    int zOrder
) : m_zOrder(zOrder)
{
}

bool
OgreViewport::hasChanges() const {
    return m_hasChanges;
}

void
OgreViewport::touch() {
    m_hasChanges = true;
}


void
OgreViewport::untouch() {
    m_hasChanges = false;
}


////////////////////////////////////////////////////////////////////////////////
// OgreViewportSystem
////////////////////////////////////////////////////////////////////////////////


static void
OgreViewportSystem_addViewport(
    std::shared_ptr<OgreViewport> viewport
) {
    Game& game = Game::instance();
    OgreViewportSystem& viewportSystem = game.engine().viewportSystem();
    viewportSystem.addViewport(viewport);
}


static void
OgreViewportSystem_removeViewport(
    std::shared_ptr<OgreViewport> viewport
) {
    Game& game = Game::instance();
    OgreViewportSystem& viewportSystem = game.engine().viewportSystem();
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

    std::list<std::shared_ptr<OgreViewport>> m_addedViewports;

    Engine* m_engine = nullptr;

    std::list<std::shared_ptr<OgreViewport>> m_removedViewports;

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
    std::shared_ptr<OgreViewport> viewport
) {
    m_impl->m_removedViewports.push_back(std::move(viewport));
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
    m_impl->m_removedViewports.clear();
    for (const auto& viewport : m_impl->m_viewports) {
        if (not viewport->hasChanges()) {
            continue;
        }
        auto* cameraComponent = m_impl->m_engine->entityManager().getComponent<OgreCameraComponent>(
            viewport->cameraEntity
        );
        if (cameraComponent) {
            viewport->m_viewport->setCamera(cameraComponent->m_camera);
        }
        else {
            viewport->m_viewport->setCamera(nullptr);
        }
        viewport->m_viewport->setDimensions(
            viewport->left,
            viewport->top,
            viewport->width,
            viewport->height
        );
        viewport->m_viewport->setBackgroundColour(
            viewport->backgroundColour
        );
        viewport->untouch();
    }
}


