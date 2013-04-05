#include "util/scope_guard.h"

ScopeGuard::ScopeGuard(
    Callback onEnter,
    Callback onExit
) : m_onExit(onExit)
{
    onEnter();
}


ScopeGuard::~ScopeGuard() {
    try {
        m_onExit();
    }
    catch (...) {
        // Don't let the exception out of the destructor
    }
}
