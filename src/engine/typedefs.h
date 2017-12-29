#pragma once

#include <cstdint>

// TODO: Is this include needed?
#include <utility>

namespace thrive {

    using ComponentTypeId = uint16_t;

    using EntityId = uint32_t;

    using SpawnerTypeId = uint32_t;

    using CompoundId = uint16_t;

    using BioProcessId = uint16_t;

    using Milliseconds = int;

    // Use (Leviathan::)NULL_OBJECT instead
    /**
    * @brief Special entity id for "no entity"
    *
    * This entity id will never be returned by EntityManager::generateNewId()
    */
    // static const EntityId NULL_ENTITY = 0;

    // Use THRIVE_COMPONENT::INVALID instead (defined in component_types.h)
    // static const ComponentTypeId NULL_COMPONENT_TYPE = 0;
}
