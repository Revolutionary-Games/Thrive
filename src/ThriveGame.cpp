// ------------------------------------ //
#include "ThriveGame.h"

#include "engine/player_data.h"
#include "general/global_keypresses.h"

#include "auto-evo/auto-evo.h"
#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"
#include "main_menu_keypresses.h"
#include "microbe_stage/microbe_editor_key_handler.h"
#include "microbe_stage/simulation_parameters.h"
#include "scripting/script_initializer.h"
#include "thrive_net_handler.h"
#include "thrive_version.h"
#include "thrive_world_factory.h"


#include <Common/DataStoring/DataStore.h>
#include <FileSystem.h>
#include <GUI/GuiView.h>
#include <Handlers/ObjectLoader.h>
#include <Networking/NetworkHandler.h>
#include <Physics/PhysicsMaterialManager.h>
#include <Rendering/GeometryHelpers.h>
#include <Rendering/Graphics.h>
#include <Script/Bindings/BindHelpers.h>
#include <Script/Bindings/StandardWorldBindHelper.h>
#include <Script/ScriptExecutor.h>
#include <Window.h>

#include <bsfCore/Components/BsCRenderable.h>
#include <bsfCore/Material/BsMaterial.h>

using namespace thrive;

// ------------------------------------ //
constexpr auto BACKGROUND_Y = -15;

