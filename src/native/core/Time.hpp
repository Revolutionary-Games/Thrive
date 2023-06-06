#pragma once

#include <chrono>
#include <cstdint>

namespace Thrive
{
using MillisecondDuration = std::chrono::duration<int64_t, std::milli>;
using MicrosecondDuration = std::chrono::duration<int64_t, std::micro>;
using SecondDuration = std::chrono::duration<float, std::ratio<1>>;
using PreciseSecondDuration = std::chrono::duration<double, std::ratio<1>>;

using TimingClock = std::chrono::high_resolution_clock;
using SteadyClock = std::chrono::steady_clock;
} // namespace Thrive
