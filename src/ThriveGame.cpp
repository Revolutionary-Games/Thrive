// ------------------------------------ //
#include "ThriveGame.h"

#include "engine/player_data.h"
#include "general/global_keypresses.h"
#include "general/locked_map.h"
#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"
#include "main_menu_keypresses.h"
#include "microbe_stage/player_microbe_control.h"
#include "microbe_stage/simulation_parameters.h"
#include "microbe_stage/species_name_controller.h"
#include "thrive_net_handler.h"
#include "thrive_version.h"
#include "thrive_world_factory.h"

#include <Addons/GameModule.h>
#include <GUI/GuiView.h>
#include <Handlers/ObjectLoader.h>
#include <Networking/NetworkHandler.h>
#include <Newton/PhysicsMaterialManager.h>
#include <Rendering/GeometryHelpers.h>
#include <Script/Bindings/BindHelpers.h>
#include <Script/Bindings/StandardWorldBindHelper.h>
#include <Window.h>

#include <OgreManualObject.h>
#include <OgreMesh2.h>
#include <OgreMeshManager2.h>
#include <OgreRoot.h>
#include <OgreSceneManager.h>
#include <OgreSubMesh2.h>

// Includes for just bindings
#include "general/hex.h"
#include "general/timed_life_system.h"

using namespace thrive;

// ------------------------------------ //
//! Contains properties that would need unnecessary large includes in the header
class ThriveGame::Implementation {
public:
    Implementation(ThriveGame& game) :
        m_game(game), m_playerData("player"),
        m_menuKeyPresses(std::make_shared<MainMenuKeyPressListener>()),
        m_globalKeyPresses(std::make_shared<GlobalUtilityKeyHandler>(
            *game.ApplicationConfiguration->GetKeyConfiguration())),
        m_cellStageKeys(std::make_shared<PlayerMicrobeControl>(
            *game.ApplicationConfiguration->GetKeyConfiguration()))
    {}

    //! Releases Ogre things. Needs to be called before shutdown
    void
        releaseOgreResources()
    {
        destroyBackgroundItem();

        if(m_microbeBackgroundMesh) {

            Ogre::MeshManager::getSingleton().remove(m_microbeBackgroundMesh);
            m_microbeBackgroundMesh.reset();
            m_microbeBackgroundSubMesh = nullptr;
        }
    }

    void
        destroyBackgroundItem()
    {
        if(m_microbeBackgroundItem) {

            m_cellStage->GetScene()->destroyItem(m_microbeBackgroundItem);
            m_microbeBackgroundItem = nullptr;
        }

        if(m_microbeEditorBackgroundItem) {
            m_microbeEditor->GetScene()->destroyItem(
                m_microbeEditorBackgroundItem);
            m_microbeEditorBackgroundItem = nullptr;
        }
    }

    void
        createBackgroundItem()
    {
        destroyBackgroundItem();

        m_microbeBackgroundItem = m_cellStage->GetScene()->createItem(
            m_microbeBackgroundMesh, Ogre::SCENE_STATIC);
        m_microbeBackgroundItem->setCastShadows(false);

        // Need to edit the render queue and add it to an early one
        m_microbeBackgroundItem->setRenderQueueGroup(1);

        // Editor version
        if(m_microbeEditor) {
            m_microbeEditorBackgroundItem =
                m_microbeEditor->GetScene()->createItem(
                    m_microbeBackgroundMesh, Ogre::SCENE_STATIC);
            m_microbeEditorBackgroundItem->setCastShadows(false);

            // Need to edit the render queue and add it to an early one
            m_microbeEditorBackgroundItem->setRenderQueueGroup(1);
        }

        // Re-attach if the nodes exist
        // Add it
        if(m_backgroundRenderNode)
            m_backgroundRenderNode->attachObject(m_microbeBackgroundItem);

        if(m_editorBackgroundRenderNode)
            m_editorBackgroundRenderNode->attachObject(
                m_microbeEditorBackgroundItem);
    }

    ThriveGame& m_game;

    PlayerData m_playerData;

    std::shared_ptr<CellStageWorld> m_cellStage;
    std::shared_ptr<MicrobeEditorWorld> m_microbeEditor;

    // This contains all the microbe_stage AngelScript code
    Leviathan::GameModule::pointer m_MicrobeScripts;

    // This contains all the microbe_editor AngelScript code
    Leviathan::GameModule::pointer m_MicrobeEditorScripts;

    //! This is the background object of the cell stage
    Ogre::MeshPtr m_microbeBackgroundMesh;
    Ogre::SubMesh* m_microbeBackgroundSubMesh;
    Ogre::Item* m_microbeBackgroundItem = nullptr;
    Ogre::SceneNode* m_backgroundRenderNode = nullptr;

    Ogre::Item* m_microbeEditorBackgroundItem = nullptr;
    Ogre::SceneNode* m_editorBackgroundRenderNode = nullptr;

    std::shared_ptr<MainMenuKeyPressListener> m_menuKeyPresses;
    std::shared_ptr<GlobalUtilityKeyHandler> m_globalKeyPresses;
    std::shared_ptr<PlayerMicrobeControl> m_cellStageKeys;
};

// ------------------------------------ //
ThriveGame::ThriveGame()
{
    StaticGame = this;
}

ThriveGame::~ThriveGame()
{
    StaticGame = nullptr;
}

std::string
    ThriveGame::GenerateWindowTitle()
{
    return "Thrive " GAME_VERSIONS;
}

ThriveGame*
    ThriveGame::Get()
{
    return StaticGame;
}

ThriveGame*
    ThriveGame::instance()
{
    return StaticGame;
}

ThriveGame* ThriveGame::StaticGame = nullptr;

Leviathan::NetworkInterface*
    ThriveGame::_GetApplicationPacketHandler()
{
    if(!Network)
        Network = std::make_unique<ThriveNetHandler>();
    return Network.get();
}

