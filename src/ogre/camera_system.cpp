#include "ogre/camera_system.h"

#include "engine/component_registry.h"
#include "engine/entity_filter.h"
#include "ogre/ogre_engine.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include <iostream>
#include <OgreSceneManager.h>
#include <OgreCamera.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// OgreCameraComponent
////////////////////////////////////////////////////////////////////////////////

static void
OgreCameraComponent_touch(
    OgreCameraComponent* self
) {
    return self->m_properties.touch();
}


static OgreCameraComponent::Properties&
OgreCameraComponent_getWorkingCopy(
    OgreCameraComponent* self
) {
    return self->m_properties.workingCopy();
}


static const OgreCameraComponent::Properties&
OgreCameraComponent_getLatest(
    OgreCameraComponent* self
) {
    return self->m_properties.latest();
}


luabind::scope
OgreCameraComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreCameraComponent, Component, std::shared_ptr<Component>>("OgreCameraComponent")
        .scope [
            def("TYPE_NAME", &OgreCameraComponent::TYPE_NAME),
            def("TYPE_ID", &OgreCameraComponent::TYPE_ID),
            class_<Properties>("Properties")
                .def_readwrite("polygonMode", &Properties::polygonMode)
                .def_readwrite("fovY", &Properties::fovY)
                .def_readwrite("nearClipDistance", &Properties::nearClipDistance)
                .def_readwrite("farClipDistance", &Properties::farClipDistance)
                .def_readwrite("aspectRatio", &Properties::aspectRatio)
        ]
        .enum_("PolygonMode") [
            value("PM_POINTS", Ogre::PM_POINTS),
            value("PM_WIREFRAME", Ogre::PM_WIREFRAME),
            value("PM_SOLID", Ogre::PM_SOLID)
        ]
        .def(constructor<std::string>())
        .property("latest", OgreCameraComponent_getLatest)
        .property("workingCopy", OgreCameraComponent_getWorkingCopy)
        .def("touch", OgreCameraComponent_touch)
    ;
}

OgreCameraComponent::OgreCameraComponent(
    std::string name
) : m_name(name)
{
}

REGISTER_COMPONENT(OgreCameraComponent)


////////////////////////////////////////////////////////////////////////////////
// OgreCameraSystem
////////////////////////////////////////////////////////////////////////////////

struct OgreCameraSystem::Implementation {

    std::unordered_map<EntityId, Ogre::Camera*> m_cameras;

    Ogre::SceneManager* m_sceneManager = nullptr;

    EntityFilter<
        OgreSceneNodeComponent,
        OgreCameraComponent
    > m_entities = {true};
};


OgreCameraSystem::OgreCameraSystem()
  : m_impl(new Implementation())
{
}


OgreCameraSystem::~OgreCameraSystem() {}


void
OgreCameraSystem::init(
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
OgreCameraSystem::shutdown() {
    m_impl->m_entities.setEngine(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreCameraSystem::update(int) {
    for (auto& value : m_impl->m_entities.addedEntities()) {
        EntityId entityId = value.first;
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(value.second);
        OgreCameraComponent* cameraComponent = std::get<1>(value.second);
        Ogre::Camera* camera = m_impl->m_sceneManager->createCamera(
            cameraComponent->m_name
        );
        cameraComponent->m_camera = camera;
        m_impl->m_cameras[entityId] = camera;
        sceneNodeComponent->m_sceneNode->attachObject(camera);
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Camera* camera = m_impl->m_cameras[entityId];
        Ogre::SceneNode* sceneNode = camera->getParentSceneNode();
        sceneNode->detachObject(camera);
        m_impl->m_sceneManager->destroyCamera(camera);
        m_impl->m_cameras.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
    for (auto& value : m_impl->m_entities) {
        OgreCameraComponent* cameraComponent = std::get<1>(value.second);
        if (cameraComponent->m_properties.hasChanges()) {
            const auto& properties = cameraComponent->m_properties.stable();
            Ogre::Camera* camera = cameraComponent->m_camera;
            // Update camera
            camera->setPolygonMode(properties.polygonMode);
            camera->setFOVy(properties.fovY);
            camera->setNearClipDistance(properties.nearClipDistance);
            camera->setFarClipDistance(properties.farClipDistance);
            camera->setAspectRatio(properties.aspectRatio);
            // Untouch
            cameraComponent->m_properties.untouch();
        }
    }
}

