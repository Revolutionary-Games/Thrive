#pragma once

namespace thrive {

    using EntityId = unsigned int;

    using Milliseconds = int;

    /**
    * @brief Special entity id for "no entity"
    *
    * This entity id will never be returned by EntityManager::generateNewId()
    */
    static const EntityId NULL_ENTITY = 0;

}