void
    ThriveGame::_ShutdownApplicationPacketHandler()
{
    Network.reset();
}
// ------------------------------------ //
void
    ThriveGame::startNewGame()
{
    // To work with instant start, we need to invoke this if we have no cell
    // stage world
    if(!m_postLoadRan) {

        Engine::Get()->Invoke([=]() { startNewGame(); });
        return;
    }

    Leviathan::Engine* engine = Engine::GetEngine();

    LOG_INFO("New game started");

    Leviathan::Window* window1 = engine->GetWindowEntity();

    // Create world if not already created //
    if(!m_impl->m_cellStage) {

        LOG_INFO("ThriveGame: startNewGame: Creating new cellstage world");
        m_impl->m_cellStage =
            std::dynamic_pointer_cast<CellStageWorld>(engine->CreateWorld(
                window1, static_cast<int>(THRIVE_WORLD_TYPE::CELL_STAGE)));
    }

    LEVIATHAN_ASSERT(m_impl->m_cellStage, "Cell stage world creation failed");

    window1->LinkObjects(m_impl->m_cellStage);

    // Set the right input handlers active //
    m_impl->m_menuKeyPresses->setEnabled(false);
    m_impl->m_cellStageKeys->setEnabled(true);

    // And switch the GUI mode to allow key presses through
    Leviathan::GUI::View* view = window1->GetGui()->GetViewByIndex(0);
    // Allow running without GUI
    if(view)
        view->SetInputMode(Leviathan::GUI::INPUT_MODE::Gameplay);


    // Clear world //
    m_impl->m_cellStage->ClearEntities();

    // TODO: unfreeze, if was in the background

    // Main camera that will be attached to the player
    m_cellCamera = Leviathan::ObjectLoader::LoadCamera(*m_impl->m_cellStage,
        Float3(0, 15, 0),
        Ogre::Quaternion(Ogre::Degree(-90), Ogre::Vector3::UNIT_X));

    // Link the camera to the camera control system
    m_impl->m_cellStage->GetMicrobeCameraSystem().setCameraEntity(m_cellCamera);

    // TODO: attach a ligth to the camera
    // -- Light
    //     local light = OgreLightComponent.new()
    //     light:setRange(200)
    //     entity:addComponent(light)

    m_impl->m_cellStage->SetCamera(m_cellCamera);

    // Setup compound clouds //

    // This is needed for the compound clouds to work in generale
    const auto compoundCount = SimulationParameters::compoundRegistry.getSize();

    LEVIATHAN_ASSERT(SimulationParameters::compoundRegistry.getSize() > 0,
        "compound registry is empty when creating cloud entities for them");
    std::unordered_map<Leviathan::ObjectID, thrive::CompoundCloudComponent> u =
        {};

    std::vector<Compound> clouds;

    for(size_t i = 0; i < compoundCount; ++i) {

        const auto& data =
            SimulationParameters::compoundRegistry.getTypeData(i);

        if(!data.isCloud)
            continue;

        clouds.push_back(data);
    }

    m_impl->m_cellStage->GetCompoundCloudSystem().registerCloudTypes(
        *m_impl->m_cellStage, clouds);

    // Let the script do setup //
    // This registers all the script defined systems to run and be
    // available from the world
    LEVIATHAN_ASSERT(m_impl->m_MicrobeScripts, "microbe scripts not loaded");

    LOG_INFO("Calling world setup script setupScriptsForWorld");

    ScriptRunningSetup setup;
    setup.SetEntrypoint("setupScriptsForWorld");

    auto result = m_impl->m_MicrobeScripts->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run script setup function: " + setup.Entryfunction);
        MarkAsClosing();
        return;
    }

    LOG_INFO("Finished calling setupScriptsForWorld");

    // TODO: move to a new function to reduce clutter here
    // Set background plane //
    // This is needed to be created here for biome.as to work correctly
    // Also this is a manual object and with infinite extent as this isn't
    // perspective projected in the shader
    m_impl->m_backgroundRenderNode =
        m_impl->m_cellStage->GetScene()->createSceneNode(Ogre::SCENE_STATIC);

    // This needs to be manually destroyed later
    m_impl->m_microbeBackgroundMesh =
        Leviathan::GeometryHelpers::CreateScreenSpaceQuad(
            "CellStage_background", -1, -1, 2, 2);

    m_impl->m_microbeBackgroundSubMesh =
        m_impl->m_microbeBackgroundMesh->getSubMesh(0);

    m_impl->m_microbeBackgroundSubMesh->setMaterialName("Background");

    // Setup render queue for it
    m_impl->m_cellStage->GetScene()->getRenderQueue()->setRenderQueueMode(
        1, Ogre::RenderQueue::FAST);

    // This now attaches the item as well (as long as the scene node is created)
    // This makes it easier to manage the multiple backgrounds and reattaching
    // them
    m_impl->createBackgroundItem();

    // Spawn player //
    setup = ScriptRunningSetup("setupPlayer");

    result = m_impl->m_MicrobeScripts->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to spawn player!");
        return;
    }
}

void
    ThriveGame::loadSaveGame(const std::string& saveFile)
{
    // i hate the very idea of writing the same code twice, so i want to call
    // startNewGame first
    LOG_INFO("saved game being loaded");
    ThriveGame::startNewGame();
    // start a new game, then run a loading script
    //(the script will disable the tutorial etc)
}

void
    ThriveGame::saveGame(const std::string& saveFile)
{
    // i hate the very idea of writing the same code twice, so i want to call
    // startNewGame first
    LOG_INFO("game being saved");
    // start a new game, then run a loading script
    //(the script will disable the tutorial etc)
}
// ------------------------------------ //
bool
    ThriveGame::scriptSetup()
{
    LOG_INFO("Calling global setup script setupProcesses");

    ScriptRunningSetup setup("setupProcesses");

    auto result = m_impl->m_MicrobeScripts->ExecuteOnModule<void>(setup, false);

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run script setup function: " + setup.Entryfunction);
        return false;
    }

    LOG_INFO("Finished calling the above setup script");

    LOG_INFO("Calling global setup script setupOrganelles");

    setup = ScriptRunningSetup("setupOrganelles");

    result = m_impl->m_MicrobeScripts->ExecuteOnModule<void>(setup, false);

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run script setup function: " + setup.Entryfunction);
        return false;
    }

    LOG_INFO("Finished calling the above setup script");


    LOG_INFO("Finished calling script setup");
    return true;
}
// ------------------------------------ //
CellStageWorld*
    ThriveGame::getCellStage()
{
    return m_impl->m_cellStage.get();
}

PlayerData&
    ThriveGame::playerData()
{
    return m_impl->m_playerData;
}

PlayerMicrobeControl*
    ThriveGame::getPlayerInput()
{
    return m_impl->m_cellStageKeys.get();
}

Leviathan::GameModule*
    ThriveGame::getMicrobeScripts()
{
    return m_impl->m_MicrobeScripts.get();
}
// ------------------------------------ //
void
    ThriveGame::onIntroSkipPressed()
{
    // Fire an event that the GUI handles //
    Engine::Get()->GetEventHandler()->CallEvent(
        new Leviathan::GenericEvent("MainMenuIntroSkipEvent"));
}

void
    ThriveGame::killPlayerCellClicked()
{
    LOG_INFO("Calling killPlayerCellClicked");

    ScriptRunningSetup setup = ScriptRunningSetup("killPlayerCellClicked");

    auto result = m_impl->m_MicrobeScripts->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run killPlayerCellClicked: " + setup.Entryfunction);
    }
}

