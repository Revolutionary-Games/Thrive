#include "engine/entity.h"

#include "engine/entity_manager.h"
#include "engine/tests/test_component.h"
#include "util/make_unique.h"

#include <gtest/gtest.h>


using namespace thrive;


struct EntityTest : public ::testing::Test {

    EntityManager entityManager;

};

TEST_F(EntityTest, Exists) {
    // Null Id should never exist
    Entity nullEntity(NULL_ENTITY, entityManager);
    EXPECT_FALSE(nullEntity.exists());
    // Entity without components doesn't exist either
    Entity entity(entityManager);
    EXPECT_FALSE(entity.exists());
    // Add some component, then it should exist
    entity.addComponent(make_unique<TestComponent<0>>());
    EXPECT_TRUE(entity.exists());
}


TEST_F(EntityTest, HasComponent) {
    Entity entity(entityManager);
    EXPECT_FALSE(entity.hasComponent(TestComponent<0>::TYPE_ID));
    entity.addComponent(make_unique<TestComponent<0>>());
    EXPECT_TRUE(entity.hasComponent(TestComponent<0>::TYPE_ID));
}


TEST_F(EntityTest, RemoveComponent) {
    Entity entity(entityManager);
    EXPECT_FALSE(entity.hasComponent(TestComponent<0>::TYPE_ID));
    entity.addComponent(make_unique<TestComponent<0>>());
    EXPECT_TRUE(entity.hasComponent(TestComponent<0>::TYPE_ID));
    entity.removeComponent(TestComponent<0>::TYPE_ID);
    entityManager.processRemovals();
    EXPECT_FALSE(entity.hasComponent(TestComponent<0>::TYPE_ID));
}


TEST_F(EntityTest, NamedEntity) {
    Entity unnamed(entityManager);
    Entity named("named", entityManager);
    Entity namedCopy("named", entityManager);
    EXPECT_FALSE(named == unnamed);
    EXPECT_TRUE(named == namedCopy);
}
