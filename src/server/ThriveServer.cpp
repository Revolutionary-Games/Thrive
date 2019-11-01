// ------------------------------------ //
#include "ThriveServer.h"

#include "engine/player_data.h"
#include "general/global_keypresses.h"

#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"
#include "main_menu_keypresses.h"
#include "microbe_stage/simulation_parameters.h"
#include "scripting/script_initializer.h"
#include "thrive_net_handler.h"
#include "thrive_version.h"
#include "thrive_world_factory.h"

#include <Addons/GameModuleLoader.h>
#include <Application/GameConfiguration.h>
#include <GUI/GuiView.h>
#include <Handlers/ObjectLoader.h>
#include <Networking/NetworkHandler.h>
#include <Physics/PhysicsMaterialManager.h>
#include <Rendering/GeometryHelpers.h>
#include <Script/Bindings/BindHelpers.h>
#include <Script/Bindings/StandardWorldBindHelper.h>
#include <Script/ScriptExecutor.h>


using namespace thrive;
// ------------------------------------ //
//! Contains properties that would need unnecessary large includes in the header
class ThriveServer::Implementation {
public:
    Implementation(ThriveServer& game) : m_game(game), m_playerData("player") {}

    ThriveServer& m_game;

    PlayerData m_playerData;

    std::shared_ptr<CellStageWorld> m_cellStage;
    // std::shared_ptr<MicrobeEditorWorld> m_microbeEditor;

    // std::shared_ptr<PlayerMicrobeControl> m_cellStageKeys;
};

// ------------------------------------ //
ThriveServer::ThriveServer()
{
    staticInstance = this;
}

ThriveServer::~ThriveServer()
{
    staticInstance = nullptr;
}

std::string
    ThriveServer::GenerateWindowTitle()
{
    return "Thrive Server " GAME_VERSIONS;
}

ThriveServer*
    ThriveServer::get()
{
    return staticInstance;
}

ThriveServer* ThriveServer::staticInstance = nullptr;

Leviathan::NetworkInterface*
    ThriveServer::_GetApplicationPacketHandler()
{
    if(!m_network)
        m_network = std::make_unique<ThriveServerNetHandler>();
    return m_network.get();
}

void
    ThriveServer::_ShutdownApplicationPacketHandler()
{
    m_network.reset();
}
// ------------------------------------ //
void
    ThriveServer::setupServerWorlds()
{
    Leviathan::Engine* engine = Engine::GetEngine();

    LOG_INFO("Starting server game state");

    // Create world if not already created //
    if(!m_impl->m_cellStage) {
        LOG_INFO("ThriveServer: startNewGame: Creating new cellstage world");
        m_impl->m_cellStage =
            std::dynamic_pointer_cast<CellStageWorld>(engine->CreateWorld(
                nullptr, static_cast<int>(THRIVE_WORLD_TYPE::CELL_STAGE),
                createPhysicsMaterials(),
                Leviathan::WorldNetworkSettings::GetSettingsForServer()));
    }

    LEVIATHAN_ASSERT(m_impl->m_cellStage, "Cell stage world creation failed");

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
    LEVIATHAN_ASSERT(getMicrobeScripts(), "microbe scripts not loaded");

    LOG_INFO("Calling world setup script setupScriptsForWorld_Server");

    ScriptRunningSetup setup;
    setup.SetEntrypoint("setupScriptsForWorld_Server");

    auto result = getMicrobeScripts()->ExecuteOnModule<void>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run script setup function: " + setup.Entryfunction);
        MarkAsClosing();
        return;
    }

    LOG_INFO("Finished calling setupScriptsForWorld");

    // // Spawn player //
    // setup = ScriptRunningSetup("setupPlayer");

    // result = getMicrobeScripts()->ExecuteOnModule<void>(
    //     setup, false, m_impl->m_cellStage.get());

    // if(result.Result != SCRIPT_RUN_RESULT::Success) {

    //     LOG_ERROR("Failed to spawn player!");
    //     return;
    // }

    // Allow players joining
    m_network->SetServerAllowPlayers(true);
    m_network->SetServerStatus(Leviathan::SERVER_STATUS::Running);
}
// ------------------------------------ //
void
    ThriveServer::spawnPlayer(
        const std::shared_ptr<Leviathan::ConnectedPlayer>& player)
{
    LOG_INFO("ThriveServer spawning player");

    ScriptRunningSetup setup;
    setup.SetEntrypoint("spawnPlayer_Server");

    auto result = getMicrobeScripts()->ExecuteOnModule<ObjectID>(
        setup, false, m_impl->m_cellStage.get());

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run player spawn function: " + setup.Entryfunction);
        return;
    }

    ObjectID playerEntity = result.Value;

    try {
        m_impl->m_cellStage->GetComponent_Sendable(playerEntity);
    } catch(const Leviathan::NotFound&) {
        LEVIATHAN_ASSERT(
            false, "spawn player on server didn't create sendable component");
    }

    try {
        m_impl->m_cellStage->GetComponent_MembraneComponent(playerEntity);
    } catch(const Leviathan::NotFound&) {
        LEVIATHAN_ASSERT(
            false, "spawn player on server didn't create membrane component");
    }

    m_impl->m_cellStage->SetLocalControl(
        playerEntity, true, player->GetConnection());
}
// ------------------------------------ //
CellStageWorld*
    ThriveServer::getCellStage()
{
    return m_impl->m_cellStage.get();
}

std::shared_ptr<CellStageWorld>
    ThriveServer::getCellStageShared()
{
    return m_impl->m_cellStage;
}
// ------------------------------------ //
void
    ThriveServer::Tick(float elapsed)
{}

void
    ThriveServer::CustomizeEnginePostLoad()
{
    try {
        m_impl = std::make_unique<Implementation>(*this);
    } catch(const Leviathan::InvalidArgument& e) {

        LOG_ERROR("ThriveServer: loading configuration data failed: ");
        e.PrintToLog();
        MarkAsClosing();
        return;
    }

    if(!loadScriptsAndConfigs()) {

        LOG_ERROR("Failed to load init data, quitting");
        MarkAsClosing();
        return;
    }

    if(!scriptSetup()) {

        LOG_ERROR("ThriveServer: failed to run setup script functions");
        MarkAsClosing();
        return;
    }

    setupServerWorlds();
}
// ------------------------------------ //
void
    ThriveServer::EnginePreShutdown()
{
    // Shutdown scripting first to allow it to still do anything it wants //
    releaseScripts();

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
    ThriveServer::CheckGameConfigurationVariables(Lock& guard,
        GameConfiguration* configobj)
{
    NamedVars* vars = configobj->AccessVariables(guard);

    // Default server port //
    if(vars->ShouldAddValueIfNotFoundOrWrongType<int>("DefaultServerPort")) {
        // Add new //
        vars->AddVar("DefaultServerPort", new VariableBlock(int(53226)));
        configobj->MarkModified(guard);
    }
}

void
    ThriveServer::CheckGameKeyConfigVariables(Lock& guard,
        KeyConfiguration* keyconfigobj)
{}
// ------------------------------------ //
bool
    ThriveServer::InitLoadCustomScriptTypes(asIScriptEngine* engine)
{
    return registerThriveScriptTypes(engine);
}
