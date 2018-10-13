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

    // For now this will be how we check species and agent type, i know its not
    // the most elegant but for what i want i dont think creating a whole array
    // of generic "properties" is a good use of time just yet.
    std::string
        getStringOne();
    std::string
        getStringTwo();
    void
        setStringOne(std::string newString);
    void
        setStringTwo(std::string newString);



    std::string string2;
    std::string string1;
};
} // namespace thrive
