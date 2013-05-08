#include "engine/system.h"

#include <assert.h>

using namespace thrive;


struct System::Implementation {

    Engine* m_engine = nullptr;

    bool m_suspended = false;

};


System::System()
  : m_impl(new Implementation())
{
}


System::~System() { }


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


bool
System::isSuspended() const {
    return m_impl->m_suspended;
}


void
System::resume() {
    m_impl->m_suspended = false;
}


void
System::shutdown() {
    m_impl->m_engine = nullptr;
}


void
System::suspend() {
    m_impl->m_suspended = true;
}

