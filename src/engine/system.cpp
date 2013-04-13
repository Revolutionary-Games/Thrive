#include "engine/system.h"

#include <assert.h>

using namespace thrive;


struct System::Implementation {

    Implementation(
        System::Order order
    ) : m_order(order)
    {
    }

    Engine* m_engine = nullptr;

    System::Order m_order;

    bool m_suspended = false;

};


System::System(
    Order order
) : m_impl(new Implementation(order))
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


System::Order
System::order() const {
    return m_impl->m_order;
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

