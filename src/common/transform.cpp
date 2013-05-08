#include "common/transform.h"

#include "engine/component_factory.h"
#include "scripting/luabind.h"


using namespace thrive;

static void
TransformComponent_touch(
    TransformComponent* self
) {
    return self->m_properties.touch();
}


static TransformComponent::Properties&
TransformComponent_getWorkingCopy(
    TransformComponent* self
) {
    return self->m_properties.workingCopy();
}


static const TransformComponent::Properties&
TransformComponent_getLatest(
    TransformComponent* self
) {
    return self->m_properties.latest();
}


luabind::scope
TransformComponent::luaBindings() {
    using namespace luabind;
    return class_<TransformComponent, Component, std::shared_ptr<Component>>("TransformComponent")
        .scope [
            def("TYPE_NAME", &TransformComponent::TYPE_NAME),
            def("TYPE_ID", &TransformComponent::TYPE_ID),
            class_<Properties>("Properties")
                .def_readwrite("orientation", &Properties::orientation)
                .def_readwrite("position", &Properties::position)
                .def_readwrite("scale", &Properties::scale)
        ]
        .def(constructor<>())
        .property("latest", TransformComponent_getLatest)
        .property("workingCopy", TransformComponent_getWorkingCopy)
        .def("touch", TransformComponent_touch)
    ;
}

REGISTER_COMPONENT(TransformComponent)

