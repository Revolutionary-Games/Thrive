#include "ogre/render_system.h"

#include "engine/engine.h"

#include <OgreRoot.h>

using namespace thrive;

struct RenderSystem::Implementation {

    Ogre::Root* m_root;

};


RenderSystem::RenderSystem()
  : m_impl(new Implementation())
{
}


RenderSystem::~RenderSystem() {}


void
RenderSystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_root = engine->ogreRoot();
    assert(m_impl->m_root != nullptr && "Root object is null. Initialize the Engine first.");
}


void
RenderSystem::shutdown() {
    m_impl->m_root = nullptr;
    System::shutdown();
}


void
RenderSystem::update(
    int milliSeconds
) {
    assert(m_impl->m_root != nullptr && "RenderSystem not initialized");
    m_impl->m_root->renderOneFrame(float(milliSeconds) / 1000);
}


