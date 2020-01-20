// ------------------------------------ //
#include "script_initializer.h"

#include "microbe_stage/patch.h"
#include "microbe_stage/patch_manager.h"

#include <Script/Bindings/BindHelpers.h>
#include <Script/ScriptConversionHelpers.h>
#include <Script/ScriptExecutor.h>

using namespace thrive;
// ------------------------------------ //
//! This is safe to use as long as scripting module is valid
static asITypeInfo* mapWrapperTypeInfo = nullptr;

CScriptArray*
    patchMapGetPatchesWrapper(const PatchMap& self)
{
    const auto& patches = self.getPatches();

    LEVIATHAN_ASSERT(
        mapWrapperTypeInfo, "map wrapper type info is not retrieved");

    CScriptArray* array = CScriptArray::Create(mapWrapperTypeInfo);

    if(!array)
        return nullptr;

    array->Reserve(static_cast<asUINT>(patches.size()));

    for(auto iter = patches.begin(); iter != patches.end(); ++iter) {
        Patch* tmp = iter->second.get();
        array->InsertLast(&tmp);
    }

    return array;
}

CScriptArray*
    patchGetNeighboursWrapper(const Patch& self)
{
    const auto& patches = self.getNeighbours();
    return Leviathan::ConvertIteratorToASArray(patches.begin(), patches.end(),
        Leviathan::ScriptExecutor::Get()->GetASEngine());
}
// ------------------------------------ //
bool
    thrive::registerPatches(asIScriptEngine* engine)
{
    // ------------------------------------ //
    // Patch
    ANGELSCRIPT_REGISTER_REF_TYPE("Patch", Patch);

    if(engine->RegisterObjectBehaviour("Patch", asBEHAVE_FACTORY,
           "Patch@ f(const string &in name, int32 id, const Biome &in "
           "biomeTemplate)",
           asFUNCTION(Patch::factory), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch", "const string& getName() const",
           asMETHOD(Patch, getName), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch", "int32 getId() const",
           asMETHOD(Patch, getId), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch", "bool addNeighbour(int32 id)",
           asMETHOD(Patch, addNeighbour), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch",
           "Float2 getScreenCoordinates() const",
           asMETHOD(Patch, getScreenCoordinates), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch",
           "void setScreenCoordinates(Float2 coordinates)",
           asMETHOD(Patch, setScreenCoordinates), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // TODO: Would be much safer to have reference counting for biomes
    if(engine->RegisterObjectMethod("Patch", "const Biome@ getBiome() const",
           asMETHODPR(Patch, getBiome, () const, const Biome&),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch", "Biome@ getBiome()",
           asMETHODPR(Patch, getBiome, (), Biome&), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch",
           "const Biome@ getBiomeTemplate() const",
           asMETHODPR(Patch, getBiomeTemplate, () const, const Biome&),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch",
           ("bool addSpecies(Species@ species, int32 population = " +
               std::to_string(INITIAL_SPECIES_POPULATION) + ")")
               .c_str(),
           asMETHOD(Patch, addSpeciesWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch", "uint64 getSpeciesCount() const",
           asMETHOD(Patch, getSpeciesCount), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch",
           "Species@ getSpecies(uint64 index) const",
           asMETHOD(Patch, getSpeciesWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch",
           "int32 getSpeciesPopulation(const Species@ species) const",
           asMETHOD(Patch, getSpeciesPopulationWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch",
           "array<int32>@ getNeighbours() const",
           asFUNCTION(patchGetNeighboursWrapper), asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // Planet
    ANGELSCRIPT_REGISTER_REF_TYPE("Planet", Planet);

    if(engine->RegisterObjectProperty("Planet", "double atmosphereMass",
           asOFFSET(Planet, atmosphereMass)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Planet",
           "double atmosphereCarbonDioxide",
           asOFFSET(Planet, atmosphereCarbonDioxide)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Planet", "double atmosphereOxygen",
           asOFFSET(Planet, atmosphereOxygen)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Planet", "double atmosphereNitrogen",
           asOFFSET(Planet, atmosphereNitrogen)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // PatchMap
    ANGELSCRIPT_REGISTER_REF_TYPE("PatchMap", PatchMap);

    if(engine->RegisterObjectBehaviour("PatchMap", asBEHAVE_FACTORY,
           "PatchMap@ f()", asFUNCTION(PatchMap::factory), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchMap", "Patch@ getCurrentPatch()",
           asMETHOD(PatchMap, getCurrentPatchWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchMap",
           "int32 getCurrentPatchId() const",
           asMETHOD(PatchMap, getCurrentPatchId), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchMap", "Patch@ getPatch(int32 id)",
           asMETHOD(PatchMap, getPatchWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchMap", "bool addPatch(Patch@ patch)",
           asMETHOD(PatchMap, addPatchWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchMap",
           "Species@ findSpeciesByName(const string &in name)",
           asMETHOD(PatchMap, findSpeciesByNameWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchMap", "Planet@ getPlanet()",
           asMETHOD(PatchMap, getPlanetWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    mapWrapperTypeInfo =
        Leviathan::ScriptExecutor::Get()->GetASEngine()->GetTypeInfoByDecl(
            "array<const Patch@>");

    if(!mapWrapperTypeInfo) {
        LOG_ERROR("could not get type info for map wrapper");
        return false;
    }

    if(engine->RegisterObjectMethod("PatchMap",
           "array<const Patch@>@ getPatches() const",
           asFUNCTION(patchMapGetPatchesWrapper), asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchMap", "array<Patch@>@ getPatches()",
           asFUNCTION(patchMapGetPatchesWrapper), asCALL_CDECL_OBJFIRST) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // PatchManager
    if(engine->RegisterObjectType(
           "PatchManager", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchManager", "PatchMap@ getCurrentMap()",
           asMETHOD(PatchManager, getCurrentMapWrapper), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}
