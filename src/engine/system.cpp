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

    void
    initNamed(
        std::string name,
        GameState* gameState
    ) override {
        this->call<void>("initNamed", name, gameState);
    }

    static void default_init(
        System* self,
        GameState* gameState
    ) {
        self->System::init(gameState);
    }

    static void default_initNamed(
        System* self,
        std::string name,
        GameState* gameState
    ) {
        self->System::initNamed(name, gameState);
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

    static void default_activate(
        System* self
    ) {
        self->System::activate();
    }

    void
    activate() override {
        this->call<void>("activate");
    }

    static void default_deactivate(
        System* self
    ) {
        self->System::deactivate();
    }

    void
    deactivate() override {
        this->call<void>("deactivate");
    }

    void
    update(
        int renderTime,
        int logicTime
    ) override {
        this->call<void>("update", renderTime, logicTime);
    }

    static void default_update(
        System*,
        int,
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
    std::string name,
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

