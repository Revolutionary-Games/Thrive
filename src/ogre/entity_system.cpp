#include "ogre/entity_system.h"

#include "engine/entity_filter.h"
#include "ogre/ogre_engine.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include <OgreEntity.h>
#include <OgreSceneManager.h>

#include <iostream>

using namespace thrive;


OgreEntityComponent::OgreEntityComponent(
    std::string meshName
) : m_meshName(meshName)
{
}


luabind::scope
OgreEntityComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreEntityComponent, Component, std::shared_ptr<Component>>("OgreEntityComponent")
        .scope [
            def("TYPE_NAME", &OgreEntityComponent::TYPE_NAME),
            def("TYPE_ID", &OgreEntityComponent::TYPE_ID)
        ]
        .def(constructor<std::string>())
        .def_readonly("meshName", &OgreEntityComponent::m_meshName)
    ;
}


////////////////////////////////////////////////////////////////////////////////
// OgreEntitySystem
////////////////////////////////////////////////////////////////////////////////

struct OgreEntitySystem::Implementation {

    Implementation()
      : m_entities(true)
    {
    }

    EntityFilter<
        OgreSceneNodeComponent,
        OgreEntityComponent
    > m_entities;

    std::unordered_map<EntityId, Ogre::Entity*> m_ogreEntities;

    Ogre::SceneManager* m_sceneManager = nullptr;

};


OgreEntitySystem::OgreEntitySystem()
  : m_impl(new Implementation())
{
}


OgreEntitySystem::~OgreEntitySystem() {}


void
OgreEntitySystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_sceneManager = ogreEngine->sceneManager();
    m_impl->m_entities.setEngine(engine);
}


void
OgreEntitySystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreEntitySystem::update(int) {
    for (const auto& entry : m_impl->m_entities.addedEntities()) {
        EntityId entityId = entry.first;
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(entry.second);
        OgreEntityComponent* ogreEntityComponent = std::get<1>(entry.second);
        Ogre::Entity* ogreEntity = m_impl->m_sceneManager->createEntity(
            ogreEntityComponent->m_meshName
        );
        sceneNodeComponent->m_sceneNode->attachObject(ogreEntity);
        m_impl->m_ogreEntities.emplace(entityId, ogreEntity);
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Entity* ogreEntity = m_impl->m_ogreEntities.at(entityId);
        ogreEntity->detachFromParent();
        m_impl->m_sceneManager->destroyEntity(ogreEntity);
        m_impl->m_ogreEntities.erase(entityId);
    }
}


