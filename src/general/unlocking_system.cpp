#include "general/unlocking_system.h"

#include "bullet/collision_filter.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"

#include <luabind/iterator_policy.hpp>


using namespace thrive;

luabind::scope
LockedMapComponent::luaBindings() {
    using namespace luabind;
    return class_<LockedMapComponent, Component>("LockedMapComponent")
        .enum_("ID") [
            value("TYPE_ID", LockedMapComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &LockedMapComponent::TYPE_NAME)
        ]
        .def(constructor<>())
        .def("addLock", &LockedMapComponent::addLock)
        .def("isLocked", &LockedMapComponent::isLocked)
        .def("unlock", &LockedMapComponent::unlock)
        .def("locksList", &LockedMapComponent::locksList, return_stl_iterator)
    ;
}

void
LockedMapComponent::addLock(
    std::string lockName
) {
    m_locks.insert(lockName);
}

bool
LockedMapComponent::isLocked(
    std::string conceptName
) const {
    auto found = m_locks.find(conceptName);
    return found != m_locks.end();
}

void
LockedMapComponent::unlock(
    std::string conceptName
) {
    m_locks.erase(conceptName);
}


void
LockedMapComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    StorageList locks = storage.get<StorageList>("locks");
    for (const StorageContainer& container : locks) {
        std::string name = container.get<std::string>("name");
        m_locks.insert(name);
    }
}

const std::unordered_set<std::string>&
LockedMapComponent::locksList() const {
    return m_locks;
}

StorageContainer
LockedMapComponent::storage() const {
    StorageContainer storage = Component::storage();
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

REGISTER_COMPONENT(LockedMapComponent)

