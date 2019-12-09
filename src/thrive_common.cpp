// ------------------------------------ //
#include "thrive_common.h"

#include "microbe_stage/simulation_parameters.h"

#include <Addons/GameModuleLoader.h>
#include <BulletCollision/NarrowPhaseCollision/btPersistentManifold.h>
#include <Engine.h>
#include <Entities/ScriptComponentHolder.h>
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
    cellHitEngulfable(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second)
{
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    ScriptRunningSetup setup("cellHitEngulfable");

    auto result =
        ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(setup,
            false, gameWorld, first.GetOwningEntity(),
            second.GetOwningEntity());

    if(result.Result != SCRIPT_RUN_RESULT::Success)
        LOG_ERROR("Failed to run script side cellHitEngulfable");
}

void
    cellHitDamageChunk(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second,
        const btPersistentManifold& manifold)
{
    GameWorld* gameWorld = physicalWorld.GetGameWorld();

    Leviathan::PhysicsShape* shape = second.GetShape();

    const int numContacts = manifold.getNumContacts();

    ScriptRunningSetup setup("cellHitDamageChunk");

    for(int i = 0; i < numContacts; ++i) {
        const btManifoldPoint& contactPoint = manifold.getContactPoint(i);

        auto returned =
            ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<void>(
                setup, false, gameWorld, first.GetOwningEntity(),
                second.GetOwningEntity(), shape, contactPoint.m_index0);

        if(returned.Result != SCRIPT_RUN_RESULT::Success)
            LOG_ERROR("Failed to run script side cellHitDamageChunk");
    }
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

//! \brief Used to skip agents hitting cells of their own species
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
    agentCollidedManifold(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second,
        const btPersistentManifold& manifold)
{
    Leviathan::PhysicsShape* cellShape = first.GetShape();
    Leviathan::PhysicsShape* otherShape = second.GetShape();

    GameWorld* gameWorld = physicalWorld.GetGameWorld();
    auto holder = gameWorld->GetScriptComponentHolder("MicrobeComponent");
    LEVIATHAN_ASSERT(holder, "GameWorld has no microbe component holder");

    // The world will hold this object while we do our thing so we can
    // immediately release our reference
    holder->Release();

    asIScriptObject* microbeComponent = nullptr;
    ObjectID otherId;
    ObjectID cellEntity;
    int cellSubCollision;

    const int numContacts = manifold.getNumContacts();

    ScriptRunningSetup setup("cellHitAgent");

    for(int i = 0; i < numContacts; ++i) {

        const btManifoldPoint& contactPoint = manifold.getContactPoint(i);

        if(contactPoint.getDistance() < 0.f) {
            if(!microbeComponent) {

                // The holder will keep the references alive, so we can release
                // them immediately
                microbeComponent = holder->Find(first.GetOwningEntity());

                if(!microbeComponent) {
                    microbeComponent = holder->Find(second.GetOwningEntity());
                    std::swap(cellShape, otherShape);
                    otherId = first.GetOwningEntity();
                    cellEntity = second.GetOwningEntity();
                    cellSubCollision = contactPoint.m_index1;
                } else {
                    otherId = second.GetOwningEntity();
                    cellEntity = first.GetOwningEntity();
                    cellSubCollision = contactPoint.m_index0;
                }

                if(!microbeComponent)
                    return;

                microbeComponent->Release();
            }

            auto returned =
                ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<bool>(
                    setup, false, gameWorld, otherId, cellEntity,
                    microbeComponent, cellShape, cellSubCollision);

            if(returned.Result != SCRIPT_RUN_RESULT::Success) {
                LOG_ERROR("Failed to run script side cell on agent collision");
                break;
            }

            if(returned.Value)
                break;
        }
    }
}

void
    cellOnManifoldCallback(Leviathan::PhysicalWorld& physicalWorld,
        Leviathan::PhysicsBody& first,
        Leviathan::PhysicsBody& second,
        const btPersistentManifold& manifold)
{
    Leviathan::PhysicsShape* shape1 = first.GetShape();
    Leviathan::PhysicsShape* shape2 = second.GetShape();

    LEVIATHAN_ASSERT(
        shape1 && shape2, "some body in physics callback has no shape");

    GameWorld* gameWorld = physicalWorld.GetGameWorld();
    auto holder = gameWorld->GetScriptComponentHolder("MicrobeComponent");
    LEVIATHAN_ASSERT(holder, "GameWorld has no microbe component holder");

    // The world will hold the holder while we do our thing
    holder->Release();

    asIScriptObject* obj1 = nullptr;
    asIScriptObject* obj2;

    bool appliedEffect = false;

    const int numContacts = manifold.getNumContacts();

    ScriptRunningSetup setup("cellOnCellActualContact");

    for(int i = 0; i < numContacts; ++i) {

        const btManifoldPoint& contactPoint = manifold.getContactPoint(i);

        if(contactPoint.getDistance() < 0.f) {
            if(!obj1) {

                // The holder will keep the references alive, so we can release
                // them immediately
                obj1 = holder->Find(first.GetOwningEntity());

                if(!obj1)
                    return;
                obj1->Release();

                obj2 = holder->Find(second.GetOwningEntity());

                if(!obj2)
                    return;
                obj2->Release();
            }

            auto returned =
                ThriveCommon::get()->getMicrobeScripts()->ExecuteOnModule<bool>(
                    setup, false, gameWorld, shape1, obj1, shape2, obj2,
                    contactPoint.m_index0, contactPoint.m_index1,
                    std::abs(static_cast<float>(contactPoint.getDistance())),
                    appliedEffect);

            if(returned.Result != SCRIPT_RUN_RESULT::Success) {
                LOG_ERROR("Failed to run script side cell on cell collision");
                break;
            }

            appliedEffect = returned.Value;
        }
    }
}

std::unique_ptr<Leviathan::PhysicsMaterialManager>
    ThriveCommon::createPhysicsMaterials() const
{
    // Setup materials
    auto cellMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("cell", 1);
    auto agentMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("agentCollision", 3);
    auto engulfableMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("engulfableMaterial", 4);
    auto chunkDamageMaterial =
        std::make_unique<Leviathan::PhysicalMaterial>("chunkDamageMaterial", 5);

    // Set callbacks //

    // Chunks
    // These two should be still updated
    cellMaterial->FormPairWith(*engulfableMaterial)
        .SetCallbacks(nullptr, cellHitEngulfable);
    cellMaterial->FormPairWith(*chunkDamageMaterial)
        .SetCallbacks(nullptr, nullptr, cellHitDamageChunk);
    // to be like this (copy code from agentCollidedManifold):
    // cellMaterial->FormPairWith(*engulfableMaterial)
    //     .SetCallbacks(nullptr, cellHitEngulfableManifold);
    // cellMaterial->FormPairWith(*chunkDamageMaterial)
    //     .SetCallbacks(nullptr, nullptr, cellHitDamageChunkManifold);
    // Agents
    cellMaterial->FormPairWith(*agentMaterial)
        .SetCallbacks(agentCallback, nullptr, agentCollidedManifold);

    // Engulfing and stabbing
    cellMaterial->FormPairWith(*cellMaterial)
        .SetCallbacks(
            cellOnCellAABBHitCallback, nullptr, cellOnManifoldCallback);

    auto manager = std::make_unique<Leviathan::PhysicsMaterialManager>();

    manager->LoadedMaterialAdd(std::move(cellMaterial));
    manager->LoadedMaterialAdd(std::move(agentMaterial));
    manager->LoadedMaterialAdd(std::move(engulfableMaterial));
    manager->LoadedMaterialAdd(std::move(chunkDamageMaterial));

    return manager;
}
