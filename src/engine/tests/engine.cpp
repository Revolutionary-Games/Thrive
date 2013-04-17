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
    TestEngine engine;
    engine.init();
    Entity::Id addedEntityId = Entity::NULL_ID;
    engine.sig_entityAdded.connect(
        [&addedEntityId] (Entity::Id entityId) {
            addedEntityId = entityId;
        }
    );
    Entity::Id id = Entity::generateNewId();
    auto component = std::make_shared<TestComponent<0>>();
    TestComponent<0>* rawComponent = component.get();
    EntityManager::instance().addComponent(id, std::move(component));
    EXPECT_EQ(Entity::NULL_ID, addedEntityId);
    engine.update();
    EXPECT_EQ(id, addedEntityId);
    EXPECT_EQ(rawComponent, engine.getComponent(id, TestComponent<0>::TYPE_ID));
}


TEST (Engine, RemoveComponent) {
    TestEngine engine;
    engine.init();
    Entity::Id removedEntityId = Entity::NULL_ID;
    engine.sig_entityRemoved.connect(
        [&removedEntityId] (Entity::Id entityId) {
            removedEntityId = entityId;
        }
    );
    Entity::Id id = Entity::generateNewId();
    EntityManager::instance().addComponent(id, std::make_shared<TestComponent<0>>());
    engine.update();
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    EntityManager::instance().removeComponent(id, TestComponent<0>::TYPE_ID);
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    engine.update();
    EXPECT_EQ(id, removedEntityId);
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID));
}


TEST (Engine, RemoveEntity) {
    TestEngine engine;
    engine.init();
    Entity::Id removedEntityId = Entity::NULL_ID;
    engine.sig_entityRemoved.connect(
        [&removedEntityId] (Entity::Id entityId) {
            removedEntityId = entityId;
        }
    );
    Entity::Id id = Entity::generateNewId();
    EntityManager::instance().addComponent(id, make_unique<TestComponent<0>>());
    engine.update();
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    EntityManager::instance().removeEntity(id);
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    engine.update();
    EXPECT_EQ(id, removedEntityId);
}


TEST (Engine, GetNullComponent) {
    TestEngine engine;
    Entity::Id id = Entity::generateNewId();
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent<0>::TYPE_ID));
}
