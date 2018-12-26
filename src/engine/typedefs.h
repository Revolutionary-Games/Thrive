#pragma once

#include <cstdint>

namespace thrive {

using ComponentTypeId = uint16_t;

using EntityId = uint32_t;

using SpawnerTypeId = uint32_t;

using CompoundId = uint16_t;

using BioProcessId = uint16_t;

using Milliseconds = int;

constexpr CompoundId NULL_COMPOUND = -1;
} // namespace thrive
