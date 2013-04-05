#pragma once

#include <functional>

class ScopeGuard {

public:

    using Callback = std::function<void()>;

    ScopeGuard(
        Callback onEnter,
        Callback onExit
    );

    ScopeGuard(
        const ScopeGuard&
    ) = delete;

    ~ScopeGuard();

private:

    Callback m_onExit;
};


