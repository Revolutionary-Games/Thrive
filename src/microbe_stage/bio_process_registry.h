#pragma once

#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include <boost/range/adaptor/map.hpp>
#include <vector>
#include <unordered_map>

namespace luabind {
class scope;
}


namespace thrive {

using BoostCompoundMapIterator = boost::range_detail::select_second_mutable_range<std::unordered_map<std::string, CompoundId>>;
using BoostAbsorbedMapIterator = boost::range_detail::select_first_range<std::unordered_map<CompoundId, float>>;

/**
* @brief Static abstract class keeping track of a predefined biological processes, their Id's, internal and displayed names as well as various data
*
* Preferably this would hold process objects in the future, but currently it just holds data for certain lua considerations
*/
class BioProcessRegistry {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - BioProcessRegistry::registerBioProcess
    * - BioProcessRegistry::getDisplayName
    * - BioProcessRegistry::getInternalName
    * - BioProcessRegistry::getSpeedFactor
    * - BioProcessRegistry::getId
    * - BioProcessRegistry::getList
    * - BioProcessRegistry::getInputCompounds
    * - BioProcessRegistry::getOutputCompounds
    * @return
    */
    static luabind::scope
    luaBindings();

    static void
    loadFromXML(
        const std::string& filename
    );

    static void
    loadFromLua(
        const luabind::object& processTable
    );

    /**
    * @brief Registers a new process type
    *
    * @param internalName
    *   The name to be used internally for reference across game instances
    *
    * @param displayName
    *   Name to be displayed to users
	*
    * @param energyCost
    *  The amount of energy needed for a full cycle of this process
    *
    * @param speedFactor
    *  Speed of this process
    *
    * @param inputCompounds
    *  The compounds and amounts needed for this process
    *
    * @param outputCompounds
    *  The compounds and amounts produced by this process
    *
    * @return
    *   Id of new process
    */
    static BioProcessId
    registerBioProcess(
        const std::string& internalName,
        const std::string& displayName,
        double speedFactor,
        std::vector<std::pair<CompoundId, int>> inputCompounds,
        std::vector<std::pair<CompoundId, int>> outputCompounds
    );

    /**
    * @brief Obtains the display name of an compound
    *
    * @param id
    *   Id of the concept to obtain display name from
    *
    * @return
    *   Name to display to users
    */
    static std::string
    getDisplayName(
         BioProcessId id
    );

    /**
    * @brief Obtains the internal name of an compound
    *
    * @param id
    *   Id of the compound to obtain internal name from
    *
    * @return
    *   Compound name for internal use
    */
    static std::string
    getInternalName(
        BioProcessId id
    );

    static double
    getSpeedFactor(
        BioProcessId id
    );

    /**
    * @brief Obtains the Id of an internal name corresponding to a registered process
    *
    * @param internalName
    *   The internal name of the process. Must not already exist in collection or invalid_argument is thrown.
    *
    * @return
    *   Id of the process if it is registered. If compound is not registered an out_of_range exception is thrown.
    */
    static BioProcessId
    getId(
        const std::string& internalName
    );


    /**
    * @brief Obtains the IDs of all currently registered concepts
    *
    * @return
    *   Array of all registered process IDs
    */
    static const BoostCompoundMapIterator
    getList();

    static const std::vector<std::pair<CompoundId, int>>&
    getInputCompounds(
        BioProcessId id
    );

    static const std::vector<std::pair<CompoundId, int>>&
    getOutputCompounds(
        BioProcessId id
    );

    BioProcessRegistry() = delete;

};

}

