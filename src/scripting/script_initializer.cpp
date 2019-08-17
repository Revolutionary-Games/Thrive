#include "script_initializer.h"

using namespace thrive;
// ------------------------------------ //

#include "engine/player_data.h"
#include "general/hex.h"
#include "general/locked_map.h"
#include "general/properties_component.h"
#include "general/timed_life_system.h"
#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"
#include "microbe_stage/patch.h"
#include "microbe_stage/player_microbe_control.h"
#include "microbe_stage/simulation_parameters.h"
#include "microbe_stage/species_name_controller.h"

#include "ThriveGame.h"


#include "Script/Bindings/BindHelpers.h"
#include "Script/Bindings/StandardWorldBindHelper.h"
#include "Script/ScriptExecutor.h"


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
    // define colors for sunglight here aswell
    if(engine->RegisterObjectProperty("Biome", "Float4 specularColors",
           asOFFSET(Biome, specularColors)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectProperty("Biome", "Float4 diffuseColors",
           asOFFSET(Biome, diffuseColors)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("Biome", "Float4 upperAmbientColor",
           asOFFSET(Biome, upperAmbientColor)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectProperty("Biome", "Float4 lowerAmbientColor",
           asOFFSET(Biome, lowerAmbientColor)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty(
           "Biome", "float lightPower", asOFFSET(Biome, lightPower)) < 0) {
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
           asMETHOD(ChunkData, getCompound), asCALL_THISCALL) < 0) {
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
static uint16_t CompoundVenterTYPEProxy =
    static_cast<uint16_t>(CompoundVenterComponent::TYPE);
static uint16_t EngulfableComponentTYPEProxy =
    static_cast<uint16_t>(EngulfableComponent::TYPE);
static uint16_t DamageOnTouchComponentTYPEProxy =
    static_cast<uint16_t>(DamageOnTouchComponent::TYPE);
static uint16_t SpawnedComponentTYPEProxy =
    static_cast<uint16_t>(SpawnedComponent::TYPE);
static uint16_t AgentCloudComponentTYPEProxy =
    static_cast<uint16_t>(AgentCloudComponent::TYPE);
static uint16_t CompoundCloudComponentTYPEProxy =
    static_cast<uint16_t>(CompoundCloudComponent::TYPE);
static uint16_t MembraneComponentTYPEProxy =
    static_cast<uint16_t>(MembraneComponent::TYPE);
static uint16_t FluidEffectComponentTYPEProxy =
    static_cast<uint16_t>(FluidEffectComponent::TYPE);
static uint16_t SpeciesComponentTYPEProxy =
    static_cast<uint16_t>(SpeciesComponent::TYPE);
static uint16_t CompoundBagComponentTYPEProxy =
    static_cast<uint16_t>(CompoundBagComponent::TYPE);
static uint16_t CompoundAbsorberComponentTYPEProxy =
    static_cast<uint16_t>(CompoundAbsorberComponent::TYPE);
static uint16_t TimedLifeComponentTYPEProxy =
    static_cast<uint16_t>(TimedLifeComponent::TYPE);
static uint16_t AgentPropertiesTYPEProxy =
    static_cast<uint16_t>(AgentProperties::TYPE);

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
           "ProcessorComponent& opAssign(const ProcessorComponent &in other)",
           asMETHODPR(ProcessorComponent, operator=,(const ProcessorComponent&),
               ProcessorComponent&),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ProcessorComponent",
           "void setCapacity(BioProcessId id, double capacity)",
           asMETHOD(ProcessorComponent, setCapacity), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("ProcessorComponent",
           "double getCapacity(BioProcessId id)",
           asMETHOD(ProcessorComponent, getCapacity), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "CompoundVenterComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "CompoundVenterComponent", &CompoundVenterTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("CompoundVenterComponent",
           "float getVentAmount()",
           asMETHOD(CompoundVenterComponent, getVentAmount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundVenterComponent",
           "void setVentAmount(float amount)",
           asMETHOD(CompoundVenterComponent, setVentAmount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundVenterComponent",
           "bool getDoDissolve()",
           asMETHOD(CompoundVenterComponent, getDoDissolve),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundVenterComponent",
           "void setDoDissolve(bool dissolve)",
           asMETHOD(CompoundVenterComponent, setDoDissolve),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "EngulfableComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "EngulfableComponent", &EngulfableComponentTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("EngulfableComponent", "float getSize()",
           asMETHOD(EngulfableComponent, getSize), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("EngulfableComponent",
           "void setSize(float size)", asMETHOD(EngulfableComponent, setSize),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    if(engine->RegisterObjectType(
           "DamageOnTouchComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "DamageOnTouchComponent", &DamageOnTouchComponentTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("DamageOnTouchComponent",
           "double getDamage()", asMETHOD(DamageOnTouchComponent, getDamage),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("DamageOnTouchComponent",
           "void setDamage(double damage)",
           asMETHOD(DamageOnTouchComponent, setDamage), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
    if(engine->RegisterObjectMethod("DamageOnTouchComponent",
           "bool getDeletes()", asMETHOD(DamageOnTouchComponent, getDeletes),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("DamageOnTouchComponent",
           "void setDeletes(bool deletes)",
           asMETHOD(DamageOnTouchComponent, setDeletes), asCALL_THISCALL) < 0) {
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
           "FluidEffectComponent", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "FluidEffectComponent", &FluidEffectComponentTYPEProxy))
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

    if(engine->RegisterObjectMethod("MembraneComponent",
           "void setHealthFraction(float value)",
           asMETHOD(MembraneComponent, setHealthFraction),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }


    if(engine->RegisterEnum("MEMBRANE_TYPE") < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_REGISTER_ENUM_VALUE(MEMBRANE_TYPE, MEMBRANE);
    ANGELSCRIPT_REGISTER_ENUM_VALUE(MEMBRANE_TYPE, DOUBLEMEMBRANE);
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
           "float calculateEncompassingCircleRadius() const",
           asMETHOD(MembraneComponent, calculateEncompassingCircleRadius),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "Float3 GetExternalOrganelle(double x, double y)",
           asMETHOD(MembraneComponent, GetExternalOrganelle),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "void sendOrganelles(double x, double y)",
           asMETHOD(MembraneComponent, sendOrganelles), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("MembraneComponent",
           "bool removeSentOrganelle(double x, double y)",
           asMETHOD(MembraneComponent, removeSentOrganelle),
           asCALL_THISCALL) < 0) {
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

    if(engine->RegisterObjectProperty("SpeciesComponent", "string stringCode",
           asOFFSET(SpeciesComponent, stringCode)) < 0) {
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

    if(engine->RegisterObjectProperty("SpeciesComponent", "double activity",
           asOFFSET(SpeciesComponent, activity)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "double focus",
           asOFFSET(SpeciesComponent, focus)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "double opportunism",
           asOFFSET(SpeciesComponent, opportunism)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "int32 population",
           asOFFSET(SpeciesComponent, population)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("SpeciesComponent", "int32 generation",
           asOFFSET(SpeciesComponent, generation)) < 0) {
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
           "void setCompound(CompoundId compound, double amount)",
           asMETHOD(CompoundBagComponent, setCompound), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "double getPrice(CompoundId compound)",
           asMETHOD(CompoundBagComponent, getPrice), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("CompoundBagComponent",
           "double getUsedLastTime(CompoundId compound)",
           asMETHOD(CompoundBagComponent, getUsedLastTime),
           asCALL_THISCALL) < 0) {
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
           "void setGrabScale(float scale)",
           asMETHOD(CompoundAbsorberComponent, setGrabScale),
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

    if(engine->RegisterObjectType(
           "AgentProperties", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(!bindComponentTypeId(
           engine, "AgentProperties", &AgentPropertiesTYPEProxy))
        return false;

    if(engine->RegisterObjectMethod("AgentProperties",
           "void setSpeciesName(string newString)",
           asMETHOD(AgentProperties, setSpeciesName), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("AgentProperties",
           "void setParentEntity(ObjectID parentId)",
           asMETHOD(AgentProperties, setParentEntity), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("AgentProperties",
           "void setAgentType(string newString)",
           asMETHOD(AgentProperties, setAgentType), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("AgentProperties",
           "string getSpeciesName()", asMETHOD(AgentProperties, getSpeciesName),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("AgentProperties",
           "ObjectID getParentEntity()",
           asMETHOD(AgentProperties, getParentEntity), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("AgentProperties", "string getAgentType()",
           asMETHOD(AgentProperties, getAgentType), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }
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

    // LOG_ERROR("findSpeciesEntityByName: no species with name: " + name);
    return NULL_OBJECT;
}

bool
    registerPatches(asIScriptEngine* engine)
{
    if(engine->RegisterObjectType("Patch", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch", "string getName()",
           asMETHOD(Patch, getName), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("Patch", "void setName(string name)",
           asMETHOD(Patch, setName), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Patch", "uint64 getBiome()",
           asMETHOD(Patch, getBiome), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Patch", "void setBiome(uint64 patchBiome)",
           asMETHOD(Patch, setBiome), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("Patch", "uint64 getId()",
           asMETHOD(Patch, getId), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectType(
           "PatchManager", 0, asOBJ_REF | asOBJ_NOCOUNT) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("PatchManager", "Patch@ getCurrentPatch()",
           asMETHOD(PatchManager, getCurrentPatch), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("PatchManager",
           "Patch@ getPatchFromKey(uint64 key)",
           asMETHOD(PatchManager, getPatchFromKey), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_ASSUMED_SIZE_T;
    if(engine->RegisterObjectMethod("PatchManager", "uint64 generatePatchMap()",
           asMETHOD(PatchManager, generatePatchMap), asCALL_THISCALL) < 0) {
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

    if(!bindScriptAccessibleSystems(engine))
        return false;

    if(!registerPlayerData(engine))
        return false;

    if(!registerSimulationDataAndJsons(engine))
        return false;

    if(!registerHexFunctions(engine))
        return false;

    if(!registerPatches(engine))
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

    if(engine->RegisterObjectMethod("ThriveGame",
           "PatchManager@ getPatchManager()",
           asMETHOD(ThriveGame, getPatchManager), asCALL_THISCALL) < 0) {
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
