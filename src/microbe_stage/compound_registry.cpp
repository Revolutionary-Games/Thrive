#include "compound_registry.h"

#include "bullet/rigid_body_system.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "game.h"
#include "ogre/scene_node_system.h"
#include "scripting/luabind.h"
#include "util/make_unique.h"
#include "microbe_stage/compound.h"

#include "tinyxml.h"

#include <luabind/iterator_policy.hpp>
#include <OgreEntity.h>
#include <OgreSceneManager.h>
#include <stdexcept>

using namespace thrive;

luabind::scope
CompoundRegistry::luaBindings() {
    using namespace luabind;
    return class_<CompoundRegistry>("CompoundRegistry")
        .scope
        [
            def("registerCompoundType", &CompoundRegistry::registerCompoundType),
            def("registerAgentType",
                static_cast<CompoundId (*)(
                    const std::string&,
                    const std::string&,
                    const std::string&,
                    double,
                    bool,
                    float,
                    const luabind::object&
                )>(&CompoundRegistry::registerAgentType)
            ),
            def("loadFromXML", &CompoundRegistry::loadFromXML),
            def("loadFromLua", &CompoundRegistry::loadFromLua),
            def("loadAgentFromLua", &CompoundRegistry::loadAgentFromLua),
            def("getCompoundDisplayName", &CompoundRegistry::getCompoundDisplayName),
            def("getCompoundInternalName", &CompoundRegistry::getCompoundInternalName),
			def("getCompoundMeshName", &CompoundRegistry::getCompoundMeshName),
            def("getCompoundUnitVolume", &CompoundRegistry::getCompoundUnitVolume),
            def("getCompoundId", &CompoundRegistry::getCompoundId),
            def("getCompoundList", &CompoundRegistry::getCompoundList, return_stl_iterator),
            def("getCompoundMeshScale", &CompoundRegistry::getCompoundMeshScale),
            def("getAgentEffect", &CompoundRegistry::getAgentEffect)
        ]
    ;
}


namespace {
    struct CompoundRegistryEntry
    {
        std::string internalName;
        std::string displayName;
        float unitVolume;
		std::string meshName;
        double meshScale;
        bool isAgent;
        bool isUseful;
        std::function<bool(EntityId, double)>* effect;
    };
}

static std::vector<CompoundRegistryEntry>&
compoundRegistry() {
    static std::vector<CompoundRegistryEntry> compoundRegistry;
    return compoundRegistry;
}
static std::unordered_map<std::string, CompoundId>&
compoundRegistryMap() {
    static std::unordered_map<std::string, CompoundId> compoundRegistryMap;
    return compoundRegistryMap;
}

