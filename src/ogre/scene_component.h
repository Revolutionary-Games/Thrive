#pragma once

#include "engine/component.h"
#include "engine/shared_state.h"
#include "engine/threads.h"

namespace thrive {

template<Thread thread>
class SceneComponent : public Component;

template<>
class SceneComponent<Thread::Render> : public Component {

public:

    

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

};
