#pragma once

#include "scripting/luabind.h"
#include "engine/component.h"
#include <OgreVector3.h>
#include <string>

namespace thrive {

class SpeciesComponent : public Component {
	COMPONENT(SpeciesComponent)

public:
	static luabind::scope
	luaBindings();

	SpeciesComponent(const std::string& _name = "");

	luabind::object organelles;
	luabind::object avgCompoundAmounts;
	Ogre::Vector3 colour;
	std::string name;

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;
private:
	static uint SPECIES_NUM;
};

}
