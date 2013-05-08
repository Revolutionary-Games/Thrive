#include "engine/entity_filter.h"

#include "engine/entity_manager.h"
#include "engine/tests/test_component.h"
#include "util/make_unique.h"

#include <gtest/gtest.h>

using namespace thrive;

namespace {

class TestEngine : public Engine { };

}

TEST(EntityFilter, Initialization) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    // Add component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    engine.update();
    // Set up filter
    EntityFilter<TestComponent<0>> filter;
    filter.setEngine(&engine);
    // Check filter
    auto filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    engine.shutdown();
}

TEST(EntityFilter, Single) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    // Set up filter
    EntityFilter<TestComponent<0>> filter;
    filter.setEngine(&engine);
    // Add component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    engine.update();
    // Check filter
    auto filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<0>::TYPE_ID()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(0, filteredEntities.count(entityId));
    EXPECT_EQ(0, filteredEntities.size());
    engine.shutdown();
}


TEST(EntityFilter, Multiple) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    // Set up filter
    EntityFilter<
        TestComponent<0>,
        TestComponent<1>
    > filter;
    filter.setEngine(&engine);
    auto filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    // Add first component
    EXPECT_EQ(0, filteredEntities.count(entityId));
    EXPECT_EQ(0, filteredEntities.size());
    // Add second component
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<1>>()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<1>::TYPE_ID()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(0, filteredEntities.count(entityId));
    EXPECT_EQ(0, filteredEntities.size());
    engine.shutdown();
}


TEST(EntityFilter, Optional) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    using TestFilter = EntityFilter<
        TestComponent<0>,
        Optional<TestComponent<1>>
    >;
    // Set up filter
    TestFilter filter;
    filter.setEngine(&engine);
    TestFilter::EntityMap filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Check group
    TestFilter::ComponentGroup group = filteredEntities[entityId];
    EXPECT_TRUE(std::get<0>(group) != nullptr);
    EXPECT_TRUE(std::get<1>(group) == nullptr);
    // Add second component
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<1>>()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Check group
    group = filteredEntities[entityId];
    EXPECT_TRUE(std::get<0>(group) != nullptr);
    EXPECT_TRUE(std::get<1>(group) != nullptr);
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<1>::TYPE_ID()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Check group
    group = filteredEntities[entityId];
    EXPECT_TRUE(std::get<0>(group) != nullptr);
    engine.shutdown();
}


TEST(EntityFilter, OptionalOnly) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    using TestFilter = EntityFilter<
        Optional<TestComponent<0>>
    >;
    // Set up filter
    TestFilter filter;
    filter.setEngine(&engine);
    TestFilter::EntityMap filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Check group
    TestFilter::ComponentGroup group = filteredEntities[entityId];
    EXPECT_TRUE(std::get<0>(group) != nullptr);
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<0>::TYPE_ID()
    );
    engine.update();
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(0, filteredEntities.count(entityId));
    EXPECT_EQ(0, filteredEntities.size());
    engine.shutdown();
}


TEST(EntityFilter, Record) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    using TestFilter = EntityFilter<
        TestComponent<0>
    >;
    // Set up filter
    TestFilter filter(true);
    filter.setEngine(&engine);
    TestFilter::EntityMap filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    engine.update();
    // Check added entities
    EXPECT_EQ(1, filter.addedEntities().count(entityId));
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<0>::TYPE_ID()
    );
    engine.update();
    // Check removed entities
    EXPECT_EQ(1, filter.removedEntities().count(entityId));
    engine.shutdown();
}


