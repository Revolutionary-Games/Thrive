#include "engine/engine.h"

#include "engine/tests/test_component.h"
#include "util/make_unique.h"

#include <gtest/gtest.h>
#include <iostream>

using namespace thrive;

namespace {

class TestEngine : public Engine {

public:

    void init() override {
        // Nothing
    }
};

}


TEST (Engine, AddComponent) {
    TestEngine engine;
    Entity::Id addedEntityId = Entity::NULL_ID;
    engine.sig_entityAdded.connect(
        [&addedEntityId] (Entity::Id entityId) {
            addedEntityId = entityId;
        }
    );
    Entity::Id id = Entity::generateNewId();
    auto component = make_unique<TestComponent>();
    TestComponent* rawComponent = component.get();
    engine.addComponent(id, std::move(component));
    EXPECT_EQ(Entity::NULL_ID, addedEntityId);
    engine.update();
    EXPECT_EQ(id, addedEntityId);
    EXPECT_EQ(rawComponent, engine.getComponent(id, TestComponent::TYPE_ID));
}


TEST (Engine, RemoveComponent) {
    TestEngine engine;
    Entity::Id removedEntityId = Entity::NULL_ID;
    engine.sig_entityRemoved.connect(
        [&removedEntityId] (Entity::Id entityId) {
            removedEntityId = entityId;
        }
    );
    Entity::Id id = Entity::generateNewId();
    engine.addComponent(id, make_unique<TestComponent>());
    engine.update();
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    engine.removeComponent(id, TestComponent::TYPE_ID);
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    engine.update();
    EXPECT_EQ(id, removedEntityId);
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent::TYPE_ID));
}


TEST (Engine, RemoveEntity) {
    TestEngine engine;
    Entity::Id removedEntityId = Entity::NULL_ID;
    engine.sig_entityRemoved.connect(
        [&removedEntityId] (Entity::Id entityId) {
            removedEntityId = entityId;
        }
    );
    Entity::Id id = Entity::generateNewId();
    engine.addComponent(id, make_unique<TestComponent>());
    engine.update();
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    engine.removeEntity(id);
    EXPECT_EQ(Entity::NULL_ID, removedEntityId);
    engine.update();
    EXPECT_EQ(id, removedEntityId);
}


TEST (Engine, GetNullComponent) {
    TestEngine engine;
    Entity::Id id = Entity::generateNewId();
    EXPECT_EQ(nullptr, engine.getComponent(id, TestComponent::TYPE_ID));
}
