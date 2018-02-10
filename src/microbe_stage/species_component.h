#pragma once

#include <OgreVector3.h>
#include <string>


#include "engine/component_types.h"
#include <Entities/Component.h>
#include <Entities/Components.h>

#include <add_on/scriptdictionary/scriptdictionary.h>
#include <add_on/scriptarray/scriptarray.h>

namespace thrive {

class SpeciesComponent : public Leviathan::Component {
public:
	SpeciesComponent(const std::string& _name = "");

    ~SpeciesComponent();

    // These are reference counted so don't forget to release
    CScriptArray* organelles = nullptr;
    CScriptDictionary* avgCompoundAmounts = nullptr;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(SpeciesComponent);
    
	Float4 colour;
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
