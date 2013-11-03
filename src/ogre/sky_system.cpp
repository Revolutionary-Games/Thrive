#include "ogre/sky_system.h"

#include "engine/component_factory.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "engine/serialization.h"
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
    return class_<SkyPlaneComponent, Component>("SkyPlaneComponent")
        .enum_("ID") [
            value("TYPE_ID", SkyPlaneComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &SkyPlaneComponent::TYPE_NAME),
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


void
SkyPlaneComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_properties.enabled = storage.get<bool>("enabled");
    m_properties.plane = storage.get<Ogre::Plane>("plane");
    m_properties.materialName = storage.get<Ogre::String>("materialName");
    m_properties.scale = storage.get<Ogre::Real>("scale");
    m_properties.tiling = storage.get<Ogre::Real>("tiling");
    m_properties.drawFirst = storage.get<bool>("drawFirst");
    m_properties.bow = storage.get<Ogre::Real>("bow");
    m_properties.xsegments = storage.get<int>("xsegments");
    m_properties.ysegments = storage.get<int>("ysegments");
    m_properties.groupName = storage.get<Ogre::String>("groupName");
}


StorageContainer
SkyPlaneComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<bool>("enabled", m_properties.enabled);
    storage.set<Ogre::Plane>("plane", m_properties.plane);
    storage.set<Ogre::String>("materialName", m_properties.materialName);
    storage.set<Ogre::Real>("scale", m_properties.scale);
    storage.set<Ogre::Real>("tiling", m_properties.tiling);
    storage.set<bool>("drawFirst", m_properties.drawFirst);
    storage.set<Ogre::Real>("bow", m_properties.bow);
    storage.set<int>("xsegments", m_properties.xsegments);
    storage.set<int>("ysegments", m_properties.ysegments);
    storage.set<Ogre::String>("groupName", m_properties.groupName);
    return storage;
}

REGISTER_COMPONENT(SkyPlaneComponent)


////////////////////////////////////////////////////////////////////////////////
// SkySystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
SkySystem::luaBindings() {
    using namespace luabind;
    return class_<SkySystem, System>("SkySystem")
        .def(constructor<>())
    ;
}


struct SkySystem::Implementation {

    Ogre::SceneManager* m_sceneManager = nullptr;

    EntityFilter<
        SkyPlaneComponent
    > m_skyPlanes = {true};
};


SkySystem::SkySystem()
  : m_impl(new Implementation())
{
}


SkySystem::~SkySystem() {}


void
SkySystem::init(
    GameState* gameState
) {
    System::init(gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_skyPlanes.setEntityManager(&gameState->entityManager());
}


void
SkySystem::shutdown() {
    m_impl->m_skyPlanes.setEntityManager(nullptr);
    m_impl->m_sceneManager->setSkyBoxEnabled(false);
    m_impl->m_sceneManager->setSkyDomeEnabled(false);
    m_impl->m_sceneManager->setSkyPlaneEnabled(false);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
SkySystem::update(int) {
    for (EntityId id : m_impl->m_skyPlanes.removedEntities()) {
        (void) id;
        m_impl->m_sceneManager->setSkyPlaneEnabled(false);
    }
    m_impl->m_skyPlanes.clearChanges();
    for (auto& item : m_impl->m_skyPlanes) {
        m_impl->m_sceneManager->setSkyPlaneEnabled(true);
        SkyPlaneComponent* plane = std::get<0>(item.second);
        if (plane->m_properties.hasChanges()) {
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
