//! Tests compound cloud operations that don't need Ogre
#include "engine/player_data.h"
#include "generated/cell_stage_world.h"
#include "microbe_stage/compound_cloud_system.h"
#include "test_thrive_game.h"

#include <Entities/Components.h>
#include <LeviathanTest/PartialEngine.h>

#include "catch.hpp"
using namespace thrive;
using namespace thrive::test;

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

class CloudManagerTestsFixture {
public:
    CloudManagerTestsFixture()
    {
        thrive.lightweightInit();

        world.SetRunInBackground(true);

        // TODO: change type when the clouds are made to run again with the
        // variable rate ticks
        REQUIRE(world.Init(
            Leviathan::WorldNetworkSettings::GetSettingsForHybrid(), nullptr));

        // Create player pos
        player = world.CreateEntity();

        thrive.playerData().setActiveCreature(player);

        REQUIRE_NOTHROW(playerPos = &world.Create_Position(player,
                            Float3(0, 0, 0), Float4::IdentityQuaternion()));
    }
    ~CloudManagerTestsFixture()
    {
        world.Release();
    }

    void
        setCloudsAndRunInitial(const std::vector<Compound>& cloudTypes)
    {
        world.GetCompoundCloudSystem().registerCloudTypes(world, cloudTypes);

        // Let it run a bit
        world.Tick(1);
    }

    std::vector<CompoundCloudComponent*>
        findClouds()
    {
        std::vector<CompoundCloudComponent*> clouds;

        for(ObjectID entity : world.GetEntities()) {

            if(entity == player)
                continue;

            REQUIRE_NOTHROW(clouds.emplace_back(
                &world.GetComponent_CompoundCloudComponent(entity)));
        }

        return clouds;
    }

protected:
    Leviathan::Test::PartialEngine<false> engine;
    TestThriveGame thrive;
    Leviathan::IDFactory ids;

    CellStageWorld world{nullptr};

    Leviathan::Position* playerPos = nullptr;
    ObjectID player = NULL_OBJECT;
};

TEST_CASE_METHOD(CloudManagerTestsFixture,
    "Cloud manager creates and positions clouds with 1 compound type correctly",
    "[microbe]")
{
    setCloudsAndRunInitial(
        {Compound{1, "a", true, true, false, Ogre::ColourValue(0, 1, 2, 3)}});

    // Find the cloud entities
    const auto clouds = findClouds();

    CHECK(clouds.size() == 9);

    // Check that cloud positioning has worked
    const auto targetPositions{
        CompoundCloudSystem::calculateGridPositions(Float3(0, 0, 0))};

    std::vector<bool> valid;
    valid.resize(targetPositions.size(), false);

    for(size_t i = 0; i < targetPositions.size(); ++i) {

        const auto& pos = targetPositions[i];

        for(CompoundCloudComponent* cloud : clouds) {

            if(cloud->getPosition() == pos) {

                CHECK(!valid[i]);
                valid[i] = true;
            }
        }
    }

    for(bool entry : valid)
        CHECK(entry);
}

TEST_CASE_METHOD(CloudManagerTestsFixture,
    "Cloud manager creates and positions clouds with 5 compound types "
    "correctly",
    "[microbe]")
{
    // This test assumes
    static_assert(CLOUDS_IN_ONE == 4, "this test assumes this");

    const std::vector<Compound> types{
        Compound{1, "a", true, true, false, Ogre::ColourValue(0, 1, 2, 1)},
        Compound{2, "b", true, true, false, Ogre::ColourValue(3, 4, 5, 1)},
        Compound{3, "c", true, true, false, Ogre::ColourValue(6, 7, 8, 1)},
        Compound{4, "d", true, true, false, Ogre::ColourValue(9, 10, 11, 1)},
        Compound{5, "e", true, true, false, Ogre::ColourValue(12, 13, 14, 1)}};

    setCloudsAndRunInitial(types);

    // Find the cloud entities
    const auto clouds = findClouds();

    CHECK(clouds.size() == 18);

    // Check that cloud positioning has worked
    const auto targetPositions{
        CompoundCloudSystem::calculateGridPositions(Float3(0, 0, 0))};

    std::vector<std::array<bool, 2>> valid;
    valid.resize(targetPositions.size(), {false, false});

    for(size_t i = 0; i < targetPositions.size(); ++i) {

        const auto& pos = targetPositions[i];

        for(CompoundCloudComponent* cloud : clouds) {

            if(cloud->getPosition() == pos) {

                // First or second
                CAPTURE(cloud->getCompoundId1());
                if(cloud->getCompoundId1() == types[0].id) {

                    // Make sure the rest of the cloud entries are also right
                    CHECK(cloud->getCompoundId2() == types[1].id);
                    CHECK(cloud->getCompoundId3() == types[2].id);
                    CHECK(cloud->getCompoundId4() == types[3].id);

                    // Position check
                    CHECK(!valid[i][0]);
                    valid[i][0] = true;

                } else if(cloud->getCompoundId1() == types[4].id) {

                    // Make sure the rest of the cloud entries are also right
                    CHECK(cloud->getCompoundId2() == NULL_COMPOUND);
                    CHECK(cloud->getCompoundId3() == NULL_COMPOUND);
                    CHECK(cloud->getCompoundId4() == NULL_COMPOUND);

                    // Position check
                    CHECK(!valid[i][1]);
                    valid[i][1] = true;

                } else {
                    FAIL_CHECK("cloud has unexpected id");
                }
            }
        }
    }

    for(const auto& entry : valid) {
        CHECK(entry[0]);
        CHECK(entry[1]);
    }
}
