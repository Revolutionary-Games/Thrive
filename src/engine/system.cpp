#include "engine/system.h"

#include "engine/engine.h"
#include "engine/game_state.h"
#include "scripting/luabind.h"

#include <assert.h>

using namespace thrive;

void System::luaBindings(
    sol::state &lua
){
    return class_<System, SystemWrapper>("System")
        .def(constructor<>())
        .def("enabled", &System::enabled)
        .def("init", &System::initNamed, &SystemWrapper::default_initNamed)
        .def("setEnabled", &System::setEnabled)
        .def("activate", &System::activate, &SystemWrapper::default_activate)
        .def("deactivate", &System::deactivate, &SystemWrapper::default_deactivate)
        .def("shutdown", &System::shutdown, &SystemWrapper::default_shutdown)
        .def("update", &System::update, &SystemWrapper::default_update)
        ;
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

