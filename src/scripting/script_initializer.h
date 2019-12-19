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

} // namespace thrive
