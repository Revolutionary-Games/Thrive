#pragma once

#include <chrono>

namespace thrive {

    using FrameDuration = std::chrono::duration<int, std::ratio<1, 60>>;

}
