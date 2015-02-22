#pragma once

#include "scripting/luabind.h"
#include "luabind/object.hpp"

namespace luabind {
class scope;
}

namespace thrive {


/**
* @brief Static class which loads microbe species and serves their data to scripts
*/
class SpeciesRegistry {

public:

    SpeciesRegistry() = delete;

    /**
    * @brief Lua bindings
    * 
    * Exposes:
    * - SpeciesRegistry::loadFromXML
    * - SpeciesRegistry::getSpeciesNames
    * - SpeciesRegistry::getSize
    * - SpeciesRegistry::getOrganelle
    * - SpeciesRegistry::getCompoundPriority
    * - SpeciesRegistry::getCompoundAmount
    * @return
    */
    static luabind::scope
    luaBindings();

    static void
    loadFromXML(
        const std::string& filename
        );


    /**
    * @brief Gets the size of the named microbe
    */
    static int
    getSize(
        const std::string& microbe_name
        );

    /**
    * @brief creates a lua table containing all the XML attributes defined for a particular organelle
    */
    static luabind::object
    getOrganelle(
        const std::string& microbe_name,
        int index
        );

    static luabind::object
    getSpeciesNames();

    static double
    getCompoundPriority(
        const std::string& microbe_name,
        const std::string& compound_name
        );

    static double
    getCompoundAmount(
        const std::string& microbe_name,
        const std::string& compound_name
        );
};

}
