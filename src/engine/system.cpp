#include "engine/system.h"

#include "engine/engine.h"
#include "scripting/luabind.h"

#include <assert.h>

using namespace thrive;


struct SystemWrapper : System, luabind::wrap_base {

    void
    init(
        Engine* engine
    ) override {
        call<void>("init", engine);
    }

    static void default_init(
        System* self, 
        Engine* engine
    ) {
        self->System::init(engine);
    }

    void
    shutdown() override {
        call<void>("shutdown");
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
        call<void>("update", milliseconds);
    }

    static void default_update(
        System*, 
        int
    ) {
        throw std::runtime_error("System::update has no default implementation");
    }

};

luabind::scope
System::luaBindings() {
    using namespace luabind;
    return class_<System, SystemWrapper>("System")
        .def(constructor<>())
        .def("active", &System::active)
        .def("init", &System::init, &SystemWrapper::default_init)
        .def("setActive", &System::setActive)
        .def("shutdown", &System::shutdown, &SystemWrapper::default_shutdown)
        .def("update", &System::update, &SystemWrapper::default_update)
    ;
}


struct System::Implementation {

    bool m_active = true;

    Engine* m_engine = nullptr;

};


System::System()
  : m_impl(new Implementation())
{
}


System::~System() { }


bool
System::active() const {
    return m_impl->m_active;
}


Engine*
System::engine() const {
    return m_impl->m_engine;
}


void
System::init(
    Engine* engine
) {
    assert(m_impl->m_engine == nullptr && "Cannot initialize system that is already attached to an engine");
    m_impl->m_engine = engine;
}


void
System::setActive(
    bool active
) {
    m_impl->m_active = active;
}



void
System::shutdown() {
    m_impl->m_engine = nullptr;
}

