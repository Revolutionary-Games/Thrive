#pragma once

#include <chrono>

namespace thrive {

    using FrameDuration = std::chrono::duration<int, std::ratio<1, 60>>;

    using FrameIndex = unsigned long;

    using EntityId = unsigned int;

    enum ThreadId : char {
        Render,
        Script,
        Unknown
    };

}
