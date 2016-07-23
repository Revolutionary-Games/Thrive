
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/compound.h"
#include "microbe_stage/compound_registry.h"
#include "scripting/luabind.h"

#include "tinyxml.h"

#include <luabind/iterator_policy.hpp>

using namespace thrive;

luabind::scope
BioProcessRegistry::luaBindings() {
    using namespace luabind;
    return (class_<BioProcessRegistry>("BioProcessRegistry")
        .scope
        [
            def("loadFromXML", &BioProcessRegistry::loadFromXML),
            def("loadFromLua", &BioProcessRegistry::loadFromLua),
            //def("registerBioProcess", &BioProcessRegistry::registerBioProcess),
            def("getDisplayName", &BioProcessRegistry::getDisplayName),
            def("getInternalName", &BioProcessRegistry::getInternalName),
            def("getSpeedFactor", &BioProcessRegistry::getSpeedFactor),
            def("getId", &BioProcessRegistry::getId),
            def("getList", &BioProcessRegistry::getList, return_stl_iterator),
            def("getInputCompounds", &BioProcessRegistry::getInputCompounds, return_stl_iterator),
            def("getOutputCompounds", &BioProcessRegistry::getOutputCompounds, return_stl_iterator)
        ]
    ,
        class_<std::pair<CompoundId, int>>("RecipyCompound")
            .def_readonly("compoundId", &std::pair<CompoundId, int>::first)
            .def_readonly("amount", &std::pair<CompoundId, int>::second)
    );
}

namespace {
    struct BioProcessEntry
    {
        std::string internalName;
        std::string displayName;
        double speedFactor;
        std::vector<std::pair<CompoundId, int>> inputCompounds;
        std::vector<std::pair<CompoundId, int>> outputCompounds;
    };
}

static std::unordered_map<std::string, BioProcessId>&
processRegistryMap() {
    static std::unordered_map<std::string, BioProcessId> processRegistry;
    return processRegistry;
}

static std::vector<BioProcessEntry>&
processRegistry() {
    static std::vector<BioProcessEntry> processRegistry;
    return processRegistry;
}

void
BioProcessRegistry::loadFromXML(
    const std::string& filename
) {
    TiXmlDocument doc(filename.c_str());
    bool loadOkay = doc.LoadFile();
    if (loadOkay)
    {
        // Handles used for null-safety
        TiXmlHandle hDoc(&doc),
                    hProcesses(0),
                    hCompounds(0);
        // Elements used for iteration
        TiXmlElement * pProcess,
                     * pCompound;

        hProcesses=hDoc.FirstChildElement("Processes");
        pProcess=hProcesses.FirstChild( "Process" ).Element();
        while (pProcess)
        {
            std::vector<std::pair<CompoundId, int>> inputs;
            std::vector<std::pair<CompoundId, int>> outputs;
            const char* compoundName; //Char pointer for null-checks
            int compoundAmount;
            hCompounds = TiXmlHandle(pProcess->FirstChildElement("Inputs"));
            pCompound=hCompounds.FirstChildElement( "Input" ).Element();
            while (pCompound)
            {
                if (pCompound->QueryIntAttribute("amount", &compoundAmount) != TIXML_SUCCESS){
                    throw std::logic_error("Could not access 'amount' attribute on Input element of " + filename);
                }
                compoundName = pCompound->Attribute("compound");
                if (compoundName == nullptr) {
                    throw std::logic_error("Could not access 'compound' attribute on Input element of " + filename);
                }
                inputs.push_back({CompoundRegistry::getCompoundId(compoundName), compoundAmount});
                pCompound=pCompound->NextSiblingElement();
            }
            hCompounds = TiXmlHandle(pProcess->FirstChildElement("Outputs"));
            pCompound=hCompounds.FirstChildElement( "Output" ).Element();
            while (pCompound)
            {
                if (pCompound->QueryIntAttribute("amount", &compoundAmount) != TIXML_SUCCESS){
                    throw std::logic_error("Could not access 'amount' attribute on Input element of " + filename);
                }
                compoundName = pCompound->Attribute("compound");
                if (compoundName == nullptr) {
                    throw std::logic_error("Could not access 'compound' attribute on Input element of " + filename);
                }
                outputs.push_back({CompoundRegistry::getCompoundId(compoundName), compoundAmount});

                pCompound=pCompound->NextSiblingElement();
            }
            int energyCost;
            double speedFactor;
            if (pProcess->QueryIntAttribute("energyCost", &energyCost) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'speedFactor' attribute on Process element of " + filename);
            }
            if (pProcess->QueryDoubleAttribute("speedFactor", &speedFactor) != TIXML_SUCCESS){
                throw std::logic_error("Could not access 'speedFactor' attribute on Process element of " + filename);
            }
            const char* processName = pProcess->Attribute("name");
            if (processName == nullptr) {
                throw std::logic_error("Could not access 'name' attribute on Process element of " + filename);
            }
            registerBioProcess(
               processName,
               processName,
               speedFactor,
               std::move(inputs),
               std::move(outputs)
            );
            pProcess=pProcess->NextSiblingElement("Process");
        }
    }
    else {
        throw std::invalid_argument(doc.ErrorDesc());
    }
}

