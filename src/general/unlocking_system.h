#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "engine/typedefs.h"
#include "scripting/luabind.h"
#include <luabind/object.hpp>
#include <string>
#include <unordered_set>

namespace luabind {
class scope;
}


namespace thrive {


/**
* @brief Component that contains a set of locked concepts represented and accessed by strings
*/
class LockedMapComponent : public Component {
    COMPONENT(LockedMapComponent)

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - LockedMapComponent()
    * - LockedMapComponent::addLock
    * - LockedMapComponent::isLocked
    * - LockedMapComponent::unlock
    *
    * @return
    */
    static luabind::scope
    luaBindings();


    /**
    * @brief Locks a concept such that calls querying for the lock may restrict themselves.
    *
    * @param lockName
    *   The name of the lock
    */
    void
    addLock(
        std::string lockName
    );

    /**
    * @brief Checks if a lock exists in this LockedMap, otherwise it may be assumed available.
    *
    * @param lockName
    *   The name of the lock
    *
    * @return
    *  True if the lock exists, false otherwise
    */
    bool
    isLocked(
        std::string conceptName
    ) const;

    /**
    * @brief Unlocks a concept such that calls querying for the lock may allow themselves.
    *
    * @param lockName
    *   The name of the lock
    */
    void
    unlock(
        std::string conceptName
    );

    /**
    * @brief Gets the set of all held locks
    *
    * @return
    *  Set with the locks
    */
    const std::unordered_set<std::string>&
    locksList() const;

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

private:

    /**
    * @brief The locks
    */
    std::unordered_set<std::string> m_locks;

};



}

