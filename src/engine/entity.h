#pragma once

#include "engine/component.h"

#include <memory>

namespace thrive {

class Entity {

public:

    using Id = unsigned int;

    static const Id NULL_ID;

    static Id
    generateNewId();

};

}
