#pragma once

#include <memory>

namespace thrive {

class Engine;

class System {

public:

    using Order = int;

    using Ptr = std::shared_ptr<System>;

    System(
        Order order
    );

    virtual ~System() = 0;

    Engine*
    engine() const;

    virtual void
    init(
        Engine* engine
    );

    bool
    isSuspended() const;

    Order
    order() const;

    virtual void
    resume();

    virtual void
    shutdown();

    virtual void
    suspend();

    virtual void
    update(
        int milliSeconds
    ) = 0;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
