#include "species_component.h"

#include <OgreMath.h>
#include "engine/engine.h"
#include "engine/serialization.h"
#include "engine/component_factory.h"
#include "game.h"

using namespace thrive;

unsigned int SpeciesComponent::SPECIES_NUM = 0;

void SpeciesComponent::luaBindings(
    sol::state &lua
){
    lua.new_usertype<SpeciesComponent>("SpeciesComponent",

        sol::constructors<sol::types<const std::string&>>(),

        sol::base_classes, sol::bases<Component>(),

        "ID", sol::var(lua.create_table_with("TYPE_ID", SpeciesComponent::TYPE_ID)),
        "TYPE_NAME", &SpeciesComponent::TYPE_NAME,

        "name", &SpeciesComponent::name,
        "organelles", &SpeciesComponent::organelles,
        "avgCompoundAmounts", &SpeciesComponent::avgCompoundAmounts,
        "colour", &SpeciesComponent::colour,
        "load", &SpeciesComponent::load,
        "storage", &SpeciesComponent::storage
    );
}

SpeciesComponent::SpeciesComponent(const std::string& _name)
	: colour(1,0,1), name(_name) {
	if (name == "") {
		name = "noname" + std::to_string(SPECIES_NUM);
		++SPECIES_NUM;
	}

    sol::state_view lua(Game::instance().engine().luaState());

	organelles = lua.create_table();
    avgCompoundAmounts = lua.create_table();
}

void
SpeciesComponent::load(const StorageContainer& storage) {
	Component::load(storage);
	name = storage.get<std::string>("name");
	colour = storage.get<Ogre::Vector3>("colour");

    sol::state_view lua(Game::instance().engine().luaState());
    
	StorageContainer orgs = storage.get<StorageContainer>("organelles");

	int i = 1;
	while (orgs.contains(std::to_string(i))) {
		StorageContainer org = orgs.get<StorageContainer>(std::to_string(i));
		sol::table organelle = lua.create_table();

		organelle["name"] = org.get<std::string>("name");
		organelle["q"] = org.get<int>("q");
		organelle["r"] = org.get<int>("r");
		organelle["rotation"] = org.get<Ogre::Real>("rotation");
		organelles[std::to_string(i)] = organelle;

		i++;
	}

	avgCompoundAmounts = lua.create_table();
	StorageContainer amts = storage.get<StorageContainer>("avgCompoundAmounts");

	for (const std::string& k : amts.keys()) {
		avgCompoundAmounts[k] = amts.get<Ogre::Real>(k);
	}
}

StorageContainer
SpeciesComponent::storage() const {
	StorageContainer storage = Component::storage();
	storage.set<std::string>("name", name);
	storage.set<Ogre::Vector3>("colour", colour);

	StorageContainer orgs;

	int i = 1;
	for (const auto& pair : organelles) {
        
        sol::table data = pair.second.as<sol::table>();

        
        
        StorageContainer org;
        org.set<std::string>("name", data.get<std::string>("name"));
        org.set<int>("q", data.get<int>("q"));
        org.set<int>("r", data.get<int>("r"));
        org.set<Ogre::Real>("rotation", data.get<Ogre::Real>("rotation"));

        orgs.set<StorageContainer>(std::to_string(i), org);

        ++i;
	}
	storage.set<StorageContainer>("organelles", orgs);

	StorageContainer amts;
	for (const auto& pair : avgCompoundAmounts) {
        
		const std::string& key = pair.first.as<std::string>();
        const Ogre::Real& data = pair.second.as<Ogre::Real>();

        amts.set<Ogre::Real>(key, data);
	}

	storage.set<StorageContainer>("avgCompoundAmounts", amts);
	return storage;
}

REGISTER_COMPONENT(SpeciesComponent)
