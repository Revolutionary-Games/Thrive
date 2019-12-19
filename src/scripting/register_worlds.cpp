// ------------------------------------ //
#include "script_initializer.h"

#include "generated/cell_stage_world.h"
#include "generated/microbe_editor_world.h"

#include <Script/Bindings/StandardWorldBindHelper.h>

using namespace thrive;
// ------------------------------------ //
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
// ------------------------------------ //
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
    thrive::bindThriveComponentTypes(asIScriptEngine* engine)
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
// ------------------------------------ //
bool
    thrive::bindWorlds(asIScriptEngine* engine)
{
    if(!bindCellStageMethods<CellStageWorld>(engine, "CellStageWorld"))
        return false;

    if(!bindMicrobeEditorMethods<MicrobeEditorWorld>(
           engine, "MicrobeEditorWorld"))
        return false;

    return true;
}
