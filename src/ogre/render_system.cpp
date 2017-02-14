#include "ogre/render_system.h"

#include "engine/engine.h"
#include "engine/game_state.h"
#include "scripting/luajit.h"

#include <OgreRoot.h>

using namespace thrive;

void RenderSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<RenderSystem>("RenderSystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>()
    );
}

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
    GameState* gameState
) {
    System::initNamed("RenderSystem", gameState);
    m_impl->m_root = this->engine()->ogreRoot();
    assert(m_impl->m_root != nullptr && "Root object is null. Initialize the Engine first.");
}


void
RenderSystem::shutdown() {
    m_impl->m_root = nullptr;
    System::shutdown();
}


void
RenderSystem::update(
    int renderTime,
    int
) {
    assert(m_impl->m_root != nullptr && "RenderSystem not initialized");
    m_impl->m_root->renderOneFrame(float(renderTime) / 1000);
}


