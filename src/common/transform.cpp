#include "common/transform.h"

#include "engine/component_registry.h"
#include "scripting/luabind.h"

#include <OgreStringConverter.h>
#include <boost/lexical_cast.hpp>


using namespace thrive;


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

static void PhysicsTransformComponent_printPosition(
    PhysicsTransformComponent* self
){
    //std::puts(boost::lexical_cast<char*,Ogre::String>(Ogre::StringConverter::toString(self->m_properties.stable().position)));
    Ogre::Vector3 p = self->m_properties.stable().position;
    std::printf("Position: x:%f y:%f z:%f\n",p.x,p.y,p.z);
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
        .def("printPosition",PhysicsTransformComponent_printPosition)
    ;
}

REGISTER_COMPONENT(PhysicsTransformComponent)

