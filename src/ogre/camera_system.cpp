#include "ogre/camera_system.h"

#include "engine/component_factory.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "engine/serialization.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"

#include <iostream>
#include <OgreSceneManager.h>
#include <OgreCamera.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// OgreCameraComponent
////////////////////////////////////////////////////////////////////////////////


static Ogre::Ray
OgreCameraComponent_getCameraToViewportRay(
    const OgreCameraComponent* self,
    Ogre::Real x,
    Ogre::Real y
) {
    if (self->m_camera) {
        return self->m_camera->getCameraToViewportRay(x, y);
    }
    else {
        return Ogre::Ray();
    }
}

luabind::scope
OgreCameraComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreCameraComponent, Component>("OgreCameraComponent")
        .enum_("ID") [
            value("TYPE_ID", OgreCameraComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &OgreCameraComponent::TYPE_NAME),
            class_<Properties, Touchable>("Properties")
                .def_readwrite("polygonMode", &Properties::polygonMode)
                .def_readwrite("fovY", &Properties::fovY)
                .def_readwrite("nearClipDistance", &Properties::nearClipDistance)
                .def_readwrite("farClipDistance", &Properties::farClipDistance)
                .def_readwrite("orthographicalMode", &Properties::orthographicalMode)
                .def_readwrite("offset", &Properties::offset)
        ]
        .enum_("PolygonMode") [
            value("PM_POINTS", Ogre::PM_POINTS),
            value("PM_WIREFRAME", Ogre::PM_WIREFRAME),
            value("PM_SOLID", Ogre::PM_SOLID)
        ]
        .def(constructor<std::string>())
        .def("getCameraToViewportRay", OgreCameraComponent_getCameraToViewportRay)
        .def_readonly("properties", &OgreCameraComponent::m_properties)
    ;
}

OgreCameraComponent::OgreCameraComponent(
    std::string name
) : m_name(name)
{
}

OgreCameraComponent::OgreCameraComponent()
  : OgreCameraComponent("")
{
}


void
OgreCameraComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_name = storage.get<Ogre::String>("name");
    m_properties.farClipDistance = storage.get<Ogre::Real>("farClipDistance", 10000.0f);
    m_properties.fovY = storage.get<Ogre::Degree>("fovY", Ogre::Degree(45.0f));
    m_properties.nearClipDistance = storage.get<Ogre::Real>("nearClipDistance", 5.0f);
    m_properties.orthographicalMode = storage.get<bool>("orthographicalMode", false);
    m_properties.polygonMode = static_cast<Ogre::PolygonMode>(
        storage.get<int16_t>("polygonMode", Ogre::PM_SOLID)
    );
    m_properties.offset = storage.get<Ogre::Vector3>("offset", Ogre::Vector3(0,0,10));
}

std::string
OgreCameraComponent::name() const {
    return m_name;
}


StorageContainer
OgreCameraComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set("name", m_name);
    storage.set<Ogre::Real>("farClipDistance", m_properties.farClipDistance);
    storage.set<Ogre::Degree>("fovY", m_properties.fovY);
    storage.set<Ogre::Real>("nearClipDistance", m_properties.nearClipDistance);
    storage.set<bool>("orthographicalMode", m_properties.orthographicalMode);
    storage.set<int16_t>("polygonMode", m_properties.polygonMode);
    storage.set<Ogre::Vector3>("offset", m_properties.offset);
    return storage;
}

REGISTER_COMPONENT(OgreCameraComponent)


////////////////////////////////////////////////////////////////////////////////
// OgreCameraSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreCameraSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreCameraSystem, System>("OgreCameraSystem")
        .def(constructor<>())
    ;
}


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
    GameState* gameState
) {
    System::initNamed("OgreCameraSystem", gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreCameraSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreCameraSystem::update(int, int) {
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::Camera* camera = m_impl->m_cameras[entityId];
        if (camera) {
            Ogre::SceneNode* sceneNode = camera->getParentSceneNode();
            sceneNode->detachObject(camera);
            m_impl->m_sceneManager->destroyCamera(camera);
        }
        m_impl->m_cameras.erase(entityId);
    }
    for (auto& value : m_impl->m_entities.addedEntities()) {
        EntityId entityId = value.first;
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(value.second);
        OgreCameraComponent* cameraComponent = std::get<1>(value.second);
        Ogre::Camera* camera = m_impl->m_sceneManager->createCamera(
            cameraComponent->name()
        );
        camera->setAutoAspectRatio(true);
        cameraComponent->m_camera = camera;
        m_impl->m_cameras[entityId] = camera;
        camera->detachFromParent();
        sceneNodeComponent->m_sceneNode->attachObject(camera);
    }
    m_impl->m_entities.clearChanges();
    for (auto& value : m_impl->m_entities) {
        OgreCameraComponent* cameraComponent = std::get<1>(value.second);
        auto& properties = cameraComponent->m_properties;
        if (properties.hasChanges()) {
            Ogre::Camera* camera = cameraComponent->m_camera;
            // Update camera
            camera->setPolygonMode(properties.polygonMode);
            camera->setNearClipDistance(properties.nearClipDistance);
            camera->setFarClipDistance(properties.farClipDistance);
            if (properties.orthographicalMode){
                camera->setProjectionType(Ogre::ProjectionType::PT_ORTHOGRAPHIC);

                camera->setOrthoWindow(properties.fovY.valueDegrees()*camera->getAspectRatio(), properties.fovY.valueDegrees()); //Abstract conversion
            }
            else {
                camera->setProjectionType(Ogre::ProjectionType::PT_PERSPECTIVE);
                camera->setFOVy(properties.fovY);
            }
            // Untouch
            properties.untouch();
        }
    }
}

