#include "workspace_system.h"

#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/camera_system.h"
#include "scripting/luabind.h"

#include <Compositor/OgreCompositorCommon.h>
#include <Compositor/OgreCompositorManager2.h>
#include <Compositor/OgreCompositorNodeDef.h>
#include <Compositor/OgreCompositorShadowNodeDef.h>
#include <Compositor/OgreCompositorWorkspace.h>
#include <Compositor/OgreCompositorWorkspaceDef.h>
#include <Compositor/OgreCompositorWorkspaceListener.h>
#include <Compositor/Pass/PassClear/OgreCompositorPassClear.h>
#include <Compositor/Pass/PassScene/OgreCompositorPassScene.h>
#include <OgreRenderWindow.h>
#include <OgreRoot.h>
#include <luabind/adopt_policy.hpp>


using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// OgreWorkspace
////////////////////////////////////////////////////////////////////////////////

static Entity
Properties_getCameraEntity(
    const OgreWorkspaceComponent::Properties* self
) {
    return Entity(self->cameraEntity);
}


static void
Properties_setCameraEntity(
    OgreWorkspaceComponent::Properties* self,
    const Entity& entity
) {
    self->cameraEntity = entity.id();
}


luabind::scope
OgreWorkspaceComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreWorkspaceComponent, Component>("OgreWorkspaceComponent")
        .scope [
            class_<Properties, Touchable>("Properties")
            .property("cameraEntity", Properties_getCameraEntity, Properties_setCameraEntity)
            .def_readwrite("position", &Properties::position)
        ]
        .def(constructor<std::string>())
        .def_readonly("properties", &OgreWorkspaceComponent::m_properties)
        ;
}

OgreWorkspaceComponent::OgreWorkspaceComponent(
    std::string name
) : m_name(name)
{
}

OgreWorkspaceComponent::OgreWorkspaceComponent() :
    m_name("dummy_component")
{
}

void
OgreWorkspaceComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_properties.cameraEntity = storage.get<EntityId>("cameraEntity");
    m_properties.position = storage.get<int>("position");
    m_name = storage.get<std::string>("name");
}


StorageContainer
OgreWorkspaceComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set("name", m_name);
    storage.set("cameraEntity", m_properties.cameraEntity);
    storage.set("position", m_properties.position);
    return storage;
}

REGISTER_COMPONENT(OgreWorkspaceComponent)
////////////////////////////////////////////////////////////////////////////////
// OgreWorkspaceSystem
////////////////////////////////////////////////////////////////////////////////


luabind::scope
OgreWorkspaceSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreWorkspaceSystem, System>("OgreWorkspaceSystem")
        .def(constructor<>())
        ;
}


struct OgreWorkspaceSystem::Implementation {


    Implementation(
        OgreWorkspaceSystem& system
    ) : m_system(system),
        m_root(Ogre::Root::getSingleton())
    {
        if(!workspacesCreated)
            createCommonWorkspaceDefinitions();
    }


    void
    removeAllWorkspaces() {

        for (const auto& item : m_entities) {
            OgreWorkspaceComponent* viewportComponent = std::get<0>(item.second);
            viewportComponent->m_workspace = nullptr;
        }

        for (const auto& pair : m_workspaces) {
            this->removeWorkspace(pair.second);
        }

        m_workspaces.clear();
    }

    void
    removeWorkspace(
        Ogre::CompositorWorkspace* workspace
    ) {
        m_root.getCompositorManager2()->removeWorkspace(workspace);
    }


    void
    restoreAllWorkspaces() {
        for (const auto& item : m_entities) {
            EntityId entityId = item.first;
            OgreWorkspaceComponent* component = std::get<0>(item.second);
            this->restoreWorkspace(entityId, component);
        }
    }

    void
    restoreWorkspace(
        EntityId entityId,
        OgreWorkspaceComponent* component
    ) {
        if (component->m_workspace) {
            // No need to restore
            return;
        }

        // TODO: add support for different targets than the main window

        // Find camera (if any)
        Ogre::Camera* camera = nullptr;
        auto cameraComponent = m_system.entityManager()->getComponent<OgreCameraComponent>(
            component->m_properties.cameraEntity
        );

        if (cameraComponent) {
            camera = cameraComponent->m_camera;
        }

        // Create workspace
        auto workspace = m_root.getCompositorManager2()->addWorkspace(m_sceneManager, m_renderWindow, camera,
            component->m_name, true, component->m_properties.position);

        component->m_workspace = workspace;

        m_workspaces.emplace(
            entityId,
            workspace
        );
    }

