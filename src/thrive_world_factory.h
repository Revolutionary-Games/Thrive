// Thrive Game
// Copyright (C) 2013-2018  Revolutionary Games
#pragma once
// ------------------------------------ //
#include "Entities/GameWorldFactory.h"


namespace thrive {

enum class THRIVE_WORLD_TYPE : int { CELL_STAGE = 1, MICROBE_EDITOR };

class ThriveWorldFactory : public Leviathan::GameWorldFactory {
public:
    ThriveWorldFactory();
    ~ThriveWorldFactory();

    std::shared_ptr<Leviathan::GameWorld>
        CreateNewWorld(int worldtype) override;
};

} // namespace thrive
