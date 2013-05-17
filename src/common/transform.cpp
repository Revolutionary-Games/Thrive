#include "common/transform.h"

#include "engine/component_registry.h"
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

static void
PhysicsTransformComponent_touch(
    PhysicsTransformComponent* self
) {
    return self->m_properties.touch();
}


static PhysicsTransformComponent::Properties&
PhysicsTransformComponent_getWorkingCopy(
    PhysicsTransformComponent* self
) {
    return self->m_properties.workingCopy();
}


static const PhysicsTransformComponent::Properties&
PhysicsTransformComponent_getLatest(
    PhysicsTransformComponent* self
) {
    return self->m_properties.latest();
}


luabind::scope
PhysicsTransformComponent::luaBindings() {
    using namespace luabind;
    return class_<PhysicsTransformComponent, Component, std::shared_ptr<Component>>("PhysicsTransformComponent")
        .scope [
            def("TYPE_NAME", &PhysicsTransformComponent::TYPE_NAME),
            def("TYPE_ID", &PhysicsTransformComponent::TYPE_ID),
            class_<Properties>("Properties")
                .def_readwrite("rotation", &Properties::rotation)
                .def_readwrite("position", &Properties::position)
                .def_readwrite("velocity", &Properties::velocity)
        ]
        .def(constructor<>())
        .property("latest", PhysicsTransformComponent_getLatest)
        .property("workingCopy", PhysicsTransformComponent_getWorkingCopy)
        .def("touch", PhysicsTransformComponent_touch)
    ;
}

REGISTER_COMPONENT(PhysicsTransformComponent)

