#include "general/locked_map.h"

#include "bullet/collision_filter.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"

#include <luabind/iterator_policy.hpp>


using namespace thrive;

luabind::scope
LockedMap::luaBindings() {
    using namespace luabind;
    return class_<LockedMap>("LockedMap")
        .def(constructor<>())
        .def("addLock", &LockedMap::addLock)
        .def("isLocked", &LockedMap::isLocked)
        .def("unlock", &LockedMap::unlock)
        .def("locksList", &LockedMap::locksList, return_stl_iterator)
    ;
}

void
LockedMap::addLock(
    std::string lockName
) {
    m_locks.insert(lockName);
}

bool
LockedMap::isLocked(
    std::string conceptName
) const {
    auto found = m_locks.find(conceptName);
    return found != m_locks.end();
}

void
LockedMap::unlock(
    std::string conceptName
) {
    m_locks.erase(conceptName);
}


void
LockedMap::load(
    const StorageContainer& storage
) {
    StorageList locks = storage.get<StorageList>("locks");
    for (const StorageContainer& container : locks) {
        std::string name = container.get<std::string>("name");
        m_locks.insert(name);
    }
}

const std::unordered_set<std::string>&
LockedMap::locksList() const {
    return m_locks;
}

StorageContainer
LockedMap::storage() const {
    StorageContainer storage;
    StorageList locks;
    locks.reserve(m_locks.size());
    for (const auto& name : m_locks) {
        StorageContainer container;
        container.set<std::string>("name", name);
        locks.append(container);
    }
    storage.set<StorageList>("locks", locks);
    return storage;
}
