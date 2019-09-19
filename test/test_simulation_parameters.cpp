// Tests that simulation parameters can be loaded
#include "microbe_stage/simulation_parameters.h"

#include "LeviathanTest/PartialEngine.h"

#include "catch.hpp"

using namespace thrive;

TEST_CASE("Simulation parameters init works (no malformed json files)",
    "[scripts]")
{
    Leviathan::Test::TestLogger log("Test/test_log.txt");

    REQUIRE_NOTHROW(thrive::SimulationParameters::init());

    SECTION("Calling init again works (for benefit of testing it shouldn't "
            "matter how many times it is called")
    {
        REQUIRE_NOTHROW(thrive::SimulationParameters::init());
    }

    SECTION("Some data got loaded")
    {
        CHECK(SimulationParameters::compoundRegistry.getSize() > 0);
        CHECK(SimulationParameters::bioProcessRegistry.getSize() > 0);
        CHECK(SimulationParameters::biomeRegistry.getSize() > 0);
        CHECK(SimulationParameters::speciesNameController.cofixes_c.size() > 0);
        CHECK(SimulationParameters::speciesNameController.cofixes_v.size() > 0);
        REQUIRE(SimulationParameters::backgroundRegistry.getSize() > 0);
        CHECK(SimulationParameters::backgroundRegistry.getTypeData(0)
                  .layers.size() > 0);
    }
}
