#include "engine/entity.h"

#include "engine/entity_manager.h"
#include "scripting/on_update.h"

#include <gtest/gtest.h>


using namespace thrive;


struct EntityTest : public ::testing::Test {

    EntityManager entityManager;

};

TEST_F(EntityTest, Exists) {
    // Null Id should never exist
    Entity nullEntity(EntityManager::NULL_ID, entityManager);
    EXPECT_FALSE(nullEntity.exists());
    // Entity without components doesn't exist either
    Entity entity(entityManager);
    EXPECT_FALSE(entity.exists());
    // Add some component, then it should exist
    entity.addComponent(std::make_shared<OnUpdateComponent>());
    EXPECT_TRUE(entity.exists());
}


TEST_F(EntityTest, HasComponent) {
    Entity entity(entityManager);
    EXPECT_FALSE(entity.hasComponent(OnUpdateComponent::TYPE_ID()));
    entity.addComponent(std::make_shared<OnUpdateComponent>());
    EXPECT_TRUE(entity.hasComponent(OnUpdateComponent::TYPE_ID()));
}


TEST_F(EntityTest, RemoveComponent) {
    Entity entity(entityManager);
    EXPECT_FALSE(entity.hasComponent(OnUpdateComponent::TYPE_ID()));
    entity.addComponent(std::make_shared<OnUpdateComponent>());
    EXPECT_TRUE(entity.hasComponent(OnUpdateComponent::TYPE_ID()));
    entity.removeComponent(OnUpdateComponent::TYPE_ID());
    EXPECT_FALSE(entity.hasComponent(OnUpdateComponent::TYPE_ID()));
}


TEST_F(EntityTest, NamedEntity) {
    Entity unnamed(entityManager);
    Entity named("named", entityManager);
    Entity namedCopy("named", entityManager);
    EXPECT_FALSE(named == unnamed);
    EXPECT_TRUE(named == namedCopy);
}
