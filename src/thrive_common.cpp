// ------------------------------------ //
#include "thrive_common.h"

#include "microbe_stage/simulation_parameters.h"

#include <Addons/GameModuleLoader.h>
#include <Engine.h>
#include <Physics/PhysicsMaterialManager.h>
#include <Script/Bindings/BindHelpers.h>
#include <Script/Bindings/StandardWorldBindHelper.h>

using namespace thrive;
// ------------------------------------ //
struct ThriveCommon::Implementation {

    // This contains all the microbe_stage AngelScript code
    Leviathan::GameModule::pointer m_microbeScripts;

    // This is "temporarily" merged with the microbe scripts as this needs to
    // share some types
    // // This contains all the microbe_editor AngelScript code
    // Leviathan::GameModule::pointer m_MicrobeEditorScripts;
};

ThriveCommon::ThriveCommon() : m_commonImpl(std::make_unique<Implementation>())
{
    staticInstance = this;
}

ThriveCommon::~ThriveCommon()
{
    if(staticInstance == this)
        staticInstance = nullptr;
}

ThriveCommon*
    ThriveCommon::get()
{
    return staticInstance;
}

ThriveCommon* ThriveCommon::staticInstance = nullptr;
// ------------------------------------ //
Leviathan::GameModule*
    ThriveCommon::getMicrobeScripts()
{
    return m_commonImpl->m_microbeScripts.get();
}
// ------------------------------------ //
bool
    ThriveCommon::loadScriptsAndConfigs()
{
    Engine* engine = Engine::Get();

    // Load json data //
    SimulationParameters::init();

    // Load scripts
    LOG_INFO("ThriveCommon: loading main scripts");

    // TODO: should these load failures be fatal errors (process would exit
    // immediately)

    try {
        m_commonImpl->m_microbeScripts =
            engine->GetGameModuleLoader()->Load("microbe_stage", "ThriveGame");
    } catch(const Leviathan::Exception& e) {

        LOG_ERROR(
            "ThriveCommon: microbe_stage module failed to load, exception:");
        e.PrintToLog();
        return false;
    }

    LOG_INFO("ThriveGame: script loading succeeded");
    return true;
}

void
    ThriveCommon::releaseScripts()
{
    if(m_commonImpl->m_microbeScripts) {
        m_commonImpl->m_microbeScripts->ReleaseScript();
        m_commonImpl->m_microbeScripts.reset();
    }
}
// ------------------------------------ //
bool
    ThriveCommon::scriptSetup()
{
    LOG_INFO("Calling global setup script setupProcesses");

    ScriptRunningSetup setup("setupProcesses");

    auto result =
        m_commonImpl->m_microbeScripts->ExecuteOnModule<void>(setup, false);

    if(result.Result != SCRIPT_RUN_RESULT::Success) {

        LOG_ERROR(
            "Failed to run script setup function: " + setup.Entryfunction);
        return false;
    }

    LOG_INFO("Finished calling the above setup script");

    LOG_INFO("Calling global setup script setupOrganelles");

    setup = ScriptRunningSetup("setupOrganelles");

    result =
        m_commonImpl->m_microbeScripts->ExecuteOnModule<void>(setup, false);

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
// Physics materials


void
    cellHitFloatingOrganelle(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second)
{
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    ScriptRunningSetup setup("cellHitFloatingOrganelle");

    auto result =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(setup,
            false, gameWorld, first.GetOwningEntity(),
            second.GetOwningEntity());

    if(result.Result != SCRIPT_RUN_RESULT::Success)
        LOG_ERROR("Failed to run script side cellHitFloatingOrganelle");
}

void
    cellHitIron(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second)
{
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    ScriptRunningSetup setup("cellHitIron");

    auto result =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(setup,
            false, gameWorld, first.GetOwningEntity(),
            second.GetOwningEntity());

    if(result.Result != SCRIPT_RUN_RESULT::Success)
        LOG_ERROR("Failed to run script side cellHitIron");
}

//! \todo This should return false when either cell is engulfing and apply the
//! damaging effect
bool
    cellOnCellAABBHitCallback(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second)
{
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    ScriptRunningSetup setup("beingEngulfed");

    auto returned =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<bool>(setup,
            false, gameWorld, first.GetOwningEntity(),
            second.GetOwningEntity());

    if(returned.Result != SCRIPT_RUN_RESULT::Success) {
        LOG_ERROR("Failed to run script side beingEngulfed");
        return true;
    }

    return returned.Value;
}


bool
    agentCallback(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second)
{
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    // Now we can do more interetsing things with agents
    ScriptRunningSetup setup("hitAgent");

    auto returned =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<bool>(setup,
            false, gameWorld, first.GetOwningEntity(),
            second.GetOwningEntity());

    if(returned.Result != SCRIPT_RUN_RESULT::Success) {
        LOG_ERROR("Failed to run script side hitAgent");
        return true;
    }

    return returned.Value;
}

void
    agentCollided(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second)
{
    // This will call a script that pulls cells in towards engulfers
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    ScriptRunningSetup setup("cellHitAgent");

    auto returned =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(setup,
            false, gameWorld, first.GetOwningEntity(),
            second.GetOwningEntity());

    if(returned.Result != SCRIPT_RUN_RESULT::Success) {
        LOG_ERROR("Failed to run script side beingEngulfed");
    }
}



void
    cellOnCellActualContact(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second)
{
    // This will call a script that pulls cells in towards engulfers
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    ScriptRunningSetup setup("cellOnCellActualContact");

    auto returned =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(setup,
            false, gameWorld, first.GetOwningEntity(),
            second.GetOwningEntity());

    if(returned.Result != SCRIPT_RUN_RESULT::Success) {
        LOG_ERROR("Failed to run script side beingEngulfed");
    }
}

std::unique_ptr<Leviathan::PhysicsMaterialManager>
    ThriveCommon::createPhysicsMaterials() const
{
    // Setup materials
    auto cellMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("cell", 1);
    auto floatingOrganelleMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("floatingOrganelle", 2);
    auto ironMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("iron", 2);
    auto agentMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("agentCollision", 3);

    // Set callbacks //

    // Floating organelles
    cellMaterial->FormPairWith(*floatingOrganelleMaterial)
        .SetCallbacks(nullptr, cellHitFloatingOrganelle);

    // Iron
    cellMaterial->FormPairWith(*ironMaterial)
        .SetCallbacks(nullptr, cellHitIron);

    // Agents
    cellMaterial->FormPairWith(*agentMaterial)
        .SetCallbacks(agentCallback, agentCollided);

    // Engulfing
    cellMaterial->FormPairWith(*cellMaterial)
        .SetCallbacks(cellOnCellAABBHitCallback, cellOnCellActualContact);

    auto manager = std::make_unique<Leviathan::PhysicsMaterialManager>();

    manager->LoadedMaterialAdd(std::move(cellMaterial));
    manager->LoadedMaterialAdd(std::move(floatingOrganelleMaterial));
    manager->LoadedMaterialAdd(std::move(ironMaterial));
    manager->LoadedMaterialAdd(std::move(agentMaterial));

    return manager;
}
