#pragma once

#include "membrane_system.h"

#include <Common/ReferenceCounted.h>

#include <add_on/scriptarray/scriptarray.h>
#include <add_on/scriptdictionary/scriptdictionary.h>

#include <json/json.h>

#include <map>
#include <string>
#include <vector>

namespace thrive {

//! Represents a microbial species
//!
//! This is no longer a component as this will now be contained in patches and
//! also sent to the auto-evo system.
//! \todo Now that this is a proper class all the properties should be made
//! private to allow locking them during auto-evo runs
//! \todo Adding an ID here and making the name optional would be nice. Needs a
//! bunch of changes to everywhere that references a species name. The genus and
//! epithet are used for display purposes. So the name is just an unique
//! identifier, which may as well be a number
class Species : public Leviathan::ReferenceCounted {
public:
    Species(const std::string& name);
    ~Species();

    void
        setPopulationFromPatches(int32_t population);

    //! \brief Immediate population change (from the player dying)
    //! \note This should be made sure to not affect auto-evo. As long as
    //! auto-evo uses the per patch population numbers this doesn't affect that.
    //! \note In addition to this an external population effect needs to be sent
    //! to auto-evo, otherwise this effect disappears when auto-evo finishes
    void
        applyImmediatePopulationChange(int32_t change);

    //! \returns True if this is the player species
    bool
        isPlayerSpecies() const;

    //! \returns A formatted name
    std::string
        getFormattedName(bool identifier = false);

    //! \brief Makes a JSON object representing this biome
    //! \todo Implement full mode, right now this skips the avg compounds (and
    //! organelles, but those are findable in stringCode)
    Json::Value
        toJSON(bool full = false) const;

    // These are reference counted so don't forget to release
    CScriptArray* organelles = nullptr;
    CScriptDictionary* avgCompoundAmounts = nullptr;

    Float4 colour = Float4::ColourWhite;
    bool isBacteria = false;
    MEMBRANE_TYPE speciesMembraneType = MEMBRANE_TYPE::MEMBRANE;
    std::string name;
    std::string genus;
    std::string epithet;

    std::string stringCode = "no string code set";

    // Behavior properties
    float aggression = 100.0f;
    float opportunism = 100.0f;
    float fear = 100.0f;
    float activity = 0.0f;
    float focus = 0.0f;

    //! This is the global population (the sum of population in all patches)
    int32_t population = 1;
    //! \todo Increment this for non-player species
    int32_t generation = 1;

    REFERENCE_COUNTED_PTR_TYPE(Species);
};

} // namespace thrive
