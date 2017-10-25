// Thrive Game
// Copyright (C) 2013-2017  Revolutionary Games
#pragma once
// ------------------------------------ //
#include "Entities/GameWorldFactory.h"


namespace thrive{

class ThriveWorldFactory : public Leviathan::GameWorldFactory{
public:

    ThriveWorldFactory();
    ~ThriveWorldFactory();

    std::shared_ptr<Leviathan::GameWorld> CreateNewWorld() override;
    
};

}