    void
    reCreateWorkspace(
        EntityId entityId,
        OgreWorkspaceComponent* component
    ) {

        if(component->m_workspace){

            // Destroy the old workspace
            for (auto iter = m_workspaces.begin(); iter != m_workspaces.end(); ++iter) {

                if (iter->second == component->m_workspace) {

                    removeWorkspace(component->m_workspace);
                    m_workspaces.erase(iter);
                    break;
                }
            }

            component->m_workspace = nullptr;
        }

        restoreWorkspace(entityId, component);
    }

    static
    void createCommonWorkspaceDefinitions(){

        using namespace Ogre;

        auto manager = Ogre::Root::getSingleton().getCompositorManager2();

        auto thriveDefault = manager->addWorkspaceDefinition("thrive_default");

        auto sceneNode = manager->addNodeDefinition("thrive_default_scene_pass");

        // TODO: finish making shadow node work
        //auto shadowNode = manager->addShadowNodeDefinition("thrive_default_shadow");

        // Only one pass is used
        sceneNode->setNumTargetPass(1);

        // only output to the main target
        sceneNode->setNumOutputChannels(1);

        // This may not be required, as no earlier rendered images are used
        sceneNode->addTextureSourceName("renderwindow", 0,
            Ogre::TextureDefinitionBase::TEXTURE_INPUT);

        // Two stage pass, clear first and then render
        auto nodePasses = sceneNode->addTargetPass("renderwindow");

        nodePasses->setNumPasses(2);

        // Clear the render target before rendering
        auto clearPass = static_cast<Ogre::CompositorPassClearDef*>(
            nodePasses->addPass(Ogre::PASS_CLEAR));

        clearPass->mClearBufferFlags = Ogre::FBT_DEPTH | Ogre::FBT_STENCIL | Ogre::FBT_COLOUR;

        // Render scene contents
        auto scenePass = static_cast<Ogre::CompositorPassSceneDef*>(
            nodePasses->addPass(Ogre::PASS_SCENE));

        scenePass->mFirstRQ = Ogre::RENDER_QUEUE_BACKGROUND;
        scenePass->mLastRQ = Ogre::RENDER_QUEUE_MAX+1;


        // Connect the main render target to the node
        thriveDefault->connectOutput("thrive_default_scene_pass", 0);

        workspacesCreated = true;
    }

    EntityFilter<OgreWorkspaceComponent> m_entities = {true};

    Ogre::RenderWindow* m_renderWindow = nullptr;

    //! @todo Allow making workspaces with different scene managers
    Ogre::SceneManager* m_sceneManager = nullptr;

    OgreWorkspaceSystem& m_system;

    std::unordered_map<EntityId, Ogre::CompositorWorkspace*> m_workspaces;

    const Ogre::Root& m_root;

    static bool workspacesCreated;
};

bool OgreWorkspaceSystem::Implementation::workspacesCreated = false;


OgreWorkspaceSystem::OgreWorkspaceSystem()
    : m_impl(new Implementation(*this))
{
}


OgreWorkspaceSystem::~OgreWorkspaceSystem() {}


void
OgreWorkspaceSystem::activate() {
    m_impl->restoreAllWorkspaces();
    m_impl->m_entities.clearChanges();
}


void
OgreWorkspaceSystem::deactivate() {
    m_impl->removeAllWorkspaces();
}


void
OgreWorkspaceSystem::init(
    GameState* gameState
) {
    System::initNamed("OgreWorkspaceSystem", gameState);
    m_impl->m_renderWindow = this->engine()->renderWindow();
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}

void
OgreWorkspaceSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_renderWindow = nullptr;
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}

void
OgreWorkspaceSystem::update(int, int) {
    for (EntityId id : m_impl->m_entities.removedEntities()) {
        auto workspace = m_impl->m_workspaces[id];
        m_impl->removeWorkspace(workspace);
    }
    for (const auto& item : m_impl->m_entities.addedEntities()) {
        EntityId entityId = item.first;
        OgreWorkspaceComponent* component = std::get<0>(item.second);
        m_impl->restoreWorkspace(entityId, component);
    }
    m_impl->m_entities.clearChanges();
    for (const auto& item : m_impl->m_entities) {
        OgreWorkspaceComponent* component = std::get<0>(item.second);
        auto& properties = component->m_properties;
        if (properties.hasChanges()) {

            m_impl->reCreateWorkspace(item.first, component);

            properties.untouch();
        }
    }
}



