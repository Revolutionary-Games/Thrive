#pragma once

#include <Script/ScriptConversionHelpers.h>
#include <add_on/scriptarray/scriptarray.h>
#include <string>
#include <vector>

namespace thrive {

class SpeciesNameController {
public:
    std::vector<std::string> prefixes;
    std::vector<std::string> cofixes;
    std::vector<std::string> suffixes;

    CScriptArray*
        getPrefixes();

    CScriptArray*
        getCofixes();

    CScriptArray*
        getSuffixes();

    SpeciesNameController();

    SpeciesNameController(std::string jsonFilePath);
};

} // namespace thrive