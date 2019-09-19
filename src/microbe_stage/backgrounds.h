#pragma once

#include "general/json_registry.h"

#include <string>
#include <vector>

namespace thrive {

//! \brief Microbe stage multi layered background
class Background : public RegistryType {
public:
    Background() = default;

    Background(Json::Value value);

    //! All current backgrounds are 4 layers. But this is coded like this to
    //! make this generally applicable for uses where a different number of
    //! layers are allowed
    std::vector<std::string> layers;
};

} // namespace thrive
