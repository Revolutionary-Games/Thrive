#include "engine/system.h"

#include <assert.h>

using namespace thrive;


struct System::Implementation {

    Engine* m_engine = nullptr;

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



void
System::shutdown() {
    m_impl->m_engine = nullptr;
}

