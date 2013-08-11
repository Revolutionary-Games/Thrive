#include "engine/system.h"

#include "scripting/luabind.h"

#include <assert.h>

using namespace thrive;


luabind::scope
System::luaBindings() {
    using namespace luabind;
    return class_<System>("System")
        .def("active", &System::active)
        .def("setActive", &System::setActive)
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

