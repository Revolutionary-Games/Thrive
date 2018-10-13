#pragma once

#include "engine/component_types.h"

#include "Entities/Component.h"
#include "Entities/System.h"



namespace thrive {


/**
 * @brief Component for entities we wnat to hold little bits of extra data
 */
class PropertiesComponent : public Leviathan::Component {
public:
    PropertiesComponent();
    // void
    // load(
    //     const StorageContainer& storage
    // ) override;

    // StorageContainer
    // storage() const override;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(PropertiesComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::PROPERTIES);
};
} // namespace thrive
