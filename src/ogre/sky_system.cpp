#include "ogre/sky_system.h"

#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"

#include <iostream>
#include <OgreSceneManager.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// SkyPlaneComponent
////////////////////////////////////////////////////////////////////////////////


luabind::scope
SkyPlaneComponent::luaBindings() {
    using namespace luabind;
    return class_<SkyPlaneComponent, Component, std::shared_ptr<Component>>("SkyPlaneComponent")
        .scope [
            def("TYPE_NAME", &SkyPlaneComponent::TYPE_NAME),
            def("TYPE_ID", &SkyPlaneComponent::TYPE_ID),
            class_<Properties, Touchable>("Properties")
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
        .def_readonly("properties", &SkyPlaneComponent::m_properties)
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
    > m_entities;
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
    m_impl->m_sceneManager = engine->sceneManager();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
SkySystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager->setSkyBoxEnabled(false);
    m_impl->m_sceneManager->setSkyDomeEnabled(false);
    m_impl->m_sceneManager->setSkyPlaneEnabled(false);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
SkySystem::update(int) {
    for (auto& value : m_impl->m_entities) {
        SkyPlaneComponent* plane = std::get<0>(value.second);
        if (plane and plane->m_properties.hasChanges()) {
            auto& properties = plane->m_properties;
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
            properties.untouch();
        }
    }
}
