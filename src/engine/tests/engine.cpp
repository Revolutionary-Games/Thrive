#include "engine/engine.h"

#include "engine/entity_manager.h"
#include "engine/tests/test_component.h"
#include "util/make_unique.h"

#include <gtest/gtest.h>
#include <iostream>

using namespace thrive;

namespace {

class TestEngine : public Engine { };

}


TEST (Engine, AddComponent) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    EntityId addedEntityId = EntityManager::NULL_ID;
    engine.sig_entityAdded.connect(
        [&addedEntityId] (EntityId entityId) {
            addedEntityId = entityId;
        }
    );
    EntityId id = entityManager.generateNewId();
    auto component = std::make_shared<TestComponent<0>>();
    TestComponent<0>* rawComponent = component.get();
    entityManager.addComponent(id, std::move(component));
    EXPECT_EQ(EntityManager::NULL_ID, addedEntityId);
    engine.update();
    EXPECT_EQ(id, addedEntityId);
    EXPECT_EQ(rawComponent, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    engine.shutdown();
}


TEST (Engine, RemoveComponent) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    EntityId removedEntityId = EntityManager::NULL_ID;
    engine.sig_entityRemoved.connect(
        [&removedEntityId] (EntityId entityId) {
            removedEntityId = entityId;
        }
    );
    EntityId id = entityManager.generateNewId();
    entityManager.addComponent(id, std::make_shared<TestComponent<0>>());
    engine.update();
    EXPECT_EQ(EntityManager::NULL_ID, removedEntityId);
    entityManager.removeComponent(id, TestComponent<0>::TYPE_ID());
    EXPECT_EQ(EntityManager::NULL_ID, removedEntityId);
    engine.update();
    EXPECT_EQ(id, removedEntityId);
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    engine.shutdown();
}


TEST (Engine, RemoveEntity) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    EntityId removedEntityId = EntityManager::NULL_ID;
    engine.sig_entityRemoved.connect(
        [&removedEntityId] (EntityId entityId) {
            removedEntityId = entityId;
        }
    );
    EntityId id = entityManager.generateNewId();
    entityManager.addComponent(id, make_unique<TestComponent<0>>());
    engine.update();
    EXPECT_EQ(EntityManager::NULL_ID, removedEntityId);
    entityManager.removeEntity(id);
    EXPECT_EQ(EntityManager::NULL_ID, removedEntityId);
    engine.update();
    EXPECT_EQ(id, removedEntityId);
    engine.shutdown();
}


TEST (Engine, GetNullComponent) {
    EntityManager entityManager;
    TestEngine engine;
    EntityId id = entityManager.generateNewId();
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
}
