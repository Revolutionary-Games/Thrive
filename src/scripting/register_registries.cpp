// ------------------------------------ //
#include "script_initializer.h"

#include "microbe_stage/simulation_parameters.h"
#include "microbe_stage/species_name_controller.h"

#include <Script/Bindings/BindHelpers.h>
// #include <Script/ScriptExecutor.h>

using namespace thrive;
// ------------------------------------ //

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

//! Wrapper for TJsonRegistry::getTypeData
template<class RegistryT, class ReturnedT>
const ReturnedT*
    getTypeDataWithInternalNameWrapper(RegistryT* self, const std::string& internalName)
{
    return &self->getTypeData(internalName);
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

TJsonRegistry<OrganelleType>*
    getOrganelleRegistryWrapper()
{
    return &SimulationParameters::organelleRegistry;
}

TJsonRegistry<MembraneType>*
    getMembraneRegistryWrapper()
{
    return &SimulationParameters::membraneRegistry;
}
// ------------------------------------ //
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

    if(engine->RegisterObjectMethod(classname,
           ("const " + returnedTypeName + "@ getTypeData(const string &in internalName)").c_str(),
           asFUNCTION((getTypeDataWithInternalNameWrapper<RegistryT, ReturnedT>)),
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

    if(!registerRegistryHeldHelperBases<BioProcess>(engine, "BioProcess"))
        return false;

    if(!registerRegistryHeldHelperBases<Biome>(engine, "Biome"))
        return false;

    if(!registerRegistryHeldHelperBases<OrganelleType>(engine, "OrganelleType"))
        return false;

    if(!registerRegistryHeldHelperBases<MembraneType>(engine, "MembraneType"))
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

    if(engine->RegisterObjectProperty("Compound", "bool isEnvironmental",
           asOFFSET(Compound, isEnvironmental)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Compound", "Float4 colour", asOFFSET(Compound, colour)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // Biome
    // TODO: bind new light properties
    if(engine->RegisterObjectProperty("Biome", "const string background",
           asOFFSET(Biome, background)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // TODO: reference counting for these
    if(engine->RegisterObjectType(
           "BiomeCompoundData", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Biome",
           "const BiomeCompoundData@ getCompound(uint64 type) const",
           asMETHODPR(
               Biome, getCompound, (size_t) const, const BiomeCompoundData*),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Biome",
           "BiomeCompoundData@ getCompound(uint64 type)",
           asMETHODPR(Biome, getCompound, (size_t), BiomeCompoundData*),
           asCALL_THISCALL) < 0) {
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

    if(engine->RegisterObjectProperty("BiomeCompoundData", "double dissolved",
           asOFFSET(BiomeCompoundData, dissolved)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectType("ChunkData", 0, asOBJ_REF | asOBJ_NOCOUNT) <
        0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Biome",
           "const ChunkData& getChunk(uint64 type) const",
           asMETHOD(Biome, getChunk), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Biome",
           "array<uint64>@ getChunkKeys() const", asMETHOD(Biome, getChunkKeys),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "ChunkData", "string name", asOFFSET(ChunkData, name)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "ChunkData", "double density", asOFFSET(ChunkData, density)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "ChunkData", "bool dissolves", asOFFSET(ChunkData, dissolves)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "ChunkData", "uint radius", asOFFSET(ChunkData, radius)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "ChunkData", "uint mass", asOFFSET(ChunkData, mass)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "ChunkData", "uint size", asOFFSET(ChunkData, size)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("ChunkData", "double ventAmount",
           asOFFSET(ChunkData, ventAmount)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "ChunkData", "double damages", asOFFSET(ChunkData, damages)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("ChunkData", "bool deleteOnTouch",
           asOFFSET(ChunkData, deleteOnTouch)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("ChunkData", "double chunkScale",
           asOFFSET(ChunkData, chunkScale)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectType(
           "ChunkCompoundData", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("ChunkData",
           "const ChunkCompoundData& getCompound(uint64 type) const",
           asMETHODPR(ChunkData, getCompound, (size_t) const,
               const ChunkCompoundData*),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("ChunkData",
           "array<uint64>@ getCompoundKeys() const",
           asMETHOD(ChunkData, getCompoundKeys), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("ChunkData",
           "const uint64 getMeshListSize() const",
           asMETHOD(ChunkData, getMeshListSize), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("ChunkData",
           "const string getMesh(uint64 index) const",
           asMETHOD(ChunkData, getMesh), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("ChunkData",
           "const string getTexture(uint64 index) const",
           asMETHOD(ChunkData, getTexture), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("ChunkCompoundData", "double amount",
           asOFFSET(ChunkCompoundData, amount)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("ChunkCompoundData", "string name",
           asOFFSET(ChunkCompoundData, name)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // OrganelleType
    if(engine->RegisterObjectProperty("OrganelleType", "const string name",
           asOFFSET(OrganelleType, name)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleType", "const string gene",
           asOFFSET(OrganelleType, gene)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleType", "const string mesh",
           asOFFSET(OrganelleType, mesh)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleType", "const string texture",
           asOFFSET(OrganelleType, texture)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleType", "const float mass",
           asOFFSET(OrganelleType, mass)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleType",
           "const float chanceToCreate",
           asOFFSET(OrganelleType, chanceToCreate)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleType",
           "const float prokaryoteChance",
           asOFFSET(OrganelleType, prokaryoteChance)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const array<string>@ getComponentKeys() const",
           asMETHOD(OrganelleType, getComponentKeys), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const array<string>@ getComponentParameterKeys(string component) "
           "const",
           asMETHOD(OrganelleType, getComponentParameterKeys),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const double getComponentParameterAsDouble(string component, "
           "string parameter) const",
           asMETHOD(OrganelleType, getComponentParameterAsDouble),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const string getComponentParameterAsString(string component, "
           "string parameter) const",
           asMETHOD(OrganelleType, getComponentParameterAsString),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const array<string>@ getProcessKeys() const",
           asMETHOD(OrganelleType, getProcessKeys), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const float getProcessTweakRate(string process) const",
           asMETHOD(OrganelleType, getProcessTweakRate), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const array<Int2>@ getHexes() const",
           asMETHOD(OrganelleType, getHexes), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const array<string>@ getInitialCompositionKeys() const",
           asMETHOD(OrganelleType, getInitialCompositionKeys),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("OrganelleType",
           "const double getInitialComposition(string compound) const",
           asMETHOD(OrganelleType, getInitialComposition),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleType", "const int mpCost",
           asOFFSET(OrganelleType, mpCost)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // MembraneType
    if(engine->RegisterObjectProperty(
           "MembraneType", "float movementFactor", asOFFSET(MembraneType, movementFactor)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "MembraneType", "float osmoregulationFactor", asOFFSET(MembraneType, osmoregulationFactor)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "MembraneType", "float resourceAbsorptionFactor", asOFFSET(MembraneType, resourceAbsorptionFactor)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "MembraneType", "float hitpoints", asOFFSET(MembraneType, hitpoints)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "MembraneType", "float physicalResistance", asOFFSET(MembraneType, physicalResistance)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "MembraneType", "float toxinResistance", asOFFSET(MembraneType, toxinResistance)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "MembraneType", "int editorCost", asOFFSET(MembraneType, editorCost)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}

bool
    thrive::registerSimulationDataAndJsons(asIScriptEngine* engine)
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
           asMETHOD(SpeciesNameController, getVowelPrefixes),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getConsonantPrefixes()",
           asMETHOD(SpeciesNameController, getConsonantPrefixes),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getVowelCofixes()",
           asMETHOD(SpeciesNameController, getVowelCofixes),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getConsonantCofixes()",
           asMETHOD(SpeciesNameController, getConsonantCofixes),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getSuffixes()",
           asMETHOD(SpeciesNameController, getSuffixes), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getConsonantSuffixes()",
           asMETHOD(SpeciesNameController, getConsonantSuffixes),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("SpeciesNameController",
           "array<string>@ getVowelSuffixes()",
           asMETHOD(SpeciesNameController, getVowelSuffixes),
           asCALL_THISCALL) < 0) {
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

    if(!registerJsonRegistry<TJsonRegistry<OrganelleType>, OrganelleType>(
           engine, "TJsonRegistryOrganelleType", "OrganelleType")) {
        return false;
    }

    if(!registerJsonRegistry<TJsonRegistry<MembraneType>, MembraneType>(
           engine, "TJsonRegistryMembraneType", "MembraneType")) {
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

    if(engine->RegisterGlobalFunction(
           "TJsonRegistryOrganelleType@ organelleRegistry()",
           asFUNCTION(getOrganelleRegistryWrapper), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction(
           "TJsonRegistryMembraneType@ membraneRegistry()",
           asFUNCTION(getMembraneRegistryWrapper), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->SetDefaultNamespace("") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}
// ------------------------------------ //
bool
    thrive::registerTweakedProcess(asIScriptEngine* engine)
{
    ANGELSCRIPT_REGISTER_REF_TYPE("TweakedProcess", TweakedProcess);

    if(engine->RegisterObjectMethod("TweakedProcess",
           "float get_tweakRate() const",
           asMETHOD(TweakedProcess, getTweakRate), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("TweakedProcess",
           "const BioProcess process", asOFFSET(TweakedProcess, process)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    return true;
}
