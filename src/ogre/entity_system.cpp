#include "ogre/entity_system.h"

#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include <OgreEntity.h>
#include <OgreSceneManager.h>

#include <iostream>

using namespace thrive;


OgreEntityComponent::OgreEntityComponent(
    std::string meshName
) : m_meshName(meshName),
    m_prefabType(Ogre::SceneManager::PT_SPHERE)
{
}


OgreEntityComponent::OgreEntityComponent(
    Ogre::SceneManager::PrefabType prefabType
) : m_meshName(""),
    m_prefabType(prefabType)
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
        .enum_("PrefabType") [
            value("PT_PLANE", Ogre::SceneManager::PT_PLANE),
            value("PT_CUBE", Ogre::SceneManager::PT_CUBE),
            value("PT_SPHERE", Ogre::SceneManager::PT_SPHERE)
        ]
        .def(constructor<std::string>())
        .def(constructor<Ogre::SceneManager::PrefabType>())
        .def_readonly("meshName", &OgreEntityComponent::m_meshName)
        .def_readonly("prefabType", &OgreEntityComponent::m_prefabType)
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
    m_impl->m_sceneManager = engine->sceneManager();
    m_impl->m_entities.setEntityManager(&engine->entityManager());
}


void
OgreEntitySystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreEntitySystem::update(int) {
    for (const auto& entry : m_impl->m_entities.addedEntities()) {
        EntityId entityId = entry.first;
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(entry.second);
        OgreEntityComponent* ogreEntityComponent = std::get<1>(entry.second);
        Ogre::Entity* ogreEntity = nullptr;
        if (not ogreEntityComponent->m_meshName.empty()) {
            ogreEntity = m_impl->m_sceneManager->createEntity(
                ogreEntityComponent->m_meshName
            );
        }
        else {
            ogreEntity = m_impl->m_sceneManager->createEntity(
                ogreEntityComponent->m_prefabType
            );
        }
        sceneNodeComponent->m_sceneNode->attachObject(ogreEntity);
        m_impl->m_ogreEntities.emplace(entityId, ogreEntity);
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Entity* ogreEntity = m_impl->m_ogreEntities.at(entityId);
        ogreEntity->detachFromParent();
        m_impl->m_sceneManager->destroyEntity(ogreEntity);
        m_impl->m_ogreEntities.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
}


