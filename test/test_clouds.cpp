//! Tests compound cloud operations that don't need Ogre
#include "microbe_stage/compound_cloud_system.h"


#include "catch.hpp"
using namespace thrive;

TEST_CASE("Cloud contains check is correct", "[microbe]")
{
    SECTION("within origin cloud")
    {
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(-20, 0, -40)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(-CLOUD_WIDTH, 0, -CLOUD_HEIGHT)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(0, 0, 0)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(40, 0, 40)));
    }

    SECTION("Right edge isn't part of a cloud, but the next one")
    {
        // One pos before the edge
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(CLOUD_WIDTH - 1, 0, 20)));
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(CLOUD_WIDTH * 2, 0, 0), Float3(CLOUD_WIDTH - 1, 0, 20)));

        // On the edge
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(CLOUD_WIDTH, 0, 20)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(CLOUD_WIDTH * 2, 0, 0), Float3(CLOUD_WIDTH, 0, 20)));

        // At the next cloud
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(CLOUD_WIDTH + 1, 0, 20)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(CLOUD_WIDTH * 2, 0, 0), Float3(CLOUD_WIDTH + 1, 0, 20)));
    }

    SECTION("Left edge is part of a cloud, and not the next one")
    {
        // One pos before the edge
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(-CLOUD_WIDTH + 1, 0, 20)));
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(-CLOUD_WIDTH * 2, 0, 0), Float3(-CLOUD_WIDTH + 1, 0, 20)));

        // On the edge
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(-CLOUD_WIDTH, 0, 20)));
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(-CLOUD_WIDTH * 2, 0, 0), Float3(-CLOUD_WIDTH, 0, 20)));

        // At the next cloud
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(-CLOUD_WIDTH - 1, 0, 20)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(-CLOUD_WIDTH * 2, 0, 0), Float3(-CLOUD_WIDTH - 1, 0, 20)));
    }

    SECTION("Bottom edge isn't part of a cloud, but the next one")
    {
        // One pos before the edge
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(0, 0, CLOUD_HEIGHT - 1)));
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, CLOUD_HEIGHT * 2), Float3(0, 0, CLOUD_HEIGHT - 1)));

        // On the edge
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(0, 0, CLOUD_HEIGHT)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, CLOUD_HEIGHT * 2), Float3(0, 0, CLOUD_HEIGHT)));

        // At the next cloud
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(0, 0, CLOUD_HEIGHT + 1)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, CLOUD_HEIGHT * 2), Float3(0, 0, CLOUD_HEIGHT + 1)));
    }

    SECTION("Top edge is part of a cloud, and not the next one")
    {
        // One pos before the edge
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(0, 0, -CLOUD_HEIGHT + 1)));
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, -CLOUD_HEIGHT * 2), Float3(0, 0, -CLOUD_HEIGHT + 1)));

        // On the edge
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(0, 0, -CLOUD_HEIGHT)));
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, -CLOUD_HEIGHT * 2), Float3(0, 0, -CLOUD_HEIGHT)));

        // At the next cloud
        CHECK(!CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, 0), Float3(0, 0, -CLOUD_HEIGHT - 1)));
        CHECK(CompoundCloudSystem::cloudContainsPosition(
            Float3(0, 0, -CLOUD_HEIGHT * 2), Float3(0, 0, -CLOUD_HEIGHT - 1)));
    }
}

TEST_CASE("Cloud local coordinate calculation math is right", "[microbe]")
{
    SECTION("Origin cloud")
    {
        // This assumes the cloud constants aren't tweaked
        CHECK(CompoundCloudSystem::convertWorldToCloudLocal(Float3(0, 0, 0),
                  Float3(-20, 0, -40)) == std::make_tuple(40, 30));

        CHECK(CompoundCloudSystem::convertWorldToCloudLocal(
                  Float3(0, 0, 0), Float3(-CLOUD_WIDTH, 0, -CLOUD_HEIGHT)) ==
              std::make_tuple(0, 0));

        CHECK(CompoundCloudSystem::convertWorldToCloudLocal(Float3(0, 0, 0),
                  Float3(CLOUD_WIDTH - 1, 0, CLOUD_HEIGHT - 1)) ==
              std::make_tuple(
                  CLOUD_SIMULATION_WIDTH - 1, CLOUD_SIMULATION_HEIGHT - 1));

        CHECK(
            CompoundCloudSystem::convertWorldToCloudLocal(Float3(0, 0, 0),
                Float3(0, 0, 0)) == std::make_tuple(CLOUD_SIMULATION_WIDTH / 2,
                                        CLOUD_SIMULATION_HEIGHT / 2));
    }

    SECTION("Out of range throws")
    {
        CHECK_THROWS_AS(CompoundCloudSystem::convertWorldToCloudLocal(
                            Float3(0, 0, 0), Float3(CLOUD_WIDTH, 0, 0)),
            Leviathan::InvalidArgument);
        CHECK_THROWS_AS(CompoundCloudSystem::convertWorldToCloudLocal(
                            Float3(0, 0, 0), Float3(0, 0, CLOUD_HEIGHT)),
            Leviathan::InvalidArgument);
        CHECK_THROWS_AS(CompoundCloudSystem::convertWorldToCloudLocal(
                            Float3(0, 0, 0), Float3(-CLOUD_WIDTH - 50, 0, 0)),
            Leviathan::InvalidArgument);
        // This may not sometimes throw when it is so little out of range that
        // the float to int conversion rounds it to 0
        CHECK_THROWS_AS(CompoundCloudSystem::convertWorldToCloudLocal(
                            Float3(0, 0, 0), Float3(-CLOUD_WIDTH - 1, 0, 0)),
            Leviathan::InvalidArgument);
    }
}
