#pragma once

#include <cstdint>
#include <utility>

namespace thrive {

    using ComponentTypeId = uint16_t;

    using EntityId = uint32_t;

    using Milliseconds = int;

    /**
    * @brief Special entity id for "no entity"
    *
    * This entity id will never be returned by EntityManager::generateNewId()
    */
    static const EntityId NULL_ENTITY = 0;

    static const ComponentTypeId NULL_COMPONENT_TYPE = 0;

}
