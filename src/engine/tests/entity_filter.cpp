#include "engine/entity_filter.h"

#include "engine/entity_manager.h"
#include "engine/tests/test_component.h"
#include "util/make_unique.h"

#include <gtest/gtest.h>

using namespace thrive;

TEST(EntityFilter, Initialization) {
    EntityManager entityManager;
    // Add component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        std::make_shared<TestComponent<0>>()
    );
    EXPECT_TRUE(nullptr != entityManager.getComponent(entityId, TestComponent<0>::TYPE_ID()));
    // Set up filter
    EntityFilter<TestComponent<0>> filter;
    filter.setEntityManager(&entityManager);
    // Check filter
    auto filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
}

TEST(EntityFilter, Single) {
    EntityManager entityManager;
    // Set up filter
    EntityFilter<TestComponent<0>> filter;
    filter.setEntityManager(&entityManager);
    // Add component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    // Check filter
    auto filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<0>::TYPE_ID()
    );
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(0, filteredEntities.count(entityId));
    EXPECT_EQ(0, filteredEntities.size());
}


TEST(EntityFilter, Multiple) {
    EntityManager entityManager;
    // Set up filter
    EntityFilter<
        TestComponent<0>,
        TestComponent<1>
    > filter;
    filter.setEntityManager(&entityManager);
    auto filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
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
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<1>::TYPE_ID()
    );
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(0, filteredEntities.count(entityId));
    EXPECT_EQ(0, filteredEntities.size());
}


TEST(EntityFilter, Optional) {
    EntityManager entityManager;
    using TestFilter = EntityFilter<
        TestComponent<0>,
        Optional<TestComponent<1>>
    >;
    // Set up filter
    TestFilter filter;
    filter.setEntityManager(&entityManager);
    TestFilter::EntityMap filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
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
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(1, filteredEntities.count(entityId));
    EXPECT_EQ(1, filteredEntities.size());
    // Check group
    group = filteredEntities[entityId];
    EXPECT_TRUE(std::get<0>(group) != nullptr);
}


TEST(EntityFilter, OptionalOnly) {
    EntityManager entityManager;
    using TestFilter = EntityFilter<
        Optional<TestComponent<0>>
    >;
    // Set up filter
    TestFilter filter;
    filter.setEntityManager(&entityManager);
    TestFilter::EntityMap filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
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
    // Check filter
    filteredEntities = filter.entities();
    EXPECT_EQ(0, filteredEntities.count(entityId));
    EXPECT_EQ(0, filteredEntities.size());
}


TEST(EntityFilter, Record) {
    EntityManager entityManager;
    using TestFilter = EntityFilter<
        TestComponent<0>
    >;
    // Set up filter
    TestFilter filter(true);
    filter.setEntityManager(&entityManager);
    TestFilter::EntityMap filteredEntities = filter.entities();
    // Add first component
    EntityId entityId = entityManager.generateNewId();
    entityManager.addComponent(
        entityId,
        make_unique<TestComponent<0>>()
    );
    // Check added entities
    EXPECT_EQ(1, filter.addedEntities().count(entityId));
    // Remove component
    entityManager.removeComponent(
        entityId,
        TestComponent<0>::TYPE_ID()
    );
    // Check removed entities
    EXPECT_EQ(1, filter.removedEntities().count(entityId));
}


