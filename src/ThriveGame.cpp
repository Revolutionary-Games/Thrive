// ------------------------------------ //
#include "ThriveGame.h"

#include "engine/player_data.h"
#include "general/global_keypresses.h"

#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"
#include "main_menu_keypresses.h"
#include "microbe_stage/microbe_editor_key_handler.h"
#include "microbe_stage/simulation_parameters.h"
#include "scripting/script_initializer.h"
#include "thrive_net_handler.h"
#include "thrive_version.h"
#include "thrive_world_factory.h"


#include <GUI/GuiView.h>
#include <Handlers/ObjectLoader.h>
#include <Networking/NetworkHandler.h>
#include <Physics/PhysicsMaterialManager.h>
#include <Rendering/GeometryHelpers.h>
#include <Script/Bindings/BindHelpers.h>
#include <Script/Bindings/StandardWorldBindHelper.h>
#include <Script/ScriptExecutor.h>
#include <Window.h>

#include <OgreManualObject.h>
#include <OgreMesh2.h>
#include <OgreMeshManager2.h>
#include <OgreRoot.h>
#include <OgreSceneManager.h>
#include <OgreSubMesh2.h>


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
            *game.ApplicationConfiguration->GetKeyConfiguration())),
        m_microbeEditorKeys(std::make_shared<MicrobeEditorKeyHandler>(
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

            LOG_INFO("Destroying background item");

            if(m_cellStage)
                m_cellStage->GetScene()->destroyItem(m_microbeBackgroundItem);
            m_microbeBackgroundItem = nullptr;
        }

        if(m_microbeEditorBackgroundItem) {

            if(m_microbeEditor)
                m_microbeEditor->GetScene()->destroyItem(
                    m_microbeEditorBackgroundItem);
            m_microbeEditorBackgroundItem = nullptr;
        }
    }

    void
        createBackgroundItem()
    {
        destroyBackgroundItem();

        LEVIATHAN_ASSERT(
            m_cellStage, "Trying to create background item before world");

        m_microbeBackgroundItem = m_cellStage->GetScene()->createItem(
            m_microbeBackgroundMesh, Ogre::SCENE_STATIC);
        m_microbeBackgroundItem->setCastShadows(false);

        // Need to edit the render queue and add it to an early one
        m_microbeBackgroundItem->setRenderQueueGroup(1);

        // Editor version
        // We only checked the first background item before we did this. Not the
        // editor one.
        if(m_microbeEditor) {
            if(!m_microbeEditorBackgroundItem) {
                m_microbeEditorBackgroundItem =
                    m_microbeEditor->GetScene()->createItem(
                        m_microbeBackgroundMesh, Ogre::SCENE_STATIC);
                m_microbeEditorBackgroundItem->setCastShadows(false);

                // Need to edit the render queue and add it to an early one
                m_microbeEditorBackgroundItem->setRenderQueueGroup(1);
            }
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
    std::shared_ptr<MicrobeEditorKeyHandler> m_microbeEditorKeys;
};

// ------------------------------------ //
ThriveGame::ThriveGame()
{
#ifndef MAKE_RELEASE
    LOG_INFO("Enabling cheats because this is a development build");
    m_cheatsEnabled = true;
#endif // MAKE_RELEASE

#ifdef _WIN32
    // This should fix the texture loading error. It is likely caused by running
    // out of file descriptors, so here we set our limit to the max Not sure why
    // it was able to happen on linux. Perhaps the launcher could do "ulimit" to
    // increase the file limit when running the game, but it is very rare to
    // happen on Linux
    if(_setmaxstdio(2048) != 2048) {
        LOG_ERROR("Max open file increase failed! _setmaxstdio");
    }
#endif //_WIN32

    StaticGame = this;
}

ThriveGame::ThriveGame(Leviathan::Engine* engine) :
    Leviathan::ClientApplication(engine)
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
    return "Thrive " GAME_VERSIONS
#ifndef MAKE_RELEASE
           " (development)"
#endif
        ;
}

ThriveGame*
    ThriveGame::Get()
{
    return StaticGame;
}

