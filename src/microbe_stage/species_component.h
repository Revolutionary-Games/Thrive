#pragma once


#include "engine/component_types.h"
#include "membrane_system.h"

#include <Entities/Component.h>
#include <Entities/Components.h>



#include <string>

namespace thrive {

class SpeciesComponent : public Leviathan::Component {
public:
    SpeciesComponent(const std::string& _name = "");

    ~SpeciesComponent();



    size_t id;

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::SPECIES);

    REFERENCE_HANDLE_UNCOUNTED_TYPE(SpeciesComponent);
};

} // namespace thrive
