#pragma once

#include "Entities/Component.h"

namespace thrive {

//! This needs to have the types of all thrive specific component types
enum class THRIVE_COMPONENT : uint16_t {

    MEMBRANE = static_cast<uint16_t>(Leviathan::COMPONENT_TYPE::Custom) + 1,
    COMPOUND_CLOUD,
    
};

//! \brief Use to convert THRIVE_COMPONENT to Leviathan::COMPONENT_TYPE
constexpr inline Leviathan::COMPONENT_TYPE componentTypeConvert(THRIVE_COMPONENT value){

    return static_cast<Leviathan::COMPONENT_TYPE>(static_cast<uint16_t>(value));
}

}