ThriveGame*
    ThriveGame::get()
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
    if(!m_network)
        m_network = std::make_unique<ThriveNetHandler>();
    return m_network.get();
}

void
    ThriveGame::_ShutdownApplicationPacketHandler()
{
    m_network.reset();
}
// ------------------------------------ //
void
    ThriveGame::startNewGame()
{
    // Used to stop it from clearing entities when rwe restart so species can
    // spawn.
    bool restarted = false;
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

        Leviathan::WorldNetworkSettings netSettings;
        netSettings.IsAuthoritative = true;
        netSettings.DoInterpolation = true;

        // TODO: switch to
        // Leviathan::WorldNetworkSettings::GetSettingsForSinglePlayer() once we
        // no longer do the interpolation once variable rate ticks are supported

        LOG_INFO("ThriveGame: startNewGame: Creating new cellstage world");
        m_impl->m_cellStage =
            std::dynamic_pointer_cast<CellStageWorld>(engine->CreateWorld(
                window1, static_cast<int>(THRIVE_WORLD_TYPE::CELL_STAGE),
                createPhysicsMaterials(), netSettings));
    } else {
        restarted = true;
        m_impl->m_cellStage->ClearEntities();
        // We also need to somehow force stop all the music here.
        LOG_INFO("ThriveGame: startNewGame called after already created once: "
                 "Creating new cellstage world");
    }

    LEVIATHAN_ASSERT(m_impl->m_cellStage, "Cell stage world creation failed");

    window1->LinkObjects(m_impl->m_cellStage);

    // Set the right input handlers active //
    m_impl->m_menuKeyPresses->setEnabled(false);
    m_impl->m_microbeEditorKeys->setEnabled(false);
    m_impl->m_cellStageKeys->setEnabled(true);

    // And switch the GUI mode to allow key presses through
    auto layer = window1->GetGui()->GetLayerByIndex(0);

    // Allow running without GUI
    if(layer)
        layer->SetInputMode(Leviathan::GUI::INPUT_MODE::Gameplay);


    // Clear world //
    if(restarted == false) {
        m_impl->m_cellStage->ClearEntities();
    }

    // TODO: unpause, if it was paused

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

    // This is needed for the compound clouds to work in general
    const auto compoundCount = SimulationParameters::compoundRegistry.getSize();


    LEVIATHAN_ASSERT(SimulationParameters::compoundRegistry.getSize() > 0,
        "compound registry is empty when creating cloud entities for them");

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
    LEVIATHAN_ASSERT(getMicrobeScripts(), "microbe scripts not loaded");

    LOG_INFO("Calling world setup script setupScriptsForWorld");

    ScriptRunningSetup setup;
    setup.SetEntrypoint("setupScriptsForWorld");

    auto result = getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run script setup function: " + setup.Entryfunction);
        MarkAsClosing();
        return;
    }

    LOG_INFO("Finished calling setupScriptsForWorld");

    // Set background plane //
    // This is needed to be created here for biome.as to work correctly
    // Also this is a manual object and with infinite extent as this isn't
    // perspective projected in the shader
    m_impl->m_backgroundRenderNode =
        m_impl->m_cellStage->GetScene()->createSceneNode(Ogre::SCENE_STATIC);

    // This needs to be manually destroyed later
    if(!m_impl->m_microbeBackgroundMesh) {
        m_impl->m_microbeBackgroundMesh =
            Leviathan::GeometryHelpers::CreateScreenSpaceQuad(
                "CellStage_background", -1, -1, 2, 2);

        m_impl->m_microbeBackgroundSubMesh =
            m_impl->m_microbeBackgroundMesh->getSubMesh(0);

        m_impl->m_microbeBackgroundSubMesh->setMaterialName("Background");
    }

    // Setup render queue for it
    m_impl->m_cellStage->GetScene()->getRenderQueue()->setRenderQueueMode(
        1, Ogre::RenderQueue::FAST);

    // This now attaches the item as well (as long as the scene node is created)
    // This makes it easier to manage the multiple backgrounds and reattaching
    // them
    if(!m_impl->m_microbeBackgroundItem) {
        m_impl->createBackgroundItem();
    }

    // Gen species if this is a restart//
    if(restarted) {
        ScriptRunningSetup setup("resetWorld");
        auto returned =
            ThriveGame::Get()->getMicrobeScripts()->ExecuteOnModule<void>(
                setup, false, m_impl->m_cellStage.get());

        if(returned.Result != SCRIPT_RUN_RESULT::Success)
            LOG_ERROR("Failed to run script side resetWorld");
    }
    // Spawn player //
    setup = ScriptRunningSetup("setupPlayer");

    result = getMicrobeScripts()->ExecuteOnModule<void>(
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
// ------------------------------------ //
void
    ThriveGame::killPlayerCellClicked()
{
    LOG_INFO("Calling killPlayerCellClicked");

    ScriptRunningSetup setup = ScriptRunningSetup("killPlayerCellClicked");

    auto result = getMicrobeScripts()->ExecuteOnModule<void>(
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

    // Create an editor world
    LOG_INFO("Entering MicrobeEditor");

    // Create world if not already created //
    if(!m_impl->m_microbeEditor) {

        LOG_INFO("ThriveGame: editorButtonClicked: Creating new microbe editor "
                 "world");

        Leviathan::WorldNetworkSettings netSettings;
        netSettings.IsAuthoritative = true;
        netSettings.DoInterpolation = true;

        m_impl->m_microbeEditor =
            std::dynamic_pointer_cast<MicrobeEditorWorld>(engine->CreateWorld(
                window1, static_cast<int>(THRIVE_WORLD_TYPE::MICROBE_EDITOR),
                createPhysicsMaterials(),
                Leviathan::WorldNetworkSettings::GetSettingsForSinglePlayer()));
    }

    LEVIATHAN_ASSERT(
        m_impl->m_microbeEditor, "Microbe editor world creation failed");

    // Link the new world to the window (this will automatically make
    // the old one go to the background)
    window1->LinkObjects(m_impl->m_microbeEditor);

    // Set the right input handlers active //
    m_impl->m_menuKeyPresses->setEnabled(false);
    m_impl->m_cellStageKeys->setEnabled(false);
    m_impl->m_microbeEditorKeys->setEnabled(true);
    // // TODO: editor hotkeys. Maybe they should be in the GUI

    // // So using this
    // // // And switch the GUI mode to allow key presses through
    // Leviathan::GUI::View* view = window1->GetGui()->GetLayerByIndex(0);

    // // Allow running without GUI
    // if(view)
    //     view->SetInputMode(Leviathan::GUI::INPUT_MODE::Menu);


    // Clear world //
    m_impl->m_microbeEditor->ClearEntities();

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

    if(!m_impl->m_editorBackgroundRenderNode) {
        m_impl->m_editorBackgroundRenderNode =
            m_impl->m_microbeEditor->GetScene()->createSceneNode(
                Ogre::SCENE_STATIC);

        // Setup render queue for it
        m_impl->m_microbeEditor->GetScene()
            ->getRenderQueue()
            ->setRenderQueueMode(1, Ogre::RenderQueue::FAST);

        // This creates and attaches the item
        m_impl->createBackgroundItem();
    }

    // Let the script do setup //
    // This registers all the script defined systems to run and be
    // available from the world
    // LEVIATHAN_ASSERT(
    //     m_impl->m_MicrobeEditorScripts, "microbe editor scripts not loaded");

    LOG_INFO("Calling editor setup script onEditorEntry");

    ScriptRunningSetup setup("onEditorEntry");

    auto result = getMicrobeScripts()->ExecuteOnModule<void>(
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
    // And make the Editor apply current changes to the player Species, the
    // microbe stage code will apply it to the player cell in onReturnFromEditor
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
    m_impl->m_microbeEditorKeys->setEnabled(false);
    // // TODO: editor hotkeys. Maybe they should be in the GUI

    // // So using this
    // // // And switch the GUI mode to allow key presses through
    // Leviathan::GUI::View* view = window1->GetGui()->GetLayerByIndex(0);

    // // Allow running without GUI
    // if(view)
    //     view->SetInputMode(Leviathan::GUI::INPUT_MODE::Gameplay);


    // Run the post editing script

    // Let the script do setup //
    // This registers all the script defined systems to run and be
    // available from the world
    LEVIATHAN_ASSERT(getMicrobeScripts(), "microbe stage scripts not loaded");

    LOG_INFO("Calling return from editor script, onReturnFromEditor");

    ScriptRunningSetup setup("onReturnFromEditor");

    auto result = getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to run return from editor function: " +
                  setup.Entryfunction);
        return;
    }
}

void
    ThriveGame::exitToMenuClicked()
{
    // Unlink window
    Leviathan::Window* window1 = Engine::GetEngine()->GetWindowEntity();
    window1->LinkObjects(nullptr);

    // Disconnect
    if(m_network->IsConnected()) {

        disconnectFromServer(true);

    } else {

        // Clear the world
        m_impl->m_cellStage->ClearEntities();
    }

    // Get proper keys setup
    m_impl->m_menuKeyPresses->setEnabled(true);
    m_impl->m_cellStageKeys->setEnabled(false);
    m_impl->m_microbeEditorKeys->setEnabled(false);

    // And switch the GUI mode to allow key presses through
    auto layer = window1->GetGui()->GetLayerByIndex(0);

    // Allow running without GUI
    if(layer)
        layer->SetInputMode(Leviathan::GUI::INPUT_MODE::Menu);

    // Fire an event to switch over the GUI
    Engine::Get()->GetEventHandler()->CallEvent(
        new Leviathan::GenericEvent("ExitedToMenu"));
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
    ThriveGame::connectToServer(const std::string& url)
{
    LOG_INFO("Connecting to server at: " + url);

    if(m_network->IsConnected()) {
        disconnectFromServer(
            false, "Disconnect by user, joining another server");
    }

    auto connection = m_network->GetOwner()->OpenConnectionTo(url);

    if(!connection) {

        Leviathan::GenericEvent::pointer event =
            new Leviathan::GenericEvent("ConnectStatusMessage");

        auto vars = event->GetVariables();

        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(true)));
        vars->Add(std::make_shared<NamedVariableList>("message",
            new Leviathan::StringBlock("Invalid address specified")));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());

    } else {

        if(!m_network->JoinServer(connection)) {

            Leviathan::GenericEvent::pointer event =
                new Leviathan::GenericEvent("ConnectStatusMessage");

            auto vars = event->GetVariables();

            vars->Add(std::make_shared<NamedVariableList>(
                "show", new Leviathan::BoolBlock(true)));
            vars->Add(std::make_shared<NamedVariableList>("message",
                new Leviathan::StringBlock(
                    "Unknown error from JoinServer (try disconnecting?)")));

            Engine::Get()->GetEventHandler()->CallEvent(event.detach());
            return;
        }

        Leviathan::GenericEvent::pointer event =
            new Leviathan::GenericEvent("ConnectStatusMessage");

        auto vars = event->GetVariables();

        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(true)));
        vars->Add(std::make_shared<NamedVariableList>(
            "server", new Leviathan::StringBlock(url)));
        vars->Add(std::make_shared<NamedVariableList>(
            "message", new Leviathan::StringBlock("Opening connection")));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());
    }
}

