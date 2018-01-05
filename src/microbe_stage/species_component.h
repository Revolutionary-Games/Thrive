#pragma once

#include <OgreVector3.h>
#include <string>


#include "engine/component_types.h"
#include "Entities/Component.h"
#include "Entities/Components.h"

namespace thrive {

class SpeciesComponent : public Leviathan::Component {
public:
	SpeciesComponent(const std::string& _name = "");

	// TODO.
	//sol::table organelles;
	//sol::table avgCompoundAmounts;
	Ogre::Vector3 colour;
	std::string name;

	// TODO: get the id from the simulation parameters.
	size_t id;

	static constexpr auto TYPE = componentTypeConvert(THRIVE_COMPONENT::SPECIES);

	/*
    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;
	*/

private:
	static unsigned int SPECIES_NUM;
};

}
