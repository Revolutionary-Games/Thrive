#pragma once

class asIScriptEngine;

namespace thrive {


//! \brief Registers Thrive script types
//! \todo Splitting this into multiple files would be optimal
bool
    registerThriveScriptTypes(asIScriptEngine* engine);

// internal register functions
bool
    registerOrganelles(asIScriptEngine* engine);

bool
    bindScriptAccessibleSystems(asIScriptEngine* engine);

bool
    bindWorlds(asIScriptEngine* engine);

bool
    bindThriveComponentTypes(asIScriptEngine* engine);

bool
    registerPatches(asIScriptEngine* engine);

bool
    registerTimedWorldOperations(asIScriptEngine* engine);

bool
    registerTweakedProcess(asIScriptEngine* engine);

bool
    registerSimulationDataAndJsons(asIScriptEngine* engine);

} // namespace thrive