void
    ThriveGame::disconnectFromServer(bool userInitiated,
        const std::string& reason)
{
    LOG_INFO("Initiating disconnect from server");
    m_network->DisconnectFromServer(reason);

    // If we had managed to enter a game then needs to do this
    if(m_impl->m_cellStage) {

        exitToMenuClicked();

        m_impl->destroyBackgroundItem();

        if(m_impl->m_cellStage) {
            m_impl->m_cellStage->Release();
        }

        m_impl->m_cellStage.reset();
    }

    if(!userInitiated) {
        Leviathan::GenericEvent::pointer event =
            new Leviathan::GenericEvent("ConnectStatusMessage");

        auto vars = event->GetVariables();

        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(true)));
        vars->Add(std::make_shared<NamedVariableList>(
            "message", new Leviathan::StringBlock("Disconnected: " + reason)));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());
    } else {
        Leviathan::GenericEvent::pointer event =
            new Leviathan::GenericEvent("ConnectStatusMessage");

        auto vars = event->GetVariables();

        // This hides it
        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(false)));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());
    }
}
// ------------------------------------ //
void
    ThriveGame::reportJoinedServerWorld(std::shared_ptr<GameWorld> world)
{
    LEVIATHAN_ASSERT(
        world->GetType() == static_cast<int>(THRIVE_WORLD_TYPE::CELL_STAGE),
        "unexpected world type");

    // TODO: fix
    if(m_impl->m_cellStage) {

        LOG_ERROR("double join happened, ignoring, TODO: FIX");
        return;
    }

    LEVIATHAN_ASSERT(!m_impl->m_cellStage, "double join happened");

    LOG_INFO("ThriveGame: client received world, moving to cell stage");

    auto casted = std::dynamic_pointer_cast<CellStageWorld>(world);
    m_impl->m_cellStage = casted;

    // Hide the join status dialog
    {
        Leviathan::GenericEvent::pointer event =
            new Leviathan::GenericEvent("ConnectStatusMessage");

        auto vars = event->GetVariables();

        // This hides it
        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(false)));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());
    }

    // Notify GUI to switch to the cell stage GUI
    Engine::Get()->GetEventHandler()->CallEvent(
        new Leviathan::GenericEvent("MicrobeStageEnteredClient"));

    Leviathan::Window* window1 = Engine::GetEngine()->GetWindowEntity();

    window1->LinkObjects(m_impl->m_cellStage);

    // Set the right input handlers active //
    m_impl->m_menuKeyPresses->setEnabled(false);
    m_impl->m_cellStageKeys->setEnabled(true);

    // And switch the GUI mode to allow key presses through
    auto layer = window1->GetGui()->GetLayerByIndex(0);

    // Allow running without GUI
    if(layer)
        layer->SetInputMode(Leviathan::GUI::INPUT_MODE::Gameplay);

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

    // This is needed for the compound clouds to work in general
    LEVIATHAN_ASSERT(SimulationParameters::compoundRegistry.getSize() > 0,
        "compound registry is empty when creating cloud entities for them");

    // Let the script do setup //
    // This registers all the script defined systems to run and be
    // available from the world
    LEVIATHAN_ASSERT(getMicrobeScripts(), "microbe scripts not loaded");

    LOG_INFO("Calling world setup script setupScriptsForWorld_Client");

    ScriptRunningSetup setup("setupScriptsForWorld_Client");

    auto result = getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run script setup function: " + setup.Entryfunction);
        MarkAsClosing();
        return;
    }

    LOG_INFO("Finished calling setupScriptsForWorld");

    // Set background plane //
    // This is needed to be created here for biome.as to work correctly
    // Also this is a manual object and with infinite extent as this isn't
    // perspective projected in the shader
    m_impl->m_backgroundRenderNode =
        m_impl->m_cellStage->GetScene()->createSceneNode(Ogre::SCENE_STATIC);

    // This needs to be manually destroyed later
    if(!m_impl->m_microbeBackgroundMesh) {
        m_impl->m_microbeBackgroundMesh =
            Leviathan::GeometryHelpers::CreateScreenSpaceQuad(
                "CellStage_background", -1, -1, 2, 2);

        m_impl->m_microbeBackgroundSubMesh =
            m_impl->m_microbeBackgroundMesh->getSubMesh(0);

        m_impl->m_microbeBackgroundSubMesh->setMaterialName("Background");
    }

    // Setup render queue for it
    m_impl->m_cellStage->GetScene()->getRenderQueue()->setRenderQueueMode(
        1, Ogre::RenderQueue::FAST);

    // This now attaches the item as well (as long as the scene node is created)
    // This makes it easier to manage the multiple backgrounds and reattaching
    // them
    m_impl->createBackgroundItem();

    // We handle spawning cells when the server tells us and we setup our
    // control when we receive a notification of a direct control entity
}