float
    backgroundYForCameraHeight(float height)
{
    return BACKGROUND_Y - height;
}

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

    //! Releases graphical things. Needs to be called before shutdown
    void
        releaseOgreResources()
    {
        m_microbeBackgroundMesh = nullptr;
        m_microbeBackgroundItem = nullptr;
        if(!m_backgroundRenderNode.isDestroyed())
            m_backgroundRenderNode->destroy();
        m_backgroundRenderNode = nullptr;

        m_microbeEditorBackgroundItem = nullptr;
        if(!m_editorBackgroundRenderNode.isDestroyed())
            m_editorBackgroundRenderNode->destroy();
        m_editorBackgroundRenderNode = nullptr;

        m_MicrobeBackgroundMaterial = nullptr;
    }

    void
        createBackgroundItem()
    {
        // Create the mesh for the items
        if(!m_microbeBackgroundMesh)
            m_microbeBackgroundMesh =
                Leviathan::GeometryHelpers::CreateXZPlane(300, 300);

        if(!m_MicrobeBackgroundMaterial) {

            auto graphics = Engine::Get()->GetGraphics();

            auto shader = graphics->LoadShaderByName("background.bsl");

            m_MicrobeBackgroundMaterial = bs::Material::create(shader);

            m_MicrobeBackgroundMaterial->setTexture(
                "gTexLayer0", graphics->LoadTextureByName("Thrive_ocean0.png"));

            m_MicrobeBackgroundMaterial->setTexture(
                "gTexLayer1", graphics->LoadTextureByName("Thrive_ocean1.png"));

            m_MicrobeBackgroundMaterial->setTexture(
                "gTexLayer2", graphics->LoadTextureByName("Thrive_ocean2.png"));

            m_MicrobeBackgroundMaterial->setTexture(
                "gTexLayer3", graphics->LoadTextureByName("Thrive_ocean3.png"));
        }

        if(m_cellStage) {
            if(!m_microbeBackgroundItem) {
                m_backgroundRenderNode =
                    bs::SceneObject::create("microbe background");
                m_backgroundRenderNode->setParent(
                    m_cellStage->GetCameraSceneObject(), false);

                // m_backgroundRenderNode->setPosition(Float3(0, 0, 100));

                m_backgroundRenderNode->setPosition(Float3(
                    0, 0, backgroundYForCameraHeight(INITIAL_CAMERA_HEIGHT)));

                m_backgroundRenderNode->setRotation(
                    bs::Quaternion(bs::Vector3::UNIT_X, bs::Degree(90)));
                m_microbeBackgroundItem =
                    m_backgroundRenderNode->addComponent<bs::CRenderable>();

                m_microbeBackgroundItem->setLayer(
                    1 << *m_cellStage->GetScene());

                m_microbeBackgroundItem->setMaterial(
                    m_MicrobeBackgroundMaterial);
                m_microbeBackgroundItem->setMesh(m_microbeBackgroundMesh);
            }

            m_cellStage->SetSkybox("Thrive_ocean_skybox");
        }

        // Editor version
        if(m_microbeEditor) {
            if(!m_microbeEditorBackgroundItem) {
                m_editorBackgroundRenderNode =
                    bs::SceneObject::create("microbe editor background");

                m_editorBackgroundRenderNode->setPosition(
                    Float3(0, BACKGROUND_Y, 0));

                m_microbeEditorBackgroundItem =
                    m_editorBackgroundRenderNode
                        ->addComponent<bs::CRenderable>();

                m_microbeEditorBackgroundItem->setLayer(
                    1 << *m_microbeEditor->GetScene());

                m_microbeEditorBackgroundItem->setMaterial(
                    m_MicrobeBackgroundMaterial);
                m_microbeEditorBackgroundItem->setMesh(m_microbeBackgroundMesh);
            }
        }
    }

    ThriveGame& m_game;

    PlayerData m_playerData;

    AutoEvo m_autoEvo;

    std::shared_ptr<CellStageWorld> m_cellStage;
    std::shared_ptr<MicrobeEditorWorld> m_microbeEditor;

    //! This is the background object of the cell stage
    bs::HMesh m_microbeBackgroundMesh;

    bs::HMaterial m_MicrobeBackgroundMaterial;

    bs::HRenderable m_microbeBackgroundItem;
    bs::HSceneObject m_backgroundRenderNode;

    bs::HRenderable m_microbeEditorBackgroundItem;
    bs::HSceneObject m_editorBackgroundRenderNode;

    std::shared_ptr<MainMenuKeyPressListener> m_menuKeyPresses;
    std::shared_ptr<GlobalUtilityKeyHandler> m_globalKeyPresses;
    std::shared_ptr<PlayerMicrobeControl> m_cellStageKeys;
    std::shared_ptr<MicrobeEditorKeyHandler> m_microbeEditorKeys;

    //! When true, on tick the auto evo status is checked, if finished entering
    //! the editor is triggered.
    //! \todo There's maybe a cleaner way to do this
    bool m_waitingForAutoEvoForEditor = false;

    //! Setting for starting auto evo while the player is swimming around
    //! \todo Add this to options menu for players with weak CPUs
    bool m_autoEvoConcurrentlyWithGameplay = true;

    //! Active microbe stage run
    std::shared_ptr<RunParameters> m_autoEvoRun;
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
        m_impl->m_cellStage->ClearEntities();
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

    // TODO: unpause, if it was paused

    // Main camera that will be attached to the player
    m_cellCamera = Leviathan::ObjectLoader::LoadCamera(*m_impl->m_cellStage,
        Float3(0, 15, 0), bs::Quaternion(bs::Vector3::UNIT_X, bs::Degree(-90)));

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
    m_impl->createBackgroundItem();

    // Create a PatchMap (it will also contain the initial species)
    LOG_INFO("Generating new PatchMap");

    {
        ScriptRunningSetup setup("generatePatchMap");
        auto returned =
            ThriveGame::Get()->getMicrobeScripts()->ExecuteOnModule<PatchMap*>(
                setup, false);

        if(returned.Result != SCRIPT_RUN_RESULT::Success) {
            LOG_ERROR("Failed to run generatePatchMap");
            return;
        }

        if(!returned.Value) {
            LOG_ERROR("generatePatchMap didn't return a patch map");
            return;
        }

        // We are keeping a reference to the result
        returned.Value->AddRef();

        try {
            m_impl->m_cellStage->GetPatchManager().setNewMap(
                PatchMap::WrapPtr(returned.Value));
        } catch(const Leviathan::Exception& e) {
            LOG_ERROR("Something is wrong with the patch map, exception: ");
            e.PrintToLog();
            return;
        }
    }

    // Make sure the player species exists (a bunch of places rely on it being
    // named "Default")

    // TODO: it would be nice to make the player species name a
    // constant or something, other than this magic value
    auto playerSpecies = m_impl->m_cellStage->GetPatchManager()
                             .getCurrentMap()
                             ->findSpeciesByName("Default");

    if(!playerSpecies) {
        LOG_ERROR("Patch map generation did not generate the default species");
        return;
    }

    // Spawn player //
    setup = ScriptRunningSetup("setupPlayer");

    result = getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR("Failed to spawn player!");
        return;
    }

    // Apply patch settings
    m_impl->m_cellStage->GetPatchManager().applyPatchSettings();
    checkAutoEvoStart();
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

    // Increase player population
    const auto playerSpecies = m_impl->m_cellStage->GetPatchManager()
                                   .getCurrentMap()
                                   ->findSpeciesByName("Default");

    playerSpecies->AddRef();
    addExternalPopulationEffect(playerSpecies.get(), 30, "player reproduced");

    // Mark that we want to enter the editor
    m_impl->m_waitingForAutoEvoForEditor = true;
    checkAutoEvoStart();

    // Move to loading screen to wait for auto-evo to finish (and apply the
    // external population effects)
    // This is done here because we unlink the world now to freeze it, the
    // loading screen hides nothing being drawn under the GUI.
    // This check makes the loading text not flash on screen if the run is
    // complete already
    if(m_impl->m_autoEvo.getQueueSize() > 0) {
        updateLoadingScreen(true, "Waiting for auto-evo", "Checking status...");
    }

    Leviathan::Engine* engine = Engine::GetEngine();
    Leviathan::Window* window1 = engine->GetWindowEntity();

    // Unlink the world
    window1->LinkObjects(nullptr);

    // Disable previous handlers
    m_impl->m_menuKeyPresses->setEnabled(false);
    m_impl->m_cellStageKeys->setEnabled(false);

    LOG_INFO("Waiting for auto-evo to finish before finishing editor entry");
}

