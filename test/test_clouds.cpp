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

TEST_CASE("CloudManager grid center calculation", "[microbe]")
{
    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(0, 0, 0)) == Float3(0, 0, 0));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(CLOUD_WIDTH - 1, 0, 0)) == Float3(0, 0, 0));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(CLOUD_WIDTH, 0, 0)) == Float3(CLOUD_X_EXTENT, 0, 0));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(CLOUD_WIDTH + 1, 0, 0)) == Float3(CLOUD_X_EXTENT, 0, 0));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(CLOUD_WIDTH, 0, CLOUD_HEIGHT)) ==
          Float3(CLOUD_X_EXTENT, 0, CLOUD_Y_EXTENT));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(CLOUD_WIDTH, 0, CLOUD_HEIGHT * 2)) ==
          Float3(CLOUD_X_EXTENT, 0, CLOUD_Y_EXTENT));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(-CLOUD_X_EXTENT, 0, 0)) == Float3(-CLOUD_Y_EXTENT, 0, 0));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(
              Float3(-CLOUD_WIDTH, 0, -CLOUD_HEIGHT)) ==
          Float3(-CLOUD_X_EXTENT, 0, -CLOUD_Y_EXTENT));

    CHECK(CompoundCloudSystem::calculateGridCenterForPlayerPos(Float3(
              -CLOUD_WIDTH + 1, 0, -CLOUD_HEIGHT + 1)) == Float3(0, 0, 0));
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

    void
        movePlayerXUnits(float amount)
    {
        // Move player
        playerPos->Members._Position.X += amount;
        playerPos->Marked = true;

        // And tick
        world.Tick(1);
    }

protected:
    Leviathan::Test::PartialEngine<false> engine;
    TestThriveGame thrive{&engine};
    Leviathan::IDFactory ids;

    CellStageWorld world{nullptr};

    Leviathan::Position* playerPos = nullptr;
    ObjectID player = NULL_OBJECT;
};

template<class PosArrayT>
auto
    simpleCloudPosCheck(const std::vector<CompoundCloudComponent*>& clouds,
        const PosArrayT& targetPositions)
{
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

    return valid;
}

//! A pretty inefficient but simple way to calculate how many clouds are at the
//! same pos and what types
auto
    calculateCloudsAtSamePos(const std::vector<CompoundCloudComponent*>& clouds)
{
    std::map<std::string, std::vector<CompoundId>> counts;

    for(auto cloud : clouds) {

        std::stringstream stream;
        stream << cloud->getPosition();

        if(counts.find(stream.str()) == counts.end()) {
            counts.insert(std::make_pair(stream.str(),
                std::vector<CompoundId>{cloud->getCompoundId1()}));
        } else {
            counts[stream.str()].push_back(cloud->getCompoundId1());
        }
    }

    return counts;
}

template<size_t Count>
void
    checkCloudsAtPos(
        const std::map<std::string, std::vector<CompoundId>>& counts,
        const std::array<CompoundId, Count>& types)
{
    CAPTURE(types);

    for(const auto& pair : counts) {
        CAPTURE(pair.first);
        CAPTURE(pair.second);

        CHECK(pair.second.size() == types.size());

        std::vector<bool> foundStatuses;
        foundStatuses.resize(types.size(), false);

        for(auto id : pair.second) {
            for(size_t i = 0; i < types.size(); ++i) {
                if(id == types[i]) {

                    CHECK(!foundStatuses[i]);
                    foundStatuses[i] = true;
                    break;
                }
            }
        }

        CAPTURE(foundStatuses);
        for(bool found : foundStatuses)
            CHECK(found);
    }
}

template<size_t OverlappingCount, class PosArrayT>
auto
    multiOverlapCloudPosCheck(
        const std::vector<CompoundCloudComponent*>& clouds,
        const PosArrayT& targetPositions,
        const std::array<CompoundId, OverlappingCount>& cloudFirstTypes)
{
    std::vector<std::array<bool, OverlappingCount>> valid;

    std::array<bool, OverlappingCount> falseArray;
    for(bool& entry : falseArray)
        entry = false;

    valid.resize(targetPositions.size(), falseArray);

    for(size_t i = 0; i < targetPositions.size(); ++i) {

        CAPTURE(i);

        const auto& pos = targetPositions[i];

        for(CompoundCloudComponent* cloud : clouds) {

            if(cloud->getPosition() == pos) {

                CAPTURE(cloud->getCompoundId1(), pos, cloudFirstTypes);

                bool matched = false;

                // Check which group it is
                for(size_t targetTypeIndex = 0;
                    targetTypeIndex < cloudFirstTypes.size();
                    ++targetTypeIndex) {

                    if(cloud->getCompoundId1() ==
                        cloudFirstTypes[targetTypeIndex]) {

                        // Position check
                        CHECK(!valid[i][targetTypeIndex]);
                        valid[i][targetTypeIndex] = true;
                        matched = true;
                        break;
                    }
                }

                CHECK(matched);
            }
        }
    }

    return valid;
}