void
    ThriveGame::doSpawnCellFromServerReceivedComponents(ObjectID id)
{
    LOG_INFO(
        "ThriveGame: doSpawnCellFromServerReceivedComponents for entity: " +
        std::to_string(id));

    try {
        m_impl->m_cellStage->GetComponent_MembraneComponent(id);
    } catch(const Leviathan::NotFound&) {

        LOG_INFO("Skipping this one as this is probably not a cell");
        return;
    }

    LEVIATHAN_ASSERT(getMicrobeScripts(), "microbe scripts not loaded");

    ScriptRunningSetup setup("setupClientSideReceivedCell");

    auto result = getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get(), id);

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to run setupClientSideReceivedCell");
        return;
    }

    LOG_INFO("Successfully ran setupClientSideReceivedCell");
}

void
    ThriveGame::reportLocalControlChanged(GameWorld* world)
{
    LOG_INFO("ThriveGame: updating our local player id, local control changed");

    const auto& control = m_impl->m_cellStage->GetOurLocalControl();

    if(control.empty()) {

        playerData().setActiveCreature(NULL_OBJECT);

    } else {

        if(control.size() > 1) {
            LOG_WARNING("ThriveGame: we have more than 1 locally controlled "
                        "entity, assuming first is our cell");
        }

        playerData().setActiveCreature(control.front());
    }

    LOG_INFO("ThriveGame: active entity is now: " +
             std::to_string(playerData().activeCreature()));
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

bool
    ThriveGame::createImpl()
{
    try {
        m_impl = std::make_unique<Implementation>(*this);
    } catch(const Leviathan::InvalidArgument& e) {

        LOG_ERROR("ThriveGame: loading configuration data failed: ");
        e.PrintToLog();
        return false;
    }

    return true;
}

void
    ThriveGame::CustomizeEnginePostLoad()
{
    Engine* engine = Engine::Get();

    if(!createImpl()) {
        MarkAsClosing();
        return;
    }

    if(!loadScriptsAndConfigs()) {

        LOG_ERROR("Failed to load init data, quitting");
        MarkAsClosing();
        return;
    }

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

    // Register utility key presses
    window1->GetInputController()->LinkReceiver(m_impl->m_globalKeyPresses);

    // Register keypresses that will be used in the menu
    window1->GetInputController()->LinkReceiver(m_impl->m_menuKeyPresses);

    // Register the player input listener
    window1->GetInputController()->LinkReceiver(m_impl->m_cellStageKeys);

    // Register the editor input listener
    window1->GetInputController()->LinkReceiver(m_impl->m_microbeEditorKeys);

    Leviathan::GUI::GuiManager* GuiManagerAccess = window1->GetGui();

    if(!GuiManagerAccess->LoadGUIFile("Data/Scripts/gui/thrive_gui.html")) {

        LOG_ERROR("Thrive: failed to load the main menu gui, quitting");
        StartRelease();
        return;
    }
}
// ------------------------------------ //
void
    ThriveGame::EnginePreShutdown()
{
    // Shutdown scripting first to allow it to still do anything it wants //
    releaseScripts();

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
    keyconfigobj->AddKeyIfMissing(guard, "SpawnPhosphateCheat", {"I"});
    keyconfigobj->AddKeyIfMissing(guard, "EngulfMode", {"G"});
    keyconfigobj->AddKeyIfMissing(guard, "ShootToxin", {"E"});
    keyconfigobj->AddKeyIfMissing(guard, "Screenshot", {"PrintScreen"});
    keyconfigobj->AddKeyIfMissing(guard, "ZoomIn", {"+", "Keypad +"});
    keyconfigobj->AddKeyIfMissing(guard, "ZoomOut", {"-", "Keypad -"});
    keyconfigobj->AddKeyIfMissing(guard, "RotateRight", {"A"});
    keyconfigobj->AddKeyIfMissing(guard, "RotateLeft", {"D"});
}
// ------------------------------------ //
bool
    ThriveGame::InitLoadCustomScriptTypes(asIScriptEngine* engine)
{
    return registerThriveScriptTypes(engine);
}
