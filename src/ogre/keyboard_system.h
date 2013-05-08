#pragma once

#include "engine/shared_data.h"
#include "engine/system.h"

#include <OISKeyboard.h>

namespace thrive {

class KeyboardSystem : public System {

public:

    struct KeyEvent {

        const OIS::KeyCode key;

        const bool pressed;

        const bool alt;

        const bool ctrl;

        const bool shift;

    };

    KeyboardSystem();

    ~KeyboardSystem();

    void
    init(
        Engine* engine
    ) override;

    InputQueue<KeyEvent>&
    eventQueue();

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