void
CompoundRegistry::loadFromLua(
    const luabind::object& compoundTable,
    const luabind::object& agentTable
) {
    for (luabind::iterator i(compoundTable), end; i != end; ++i) {
        std::string key = luabind::object_cast<std::string>(i.key());
        luabind::object data = *i;
        std::string name = luabind::object_cast<std::string>(data["name"]);
        float weight = luabind::object_cast<float>(data["weight"]);
        std::string meshname = luabind::object_cast<std::string>(data["mesh"]);
        bool isUseful = luabind::object_cast<bool>(data["isUseful"]);
        float size = luabind::object_cast<float>(data["size"]);
        registerCompoundType(
                key,
                name,
                meshname,
                size,
                isUseful,
                weight
            );
    }
    for (luabind::iterator i(agentTable), end; i != end; ++i) {
        std::string key = luabind::object_cast<std::string>(i.key());
        luabind::object data = *i;
        std::string name = luabind::object_cast<std::string>(data["name"]);
        float weight = luabind::object_cast<float>(data["weight"]);
        std::string meshname = luabind::object_cast<std::string>(data["mesh"]);
        float size = luabind::object_cast<float>(data["size"]);
        // std::cerr << "before casting effect" << std::endl;
        luabind::object effect = data["effect"];
        registerAgentType(
                key,
                name,
                meshname,
                size,
                true,
                weight,
                effect
            );
    }
}
/*||=== Build: install in Thrive (compiler: GNU GCC Compiler) ===|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|121|error: no matching function for call to 'thrive::CompoundRegistry::registerAgentType(
std::string&,
std::string&,
std::string&,
float&,
bool,
float&,
luabind::adl::object&)

static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(
const string&,
const string&,
const string&,
double, int,
std::function<bool(unsigned int, double)>*)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, const luabind::adl::object&)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|145|error: no matching function for call to 'thrive::CompoundRegistry::registerAgentType(std::string&, std::string&, std::string&, float&, bool, float&, luabind::adl::object&)'|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|145|note: candidates are:|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, std::function<bool(unsigned int, double)>*)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, const luabind::adl::object&)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|254|error: no matching function for call to 'thrive::CompoundRegistry::registerAgentType(const char*&, const char*&, const char*&, double&, bool, int&, std::function<bool(unsigned int, double)>*&)'|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|254|note: candidates are:|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, std::function<bool(unsigned int, double)>*)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, const luabind::adl::object&)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|281|error: no matching function for call to 'thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double&, bool&, int&, std::function<bool(unsigned int, double)>*)'|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|281|note: candidates are:|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, std::function<bool(unsigned int, double)>*)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, const luabind::adl::object&)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|169|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|309|error: no matching function for call to 'thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double&, int&, bool, std::function<bool(unsigned int, double)>*&)'|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|309|note: candidates are:|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, std::function<bool(unsigned int, double)>*)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|287|note: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, const luabind::adl::object&)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|287|note:   candidate expects 6 arguments, 7 provided|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|313|error: prototype for 'thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, bool, float, std::function<bool(unsigned int, double)>*)' does not match any in class 'thrive::CompoundRegistry'|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|287|error: candidates are: static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, const luabind::adl::object&)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.h|134|error:                 static thrive::CompoundId thrive::CompoundRegistry::registerAgentType(const string&, const string&, const string&, double, int, std::function<bool(unsigned int, double)>*)|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|282|error: control reaches end of non-void function [-Werror=return-type]|
C:\Users\User\Documents\GitHub\Thrive\Thrive\src\microbe_stage\compound_registry.cpp|310|error: control reaches end of non-void function [-Werror=return-type]|
CMakeFiles\ThriveLib.dir\build.make|962|recipe for target 'CMakeFiles/ThriveLib.dir/src/microbe_stage/compound_registry.cpp.obj' failed|
CMakeFiles\Makefile2|99|recipe for target 'CMakeFiles/ThriveLib.dir/all' failed|
C:\Users\User\Documents\GitHub\Thrive\Build\Makefile|126|recipe for target 'all' failed|
||=== Build failed: 13 error(s), 0 warning(s) (2 minute(s), 21 second(s)) ===|
*/
void
CompoundRegistry::loadAgentFromLua(
    const luabind::object& internalName,
    const luabind::object& data
) {
    std::string internal_name = luabind::object_cast<std::string>(internalName);
    std::string name = luabind::object_cast<std::string>(data["name"]);
    float weight = luabind::object_cast<float>(data["weight"]);
    std::string meshname = luabind::object_cast<std::string>(data["mesh"]);
    float size = luabind::object_cast<float>(data["size"]);
    // std::cerr << "before casting effect" << std::endl;
    luabind::object effect = data["effect"];
    registerAgentType(
            internal_name,
            name,
            meshname,
            size,
            true,
            weight,
            effect
        );
}

//Currently unused, need's updating to work properly.
void
CompoundRegistry::loadFromXML(
    const std::string& filename
) {
    TiXmlDocument doc(filename.c_str());
    bool loadOkay = doc.LoadFile();
    if (loadOkay)
	{
	    // Handles used for null-safety when possible
		TiXmlHandle hDoc(&doc),
                    hCompounds(0),
                    hDisplay(0),
                    hModel(0),
                    hAgents(0),
                    hEffect(0);
        // Elements used for iteration with explicit null-checks
        TiXmlElement * pCompound,
                     * pAgent;

        hCompounds=hDoc.FirstChildElement("Compounds");

		pCompound=hCompounds.FirstChildElement("Compound").Element();
        while (pCompound)
		{
		    hDisplay = TiXmlHandle(pCompound->FirstChildElement("Display"));
            hModel = TiXmlHandle(hDisplay.FirstChildElement("Model"));
            int molecularWeight;
            double modelSize;
            if (pCompound->QueryIntAttribute("weight", &molecularWeight) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'weight' attribute on compound element of " + filename);
            }
            if (hModel.Element()->QueryDoubleAttribute("size", &modelSize) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'size' attribute on Model element of " + filename);
            }
            const char* name = pCompound->Attribute("name");
            if (name == nullptr) {
                throw std::logic_error("Could not access 'name' attribute on compound element of " + filename);
            }
            const char* displayName = hDisplay.Element()->Attribute("text");
            if (displayName == nullptr) {
                throw std::logic_error("Could not access 'text' attribute on Display element of " + filename);
            }
            const char* meshname = hModel.Element()->Attribute("file");
            if (meshname == nullptr) {
                throw std::logic_error("Could not access 'file' attribute on Model element of " + filename);
            }
            registerCompoundType(
                name,
                displayName,
                meshname,
                modelSize,
                true,
                molecularWeight
            );
            pCompound=pCompound->NextSiblingElement("Compound");
		}
		hAgents=hCompounds.FirstChildElement("AgentCompounds");
		pAgent=hAgents.FirstChildElement("Agent").Element();
        while (pAgent)
		{
		    hDisplay = TiXmlHandle(pAgent->FirstChildElement("Display"));
            hModel = TiXmlHandle(hDisplay.FirstChildElement("Model"));
            hEffect = TiXmlHandle(pAgent->FirstChildElement("Effect"));
            int molecularWeight;
            double modelSize;
            if (pAgent->QueryIntAttribute("weight", &molecularWeight) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'weight' attribute on Compound element of " + filename);
            }
            if (hModel.Element()->QueryDoubleAttribute("size", &modelSize) != TIXML_SUCCESS){

                throw std::logic_error("Could not access 'size' attribute on Model element of " + filename);
            }
            const char* functionName = hEffect.Element()->Attribute("function");
            if (functionName == nullptr) {
                throw std::logic_error("Could not access 'function' attribute on Effect element of " + filename);
            }
            std::string luaFunctionName = std::string(functionName);
            // Create a lambda to call the function defined in the XML document
            auto effectLambda = new std::function<bool(EntityId, double)>(
                [luaFunctionName](EntityId entityId, double potency) -> bool
                {
                    luabind::call_function<void>(Game::instance().engine().luaState(), luaFunctionName.c_str(), entityId, potency);
                    return true;
                });
            const char* name = pAgent->Attribute("name");
            if (name == nullptr) {
                throw std::logic_error("Could not access 'name' attribute on compound element of " + filename);
            }
            const char* displayName = hDisplay.Element()->Attribute("text");
            if (displayName == nullptr) {
                throw std::logic_error("Could not access 'text' attribute on Display element of " + filename);
            }
            const char* meshname = hModel.Element()->Attribute("file");
            if (meshname == nullptr) {
                throw std::logic_error("Could not access 'file' attribute on Model element of " + filename);
            }
            //Register the agent type
            registerAgentType(
                name,
                displayName,
                meshname,
                modelSize,
                true,
                molecularWeight,
                effectLambda
            );
            pAgent=pAgent->NextSiblingElement("Agent");
		}
	}
	else {
		throw std::invalid_argument(doc.ErrorDesc());
	}
}

