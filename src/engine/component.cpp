/**
* @brief 
*/
#include "engine/component.h"
#include "scripting/luabind.h"


using namespace thrive;

/**
* @brief Generates a unique component type id
*
* @return A unique component type id
*/
Component::TypeId
Component::generateTypeId() {
    static Component::TypeId nextTypeId = 1;
    return nextTypeId++;
}


/**
* @brief Creates Lua bindings
*
*/
luabind::scope
Component::luaBindings() {
    using namespace luabind;
    return class_<Component, std::shared_ptr<Component>>("Component")
        .def("typeId", &Component::typeId)
        .def("typeName", &Component::typeName)
    ;
}


Component::~Component() {}
