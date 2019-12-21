// ------------------------------------ //
#include "script_initializer.h"

#include "general/timed_life_system.h"
#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"
#include "microbe_stage/player_microbe_control.h"

#include <Script/Bindings/BindHelpers.h>
#include <Script/ScriptExecutor.h>

#include <boost/scope_exit.hpp>

using namespace thrive;
// ------------------------------------ //
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
    commonScriptReceivedOrganelleArrayHelper(const CScriptArray* organelles,
        const Patch* patch,
        std::vector<OrganelleTemplate::pointer>& convertedOrganelles)
{
    if(!patch) {
        asGetActiveContext()->SetException("patch may not be null");
        return false;
    }
    if(!organelles) {
        asGetActiveContext()->SetException("organelles may not be null");
        return false;
    }

    static const auto wantedId =
        Leviathan::AngelScriptTypeIDResolver<OrganelleTemplate>::Get(
            Leviathan::ScriptExecutor::Get());
    static const auto wantedIdConst =
        Leviathan::AngelScriptTypeIDResolver<const OrganelleTemplate>::Get(
            Leviathan::ScriptExecutor::Get());

    if(organelles->GetElementTypeId() != wantedId &&
        organelles->GetElementTypeId() != wantedIdConst) {
        asGetActiveContext()->SetException("organelles array type mismatch");
        return false;
    }

    convertedOrganelles.reserve(organelles->GetSize());

    for(unsigned i = 0; i < organelles->GetSize(); ++i) {
        convertedOrganelles.push_back(OrganelleTemplate::pointer(
            *reinterpret_cast<OrganelleTemplate* const*>(organelles->At(i))));
    }

    return true;
}

std::string
    computeOrganelleProcessEfficienciesWrapper(ProcessSystem& self,
        const CScriptArray* organelles,
        const Patch* patch)
{
    BOOST_SCOPE_EXIT(&organelles, &patch)
    {
        if(organelles)
            organelles->Release();

        if(patch)
            patch->Release();
    }
    BOOST_SCOPE_EXIT_END;

    std::vector<OrganelleTemplate::pointer> convertedOrganelles;
    if(!commonScriptReceivedOrganelleArrayHelper(
           organelles, patch, convertedOrganelles))
        return "";

    return self.computeOrganelleProcessEfficiencies(
        convertedOrganelles, patch->getBiome());
}

std::string
    computeEnergyBalanceWrapper(ProcessSystem& self,
        const CScriptArray* organelles,
        const Patch* patch)
{
    BOOST_SCOPE_EXIT(&organelles, &patch)
    {
        if(organelles)
            organelles->Release();

        if(patch)
            patch->Release();
    }
    BOOST_SCOPE_EXIT_END;

    std::vector<OrganelleTemplate::pointer> convertedOrganelles;
    if(!commonScriptReceivedOrganelleArrayHelper(
           organelles, patch, convertedOrganelles))
        return "";

    return self.computeEnergyBalance(convertedOrganelles, patch->getBiome());
}
// ------------------------------------ //
class WorldEffectScript : public WorldEffect {
public:
    //! \note Caller must have incremented ref count already on func
    WorldEffectScript(asIScriptFunction* func) : m_func(func)
    {
        if(!m_func)
            throw InvalidArgument("no func given to WorldEffectScript");
    }

    ~WorldEffectScript()
    {
        m_func->Release();
    }

    void
        onTimePassed(double elapsed, long double totalTimePassed) override
    {
        double totalConverted = static_cast<double>(totalTimePassed);

        ScriptRunningSetup setup;
        auto result = Leviathan::ScriptExecutor::Get()->RunScript<void>(
            m_func, nullptr, setup, m_world, elapsed, totalConverted);

        if(result.Result != SCRIPT_RUN_RESULT::Success) {

            LOG_ERROR("Failed to run WorldEffectScript function");
        }
    }

    asIScriptFunction* m_func;
};


void
    registerEffectProxy(TimedWorldOperations& self,
        const std::string& name,
        asIScriptFunction* func)
{
    self.registerEffect(name, std::make_unique<WorldEffectScript>(func));
}
// ------------------------------------ //
bool
    thrive::bindScriptAccessibleSystems(asIScriptEngine* engine)
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

    // Process System
    if(engine->RegisterObjectType(
           "ProcessSystem", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ProcessSystem",
           "void setProcessBiome(int biomeId)",
           asMETHOD(ProcessSystem, setProcessBiome), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ProcessSystem",
           "double getDissolved(CompoundId compoundData)",
           asMETHOD(ProcessSystem, getDissolved), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ProcessSystem",
           "string computeOrganelleProcessEfficiencies(const "
           "array<OrganelleTemplate@>@ organelles, const Patch@ patch)",
           asFUNCTION(computeOrganelleProcessEfficienciesWrapper),
           asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ProcessSystem",
           "string computeOrganelleProcessEfficiencies(const "
           "array<const OrganelleTemplate@>@ organelles, const Patch@ patch)",
           asFUNCTION(computeOrganelleProcessEfficienciesWrapper),
           asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ProcessSystem",
           "string computeEnergyBalance(const "
           "array<const OrganelleTemplate@>@ organelles, const Patch@ patch)",
           asFUNCTION(computeEnergyBalanceWrapper),
           asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // CompoundCloudSystem
    if(engine->RegisterObjectType(
           "CompoundCloudSystem", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundCloudSystem",
           "bool addCloud(CompoundId compound, float density, const Float3 &in "
           "worldPosition)",
           asMETHOD(CompoundCloudSystem, addCloud), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundCloudSystem",
           "int takeCompound(CompoundId compound, const Float3 &in "
           "worldPosition, float rate)",
           asMETHOD(CompoundCloudSystem, takeCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundCloudSystem",
           "int amountAvailable(CompoundId compound, const Float3 &in "
           "worldPosition, float rate)",
           asMETHOD(CompoundCloudSystem, takeCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // PlayerMicrobeControlSystem

    // static
    if(engine->SetDefaultNamespace("PlayerMicrobeControlSystem") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "Float3 getTargetPoint(GameWorld &in worldWithCamera)",
           asFUNCTION(PlayerMicrobeControlSystem::getTargetPoint),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->SetDefaultNamespace("") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    return true;
}
// ------------------------------------ //
bool
    thrive::registerTimedWorldOperations(asIScriptEngine* engine)
{
    // ------------------------------------ //
    // PatchManager
    if(engine->RegisterObjectType(
           "TimedWorldOperations", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("TimedWorldOperations",
           "void onTimePassed(double timePassed)",
           asMETHOD(TimedWorldOperations, onTimePassed), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterFuncdef("void ElapsedTimeFunc(GameWorld@ world, double "
                               "elapsed, double totalTimePassed)") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("TimedWorldOperations",
           "void registerEffect(const string &in name, ElapsedTimeFunc@ "
           "callback)",
           asFUNCTION(registerEffectProxy), asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}
