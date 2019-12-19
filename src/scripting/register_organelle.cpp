// ------------------------------------ //
#include "script_initializer.h"

#include "microbe_stage/organelle_table.h"

#include <Script/Bindings/BindHelpers.h>
#include <Script/ScriptExecutor.h>

using namespace thrive;
// ------------------------------------ //
bool
    thrive::registerOrganelles(asIScriptEngine* engine)
{
    // Empty interface for passing organelle components through C++
    if(engine->RegisterInterface("OrganelleComponentType") < 0) {

        ANGELSCRIPT_REGISTERFAIL;
    }

    ANGELSCRIPT_REGISTER_REF_TYPE("OrganelleTemplate", OrganelleTemplate);

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "bool containsHex(int q, int r) const",
           asMETHOD(OrganelleTemplate, containsHex), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "const array<Int2>@ getRotatedHexes(int rotation) const",
           asMETHOD(OrganelleTemplate, getRotatedHexesWrapper),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "Float3 calculateCenterOffset() const",
           asMETHOD(OrganelleTemplate, calculateCenterOffset),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "Float3 calculateModelOffset() const",
           asMETHOD(OrganelleTemplate, calculateModelOffset),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "Int2 getHex(uint64 index) const",
           asMETHOD(OrganelleTemplate, getHex), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "uint64 getHexCount() const",
           asMETHOD(OrganelleTemplate, getHexCount), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "uint64 getComponentCount() const",
           asMETHOD(OrganelleTemplate, getComponentCount),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "OrganelleComponentType@ createComponent(uint64 index) const",
           asMETHOD(OrganelleTemplate, createComponent), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "TweakedProcess@ getProcess(uint64 index) const",
           asMETHOD(OrganelleTemplate, getProcessWrapper),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "uint64 getProcessCount() const",
           asMETHOD(OrganelleTemplate, getProcessCount), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "const dictionary@ getInitialComposition() const",
           asMETHOD(OrganelleTemplate, getInitialCompositionDictionary),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "const dictionary@ get_initialComposition() const",
           asMETHOD(OrganelleTemplate, getInitialCompositionDictionary),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "float get_chanceToCreate() const",
           asMETHOD(OrganelleTemplate, getChanceToCreate),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "float get_prokaryoteChance() const",
           asMETHOD(OrganelleTemplate, getProkaryoteChance),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "int get_mpCost() const", asMETHOD(OrganelleTemplate, getMPCost),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "float get_organelleCost() const",
           asMETHOD(OrganelleTemplate, getOrganelleCost),
           asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // properties
    if(engine->RegisterObjectMethod("OrganelleTemplate",
           "bool hasComponent(const string &in name) const",
           asMETHOD(OrganelleTemplate, hasComponent), asCALL_THISCALL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleTemplate", "const string name",
           asOFFSET(OrganelleTemplate, m_name)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleTemplate", "const string gene",
           asOFFSET(OrganelleTemplate, m_gene)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleTemplate", "const float mass",
           asOFFSET(OrganelleTemplate, m_mass)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleTemplate", "const string mesh",
           asOFFSET(OrganelleTemplate, m_mesh)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterObjectProperty("OrganelleTemplate",
           "const string texture",
           asOFFSET(OrganelleTemplate, m_texture)) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    // ------------------------------------ //
    // global access
    if(engine->RegisterGlobalFunction(
           "OrganelleTemplate@ getOrganelleDefinition(const string &in name)",
           asFUNCTION(OrganelleTable::getOrganelleDefinition),
           asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }

    if(engine->RegisterGlobalFunction("array<string>@ getOrganelleNames()",
           asFUNCTION(OrganelleTable::getOrganelleNames), asCALL_CDECL) < 0) {
        ANGELSCRIPT_REGISTERFAIL;
    }



    return true;
}
