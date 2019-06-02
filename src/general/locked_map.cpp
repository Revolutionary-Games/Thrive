#include "general/locked_map.h"

using namespace thrive;

// void LockedMap::luaBindings(
//     sol::state &lua
// ){
//     lua.new_usertype<LockedMap>("LockedMap",

//         sol::constructors<sol::types<>>(),

//         "addLock", &LockedMap::addLock,
//         "isLocked", &LockedMap::isLocked,
//         "unlock", &LockedMap::unlock,
//         "locksList", &LockedMap::locksList
//     );
// }

void
    LockedMap::addLock(std::string lockName)
{
    m_locks.insert(lockName);
}

bool
    LockedMap::isLocked(std::string conceptName) const
{
    auto found = m_locks.find(conceptName);
    return found != m_locks.end();
}

void
    LockedMap::unlock(std::string conceptName)
{
    m_locks.erase(conceptName);
}


// void
//     LockedMap::load(const StorageContainer& storage)
// {
//     StorageList locks = storage.get<StorageList>("locks");
//     for(const StorageContainer& container : locks) {
//         std::string name = container.get<std::string>("name");
//         m_locks.insert(name);
//     }
// }

// const std::unordered_set<std::string>&
//     LockedMap::locksList() const
// {
//     return m_locks;
// }

// StorageContainer
//     LockedMap::storage() const
// {
//     StorageContainer storage;
//     StorageList locks;
//     locks.reserve(m_locks.size());
//     for(const auto& name : m_locks) {
//         StorageContainer container;
//         container.set<std::string>("name", name);
//         locks.append(container);
//     }
//     storage.set<StorageList>("locks", locks);
//     return storage;
// }
