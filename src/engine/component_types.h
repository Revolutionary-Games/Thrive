#pragma once

#include "Entities/Component.h"

namespace thrive {

//! This needs to have the types of all thrive specific component types
//! \note Don't forget to use componentTypeConvert(...) when these are used to
//! make them the right type. Also for the base Component class should be
//! constructed like this: "Leviathan::Component(TYPE)" instead of duplicating
//! the enum value there to avoid errors
enum class THRIVE_COMPONENT : uint16_t {

    MEMBRANE = static_cast<uint16_t>(Leviathan::COMPONENT_TYPE::Custom) + 1,
    COMPOUND_CLOUD,
    PROCESSOR,
    COMPOUND_BAG,
    SPECIES,
    AGENT_CLOUD,
    SPAWNED,
    ABSORBER,
    TIMED_LIFE,
    PROPERTIES,
    COMPOUND_VENTER,
    ENGULFABLE,
    // TODO: check is this needed for anything
    // INVALID
};

//! \brief Use to convert THRIVE_COMPONENT to Leviathan::COMPONENT_TYPE
constexpr inline Leviathan::COMPONENT_TYPE
    componentTypeConvert(THRIVE_COMPONENT value)
{
    return static_cast<Leviathan::COMPONENT_TYPE>(static_cast<uint16_t>(value));
}

} // namespace thrive
