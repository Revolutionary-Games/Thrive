#include "engine/component.h"
#include "scripting/luabind.h"


using namespace thrive;

Component::TypeId
Component::generateTypeId() {
    static Component::TypeId nextTypeId = 1;
    return nextTypeId++;
}


luabind::scope
Component::luaBindings() {
    using namespace luabind;
    return class_<Component, std::shared_ptr<Component>>("Component")
        .def("typeId", &Component::typeId)
        .def("typeName", &Component::typeName)
        .def("touch", &Component::touch)
    ;
}


Component::~Component() {}


bool
Component::hasChanges() const {
    return m_hasChanges;
}


void
Component::touch() {
    m_hasChanges = true;
}


void
Component::untouch() {
    m_hasChanges = false;
}
