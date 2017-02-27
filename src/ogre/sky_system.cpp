#include "ogre/sky_system.h"

#include "engine/component_factory.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "engine/serialization.h"
#include "scripting/luajit.h"

#include <iostream>
#include <OgreSceneManager.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// SkyPlaneComponent
////////////////////////////////////////////////////////////////////////////////

void SkyPlaneComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<Properties>("SkyPlaneComponentProperties",

        sol::base_classes, sol::bases<Touchable>(),

        "enabled", &Properties::enabled,
        "plane", &Properties::plane,
        "materialName", &Properties::materialName,
        "scale", &Properties::scale,
        "tiling", &Properties::tiling,
        "drawFirst", &Properties::drawFirst,
        "bow", &Properties::bow,
        "xsegments", &Properties::xsegments,
        "ysegments", &Properties::ysegments,
        "groupName", &Properties::groupName
    );
    
    lua.new_usertype<SkyPlaneComponent>("SkyPlaneComponent",

        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<Component>(),

        "TYPE_ID", sol::var(SkyPlaneComponent::TYPE_ID),
        "TYPE_NAME", &SkyPlaneComponent::TYPE_NAME,

        "properties", sol::readonly(&SkyPlaneComponent::m_properties)
    );
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

void SkySystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<SkySystem>("SkySystem",

        sol::constructors<sol::types<>>(),
        
        sol::base_classes, sol::bases<System>()
    );
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
    GameStateData* gameState
) {
    System::initNamed("SkySystem", gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_skyPlanes.setEntityManager(gameState->entityManager());
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
SkySystem::update(int, int) {
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