template<size_t OverlappingCount>
void
    multiCloudPositionCheckHelper(
        const std::vector<CompoundCloudComponent*>& clouds,
        const Float3& playerPos,
        const std::array<CompoundId, OverlappingCount>& cloudFirstTypes)
{
    // There needs to be 2 clouds at each position
    checkCloudsAtPos(calculateCloudsAtSamePos(clouds), cloudFirstTypes);

    // And then check that update succeeded
    const auto valid = multiOverlapCloudPosCheck(clouds,
        CompoundCloudSystem::calculateGridPositions(
            CompoundCloudSystem::calculateGridCenterForPlayerPos(playerPos)),
        cloudFirstTypes);

    for(const auto& entry : valid) {
        CAPTURE(entry);
        for(auto subentry : entry)
            CHECK(subentry);
    }
}

TEST_CASE_METHOD(CloudManagerTestsFixture,
    "Cloud manager creates and positions clouds with 1 compound type",
    "[microbe]")
{
    setCloudsAndRunInitial(
        {Compound{1, "a", true, true, false, Ogre::ColourValue(0, 1, 2, 3)}});

    // Find the cloud entities
    const auto clouds = findClouds();

    CHECK(clouds.size() == 9);

    // Check that cloud positioning has worked
    const auto valid = simpleCloudPosCheck(
        clouds, CompoundCloudSystem::calculateGridPositions(Float3(0, 0, 0)));

    for(bool entry : valid)
        CHECK(entry);
}

TEST_CASE_METHOD(CloudManagerTestsFixture,
    "Cloud manager creates and positions clouds with 5 compound types",
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

    std::array<CompoundId, 2> cloudFirstTypes;
    cloudFirstTypes[0] = types[0].id;
    cloudFirstTypes[1] = types[4].id;

    setCloudsAndRunInitial(types);

    // Find the cloud entities
    const auto clouds = findClouds();

    CHECK(clouds.size() == 18);

    multiCloudPositionCheckHelper(
        clouds, playerPos->Members._Position, cloudFirstTypes);
}


TEST_CASE_METHOD(CloudManagerTestsFixture,
    "Cloud manager repositions on player move clouds with 1 compound type",
    "[microbe]")
{
    constexpr auto PLAYER_MOVE_AMOUNT = 1000;

    setCloudsAndRunInitial(
        {Compound{1, "a", true, true, false, Ogre::ColourValue(0, 1, 2, 3)}});

    // Find the cloud entities
    const auto clouds = findClouds();

    CHECK(clouds.size() == 9);

    // Check that cloud positioning has worked
    auto valid = simpleCloudPosCheck(
        clouds, CompoundCloudSystem::calculateGridPositions(Float3(0, 0, 0)));

    for(bool entry : valid)
        CHECK(entry);

    // Move player
    playerPos->Members._Position.X += PLAYER_MOVE_AMOUNT;
    playerPos->Marked = true;

    // And tick
    world.Tick(1);

    // And then check that update succeeded
    valid = simpleCloudPosCheck(
        clouds, CompoundCloudSystem::calculateGridPositions(
                    CompoundCloudSystem::calculateGridCenterForPlayerPos(
                        Float3(PLAYER_MOVE_AMOUNT, 0, 0))));

    for(bool entry : valid)
        CHECK(entry);
}

TEST_CASE_METHOD(CloudManagerTestsFixture,
    "Cloud manager repositions on player move clouds with 5 compound types",
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

    std::array<CompoundId, 2> cloudFirstTypes;
    cloudFirstTypes[0] = types[0].id;
    cloudFirstTypes[1] = types[4].id;

    setCloudsAndRunInitial(types);

    // Find the cloud entities
    const auto clouds = findClouds();

    CHECK(clouds.size() == 18);

    multiCloudPositionCheckHelper(
        clouds, playerPos->Members._Position, cloudFirstTypes);

    // SECTION("Moving CLOUD_WIDTH/2 units") {}

    SECTION("Moving 1000 units")
    {
        constexpr auto PLAYER_MOVE_AMOUNT = 1000;

        movePlayerXUnits(PLAYER_MOVE_AMOUNT);

        multiCloudPositionCheckHelper(
            clouds, playerPos->Members._Position, cloudFirstTypes);
    }

    // SECTION("Moving CLOUD_WIDTH * 1.5 units") {}
}
