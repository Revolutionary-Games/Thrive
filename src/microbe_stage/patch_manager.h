#pragma once

#include "patch.h"

#include <Entities/PerWorldData.h>

namespace thrive {

class PatchManager : public Leviathan::PerWorldData {
public:
    PatchManager(GameWorld& world);

    PatchMap::pointer
        getCurrentMap()
    {
        return currentMap;
    }

private:
    PatchMap::pointer currentMap;
};

} // namespace thrive