void
    ThriveGame::editorButtonClicked()
{
    LOG_INFO("Editor button pressed");

    // Fire an event to switch over the GUI
    Engine::Get()->GetEventHandler()->CallEvent(
        new Leviathan::GenericEvent("MicrobeEditorEntered"));

    Leviathan::Engine* engine = Engine::GetEngine();
    Leviathan::Window* window1 = engine->GetWindowEntity();

    // Make the cell world be in the background

    // Create an editor world
    LOG_INFO("Entering MicrobeEditor");

    // Create world if not already created //
    if(!m_impl->m_microbeEditor) {

        LOG_INFO("ThriveGame: editorButtonClicked: Creating new microbe editor "
                 "world");
        m_impl->m_microbeEditor =
            std::dynamic_pointer_cast<MicrobeEditorWorld>(engine->CreateWorld(
                window1, static_cast<int>(THRIVE_WORLD_TYPE::MICROBE_EDITOR)));
    }

    LEVIATHAN_ASSERT(
        m_impl->m_microbeEditor, "Microbe editor world creation failed");

    // Link the new world to the window (this will automatically make
    // the old one go to the background)
    window1->LinkObjects(m_impl->m_microbeEditor);

    // Set the right input handlers active //
    m_impl->m_menuKeyPresses->setEnabled(false);
    m_impl->m_cellStageKeys->setEnabled(false);
    // TODO: editor hotkeys

    // Clear world //
    m_impl->m_microbeEditor->ClearEntities();

    // TODO: unfreeze, if was in the background

    // Main camera that will be attached to the player
    auto camera = Leviathan::ObjectLoader::LoadCamera(*m_impl->m_microbeEditor,
        Float3(0, 15, 0),
        Ogre::Quaternion(Ogre::Degree(-90), Ogre::Vector3::UNIT_X));

    // TODO: attach a ligth to the camera
    // -- Light
    //     local light = OgreLightComponent.new()
    //     light:setRange(200)
    //     entity:addComponent(light)

    m_impl->m_microbeEditor->SetCamera(camera);

    // Background
    m_impl->m_editorBackgroundRenderNode =
        m_impl->m_microbeEditor->GetScene()->createSceneNode(
            Ogre::SCENE_STATIC);

    // Setup render queue for it
    m_impl->m_microbeEditor->GetScene()->getRenderQueue()->setRenderQueueMode(
        1, Ogre::RenderQueue::FAST);

    // This also attaches it
    m_impl->createBackgroundItem();

    // Let the script do setup //
    // This registers all the script defined systems to run and be
    // available from the world
    LEVIATHAN_ASSERT(
        m_impl->m_MicrobeEditorScripts, "microbe editor scripts not loaded");

    LOG_INFO("Calling editor setup script onEditorEntry");

    ScriptRunningSetup setup("onEditorEntry");

    auto result = m_impl->m_MicrobeEditorScripts->ExecuteOnModule<void>(
        setup, false, m_impl->m_microbeEditor.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run editor setup function: " + setup.Entryfunction);
        return;
    }
}

void
    ThriveGame::finishEditingClicked()
{
    LOG_INFO("Finish editing pressed");

    // Fire an event to switch over the GUI
    Engine::Get()->GetEventHandler()->CallEvent(
        new Leviathan::GenericEvent("MicrobeEditorExited"));

    Leviathan::Engine* engine = Engine::GetEngine();
    Leviathan::Window* window1 = engine->GetWindowEntity();

    // Make the cell world to be back in the foreground
    LEVIATHAN_ASSERT(m_impl->m_cellStage,
        "Cell stage world not created before exiting the editor");

    // This will automatically background the editor world
    window1->LinkObjects(m_impl->m_cellStage);

    // Set the right input handlers active //
    m_impl->m_menuKeyPresses->setEnabled(false);
    m_impl->m_cellStageKeys->setEnabled(true);
    // TODO: editor hotkeys

    // Run the post editing script

    // Let the script do setup //
    // This registers all the script defined systems to run and be
    // available from the world
    LEVIATHAN_ASSERT(
        m_impl->m_MicrobeScripts, "microbe stage scripts not loaded");

    LOG_INFO("Calling return from editor script, onReturnFromEditor");

    ScriptRunningSetup setup("onReturnFromEditor");

    auto result = m_impl->m_MicrobeScripts->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to run return from editor function: " +
                  setup.Entryfunction);
        return;
    }
}
// ------------------------------------ //
void
    ThriveGame::onZoomChange(float amount)
{
    if(m_impl->m_cellStage)
        m_impl->m_cellStage->GetMicrobeCameraSystem().changeCameraOffset(
            amount);
}

// ------------------------------------ //
void
    ThriveGame::setBackgroundMaterial(const std::string& material)
{
    LOG_INFO("Setting microbe background to: " + material);
    m_impl->m_microbeBackgroundSubMesh->setMaterialName(material);

    m_impl->createBackgroundItem();
}

// ------------------------------------ //
void
    ThriveGame::Tick(int mspassed)
{}

void
    ThriveGame::CustomizeEnginePostLoad()
{
    Engine* engine = Engine::Get();

    try {
        m_impl = std::make_unique<Implementation>(*this);
    } catch(const Leviathan::InvalidArgument& e) {

        LOG_ERROR("ThriveGame: loading configuration data failed: ");
        e.PrintToLog();
        MarkAsClosing();
        return;
    }

    // Load json data //
    SimulationParameters::init();

    // Load scripts
    LOG_INFO("ThriveGame: loading main scripts");

    // TODO: should these load failures be fatal errors (process would exit
    // immediately)

    try {
        m_impl->m_MicrobeScripts =
            Leviathan::GameModule::MakeShared<Leviathan::GameModule>(
                "microbe_stage", "ThriveGame");
    } catch(const Leviathan::Exception& e) {

        LOG_ERROR(
            "ThriveGame: microbe_stage module failed to load, exception:");
        e.PrintToLog();
        MarkAsClosing();
        return;
    }

    if(!m_impl->m_MicrobeScripts->Init()) {

        LOG_ERROR("ThriveGame: microbe_stage module init failed");
        MarkAsClosing();
        return;
    }

    try {
        m_impl->m_MicrobeEditorScripts =
            Leviathan::GameModule::MakeShared<Leviathan::GameModule>(
                "microbe_editor", "ThriveGame");
    } catch(const Leviathan::Exception& e) {

        LOG_ERROR(
            "ThriveGame: microbe_editor module failed to load, exception:");
        e.PrintToLog();
        MarkAsClosing();
        return;
    }

    if(!m_impl->m_MicrobeEditorScripts->Init()) {

        LOG_ERROR("ThriveGame: microbe_editor module init failed");
        MarkAsClosing();
        return;
    }

    LOG_INFO("ThriveGame: script loading succeeded");

    if(!scriptSetup()) {

        LOG_ERROR("ThriveGame: failed to run setup script functions");
        MarkAsClosing();
        return;
    }

    // This is fine to set here to avoid putting this behind the next no gui
    // check //
    m_postLoadRan = true;

    // Load GUI documents (but only if graphics are enabled) //
    if(engine->GetNoGui()) {

        // Skip the graphical objects when not in graphical mode //
        return;
    }

    Leviathan::Window* window1 = Engine::GetEngine()->GetWindowEntity();

    // Register custom listener for detecting keypresses for skipping the intro
    // video
    // TODO: these need to be disabled when not used
    window1->GetInputController()->LinkReceiver(m_impl->m_globalKeyPresses);

    window1->GetInputController()->LinkReceiver(m_impl->m_menuKeyPresses);

    // Register the player input listener
    window1->GetInputController()->LinkReceiver(m_impl->m_cellStageKeys);

    Leviathan::GUI::GuiManager* GuiManagerAccess = window1->GetGui();

    if(!GuiManagerAccess->LoadGUIFile("Data/Scripts/gui/thrive_gui.html")) {

        LOG_ERROR("Thrive: failed to load the main menu gui, quitting");
        StartRelease();
        return;
    }
}

