#include "ogre/mesh_system.h"

#include "common/transform.h"
#include "engine/component_factory.h"
#include "engine/entity_filter.h"
#include "ogre/ogre_engine.h"
#include "scripting/luabind.h"

#include <OgreEntity.h>
#include <OgreSceneManager.h>

#include <iostream>

using namespace thrive;


static void
MeshComponent_touch(
    MeshComponent* self
) {
    return self->m_properties.touch();
}


static MeshComponent::Properties&
MeshComponent_getWorkingCopy(
    MeshComponent* self
) {
    return self->m_properties.workingCopy();
}


static const MeshComponent::Properties&
MeshComponent_getLatest(
    MeshComponent* self
) {
    return self->m_properties.latest();
}


luabind::scope
MeshComponent::luaBindings() {
    using namespace luabind;
    return class_<MeshComponent, Component, std::shared_ptr<Component>>("MeshComponent")
        .scope [
            def("TYPE_NAME", &MeshComponent::TYPE_NAME),
            def("TYPE_ID", &MeshComponent::TYPE_ID),
            class_<Properties>("Properties")
                .def_readwrite("meshName", &Properties::meshName)
        ]
        .def(constructor<>())
        .property("latest", MeshComponent_getLatest)
        .property("workingCopy", MeshComponent_getWorkingCopy)
        .def("touch", MeshComponent_touch)
    ;
}

REGISTER_COMPONENT(MeshComponent)

////////////////////////////////////////////////////////////////////////////////
// MeshSystem
////////////////////////////////////////////////////////////////////////////////

struct MeshSystem::Implementation {

    struct OgreEntity {

        Ogre::Entity* entity = nullptr;

        Ogre::SceneNode* sceneNode = nullptr;

        FrameIndex lastMeshUpdate = 0;

        FrameIndex lastTransformUpdate = 0;

    };

    Implementation()
      : m_entities(true)
    {
    }

    EntityFilter<
        MeshComponent,
        TransformComponent
    > m_entities;

    Ogre::SceneManager* m_sceneManager = nullptr;

    std::unordered_map<EntityId, OgreEntity> m_ogreEntities;

};


MeshSystem::MeshSystem()
  : m_impl(new Implementation())
{
}


MeshSystem::~MeshSystem() {}


void
MeshSystem::init(
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
MeshSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
MeshSystem::update(int) {
    for (EntityId entityId : m_impl->m_entities.addedEntities()) {
        Implementation::OgreEntity ogreEntity;
        Ogre::SceneNode* rootNode = m_impl->m_sceneManager->getRootSceneNode();
        ogreEntity.sceneNode = rootNode->createChildSceneNode();
        m_impl->m_ogreEntities.emplace(entityId, std::move(ogreEntity));
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Implementation::OgreEntity& ogreEntity = m_impl->m_ogreEntities.at(entityId);
        ogreEntity.sceneNode->removeAndDestroyAllChildren();
        m_impl->m_sceneManager->destroyEntity(ogreEntity.entity);
        m_impl->m_sceneManager->destroySceneNode(ogreEntity.sceneNode);
        m_impl->m_ogreEntities.erase(entityId);
    }
    m_impl->m_entities.addedEntities().clear();
    m_impl->m_entities.removedEntities().clear();
    // Update entities, if necessary
    for (auto& value : m_impl->m_entities.entities()) {
        EntityId entityId = value.first;
        MeshComponent* meshComponent = std::get<0>(value.second);
        TransformComponent* transformComponent = std::get<1>(value.second);
        Implementation::OgreEntity& ogreEntity = m_impl->m_ogreEntities.at(entityId);
        if (ogreEntity.lastMeshUpdate < meshComponent->m_properties.stableVersion()) {
            if (ogreEntity.entity) {
                ogreEntity.sceneNode->detachObject(ogreEntity.entity);
            }
            ogreEntity.entity = m_impl->m_sceneManager->createEntity(
                meshComponent->m_properties.stable().meshName
            );
            ogreEntity.entity->setMaterialName("Examples/SphereMappedRustySteel");
            std::cout << "Created ogre entity with mesh: " << meshComponent->m_properties.stable().meshName << std::endl;
            ogreEntity.sceneNode->attachObject(ogreEntity.entity);
            ogreEntity.lastMeshUpdate = meshComponent->m_properties.stableVersion();
        }
        if (ogreEntity.lastTransformUpdate < transformComponent->m_properties.stableVersion()) {
            const TransformComponent::Properties& properties = transformComponent->m_properties.stable();
            ogreEntity.sceneNode->setOrientation(properties.orientation);
            ogreEntity.sceneNode->setPosition(properties.position);
            ogreEntity.sceneNode->setScale(properties.scale);
            ogreEntity.lastTransformUpdate = transformComponent->m_properties.stableVersion();
        }
    }
}