void
    ThriveGame::finishEditingClicked()
{
    LOG_INFO("Finish editing pressed");

    // Clear last auto-evo run
    m_impl->m_autoEvoRun = nullptr;

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

    // Apply patch settings
    m_impl->m_cellStage->GetPatchManager().applyPatchSettings();
    checkAutoEvoStart();
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
        LOG_INFO("Clearing the world before exiting to menu");
        if(m_impl->m_cellStage)
            m_impl->m_cellStage->ClearEntities();
        if(m_impl->m_microbeEditor)
            m_impl->m_microbeEditor->ClearEntities();

        if(m_impl->m_autoEvoRun) {
            LOG_INFO("Stopping auto evo run, returning to menu");
            m_impl->m_autoEvoRun->abort();
            m_impl->m_autoEvoRun = nullptr;
        }
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
    ThriveGame::toggleDebugOverlay()
{
    if(m_debugOverlayEnabled) {

        m_debugOverlayEnabled = false;

        auto event =
            Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                "ThriveDebugOverlayData");

        auto vars = event->GetVariables();

        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(false)));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());

    } else {

        m_debugOverlayEnabled = true;

        // The data event will make it visible
    }
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
        auto event =
            Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                "ConnectStatusMessage");

        auto vars = event->GetVariables();

        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(true)));
        vars->Add(std::make_shared<NamedVariableList>("message",
            new Leviathan::StringBlock("Invalid address specified")));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());

    } else {

        if(!m_network->JoinServer(connection)) {
            auto event =
                Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                    "ConnectStatusMessage");

            auto vars = event->GetVariables();

            vars->Add(std::make_shared<NamedVariableList>(
                "show", new Leviathan::BoolBlock(true)));
            vars->Add(std::make_shared<NamedVariableList>("message",
                new Leviathan::StringBlock(
                    "Unknown error from JoinServer (try disconnecting?)")));

            Engine::Get()->GetEventHandler()->CallEvent(event.detach());
            return;
        }

        auto event =
            Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                "ConnectStatusMessage");

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

        // m_impl->destroyBackgroundItem();

        if(m_impl->m_cellStage) {
            m_impl->m_cellStage->Release();
        }

        m_impl->m_cellStage.reset();
    }

    if(!userInitiated) {

        auto event =
            Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                "ConnectStatusMessage");

        auto vars = event->GetVariables();

        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(true)));
        vars->Add(std::make_shared<NamedVariableList>(
            "message", new Leviathan::StringBlock("Disconnected: " + reason)));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());
    } else {
        auto event =
            Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                "ConnectStatusMessage");

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
        auto event =
            Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                "ConnectStatusMessage");

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
        Float3(0, 15, 0), bs::Quaternion(bs::Vector3::UNIT_X, bs::Degree(-90)));

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
    LEVIATHAN_ASSERT(m_impl->m_MicrobeBackgroundMaterial, "no material yet");

    LOG_WRITE("TODO: redo setBackgroundMaterial");

    // // TODO: use material here
    // bs::HTexture texture =
    //     Engine::Get()->GetGraphics()->LoadTextureByName("Thrive_ocean0.png");

    // LEVIATHAN_ASSERT(texture, "failed to load background: " + material);
    // m_impl->m_MicrobeBackgroundMaterial->setTexture("gAlbedoTex", texture);
}

void
    ThriveGame::notifyCameraDistance(float height)
{
    if(m_impl->m_backgroundRenderNode) {
        m_impl->m_backgroundRenderNode->setPosition(
            Float3(0, 0, backgroundYForCameraHeight(height)));
    }

    // Editor background is static
}
// ------------------------------------ //
void
    ThriveGame::addExternalPopulationEffect(Species* species,
        int32_t change,
        const std::string& reason)
{
    if(!species)
        return;

    const auto wrapped = Species::WrapPtr(species);

    if(!m_impl->m_autoEvoRun) {

        LOG_ERROR(
            "No current auto-evo run, losing addExternalPopulationEffect");
        return;
    }

    m_impl->m_autoEvoRun->addExternalPopulationEffect(wrapped, change, reason);
}