CompoundId
CompoundRegistry::registerCompoundType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    bool isUseful,
    float unitVolume

) {
    return registerAgentType(
        internalName,
        displayName,
        meshName,
        meshScale,
        isUseful,
        unitVolume,
        static_cast<std::function<bool(EntityId, double)>*>(nullptr)
    );
}


//Luabind version
CompoundId
CompoundRegistry::registerAgentType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    bool isUseful,
    float unitVolume,
    const luabind::object& effect
) {
    auto effectLambda = new std::function<bool(EntityId, double)>(
        [effect](EntityId entityId, double potency) -> bool
        {
            luabind::call_function<void>(effect, entityId, potency);
            return true;
        });
    //Call overload
    return registerAgentType(
        internalName,
         displayName,
         meshName,
         meshScale,
         isUseful,
         unitVolume,
         effectLambda);
}

CompoundId
CompoundRegistry::registerAgentType(
    const std::string& internalName,
    const std::string& displayName,
	const std::string& meshName,
    double meshScale,
    bool isUseful,
    float unitVolume,
    std::function<bool(EntityId, double)>* effect
) {
    if (compoundRegistryMap().count(internalName) == 0)
    {
        CompoundRegistryEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
		entry.meshName = meshName;
        entry.meshScale = meshScale;
        entry.unitVolume = unitVolume;
        entry.effect = effect;
        entry.isAgent = (effect != nullptr);
        entry.isUseful = entry.isAgent || isUseful;
        compoundRegistry().push_back(entry);
        compoundRegistryMap().emplace(std::string(internalName), compoundRegistry().size());
        return compoundRegistry().size();
    }
    else
    {
        throw std::invalid_argument("Duplicate internalName not allowed.");
    }
}

std::string
CompoundRegistry::getCompoundDisplayName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].displayName;
}

std::string
CompoundRegistry::getCompoundInternalName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].internalName;
}

int
CompoundRegistry::getCompoundUnitVolume(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].unitVolume;
}

CompoundId
CompoundRegistry::getCompoundId(
    const std::string& internalName
) {
    CompoundId compoundId;
    try
    {
        compoundId = compoundRegistryMap().at(internalName);
    }
    catch(std::out_of_range&)
    {
        throw std::out_of_range("Internal name of compound does not exist.");
    }
    return compoundId;
}

std::string
CompoundRegistry::getCompoundMeshName(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].meshName;
}

double
CompoundRegistry::getCompoundMeshScale(
    CompoundId compoundId


) {
    if (static_cast<std::size_t>(compoundId) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[compoundId-1].meshScale;
}
const BoostCompoundMapIterator
CompoundRegistry::getCompoundList(
) {
    return compoundRegistryMap() | boost::adaptors::map_values;
}

std::function<bool(EntityId, double)>*
CompoundRegistry::getAgentEffect(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].effect;
}


bool
CompoundRegistry::isAgentType(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].isAgent;
}

bool
CompoundRegistry::isUseful(
    CompoundId id
) {
    if (static_cast<std::size_t>(id) > compoundRegistry().size())
        throw std::out_of_range("Index of compound does not exist.");
    return compoundRegistry()[id-1].isUseful;
}
