#pragma once

#include <boost/range/adaptor/map.hpp>

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "scripting/luabind.h"
#include "engine/typedefs.h"

#include <luabind/object.hpp>
#include <memory>
#include <OgreCommon.h>
#include <OgreMath.h>
#include <OgreVector3.h>
#include <unordered_set>

namespace thrive {

static const CompoundId NULL_COMPOUND = 0;

using BoostCompoundMapIterator = boost::range_detail::select_second_mutable_range<std::unordered_map<std::string, CompoundId>>;
using BoostAbsorbedMapIterator = boost::range_detail::select_first_range<std::unordered_map<CompoundId, float>>;

/**
* @brief Static class keeping track of compounds, their Id's, internal and displayed names
*/
class CompoundRegistry final {

public:

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CompoundRegistry::loadFromXML
    * - CompoundRegistry::registerCompoundType
    * - CompoundRegistry::registerAgentType
    * - CompoundRegistry::getCompoundDisplayName
    * - CompoundRegistry::getCompoundInternalName
    * - CompoundRegistry::getCompoundUnitVolume
    * - CompoundRegistry::getCompoundId
    * - CompoundRegistry::getCompoundList
    * - CompoundRegistry::getCompoundMeshName
    * - CompoundRegistry::getCompoundMeshScale
    * @return
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Loads compounds from an XML document
    *
    * @param filename
    *  The document to load from
    */
    static void
    loadFromXML(
        const std::string& filename
    );

    /**
    * @brief loads compounds from a lua config table
    */
    static void
    loadFromLua(
        luabind::object configTable
    );

    /**
    * @brief Registers a new compound type
    *
    * @param internalName
    *   The name to be used internally for reference across game instances
    *
    * @param displayName
    *   Name to be displayed to users
    *
    * @param meshName
    *   Name of the model to use for this compound
    *
    * @param meshScale
    *   The relative size of the mesh
    *
	* @param unitVolume
    *   Size of the compound when stored and transported
	*
    * @return
    *   Id of new compound
    */
    static CompoundId
    registerCompoundType(
        const std::string& internalName,
        const std::string& displayName,
        const std::string& meshName,
        double meshScale,
		int unitVolume
    );

    /**
    * @brief Registers a new agent type
    *
    * @param internalName
    *   The name to be used internally for reference across game instances
    *
    * @param displayName
    *   Name to be displayed to users
    *
    * @param meshName
    *   Name of the model to use for this compound
    *
    * @param meshScale
    *   The relative size of the mesh
    *
	* @param unitVolume
    *   Size of the compound when stored and transported
	*
	* @param effect
	*   A function pointer for the action to be performed on the absorbing entity.
	*   This function should take the id of the recieving entity as parameter and return true.
	*
    * @return
    *   Id of new agent
    */
    static CompoundId
    registerAgentType(
        const std::string& internalName,
        const std::string& displayName,
        const std::string& meshName,
        double meshScale,
        int unitVolume,
        std::function<bool(EntityId, double)>* effect
    );

    /**
    * @brief Registers a new agent type
    *
    * @param internalName
    *   The name to be used internally for reference across game instances
    *
    * @param displayName
    *   Name to be displayed to users
    *
    * @param meshName
    *   Name of the model to use for this agent
    *
    * @param meshScale
    *   The relative size of the mesh
    *
	* @param unitVolume
    *   Size of the compound when stored and transported
	*
	* @param effect
	*   A lua function for the action to be performed on the absorbing entity.
	*   This function should take the id of the recieving entity as parameter and return true.
	*
    * @return
    *   Id of new agent
    */
    static CompoundId
    registerAgentType(
        const std::string& internalName,
        const std::string& displayName,
        const std::string& meshName,
        double meshScale,
        int unitVolume,
        const luabind::object& effect
    );

    /**
    * @brief Obtains the display name of an compound
    *
    * @param id
    *   Id of the compound to obtain display name from
    *
    * @return
    *   Compound name to display to users
    */
    static std::string
    getCompoundDisplayName(
        CompoundId id
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
    getCompoundInternalName(
        CompoundId id
    );

    /**
    * @brief Obtains the size of a compound
    *
    * @param id
    *   Id of the compound to obtain size from
    *
    * @return
    *   Compound size for internal use
    */
    static int
    getCompoundUnitVolume(
        CompoundId id
    );

    /**
    * @brief Obtains the Id of an internal name corresponding to a registered compound
    *
    * @param internalName
    *   The internal name of the compound. Must not already exist in collection or invalid_argument is thrown.
    *
    * @return
    *   CompoundId of the compound if it is registered. If compound is not registered an out_of_range exception is thrown.
    */
    static CompoundId
    getCompoundId(
        const std::string& internalName
    );

    /**
    * @brief Obtains the IDs of all currently registered compounds
    *
    * @return
    *   Array of all registered compound IDs
    */
    static const BoostCompoundMapIterator
    getCompoundList(
    );

	/**
    * @brief Obtains the name of the corresponding mesh
    *
    * @param compoundId
    *   The id of the compound to acquire the mesh name from
    *
    * @return
    *   A string containing the name of the compounds mesh.
    *   If compound is not registered an out_of_range exception is thrown.
    */
    static std::string
    getCompoundMeshName(
        CompoundId compoundId
    );

    /**
    * @brief Obtains the scale of the corresponding mesh
    *
    * @param compoundId
    *   The id of the compound to acquire the mesh scale from
    *
    * @return
    *   A double equal to the scale of the model
    *   If compound is not registered an out_of_range exception is thrown.
    */
    static double
    getCompoundMeshScale(
        CompoundId compoundId
    );

    /**
    * @brief Obtains the effect of the corresponding agent
    *
    * @param compoundId
    *   The id of the compound to acquire the effect of
    *
    * @return A function pointer, to the agent effect function
    */
    static std::function<bool(EntityId, double)>*
    getAgentEffect(
        CompoundId id
    );

    /**
    * @brief Obtains the scale of the corresponding mesh
    *
    * @param compoundId
    *   The id of the compound to check if its an agent
    *
    * @return
    *   True if compound is an agent, false otherwise
    *   If compound is not registered an out_of_range exception is thrown.
    */
    static bool
    isAgentType(
        CompoundId id
    );

    CompoundRegistry() = delete;

};

}
