#pragma once

#include "engine/component.h"

#include "scripting/luajit.h"

#include <OgreVector3.h>
#include <string>

namespace thrive {

class SpeciesComponent : public Component {
	COMPONENT(SpeciesComponent)

public:
    static void luaBindings(sol::state &lua);

	SpeciesComponent(const std::string& _name = "");

	sol::table organelles;
	sol::table avgCompoundAmounts;
	Ogre::Vector3 colour;
	std::string name;

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;
private:
	static unsigned int SPECIES_NUM;
};

}
