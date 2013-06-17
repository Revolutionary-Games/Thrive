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


luabind::scope
SkyPlaneComponent::luaBindings() {
    using namespace luabind;
    return class_<SkyPlaneComponent, Component, std::shared_ptr<Component>>("SkyPlaneComponent")
        .scope [
            def("TYPE_NAME", &SkyPlaneComponent::TYPE_NAME),
            def("TYPE_ID", &SkyPlaneComponent::TYPE_ID)
        ]
        .def(constructor<>())
        .def_readwrite("enabled", &SkyPlaneComponent::enabled)
        .def_readwrite("plane", &SkyPlaneComponent::plane)
        .def_readwrite("materialName", &SkyPlaneComponent::materialName)
        .def_readwrite("scale", &SkyPlaneComponent::scale)
        .def_readwrite("tiling", &SkyPlaneComponent::tiling)
        .def_readwrite("drawFirst", &SkyPlaneComponent::drawFirst)
        .def_readwrite("bow", &SkyPlaneComponent::bow)
        .def_readwrite("xsegments", &SkyPlaneComponent::xsegments)
        .def_readwrite("ysegments", &SkyPlaneComponent::ysegments)
        .def_readwrite("groupName", &SkyPlaneComponent::groupName)
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
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_sceneManager = ogreEngine->sceneManager();
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
        if (plane and plane->hasChanges()) {
            m_impl->m_sceneManager->setSkyPlane(
                plane->enabled,
                plane->plane,
                plane->materialName,
                plane->scale,
                plane->tiling,
                plane->drawFirst,
                plane->bow,
                plane->xsegments,
                plane->ysegments,
                plane->groupName
            );
            plane->untouch();
        }
    }
}
