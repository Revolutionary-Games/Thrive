#include "engine/component.h"
#include "engine/serialization.h"
#include "scripting/luabind.h"


using namespace thrive;

luabind::scope
Component::luaBindings() {
    using namespace luabind;
    return class_<Component>("Component")
        .def("typeId", &Component::typeId)
        .def("typeName", &Component::typeName)
    ;
}


Component::~Component() {}

void
Component::load(
    const StorageContainer& storage
) {
    m_owner = storage.get<EntityId>("owner");
}


StorageContainer
Component::storage() const {
    StorageContainer storage;
    storage.set<EntityId>("owner", m_owner);
    return storage;
}


