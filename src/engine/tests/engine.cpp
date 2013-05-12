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
    EntityId id = entityManager.generateNewId();
    auto component = std::make_shared<TestComponent<0>>();
    TestComponent<0>* rawComponent = component.get();
    entityManager.addComponent(id, std::move(component));
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    engine.update();
    EXPECT_EQ(rawComponent, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    engine.shutdown();
}


TEST (Engine, RemoveComponent) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    EntityId id = entityManager.generateNewId();
    auto component = std::make_shared<TestComponent<0>>();
    TestComponent<0>* rawComponent = component.get();
    // Add a component
    entityManager.addComponent(id, component);
    engine.update();
    EXPECT_EQ(rawComponent, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    // Schedule it for removal
    entityManager.removeComponent(id, TestComponent<0>::TYPE_ID());
    // Shouldn't be removed yet
    EXPECT_EQ(rawComponent, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    // Actually remove it
    engine.update();
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    engine.shutdown();
}


TEST (Engine, RemoveEntity) {
    EntityManager entityManager;
    TestEngine engine;
    engine.init(&entityManager);
    // Add two components
    EntityId id = entityManager.generateNewId();
    entityManager.addComponent(id, make_unique<TestComponent<0>>());
    entityManager.addComponent(id, make_unique<TestComponent<1>>());
    engine.update();
    // Remove both of them
    entityManager.removeEntity(id);
    // Shouldn't be removed yet
    EXPECT_TRUE(nullptr != engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    EXPECT_TRUE(nullptr != engine.getComponent(id, TestComponent<1>::TYPE_ID()));
    // Actually remove them
    engine.update();
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<1>::TYPE_ID()));
    engine.shutdown();
}


TEST (Engine, GetNullComponent) {
    EntityManager entityManager;
    TestEngine engine;
    EntityId id = entityManager.generateNewId();
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID()));
}
