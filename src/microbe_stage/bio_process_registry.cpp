
#include "microbe_stage/bio_process_registry.h"
#include "scripting/luabind.h"
#include "tinyxml/tinyxml.h"

#include <luabind/iterator_policy.hpp>

using namespace thrive;

luabind::scope
BioProcessRegistry::luaBindings() {
    using namespace luabind;
    return (class_<BioProcessRegistry>("BioProcessRegistry")
        .scope
        [
            def("loadFromXML", &BioProcessRegistry::loadFromXML),
            //def("registerBioProcess", &BioProcessRegistry::registerBioProcess),
            def("getDisplayName", &BioProcessRegistry::getDisplayName),
            def("getInternalName", &BioProcessRegistry::getInternalName),
            def("getId", &BioProcessRegistry::getId),
            def("getList", &BioProcessRegistry::getList, return_stl_iterator),
            def("getInputCompounds", &BioProcessRegistry::getInputCompounds, return_stl_iterator),
            def("getOutputCompounds", &BioProcessRegistry::getOutputCompounds, return_stl_iterator)
        ]
    ,
        class_<std::pair<CompoundId, int>>("RecipyCompound")
            .def_readwrite("compoundId", &std::pair<CompoundId, int>::first)
            .def_readwrite("amount", &std::pair<CompoundId, int>::second)
    );
}


namespace {
    struct BioProcessEntry
    {
        std::string internalName;
        std::string displayName;
        int energyCost;
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
		TiXmlHandle hDoc(&doc);
        TiXmlElement* pElem,
                    * pElem2;
        TiXmlHandle hRoot(0);

        pElem=hDoc.FirstChildElement().Element();
        hRoot=TiXmlHandle(pElem);



      //  pElem=hRoot.FirstChild( "Messages" ).FirstChild().Element();
		pElem=pElem->NextSiblingElement();
        while (pElem)
		{
		    pElem2=hRoot.FirstChild( "Inputs" ).FirstChild().Element();
            pElem2=pElem2->NextSiblingElement();
            while (pElem2)
            {
                int amount;
                pElem2->Attribute("aminoacid", &amount);
                std::cout << "input found: " << amount << std::endl;

                //const char *pKey=pElem->Value();
              /*  const char *pText=pElem->GetText();
                if (pKey && pText)
                {
                    m_messages[pKey]=pText;
                }*/
                pElem2=pElem2->NextSiblingElement();
            }
            pElem=pElem->NextSiblingElement();
		}

	}
	else {

		throw std::invalid_argument(doc.ErrorDesc());
	}
}


BioProcessId
BioProcessRegistry::registerBioProcess(
    const std::string& internalName,
    const std::string& displayName,
    int energyCost,
    double speedFactor,
    std::vector<std::pair<CompoundId, int>> inputCompounds,
    std::vector<std::pair<CompoundId, int>> outputCompounds
) {
    if (processRegistryMap().count(internalName) == 0) {
        BioProcessEntry entry;
        entry.internalName = internalName;
        entry.displayName = displayName;
		entry.energyCost = energyCost;
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
