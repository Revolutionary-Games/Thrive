#include "engine/system.h"

#include "engine/engine.h"
#include "engine/game_state.h"
#include "scripting/luabind.h"

#include <assert.h>

using namespace thrive;


/**
* @brief Wrapper class to enable subclassing System in Lua
*
* \cond
*/
struct SystemWrapper : System, luabind::wrap_base {

    void
    init(
        GameState* gameState
    ) override {
        this->call<void>("init", gameState);
    }

    static void default_init(
        System* self, 
        GameState* gameState
    ) {
        self->System::init(gameState);
    }

    void
    shutdown() override {
        this->call<void>("shutdown");
    }

    static void default_shutdown(
        System* self
    ) {
        self->System::shutdown();
    }

    void
    update(
        int milliseconds
    ) override {
        this->call<void>("update", milliseconds);
    }

    static void default_update(
        System*, 
        int
    ) {
        throw std::runtime_error("System::update has no default implementation");
    }

};

/**
* \endcond
*/


luabind::scope
System::luaBindings() {
    using namespace luabind;
    return class_<System, SystemWrapper>("System")
        .def(constructor<>())
        .def("enabled", &System::enabled)
        .def("init", &System::init, &SystemWrapper::default_init)
        .def("setEnabled", &System::setEnabled)
        .def("shutdown", &System::shutdown, &SystemWrapper::default_shutdown)
        .def("update", &System::update, &SystemWrapper::default_update)
    ;
}


struct System::Implementation {

    bool m_enabled = true;

    GameState* m_gameState = nullptr;

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
System::setEnabled(
    bool enabled
) {
    m_impl->m_enabled = enabled;
}



void
System::shutdown() {
    m_impl->m_gameState = nullptr;
}

