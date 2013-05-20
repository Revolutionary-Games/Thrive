#pragma once

#include "engine/component.h"
#include "engine/system.h"

#include <OgreVector3.h>
#include <OgreQuaternion.h>

namespace thrive {


class TransformUpdateSystem : public System {

public:

    TransformUpdateSystem();

    ~TransformUpdateSystem();

    void init(Engine* engine) override;

    void shutdown() override;

    void update(int milliSeconds) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}

