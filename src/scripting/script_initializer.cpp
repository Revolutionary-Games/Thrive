// ------------------------------------ //
#include "script_initializer.h"

#include "auto-evo/auto-evo_script_helpers.h"
#include "auto-evo/run_results.h"
#include "engine/player_data.h"
#include "general/hex.h"
#include "general/locked_map.h"
#include "general/properties_component.h"
#include "microbe_stage/species.h"

#include "ThriveGame.h"

#include <Script/Bindings/BindHelpers.h>
#include <Script/ScriptExecutor.h>

using namespace thrive;
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

    if(engine->RegisterObjectMethod("PlayerData", "bool isFreeBuilding() const",
           asMETHOD(PlayerData, isFreeBuilding), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}


bool
    registerSpecies(asIScriptEngine* engine)
{
    ANGELSCRIPT_REGISTER_REF_TYPE("Species", Species);

    if(engine->RegisterObjectBehaviour("Species", asBEHAVE_FACTORY,
           "Species@ f(const string &in name)", asFUNCTION(Species::factory),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Species",
           "void applyImmediatePopulationChange(int32 change)",
           asMETHOD(Species, applyImmediatePopulationChange),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // A bit hacky
    if(engine->RegisterInterface("SpeciesStoredOrganelleType") < 0) {

        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Species",
           "array<SpeciesStoredOrganelleType@>@ organelles",
           asOFFSET(Species, organelles)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Species",
           "dictionary@ avgCompoundAmounts",
           asOFFSET(Species, avgCompoundAmounts)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Species",
           "MEMBRANE_TYPE speciesMembraneType",
           asOFFSET(Species, speciesMembraneType)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "string stringCode", asOFFSET(Species, stringCode)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "Float4 colour", asOFFSET(Species, colour)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectProperty(
           "Species", "const string name", asOFFSET(Species, name)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "string genus", asOFFSET(Species, genus)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "string epithet", asOFFSET(Species, epithet)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "bool isBacteria", asOFFSET(Species, isBacteria)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "float aggression", asOFFSET(Species, aggression)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "float fear", asOFFSET(Species, fear)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "float activity", asOFFSET(Species, activity)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "float focus", asOFFSET(Species, focus)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Species", "float opportunism",
           asOFFSET(Species, opportunism)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Species", "const int32 population",
           asOFFSET(Species, population)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Species", "int32 generation", asOFFSET(Species, generation)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

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

bool
    registerAutoEvo(asIScriptEngine* engine)
{
    // ------------------------------------ //
    // RunResults
    ANGELSCRIPT_REGISTER_REF_TYPE("RunResults", autoevo::RunResults);

    if(engine->RegisterObjectBehaviour("RunResults", asBEHAVE_FACTORY,
           "RunResults@ f()", asFUNCTION(autoevo::RunResults::factory),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // TODO: it's a bit dirty that this accepts a const Species as the result
    // apply stage may modify it. Maybe some of the const patch methods should
    // return non const Species
    if(engine->RegisterObjectMethod("RunResults",
           "void addPopulationResultForSpecies(const Species@ species, int32 "
           "patch, int32 newPopulation)",
           asMETHOD(autoevo::RunResults, addPopulationResultForSpeciesWrapper),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // This is fine having const species
    if(engine->RegisterObjectMethod("RunResults",
           "int32 getPopulationInPatch(const Species@ species, int32 "
           "patch)",
           asMETHOD(autoevo::RunResults, getPopulationInPatchWrapper),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // SpeciesMigration
    ANGELSCRIPT_REGISTER_REF_TYPE(
        "SpeciesMigration", autoevo::SpeciesMigration);

    if(engine->RegisterObjectProperty("SpeciesMigration", "int32 fromPatch",
           asOFFSET(autoevo::SpeciesMigration, fromPatch)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesMigration", "int32 toPatch",
           asOFFSET(autoevo::SpeciesMigration, toPatch)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesMigration", "int32 population",
           asOFFSET(autoevo::SpeciesMigration, population)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesMigration",
           "const Species@ getSpecies() const",
           asMETHOD(autoevo::SpeciesMigration, getSpecies),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // SimulationConfiguration
    ANGELSCRIPT_REGISTER_REF_TYPE(
        "SimulationConfiguration", autoevo::SimulationConfiguration);

    if(engine->RegisterObjectProperty("SimulationConfiguration", "int32 steps",
           asOFFSET(autoevo::SimulationConfiguration, steps)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SimulationConfiguration",
           "uint64 getExcludedSpeciesCount() const",
           asMETHOD(autoevo::SimulationConfiguration, getExcludedSpeciesCount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SimulationConfiguration",
           "const Species@ getExcludedSpecies(uint64 index) const",
           asMETHOD(autoevo::SimulationConfiguration, getExcludedSpecies),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SimulationConfiguration",
           "uint64 getExtraSpeciesCount() const",
           asMETHOD(autoevo::SimulationConfiguration, getExtraSpeciesCount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SimulationConfiguration",
           "const Species@ getExtraSpecies(uint64 index) const",
           asMETHOD(autoevo::SimulationConfiguration, getExtraSpecies),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SimulationConfiguration",
           "uint64 getMigrationsCount() const",
           asMETHOD(autoevo::SimulationConfiguration, getMigrationsCount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SimulationConfiguration",
           "const SpeciesMigration@ getMigration(uint64 index) const",
           asMETHOD(autoevo::SimulationConfiguration, getMigration),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

bool
    thrive::registerThriveScriptTypes(asIScriptEngine* engine)
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

    if(!registerSimulationDataAndJsons(engine))
        return false;

    if(!registerTweakedProcess(engine))
        return false;

    if(!registerOrganelles(engine))
        return false;

    if(!registerSpecies(engine))
        return false;

    if(!registerPatches(engine))
        return false;

    if(!bindScriptAccessibleSystems(engine))
        return false;

    if(!registerPlayerData(engine))
        return false;

    if(!registerHexFunctions(engine))
        return false;

    if(!registerTimedWorldOperations(engine))
        return false;

    if(!registerAutoEvo(engine))
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

    if(engine->RegisterObjectMethod("ThriveGame",
           "void addExternalPopulationEffect(Species@ species, int32 change, "
           "const string &in reason)",
           asMETHOD(ThriveGame, addExternalPopulationEffect),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ThriveGame",
           "void playerMovedToPatch(int32 patch)",
           asMETHOD(ThriveGame, playerMovedToPatch), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // if(engine->RegisterObjectMethod("Client",
    //         "bool Connect(const string &in address, string &out
    //         errormessage)", asMETHODPR(Client, Connect, (const std::string&,
    //         std::string&), bool), asCALL_THISCALL) < 0)
    // {
    //     ANGELSCRIPT_REGISTERFAIL;
    // }

    if(!bindWorlds(engine))
        return false;

    if(engine->RegisterObjectMethod("ThriveGame",
           "CellStageWorld@ getCellStage()", asMETHOD(ThriveGame, getCellStage),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // if(engine->RegisterGlobalFunction(
    //        "ObjectID findSpeciesEntityByName(CellStageWorld@ world, "
    //        "const string &in name)",
    //        asFUNCTION(findSpeciesEntityByName), asCALL_CDECL) < 0) {
    //     ANGELSCRIPT_REGISTERFAIL;
    // }

    return true;
}
