#include "ogre/sky_system.h"

#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "ogre/ogre_engine.h"
#include "scripting/luabind.h"

#include <iostream>
#include <OgreSceneManager.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// SkyPlaneComponent
////////////////////////////////////////////////////////////////////////////////

static void
SkyPlaneComponent_touch(
    SkyPlaneComponent* self
) {
    return self->m_properties.touch();
}


static SkyPlaneComponent::Properties&
SkyPlaneComponent_getWorkingCopy(
    SkyPlaneComponent* self
) {
    return self->m_properties.workingCopy();
}


static const SkyPlaneComponent::Properties&
SkyPlaneComponent_getLatest(
    SkyPlaneComponent* self
) {
    return self->m_properties.latest();
}


luabind::scope
SkyPlaneComponent::luaBindings() {
    using namespace luabind;
    return class_<SkyPlaneComponent, Component, std::shared_ptr<Component>>("SkyPlaneComponent")
        .scope [
            def("TYPE_NAME", &SkyPlaneComponent::TYPE_NAME),
            def("TYPE_ID", &SkyPlaneComponent::TYPE_ID),
            class_<Properties>("Properties")
                .def_readwrite("enabled", &Properties::enabled)
                .def_readwrite("plane", &Properties::plane)
                .def_readwrite("materialName", &Properties::materialName)
                .def_readwrite("scale", &Properties::scale)
                .def_readwrite("tiling", &Properties::tiling)
                .def_readwrite("drawFirst", &Properties::drawFirst)
                .def_readwrite("bow", &Properties::bow)
                .def_readwrite("xsegments", &Properties::xsegments)
                .def_readwrite("ysegments", &Properties::ysegments)
                .def_readwrite("groupName", &Properties::groupName)
        ]
        .def(constructor<>())
        .property("latest", SkyPlaneComponent_getLatest)
        .property("workingCopy", SkyPlaneComponent_getWorkingCopy)
        .def("touch", SkyPlaneComponent_touch)
    ;
}

REGISTER_COMPONENT(SkyPlaneComponent)


////////////////////////////////////////////////////////////////////////////////
// SkySystem
////////////////////////////////////////////////////////////////////////////////

struct SkySystem::Implementation {

    Ogre::SceneManager* m_sceneManager = nullptr;

    EntityFilter<
        Optional<SkyPlaneComponent>
    > m_skyEntities;
};


SkySystem::SkySystem()
  : m_impl(new Implementation())
{
}


SkySystem::~SkySystem() {}


void
SkySystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_sceneManager = ogreEngine->sceneManager();
    m_impl->m_skyEntities.setEngine(engine);
}


void
SkySystem::shutdown() {
    m_impl->m_skyEntities.setEngine(nullptr);
    m_impl->m_sceneManager->setSkyBoxEnabled(false);
    m_impl->m_sceneManager->setSkyDomeEnabled(false);
    m_impl->m_sceneManager->setSkyPlaneEnabled(false);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
SkySystem::update(int) {
    for (auto& value : m_impl->m_skyEntities) {
        SkyPlaneComponent* plane = std::get<0>(value.second);
        if (plane and plane->m_properties.hasChanges()) {
            const SkyPlaneComponent::Properties& properties = plane->m_properties.stable();
            m_impl->m_sceneManager->setSkyPlane(
                properties.enabled,
                properties.plane,
                properties.materialName,
                properties.scale,
                properties.tiling,
                properties.drawFirst,
                properties.bow,
                properties.xsegments,
                properties.ysegments,
                properties.groupName
            );
            plane->m_properties.untouch();
        }
    }
}
