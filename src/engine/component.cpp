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
    return class_<Component>("Component")
        .def("typeId", &Component::typeId)
        .def("typeName", &Component::typeName)
    ;
}


Component::~Component() {}


