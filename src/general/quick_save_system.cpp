#include "engine/engine.h"
#include "engine/game_state.h"
#include "general/quick_save_system.h"
#include "ogre/keyboard.h"
#include "scripting/luajit.h"

using namespace thrive;

void QuickSaveSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<QuickSaveSystem>( "QuickSaveSystem",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<System>(),

        "init", &QuickSaveSystem::init
    );
}

struct QuickSaveSystem::Implementation {
    bool saveDown = false;
    bool loadDown = false;
};

QuickSaveSystem::QuickSaveSystem()
  : m_impl(new Implementation())
{
}

void
QuickSaveSystem::init(
    GameStateData* gameState
) {
    System::initNamed("QuickSaveSystem", gameState);
    m_impl->saveDown = false;
    m_impl->loadDown = false;
}

void
QuickSaveSystem::update(int, int) {
    Engine* engine = gameState()->engine();
    bool saveDownThisFrame = engine->keyboard().isKeyDown(OIS::KeyCode::KC_F4);
    bool loadDownThisFrame = engine->keyboard().isKeyDown(OIS::KeyCode::KC_F10);

    if(saveDownThisFrame && !m_impl->saveDown)
      engine->save("quick.sav");

    if(loadDownThisFrame && !m_impl->loadDown)
      engine->load("quick.sav");

    m_impl->saveDown = saveDownThisFrame;
    m_impl->loadDown = loadDownThisFrame;
}
