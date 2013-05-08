#pragma once

#include "engine/system.h"

namespace thrive {

class RenderSystem : public System {

public:

    RenderSystem();

    ~RenderSystem();

    void
    init(
        Engine* engine
    ) override;

    void
    shutdown() override;

    void
    update(
        int milliSeconds
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
