#pragma once

#include "organelle_template.h"

#include <unordered_map>

class CScriptArray;

namespace thrive {

class OrganelleTemplate;

//! \brief Handles creating OrganelleTemplate objects from the
//! SimulationParameters json data
//!
//! Requires microbe scripts to be loaded
class OrganelleTable {
public:
    //! \returns True if initialization was performed. False if already
    //! initialized
    static bool
        initIfNeeded();

    static void
        release();

    //! \brief Finds an OrganelleTemplate by name
    //! \note This increments the refcount
    static OrganelleTemplate*
        getOrganelleDefinition(const std::string& name);

    static uint64_t
        getOrganelleDefinitionCount();

    static CScriptArray*
        getOrganelleNames();

private:
    static std::unordered_map<std::string, OrganelleTemplate::pointer>
        mainOrganelleTable;

    static bool initialized;
};

}; // namespace thrive