//! \note This is called from a background thread
void
    cellHitFloatingOrganelle(const NewtonJoint* contact,
        dFloat timestep,
        int threadIndex)
{
    NewtonBody* first = NewtonJointGetBody0(contact);
    NewtonBody* second = NewtonJointGetBody1(contact);

    if(!first || !second)
        return;

    Leviathan::Physics* firstPhysics =
        static_cast<Leviathan::Physics*>(NewtonBodyGetUserData(first));
    Leviathan::Physics* secondPhysics =
        static_cast<Leviathan::Physics*>(NewtonBodyGetUserData(second));

    NewtonWorld* world = NewtonBodyGetWorld(first);
    Leviathan::PhysicalWorld* physicalWorld =
        static_cast<Leviathan::PhysicalWorld*>(NewtonWorldGetUserData(world));

    GameWorld* gameWorld = physicalWorld->GetGameWorld();

    ScriptRunningSetup setup("cellHitFloatingOrganelle");

    auto result = ThriveGame::Get()->getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, gameWorld, firstPhysics->ThisEntity,
        secondPhysics->ThisEntity);

    if(result.Result != SCRIPT_RUN_RESULT::Success)
        LOG_ERROR("Failed to run script side cellHitFloatingOrganelle");
}

//! \note This is called from a background thread
//! \todo This should return 0 when either cell is engulfing and apply the
//! damaging effect
int
    cellOnCellAABBHitCallback(const NewtonMaterial* material,
        const NewtonBody* body0,
        const NewtonBody* body1,
        int threadIndex)
{
    // LOG_INFO("Cell on cell AABB overlap");
    if(!body0 || !body1)
        return 1;

    Leviathan::Physics* firstPhysics =
        static_cast<Leviathan::Physics*>(NewtonBodyGetUserData(body0));
    Leviathan::Physics* secondPhysics =
        static_cast<Leviathan::Physics*>(NewtonBodyGetUserData(body1));

    NewtonWorld* world = NewtonBodyGetWorld(body0);
    Leviathan::PhysicalWorld* physicalWorld =
        static_cast<Leviathan::PhysicalWorld*>(NewtonWorldGetUserData(world));
    GameWorld* gameWorld = physicalWorld->GetGameWorld();

    // Grab microbe component


    // How do i grab the microbe info here and return information from
    // angelscript method? Return 0 for now to test it
    ScriptRunningSetup setup("beingEngulfed");

    // Causes errors as this has to release
    auto returned =
        ThriveGame::Get()->getMicrobeScripts()->ExecuteOnModule<int>(setup,
            false, gameWorld, firstPhysics->ThisEntity,
            secondPhysics->ThisEntity);

    if(returned.Result != SCRIPT_RUN_RESULT::Success)
        LOG_ERROR("Failed to run script side beingEngulfed");

    return returned.Value;
}

void
    cellOnCellActualContact(const NewtonJoint* contact,
        dFloat timestep,
        int threadIndex)
{
    NewtonBody* first = NewtonJointGetBody0(contact);
    NewtonBody* second = NewtonJointGetBody1(contact);

    if(!first || !second)
        return;

    Leviathan::Physics* firstPhysics =
        static_cast<Leviathan::Physics*>(NewtonBodyGetUserData(first));
    Leviathan::Physics* secondPhysics =
        static_cast<Leviathan::Physics*>(NewtonBodyGetUserData(second));

    NewtonWorld* world = NewtonBodyGetWorld(first);
    Leviathan::PhysicalWorld* physicalWorld =
        static_cast<Leviathan::PhysicalWorld*>(NewtonWorldGetUserData(world));

    GameWorld* gameWorld = physicalWorld->GetGameWorld();

    ScriptRunningSetup setup("cellOnCellActualContact");

    auto result = ThriveGame::Get()->getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, gameWorld, firstPhysics->ThisEntity,
        secondPhysics->ThisEntity);

    if(result.Result != SCRIPT_RUN_RESULT::Success)
        LOG_ERROR("Failed to run script side cellOnCellActualContact");
    // placeholder code taht runs when a cell is hit
}

//! \brief This registers the physical materials (with callbacks for
//! collision detection)
void
    ThriveGame::RegisterApplicationPhysicalMaterials(
        Leviathan::PhysicsMaterialManager* manager)
{
    // Setup materials
    auto cellMaterial = std::make_shared<Leviathan::PhysicalMaterial>("cell");
    auto floatingOrganelleMaterial =
        std::make_shared<Leviathan::PhysicalMaterial>("floatingOrganelle");

    // Set callbacks //
    cellMaterial->FormPairWith(*floatingOrganelleMaterial)
        .SetCallbacks(nullptr, cellHitFloatingOrganelle);
    cellMaterial->FormPairWith(*cellMaterial)
        .SetCallbacks(cellOnCellAABBHitCallback, cellOnCellActualContact);

    manager->LoadedMaterialAdd(cellMaterial);
    manager->LoadedMaterialAdd(floatingOrganelleMaterial);
}

void
    ThriveGame::EnginePreShutdown()
{
    // Shutdown scripting first to allow it to still do anything it wants //
    if(m_impl->m_MicrobeScripts) {
        m_impl->m_MicrobeScripts->ReleaseScript();
        m_impl->m_MicrobeScripts.reset();
    }

    if(m_impl->m_MicrobeEditorScripts) {
        m_impl->m_MicrobeEditorScripts->ReleaseScript();
        m_impl->m_MicrobeEditorScripts.reset();
    }

    // All resources that need Ogre or the engine to be available when
    // they are destroyed need to be released here

    m_impl->releaseOgreResources();

    if(m_impl->m_cellStage != nullptr)
        m_impl->m_cellStage->Release();

    // And garbage collect //
    // This is needed here as otherwise script destructors might use the deleted
    // world
    Leviathan::ScriptExecutor::Get()->CollectGarbage();

    m_impl->m_cellStage.reset();

    m_impl.reset();

    LOG_INFO("Thrive EnginePreShutdown ran");
}
// ------------------------------------ //
void
    ThriveGame::CheckGameConfigurationVariables(Lock& guard,
        GameConfiguration* configobj)
{}

