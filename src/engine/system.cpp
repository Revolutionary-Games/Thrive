#include "engine/system.h"

#include "engine/engine.h"
#include "engine/game_state.h"
#include "scripting/luajit.h"

#include <assert.h>

using namespace thrive;

void System::luaBindings(
    sol::state &lua
){
    lua.new_usertype<System>("System",

        // We are an abstract class
        "new", sol::no_constructor,
        
        "enabled", &System::enabled,
        "init", &System::initNamed, 
        "setEnabled", &System::setEnabled,
        "activate", &System::activate, 
        "deactivate", &System::deactivate, 
        "shutdown", &System::shutdown, 
        "update", &System::update
    );
}

struct System::Implementation {

    bool m_enabled = true;

    GameState* m_gameState = nullptr;

    std::string m_name = "Unknown-System";


};


System::System()
  : m_impl(new Implementation())
{
}


System::~System() { }


void
System::activate() {
    // Nothing
}


void
System::deactivate() {
    // Nothing
}


bool
System::enabled() const {
    return m_impl->m_enabled;
}


Engine*
System::engine() const {
    if (m_impl->m_gameState) {
        return &m_impl->m_gameState->engine();
    }
    else {
        return nullptr;
    }
}


EntityManager*
System::entityManager() const {
    if (m_impl->m_gameState) {
        return &m_impl->m_gameState->entityManager();
    }
    else {
        return nullptr;
    }
}


GameState*
System::gameState() const {
    return m_impl->m_gameState;
}


void
System::init(
    GameState* gameState
) {
    assert(m_impl->m_gameState == nullptr && "Cannot initialize system that is already attached to a GameState");
    m_impl->m_gameState = gameState;
}

void
System::initNamed(
    const std::string &name,
    GameState* gameState
) {
    assert(m_impl->m_gameState == nullptr && "Cannot initialize system that is already attached to a GameState");
    m_impl->m_gameState = gameState;
    m_impl->m_name = name;
}

std::string
System::getName()
{
    return m_impl->m_name;
}

void
System::setEnabled(
    bool enabled
) {
    m_impl->m_enabled = enabled;
}



void
System::shutdown() {
    m_impl->m_gameState = nullptr;
}