void
BioProcessRegistry::loadFromLua(
    const luabind::object& processTable
    )
{
    for (luabind::iterator i(processTable), end; i != end; ++i) {
        std::string key = luabind::object_cast<std::string>(i.key());
        luabind::object data = *i;

        float speedFactor = luabind::object_cast<float>(data["speedFactor"]);
        luabind::object inputTable = data["inputs"];
        luabind::object outputTable = data["outputs"];
        std::vector<std::pair<CompoundId, int>> inputs;
        std::vector<std::pair<CompoundId, int>> outputs;

        for (luabind::iterator ii(inputTable), end; ii != end; ++ii) {
            std::string compound = luabind::object_cast<std::string>(ii.key());
            float amount = luabind::object_cast<float>(*ii);
            inputs.push_back({CompoundRegistry::getCompoundId(compound), amount});
        }

        for (luabind::iterator oi(outputTable), end; oi != end; ++oi) {
            std::string compound = luabind::object_cast<std::string>(oi.key());
            float amount = luabind::object_cast<float>(*oi);
            outputs.push_back({CompoundRegistry::getCompoundId(compound), amount});
        }

        registerBioProcess(
               key,
               key,
               speedFactor,
               std::move(inputs),
               std::move(outputs)
            );
    }
}

BioProcessId
BioProcessRegistry::registerBioProcess(
    const std::string& internalName,
    const std::string& displayName,
    double speedFactor,
    std::vector<std::pair<CompoundId, int>> inputCompounds,
    std::vector<std::pair<CompoundId, int>> outputCompounds
) {
    if (processRegistryMap().count(internalName) == 0) {
        BioProcessEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
        entry.speedFactor = speedFactor;
        entry.inputCompounds = inputCompounds;
        entry.outputCompounds = outputCompounds;
        processRegistry().push_back(entry);
        processRegistryMap().emplace(std::string(internalName), processRegistry().size());
        return processRegistry().size();
    }
    else {
        throw std::invalid_argument("Duplicate internalName not allowed.");
    }
}

std::string
BioProcessRegistry::getDisplayName(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].displayName;
}


std::string
BioProcessRegistry::getInternalName(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].internalName;
}

double
BioProcessRegistry::getSpeedFactor(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].speedFactor;
}

BioProcessId
BioProcessRegistry::getId(
    const std::string& internalName
) {
    BioProcessId processId;
    try
    {
        processId = processRegistryMap().at(internalName);
    }
    catch(std::out_of_range&)
    {
        throw std::out_of_range("Internal name of process does not exist.");
    }
    return processId;
}


const BoostCompoundMapIterator
BioProcessRegistry::getList(
) {
    return processRegistryMap() | boost::adaptors::map_values;
}

const std::vector<std::pair<CompoundId, int>>&
BioProcessRegistry::getInputCompounds(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].inputCompounds;
}

const std::vector<std::pair<CompoundId, int>>&
BioProcessRegistry::getOutputCompounds(
    BioProcessId id
) {
    if (static_cast<std::size_t>(id) > processRegistry().size())
        throw std::out_of_range("Index of process does not exist.");
    return processRegistry()[id-1].outputCompounds;
}