void
    ThriveGame::CheckGameKeyConfigVariables(Lock& guard,
        KeyConfiguration* keyconfigobj)
{
    keyconfigobj->AddKeyIfMissing(guard, "MoveForward", {"W"});
    keyconfigobj->AddKeyIfMissing(guard, "MoveBackwards", {"S"});
    keyconfigobj->AddKeyIfMissing(guard, "MoveLeft", {"A"});
    keyconfigobj->AddKeyIfMissing(guard, "MoveRight", {"D"});
    keyconfigobj->AddKeyIfMissing(guard, "ReproduceCheat", {"P"});
    keyconfigobj->AddKeyIfMissing(guard, "SpawnGlucoseCheat", {"O"});
    keyconfigobj->AddKeyIfMissing(guard, "EngulfMode", {"G"});
    keyconfigobj->AddKeyIfMissing(guard, "Screenshot", {"PrintScreen"});
    keyconfigobj->AddKeyIfMissing(guard, "ZoomIn", {"+", "Keypad +"});
    keyconfigobj->AddKeyIfMissing(guard, "ZoomOut", {"-", "Keypad -"});
}
// ------------------------------------ //
bool
    registerLockedMap(asIScriptEngine* engine)
{
    if(engine->RegisterObjectType("LockedMap", 0, asOBJ_REF | asOBJ_NOCOUNT) <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("LockedMap",
           "void addLock(string lockName)", asMETHOD(LockedMap, addLock),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("LockedMap",
           "bool isLocked(string conceptName)", asMETHOD(LockedMap, isLocked),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("LockedMap",
           "void unlock(string conceptName)", asMETHOD(LockedMap, unlock),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

bool
    registerPlayerData(asIScriptEngine* engine)
{

    if(engine->RegisterObjectType("PlayerData", 0, asOBJ_REF | asOBJ_NOCOUNT) <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PlayerData", "LockedMap& lockedMap()",
           asMETHOD(PlayerData, lockedMap), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PlayerData", "ObjectID activeCreature()",
           asMETHOD(PlayerData, activeCreature), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PlayerData",
           "void setActiveCreature(ObjectID creatureId)",
           asMETHOD(PlayerData, setActiveCreature), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PlayerData",
           "bool isBoolSet(const string &in key) const",
           asMETHOD(PlayerData, isBoolSet), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PlayerData",
           "void setBool(const string &in key, bool value)",
           asMETHOD(PlayerData, setBool), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

//! Wrapper for TJsonRegistry::getSize
template<class RegistryT>
uint64_t
    getSizeWrapper(RegistryT* self)
{

    return static_cast<uint64_t>(self->getSize());
}

//! Wrapper for TJsonRegistry::getTypeData
template<class RegistryT, class ReturnedT>
const ReturnedT*
    getTypeDataWrapper(RegistryT* self, uint64_t id)
{

    return &self->getTypeData(id);
}

//! Helper for registerSimulationDataAndJsons
template<class RegistryT, class ReturnedT>
bool
    registerJsonRegistry(asIScriptEngine* engine,
        const char* classname,
        const std::string& returnedTypeName)
{
    if(engine->RegisterObjectType(classname, 0, asOBJ_REF | asOBJ_NOCOUNT) <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod(classname, "uint64 getSize()",
           asFUNCTION(getSizeWrapper<RegistryT>), asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod(classname,
           ("const " + returnedTypeName + "@ getTypeData(uint64 id)").c_str(),
           asFUNCTION((getTypeDataWrapper<RegistryT, ReturnedT>)),
           asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod(classname,
           "uint64 getTypeId(const string &in internalName)",
           asMETHOD(RegistryT, getTypeId), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod(classname,
           "const string& getInternalName(uint64 id)",
           asMETHOD(RegistryT, getInternalName), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

//! Helper for registerJsonregistryHeldTypes
template<class RegistryT>
bool
    registerRegistryHeldHelperBases(asIScriptEngine* engine,
        const char* classname)
{
    if(engine->RegisterObjectType(classname, 0, asOBJ_REF | asOBJ_NOCOUNT) <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectProperty(
           classname, "uint64 id", asOFFSET(RegistryT, id)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(classname, "string displayName",
           asOFFSET(RegistryT, displayName)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(classname, "const string internalName",
           asOFFSET(RegistryT, internalName)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

bool
    registerJsonRegistryHeldTypes(asIScriptEngine* engine)
{

    if(!registerRegistryHeldHelperBases<Compound>(engine, "Compound"))
        return false;

    if(!registerRegistryHeldHelperBases<Compound>(engine, "BioProcess"))
        return false;

    if(!registerRegistryHeldHelperBases<Compound>(engine, "Biome"))
        return false;

    // Compound specific properties //
    // ------------------------------------ //
    // Compound
    if(engine->RegisterObjectProperty(
           "Compound", "double volume", asOFFSET(Compound, volume)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Compound", "bool isCloud", asOFFSET(Compound, isCloud)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Compound", "bool isUseful", asOFFSET(Compound, isUseful)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Compound", "Ogre::ColourValue colour",
           asOFFSET(Compound, colour)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // Biome
    // define colors for sunglight here aswell
    if(engine->RegisterObjectProperty("Biome",
           "Ogre::ColourValue specularColors",
           asOFFSET(Biome, specularColors)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectProperty("Biome",
           "Ogre::ColourValue diffuseColors",
           asOFFSET(Biome, diffuseColors)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectProperty("Biome", "float oxygenPercentage",
           asOFFSET(Biome, oxygenPercentage)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectProperty("Biome", "float carbonDioxidePercentage",
           asOFFSET(Biome, carbonDioxidePercentage)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Biome", "const string background",
           asOFFSET(Biome, background)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectType(
           "BiomeCompoundData", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Biome",
           "const BiomeCompoundData& getCompound(uint64 type) const",
           asMETHOD(Biome, getCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Biome",
           "array<uint64>@ getCompoundKeys() const",
           asMETHOD(Biome, getCompoundKeys), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("BiomeCompoundData", "uint amount",
           asOFFSET(BiomeCompoundData, amount)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("BiomeCompoundData", "double density",
           asOFFSET(BiomeCompoundData, density)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

// Wrappers for registerSimulationDataAndJsons

SpeciesNameController*
    getNameWrapper()
{

    return &SimulationParameters::speciesNameController;
}

TJsonRegistry<Compound>*
    getCompoundRegistryWrapper()
{

    return &SimulationParameters::compoundRegistry;
}

TJsonRegistry<BioProcess>*
    getBioProcessRegistryWrapper()
{

    return &SimulationParameters::bioProcessRegistry;
}

TJsonRegistry<Biome>*
    getBiomeRegistryWrapper()
{

    return &SimulationParameters::biomeRegistry;
}

bool
    registerSimulationDataAndJsons(asIScriptEngine* engine)
{

    if(engine->RegisterObjectType(
           "SimulationParameters", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!registerJsonRegistryHeldTypes(engine))
        return false;

    if(engine->RegisterObjectType(
           "SpeciesNameController", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getVowelPrefixes()",
           asMETHOD(SpeciesNameController, getPrefixes), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getConsonantPrefixes()",
           asMETHOD(SpeciesNameController, getConsonantPrefixes), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getVowelCofixes()",
           asMETHOD(SpeciesNameController, getVowelCofixes), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getConsonantCofixes()",
           asMETHOD(SpeciesNameController, getConsonantCofixes), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getSuffixes()",
           asMETHOD(SpeciesNameController, getSuffixes), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getPrefixCofix()",
           asMETHOD(SpeciesNameController, getPrefixCofix),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!registerJsonRegistry<TJsonRegistry<Compound>, Compound>(
           engine, "TJsonRegistryCompound", "Compound")) {
        return false;
    }

    if(!registerJsonRegistry<TJsonRegistry<BioProcess>, BioProcess>(
           engine, "TJsonRegistryBioProcess", "BioProcess")) {
        return false;
    }

    if(!registerJsonRegistry<TJsonRegistry<Biome>, Biome>(
           engine, "TJsonRegistryBiome", "Biome")) {
        return false;
    }


    if(engine->SetDefaultNamespace("SimulationParameters") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "SpeciesNameController@ speciesNameController()",
           asFUNCTION(getNameWrapper), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "TJsonRegistryCompound@ compoundRegistry()",
           asFUNCTION(getCompoundRegistryWrapper), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "TJsonRegistryBioProcess@ bioProcessRegistry()",
           asFUNCTION(getBioProcessRegistryWrapper), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("TJsonRegistryBiome@ biomeRegistry()",
           asFUNCTION(getBiomeRegistryWrapper), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->SetDefaultNamespace("") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}


static uint16_t ProcessorComponentTYPEProxy =
    static_cast<uint16_t>(ProcessorComponent::TYPE);
static uint16_t SpawnedComponentTYPEProxy =
    static_cast<uint16_t>(SpawnedComponent::TYPE);
static uint16_t AgentCloudComponentTYPEProxy =
    static_cast<uint16_t>(AgentCloudComponent::TYPE);
static uint16_t CompoundCloudComponentTYPEProxy =
    static_cast<uint16_t>(CompoundCloudComponent::TYPE);
static uint16_t MembraneComponentTYPEProxy =
    static_cast<uint16_t>(MembraneComponent::TYPE);
static uint16_t SpeciesComponentTYPEProxy =
    static_cast<uint16_t>(SpeciesComponent::TYPE);
static uint16_t CompoundBagComponentTYPEProxy =
    static_cast<uint16_t>(CompoundBagComponent::TYPE);
static uint16_t CompoundAbsorberComponentTYPEProxy =
    static_cast<uint16_t>(CompoundAbsorberComponent::TYPE);
static uint16_t TimedLifeComponentTYPEProxy =
    static_cast<uint16_t>(TimedLifeComponent::TYPE);

//! Helper for bindThriveComponentTypes
bool
    bindComponentTypeId(asIScriptEngine* engine,
        const char* name,
        uint16_t* value)
{
    if(engine->SetDefaultNamespace(name) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalProperty("const uint16 TYPE", value) < 0) {

        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->SetDefaultNamespace("") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

bool
    bindThriveComponentTypes(asIScriptEngine* engine)
{

    if(engine->RegisterObjectType(
           "ProcessorComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "ProcessorComponent", &ProcessorComponentTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("ProcessorComponent",
           "void setCapacity(BioProcessId id, double capacity)",
           asMETHOD(ProcessorComponent, setCapacity), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "SpawnedComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "SpawnedComponent", &SpawnedComponentTYPEProxy))
        return false;

    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "AgentCloudComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "AgentCloudComponent", &AgentCloudComponentTYPEProxy))
        return false;

    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "CompoundCloudComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "CompoundCloudComponent", &CompoundCloudComponentTYPEProxy))
        return false;

    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "MembraneComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "MembraneComponent", &MembraneComponentTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("MembraneComponent",
           "void setColour(const Float4 &in colour)",
           asMETHOD(MembraneComponent, setColour), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterEnum("MEMBRANE_TYPE") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_REGISTER_ENUM_VALUE(MEMBRANE_TYPE, MEMBRANE);
    ANGELSCRIPT_REGISTER_ENUM_VALUE(MEMBRANE_TYPE, WALL);
    ANGELSCRIPT_REGISTER_ENUM_VALUE(MEMBRANE_TYPE, CHITIN);

    if(engine->RegisterObjectMethod("MembraneComponent",
           "MEMBRANE_TYPE getMembraneType() const",
           asMETHOD(MembraneComponent, getMembraneType), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "void setMembraneType(MEMBRANE_TYPE type)",
           asMETHOD(MembraneComponent, setMembraneType), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "Float4 getColour() const", asMETHOD(MembraneComponent, getColour),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent", "void clear()",
           asMETHOD(MembraneComponent, clear), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "int getCellDimensions()",
           asMETHOD(MembraneComponent, getCellDimensions),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "Ogre::Vector3 GetExternalOrganelle(double x, double y)",
           asMETHOD(MembraneComponent, GetExternalOrganelle),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "void sendOrganelles(double x, double y)",
           asMETHOD(MembraneComponent, sendOrganelles), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }



    // ------------------------------------ //

    if(engine->RegisterObjectType(
           "SpeciesComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "SpeciesComponent", &SpeciesComponentTYPEProxy))
        return false;

    // A bit hacky
    if(engine->RegisterInterface("SpeciesStoredOrganelleType") < 0) {

        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent",
           "array<SpeciesStoredOrganelleType@>@ organelles",
           asOFFSET(SpeciesComponent, organelles)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent",
           "dictionary@ avgCompoundAmounts",
           asOFFSET(SpeciesComponent, avgCompoundAmounts)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent",
           "MEMBRANE_TYPE speciesMembraneType",
           asOFFSET(SpeciesComponent, speciesMembraneType)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "Float4 colour",
           asOFFSET(SpeciesComponent, colour)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "string name",
           asOFFSET(SpeciesComponent, name)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "string genus",
           asOFFSET(SpeciesComponent, genus)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "string epithet",
           asOFFSET(SpeciesComponent, epithet)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "bool isBacteria",
           asOFFSET(SpeciesComponent, isBacteria)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "double aggression",
           asOFFSET(SpeciesComponent, aggression)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "double fear",
           asOFFSET(SpeciesComponent, fear)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //

    if(engine->RegisterObjectType(
           "CompoundBagComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "CompoundBagComponent", &CompoundBagComponentTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "double getCompoundAmount(CompoundId compound)",
           asMETHOD(CompoundBagComponent, getCompoundAmount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "double takeCompound(CompoundId compound, double to_take)",
           asMETHOD(CompoundBagComponent, takeCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "void giveCompound(CompoundId compound, double amount)",
           asMETHOD(CompoundBagComponent, giveCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "double getPrice(CompoundId compound)",
           asMETHOD(CompoundBagComponent, getPrice), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "double getDemand(CompoundId compound)",
           asMETHOD(CompoundBagComponent, getDemand), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "void setProcessor(ProcessorComponent@ processor, const string &in "
           "speciesName)",
           asMETHOD(CompoundBagComponent, setProcessor), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("CompoundBagComponent",
           "double storageSpace",
           asOFFSET(CompoundBagComponent, storageSpace)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("CompoundBagComponent",
           "double storageSpaceOccupied",
           asOFFSET(CompoundBagComponent, storageSpaceOccupied)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("CompoundBagComponent",
           "string speciesName",
           asOFFSET(CompoundBagComponent, speciesName)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // CompoundAbsorberComponent
    if(engine->RegisterObjectType(
           "CompoundAbsorberComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(engine, "CompoundAbsorberComponent",
           &CompoundAbsorberComponentTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("CompoundAbsorberComponent",
           "void enable()", asMETHOD(CompoundAbsorberComponent, enable),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundAbsorberComponent",
           "void disable()", asMETHOD(CompoundAbsorberComponent, disable),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterObjectMethod("CompoundAbsorberComponent",
           "array<CompoundId>@ getAbsorbedCompounds()",
           asMETHOD(CompoundAbsorberComponent, getAbsorbedCompounds),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterObjectMethod("CompoundAbsorberComponent",
           "float absorbedCompoundAmount(CompoundId compound)",
           asMETHOD(CompoundAbsorberComponent, absorbedCompoundAmount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundAbsorberComponent",
           "void setAbsorbtionCapacity(double capacity)",
           asMETHOD(CompoundAbsorberComponent, setAbsorbtionCapacity),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundAbsorberComponent",
           "void setCanAbsorbCompound(CompoundId id, bool canAbsorb)",
           asMETHOD(CompoundAbsorberComponent, setCanAbsorbCompound),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "TimedLifeComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "TimedLifeComponent", &TimedLifeComponentTYPEProxy))
        return false;



    return true;
}

template<class WorldType>
bool
    bindCellStageMethods(asIScriptEngine* engine, const char* classname)
{

    if(!Leviathan::BindStandardWorldMethods<CellStageWorld>(engine, classname))
        return false;

#include "generated/cell_stage_bindings.h"

    ANGLESCRIPT_BASE_CLASS_CASTS_NO_REF(Leviathan::StandardWorld,
        "StandardWorld", CellStageWorld, "CellStageWorld");

    return true;
}

template<class WorldType>
bool
    bindMicrobeEditorMethods(asIScriptEngine* engine, const char* classname)
{

    if(!Leviathan::BindStandardWorldMethods<MicrobeEditorWorld>(
           engine, classname))
        return false;

#include "generated/microbe_editor_bindings.h"

    ANGLESCRIPT_BASE_CLASS_CASTS_NO_REF(Leviathan::StandardWorld,
        "StandardWorld", CellStageWorld, "MicrobeEditorWorld");

    return true;
}

bool
    registerHexFunctions(asIScriptEngine* engine)
{

    // This doesn't need to be restored if we fail //
    if(engine->SetDefaultNamespace("Hex") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("double getHexSize()",
           asFUNCTION(Hex::getHexSize), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "Float3 axialToCartesian(double q, double r)",
           asFUNCTIONPR(Hex::axialToCartesian, (double q, double r), Float3),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "Float3 axialToCartesian(const Int2 &in hex)",
           asFUNCTIONPR(Hex::axialToCartesian, (const Int2& hex), Float3),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction(
           "Int2 cartesianToAxial(double x, double z)",
           asFUNCTIONPR(Hex::cartesianToAxial, (double x, double z), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "Int2 cartesianToAxial(const Float3 &in coordinates)",
           asFUNCTIONPR(
               Hex::cartesianToAxial, (const Float3& coordinates), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction("Int3 axialToCube(double q, double r)",
           asFUNCTIONPR(Hex::axialToCube, (double q, double r), Int3),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("Int3 axialToCube(const Int2 &in hex)",
           asFUNCTIONPR(Hex::axialToCube, (const Int2& hex), Int3),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction(
           "Int2 cubeToAxial(double x, double y, double z)",
           asFUNCTIONPR(Hex::cubeToAxial, (double x, double y, double z), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("Int2 cubeToAxial(const Int3 &in hex)",
           asFUNCTIONPR(Hex::cubeToAxial, (const Int3& hex), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction(
           "Int3 cubeHexRound(double x, double y, double z)",
           asFUNCTIONPR(
               Hex::cubeHexRound, (double x, double y, double z), Int3),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction("Int3 cubeHexRound(const Float3 &in hex)",
           asFUNCTIONPR(Hex::cubeHexRound, (const Float3& hex), Int3),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction("int64 encodeAxial(double q, double r)",
           asFUNCTIONPR(Hex::encodeAxial, (double q, double r), int64_t),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("int64 encodeAxial(const Int2 &in hex)",
           asFUNCTIONPR(Hex::encodeAxial, (const Int2& hex), int64_t),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("Int2 decodeAxial(int64 s)",
           asFUNCTIONPR(Hex::decodeAxial, (int64_t s), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction("Int2 rotateAxial(double q, double r)",
           asFUNCTIONPR(Hex::rotateAxial, (double q, double r), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("Int2 rotateAxial(const Int2 &in hex)",
           asFUNCTIONPR(Hex::rotateAxial, (const Int2& hex), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction(
           "Int2 rotateAxialNTimes(double q0, double r0, "
           "uint32 n)",
           asFUNCTIONPR(Hex::rotateAxialNTimes,
               (double q0, double r0, uint32_t n), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "Int2 rotateAxialNTimes(const Int2 &in hex, uint32 n)",
           asFUNCTIONPR(
               Hex::rotateAxialNTimes, (const Int2& hex, uint32_t n), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterGlobalFunction(
           "Int2 flipHorizontally(double q, double r)",
           asFUNCTIONPR(Hex::flipHorizontally, (double q, double r), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "Int2 flipHorizontally(const Int2 &in hex)",
           asFUNCTIONPR(Hex::flipHorizontally, (const Int2& hex), Int2),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }



    if(engine->SetDefaultNamespace("") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

class ScriptSpawnerWrapper {
public:
    //! \note Caller must have incremented ref count already on func
    ScriptSpawnerWrapper(asIScriptFunction* func) : m_func(func)
    {

        if(!m_func)
            throw std::runtime_error("no func given for ScriptSpawnerWrapper");
    }

    ~ScriptSpawnerWrapper()
    {

        m_func->Release();
    }

    ObjectID
        run(CellStageWorld& world, Float3 pos)
    {

        ScriptRunningSetup setup;
        auto result = Leviathan::ScriptExecutor::Get()->RunScript<ObjectID>(
            m_func, nullptr, setup, &world, pos);

        if(result.Result != SCRIPT_RUN_RESULT::Success) {

            LOG_ERROR("Failed to run Wrapped SpawnSystem function");
            // This makes the spawn system just ignore the return value
            return NULL_OBJECT;
        }

        return result.Value;
    }

    asIScriptFunction* m_func;
};

SpawnerTypeId
    addSpawnTypeProxy(SpawnSystem* self,
        asIScriptFunction* func,
        double spawnDensity,
        double spawnRadius)
{
    auto wrapper = std::make_shared<ScriptSpawnerWrapper>(func);

    return self->addSpawnType(
        [=](CellStageWorld& world, Float3 pos) -> ObjectID {
            return wrapper->run(world, pos);
        },
        spawnDensity, spawnRadius);
}

bool
    bindScriptAccessibleSystems(asIScriptEngine* engine)
{

    // ------------------------------------ //
    // SpawnSystem
    if(engine->RegisterFuncdef(
           "ObjectID SpawnFactoryFunc(CellStageWorld@ world, Float3 pos)") <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectType("SpawnSystem", 0, asOBJ_REF | asOBJ_NOCOUNT) <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpawnSystem",
           "void removeSpawnType(SpawnerTypeId spawnId)",
           asMETHOD(SpawnSystem, removeSpawnType), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpawnSystem",
           "SpawnerTypeId addSpawnType(SpawnFactoryFunc@ factory, double "
           "spawnDensity, "
           "double spawnRadius)",
           asFUNCTION(addSpawnTypeProxy), asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    // ------------------------------------ //
    // CompoundCloudSystem
    if(engine->RegisterObjectType(
           "CompoundCloudSystem", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundCloudSystem",
           "bool addCloud(CompoundId compound, float density, int x, int y)",
           asMETHOD(CompoundCloudSystem, addCloud), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundCloudSystem",
           "int takeCompound(CompoundId compound, int x, int y, float rate)",
           asMETHOD(CompoundCloudSystem, takeCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundCloudSystem",
           "int amountAvailable(CompoundId compound, int x, int y, float rate)",
           asMETHOD(CompoundCloudSystem, takeCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}


//! \todo This might be good to also be available to other c++ files
ObjectID
    findSpeciesEntityByName(CellStageWorld* world, const std::string& name)
{

    if(!world || name.empty())
        return NULL_OBJECT;

    const auto& allSpecies = world->GetComponentIndex_SpeciesComponent();

    for(const auto& tuple : allSpecies) {

        SpeciesComponent* species = std::get<1>(tuple);

        if(species->name == name)
            return std::get<0>(tuple);
    }

    LOG_ERROR("findSpeciesEntityByName: no species with name: " + name);
    return NULL_OBJECT;
}


bool
    ThriveGame::InitLoadCustomScriptTypes(asIScriptEngine* engine)
{

    if(!registerLockedMap(engine))
        return false;

    if(engine->RegisterTypedef("CompoundId", "uint16") < 0) {

        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterTypedef("BioProcessId", "uint16") < 0) {

        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterTypedef("SpawnerTypeId", "uint32") < 0) {

        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectType(
           "CellStageWorld", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectType(
           "MicrobeEditorWorld", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindThriveComponentTypes(engine))
        return false;

    if(!bindScriptAccessibleSystems(engine))
        return false;

    if(!registerPlayerData(engine))
        return false;

    if(!registerSimulationDataAndJsons(engine))
        return false;

    if(!registerHexFunctions(engine))
        return false;

    if(engine->RegisterObjectType("ThriveGame", 0, asOBJ_REF | asOBJ_NOCOUNT) <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("ThriveGame@ GetThriveGame()",
           asFUNCTION(ThriveGame::Get), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGLESCRIPT_BASE_CLASS_CASTS_NO_REF(
        LeviathanApplication, "LeviathanApplication", ThriveGame, "ThriveGame");

    if(engine->RegisterObjectMethod("ThriveGame", "PlayerData& playerData()",
           asMETHOD(ThriveGame, playerData), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // if(engine->RegisterObjectMethod("ThriveGame",
    //         "SoundPlayer@ getGuiSoundPlayer()",
    //         asMETHOD(ThriveGame, getGuiSoundPlayer),
    //         asCALL_THISCALL) < 0)
    // {
    //     ANGELSCRIPT_REGISTERFAIL;
    // }


    if(engine->RegisterObjectMethod("ThriveGame", "void startNewGame()",
           asMETHOD(ThriveGame, startNewGame), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ThriveGame",
           "void loadSaveGame(const string &in saveFile)",
           asMETHOD(ThriveGame, loadSaveGame), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ThriveGame",
           "void saveGame(const string &in saveFile)",
           asMETHOD(ThriveGame, saveGame), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ThriveGame",
           "void setBackgroundMaterial(const string &in material)",
           asMETHOD(ThriveGame, setBackgroundMaterial), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ThriveGame", "void editorButtonClicked()",
           asMETHOD(ThriveGame, editorButtonClicked), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ThriveGame",
           "void killPlayerCellClicked()",
           asMETHOD(ThriveGame, killPlayerCellClicked), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ThriveGame", "void finishEditingClicked()",
           asMETHOD(ThriveGame, finishEditingClicked), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // if(engine->RegisterObjectMethod("Client",
    //         "bool Connect(const string &in address, string &out
    //         errormessage)", asMETHODPR(Client, Connect, (const std::string&,
    //         std::string&), bool), asCALL_THISCALL) < 0)
    // {
    //     ANGELSCRIPT_REGISTERFAIL;
    // }



    if(!bindCellStageMethods<CellStageWorld>(engine, "CellStageWorld"))
        return false;

    if(!bindMicrobeEditorMethods<MicrobeEditorWorld>(
           engine, "MicrobeEditorWorld"))
        return false;

    if(engine->RegisterObjectMethod("ThriveGame",
           "CellStageWorld@ getCellStage()", asMETHOD(ThriveGame, getCellStage),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "ObjectID findSpeciesEntityByName(CellStageWorld@ world, "
           "const string &in name)",
           asFUNCTION(findSpeciesEntityByName), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    return true;
}
