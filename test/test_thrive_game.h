// Thrive Game
// Copyright (C) 2013-2019  Revolutionary Games
#pragma once
// ------------------------------------ //
#include "ThriveGame.h"

#include "catch.hpp"

namespace thrive { namespace test {
//! \brief A test dummy for tests needing ThriveGame
class TestThriveGame : public ThriveGame {
public:
    TestThriveGame(Leviathan::Engine* engine) : ThriveGame(engine)
    {
        // We need to fake key configurations for things
        ApplicationConfiguration = new Leviathan::AppDef(true);
        ApplicationConfiguration->ReplaceGameAndKeyConfigInMemory(
            nullptr, &ThriveGame::CheckGameKeyConfigVariables);
    }

    ~TestThriveGame()
    {
        delete ApplicationConfiguration;
    }

    void
        lightweightInit()
    {
        REQUIRE(createImpl());
    }
};

}} // namespace thrive::test