void
    ThriveGame::checkAutoEvoStart()
{
    if(!m_impl->m_autoEvoRun) {

        // Auto-evo not running yet
        if(m_impl->m_waitingForAutoEvoForEditor ||
            m_impl->m_autoEvoConcurrentlyWithGameplay) {

            m_impl->m_autoEvoRun = std::make_shared<RunParameters>(
                m_impl->m_cellStage->GetPatchManager().getCurrentMap());

            m_impl->m_autoEvo.beginRun(m_impl->m_autoEvoRun);
        }
    }
}
// ------------------------------------ //
void
    ThriveGame::updateLoadingScreen(bool enabled,
        const std::string& status,
        const std::string& message)
{
    auto event = GenericEvent::MakeShared<GenericEvent>("UpdateLoadingScreen");

    auto vars = event->GetVariables();

    vars->Add(std::make_shared<NamedVariableList>(
        "status", new Leviathan::StringBlock(status)));

    vars->Add(std::make_shared<NamedVariableList>(
        "message", new Leviathan::StringBlock(message)));

    vars->Add(std::make_shared<NamedVariableList>(
        "show", new Leviathan::BoolBlock(enabled)));

    Engine::Get()->GetEventHandler()->CallEvent(event);
}

// ------------------------------------ //
void
    ThriveGame::Tick(int mspassed)
{
    if(m_debugOverlayEnabled) {
        auto event =
            Leviathan::GenericEvent::MakeShared<Leviathan::GenericEvent>(
                "ThriveDebugOverlayData");

        auto vars = event->GetVariables();

        auto store = Leviathan::DataStore::Get();

        vars->Add(std::make_shared<NamedVariableList>(
            "show", new Leviathan::BoolBlock(true)));
        vars->Add(std::make_shared<NamedVariableList>(
            "fps", new Leviathan::IntBlock(store->GetFPS())));
        // Convert from micro to milliseconds
        vars->Add(std::make_shared<NamedVariableList>("frameTime",
            new Leviathan::FloatBlock(store->GetFrameTime() / 1000.f)));
        vars->Add(std::make_shared<NamedVariableList>("maxFrameTime",
            new Leviathan::FloatBlock(store->GetFrameTimeMax() / 1000.f)));
        vars->Add(std::make_shared<NamedVariableList>("avgFrameTime",
            new Leviathan::FloatBlock(store->GetFrameTimeAverage() / 1000.f)));

        vars->Add(std::make_shared<NamedVariableList>(
            "tickTime", new Leviathan::FloatBlock(store->GetTickTime())));

        vars->Add(std::make_shared<NamedVariableList>(
            "ticksBehind", new Leviathan::IntBlock(store->GetTicksBehind())));

        Engine::Get()->GetEventHandler()->CallEvent(event.detach());
    }

    if(m_impl->m_waitingForAutoEvoForEditor) {

        _checkIsEditorEntryReady();
    }
}

void
    ThriveGame::_checkIsEditorEntryReady()
{
    if(m_impl->m_autoEvo.getQueueSize() > 0) {
        // Not time to enter yet
        updateLoadingScreen(
            true, "Waiting for Auto-evo", m_impl->m_autoEvo.getStatusString());
        return;
    }

    m_impl->m_autoEvoRun->applyExternalEffects();

    Leviathan::Engine* engine = Engine::GetEngine();
    Leviathan::Window* window1 = engine->GetWindowEntity();

    LOG_INFO("Auto-evo queue is now empty, entering editor");
    m_impl->m_waitingForAutoEvoForEditor = false;

    // Fire an event to switch over the GUI
    {
        auto event =
            GenericEvent::MakeShared<GenericEvent>("MicrobeEditorEntered");

        auto vars = event->GetVariables();

        vars->Add(std::make_shared<NamedVariableList>("patchMapJSON",
            new Leviathan::StringBlock("{'todo': 'map here'}")));

        Engine::Get()->GetEventHandler()->CallEvent(event);
    }

    // Hide loading screen
    updateLoadingScreen(false, "", "");

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
        Float3(0, 15, 0), bs::Quaternion(bs::Vector3::UNIT_X, bs::Degree(-90)));

    // TODO: attach a ligth to the camera
    // -- Light
    //     local light = OgreLightComponent.new()
    //     light:setRange(200)
    //     entity:addComponent(light)

    m_impl->m_microbeEditor->SetCamera(camera);

    // Create backgrounds if they don't exist
    m_impl->createBackgroundItem();

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
// ------------------------------------ //
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
    // Make sure all simulations have stopped
    m_impl->m_autoEvo.abortSimulations();

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
    keyconfigobj->AddKeyIfMissing(guard, "ToggleDebugOverlay", {"F3"});
}
// ------------------------------------ //
bool
    ThriveGame::InitLoadCustomScriptTypes(asIScriptEngine* engine)
{
    return registerThriveScriptTypes(engine);
}
