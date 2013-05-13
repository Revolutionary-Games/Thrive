#include "ogre/render_system.h"

#include "ogre/ogre_engine.h"

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
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "RenderSystem requires an OgreEngine");
    m_impl->m_root = ogreEngine->root();
    assert(m_impl->m_root != nullptr && "Root object is null. Initialize the OgreEngine first.");
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


