#include "microbe_stage/species_registry.h"
#include "game.h"
#include "engine/engine.h"

#include <vector>
#include <unordered_map>
#include <boost/range/adaptor/map.hpp>

#include "tinyxml.h"

using namespace thrive;

luabind::scope
SpeciesRegistry::luaBindings() {
    using namespace luabind;
    return class_<SpeciesRegistry>("SpeciesRegistry")
        .scope
        [
            def("loadFromXML", &SpeciesRegistry::loadFromXML),
            def("getSpeciesNames", &SpeciesRegistry::getSpeciesNames),
            def("getSize", &SpeciesRegistry::getSize),
            def("getCompoundPriority", &SpeciesRegistry::getCompoundPriority),
            def("getCompoundAmount", SpeciesRegistry::getCompoundAmount),
            def("getOrganelle", SpeciesRegistry::getOrganelle)
        ]
    ;
}

namespace {
    struct Organelle { // TODO make Organelle a map of some sort?
        int q, r;
        std::string internalName;
    };
    struct Compound {
        double amount, priority;
        Compound() : amount(0), priority(0) {}
    };
    struct Species {
        // std::string displayName; // internal name is in the speciesMap
        std::unordered_map<std::string, Compound> compounds;
        std::vector<Organelle> organelles;
        Species(){}
    };
}

static std::unordered_map<std::string, Species>&
speciesMap() {
    static std::unordered_map<std::string, Species> speciesMap;
    return speciesMap;
}

// luabound:

int
SpeciesRegistry::getSize(
    const std::string& microbe_name) {
    try {
        return speciesMap()[microbe_name].organelles.size();
    } catch (...) {
        return 0;
    }
}

luabind::object
SpeciesRegistry::getOrganelle(
    const std::string& microbe_name,
    int index) {
    luabind::object table = luabind::newtable(Game::instance().engine().luaState());
    try {
        Organelle organelle = speciesMap()[microbe_name].organelles[index];
        table["name"] = organelle.internalName;
        table["q"] = organelle.q;
        table["r"] = organelle.r;
    } catch (...) {
        table["name"] = "";
        table["q"] = 0;
        table["r"] = 0;
    }
    return table;
}

luabind::object
SpeciesRegistry::getSpeciesNames() {
    luabind::object table = luabind::newtable(Game::instance().engine().luaState());
    int i = 1;
    for (std::string name : speciesMap() | boost::adaptors::map_keys) {
        table[i] = name;
        i++;
    }
    return table;
}

double
SpeciesRegistry::getCompoundPriority(
    const std::string& microbe_name,
    const std::string& compound_name) {
    try {
        return speciesMap()[microbe_name].compounds[compound_name].priority;
    } catch (...) {
        return 0;
    }
}

double
SpeciesRegistry::getCompoundAmount(
    const std::string& microbe_name,
    const std::string& compound_name) {
    try {
        return speciesMap()[microbe_name].compounds[compound_name].amount;
    } catch (...) {
        return 0;
    }
}

void
SpeciesRegistry::loadFromXML(
    const std::string& filename) {
    TiXmlDocument doc(filename.c_str());
    bool loadOkay = doc.LoadFile();
    if (!loadOkay) throw std::invalid_argument(doc.ErrorDesc());

    TiXmlHandle hDoc(&doc),
                hMicrobes(0),
                hOrganelles(0),
                hCompounds(0);
    TiXmlElement * pSpecies,
                 * pOrganelle,
                 * pCompound;
    hMicrobes = hDoc.FirstChildElement("Microbes");
    pSpecies = hMicrobes.FirstChild("Species").Element();
    while (pSpecies) {
        Species species;
        const char* compoundName;

        hCompounds = TiXmlHandle(pSpecies->FirstChildElement("Compounds"));
        pCompound = hCompounds.FirstChildElement("Compound").Element();

        while (pCompound) {
            Compound compound;
            if (pCompound->QueryDoubleAttribute("priority", &(compound.priority)) != TIXML_SUCCESS) {
                compound.priority = 0;
            }
            if (pCompound->QueryDoubleAttribute("amount", &(compound.amount)) != TIXML_SUCCESS) {
                compound.amount = 0;
            }
            compoundName = pCompound->Attribute("name");
            if (compoundName == nullptr) {
                throw std::logic_error("Could not access 'name' attribute on Compound element of " + filename);
            }
            species.compounds[static_cast<std::string>(compoundName)] = compound;
            pCompound = pCompound->NextSiblingElement();
        }

        const char* organelleName;
        hOrganelles = TiXmlHandle(pSpecies->FirstChildElement("Organelles"));
        pOrganelle = hOrganelles.FirstChildElement("Organelle").Element();
        
        while (pOrganelle) {
            Organelle organelle;
            if (pOrganelle->QueryIntAttribute("q", &(organelle.q)) != TIXML_SUCCESS) {
                throw std::logic_error("Could not access 'q' attribute on Organelle element of " + filename);
            }
            if (pOrganelle->QueryIntAttribute("r", &(organelle.r)) != TIXML_SUCCESS) {
                throw std::logic_error("Could not access 'r' attribute on Organelle element of " + filename);
            }
            organelleName = pOrganelle->Attribute("name");
            if (organelleName == nullptr) {
                throw std::logic_error("Could not access 'name' attribute on Organelle element of " + filename);
            }
            organelle.internalName = static_cast<std::string>(organelleName);
            species.organelles.push_back(organelle);
            pOrganelle = pOrganelle->NextSiblingElement();
        }

        const char* speciesName = pSpecies->Attribute("name");
        if (nullptr == speciesName) {
            throw std::logic_error("Could not access 'name' attribute on Species element of " + filename);
        }
        speciesMap()[static_cast<std::string>(speciesName)] = species;
        pSpecies = pSpecies->NextSiblingElement("Species");
    }
}
