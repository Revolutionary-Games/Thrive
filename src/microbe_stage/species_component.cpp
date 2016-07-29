#include "species_component.h"

#include <luabind/iterator_policy.hpp>
#include <OgreMath.h>
#include "engine/engine.h"
#include "engine/serialization.h"
#include "engine/component_factory.h"
#include "game.h"

using namespace thrive;

uint SpeciesComponent::SPECIES_NUM = 0;

luabind::scope
SpeciesComponent::luaBindings() {
	using namespace luabind;
	return class_<SpeciesComponent, Component>("SpeciesComponent")
		.enum_("ID") [
			value("TYPE_ID", SpeciesComponent::TYPE_ID)
		]
		.scope [
			def("TYPE_NAME", &SpeciesComponent::TYPE_NAME)
		]
		.def(constructor<const std::string&>())
		.def_readwrite("name", &SpeciesComponent::name)
		.def_readwrite("organelles", &SpeciesComponent::organelles)
		.def_readwrite("avgCompoundAmounts", &SpeciesComponent::avgCompoundAmounts)
		.def_readwrite("colour", &SpeciesComponent::colour)
		.def("load", &SpeciesComponent::load)
		.def("storage", &SpeciesComponent::storage)
	;
}

SpeciesComponent::SpeciesComponent(const std::string& _name)
	: colour(1,0,1), name(_name) {
	if (name == "") {
		name = "noname" + SPECIES_NUM;
		++SPECIES_NUM;
	}

	lua_State* lua_state = Game::instance().engine().luaState();

	organelles = luabind::newtable(lua_state);
	avgCompoundAmounts = luabind::newtable(lua_state);
}

void
SpeciesComponent::load(const StorageContainer& storage) {
	Component::load(storage);
	name = storage.get<std::string>("name");
	colour = storage.get<Ogre::Vector3>("colour");

	lua_State* lua_state = Game::instance().engine().luaState();

	organelles = luabind::newtable(lua_state);
	StorageContainer orgs = storage.get<StorageContainer>("organelles");

	uint i = 0;
	while (orgs.contains("" + i)) {
		StorageContainer org = orgs.get<StorageContainer>("" + i);
		luabind::object organelle = luabind::newtable(lua_state);

		organelle["name"] = org.get<std::string>("name");
		organelle["q"] = org.get<int>("q");
		organelle["r"] = org.get<int>("r");
		organelle["rotation"] = org.get<Ogre::Real>("rotation");
		organelles["" + ++i] = organelle;
	}

	avgCompoundAmounts = luabind::newtable(lua_state);
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

	uint i = 0;
	for (luabind::iterator it(organelles), end; it != end; ++it, ++i) {
        const luabind::object& data = *it;

        StorageContainer org;
        org.set<std::string>("name", luabind::object_cast<std::string>(data["name"]));
        org.set<int>("q", luabind::object_cast<int>(data["q"]));
        org.set<int>("r", luabind::object_cast<int>(data["r"]));
        org.set<Ogre::Real>("rotation", luabind::object_cast<Ogre::Real>(data["rotation"]));

        orgs.set<StorageContainer>("" + i, org);
	}

	storage.set<StorageContainer>("organelles", orgs);

	StorageContainer amts;
	for (luabind::iterator it(avgCompoundAmounts), end; it != end; ++it) {
		const std::string& key = luabind::object_cast<std::string>(it.key());
        const Ogre::Real& data = luabind::object_cast<Ogre::Real>(*it);

        amts.set<Ogre::Real>(key, data);
	}

	storage.set<StorageContainer>("avgCompoundAmounts", amts);
	return storage;
}

REGISTER_COMPONENT(SpeciesComponent)
