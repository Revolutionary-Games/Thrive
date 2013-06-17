#include "scripting/on_update.h"

#include "engine/entity_manager.h"
#include "game.h"
#include "scripting/lua_state.h"
#include "scripting/script_engine.h"

#include <gtest/gtest.h>
#include <iostream>
#include <luabind/luabind.hpp>

using namespace thrive;

TEST(OnUpdate, NilCallback) {
    // Setup
    LuaState luaState;
    EntityManager entityManager;
    ScriptEngine engine(entityManager, luaState);
    engine.init();
    // Add component
    EntityId entityId = entityManager.generateNewId();
    auto onUpdate = std::make_shared<OnUpdateComponent>();
    entityManager.addComponent(
        entityId,
        onUpdate
    );
    // Update
    engine.update(1);
    // I'm content with not crashing here
    engine.shutdown();
}

TEST(OnUpdate, Callback) {
    // Setup
    LuaState luaState;
    EntityManager entityManager;
    ScriptEngine engine(entityManager, luaState);
    engine.init();
    EXPECT_TRUE(luaState.doString(
        "called = 0\n"
        "function callback(entityId, milliseconds)\n"
        "   called = called + 1\n"
        "end"
    ));
    // Add component
    EntityId entityId = entityManager.generateNewId();
    auto onUpdate = std::make_shared<OnUpdateComponent>();
    entityManager.addComponent(
        entityId,
        onUpdate
    );
    luabind::object globals = luabind::globals(luaState);
    onUpdate->m_callback = globals["callback"];
    // Check that "called" is 0
    int called = luabind::object_cast<int>(globals["called"]);
    EXPECT_EQ(0, called);
    // Update
    engine.update(1);
    called = luabind::object_cast<int>(globals["called"]);
    EXPECT_EQ(1, called);
    engine.shutdown();
}


TEST(OnUpdate, SetCallbackFromLua) {
    // Setup
    LuaState luaState;
    luabind::object globals = luabind::globals(luaState);
    luabind::open(luaState);
    luabind::module(luaState)[
        Component::luaBindings(),
        OnUpdateComponent::luaBindings()
    ];
    EntityManager entityManager;
    ScriptEngine engine(entityManager, luaState);
    engine.init();
    EXPECT_TRUE(luaState.doString(
        "called = 0\n"
        "function callback(entityId, milliseconds)\n"
        "   called = called + 1\n"
        "end"
    ));
    // Add component
    EntityId entityId = entityManager.generateNewId();
    auto onUpdate = std::make_shared<OnUpdateComponent>();
    entityManager.addComponent(
        entityId,
        onUpdate
    );
    // Set callback
    globals["onUpdateComponent"] = onUpdate.get();
    EXPECT_TRUE(luaState.doString(
        "onUpdateComponent.callback = callback"
    ));
    // Check that "called" is 0
    int called = -1;
    called = luabind::object_cast<int>(globals["called"]);
    EXPECT_EQ(0, called);
    // Update
    engine.update(1);
    called = luabind::object_cast<int>(globals["called"]);
    EXPECT_EQ(1, called);
    // Unset callback
    EXPECT_TRUE(luaState.doString(
        "onUpdateComponent.callback = nil"
    ));
    // Update
    engine.update(1);
    called = luabind::object_cast<int>(globals["called"]);
    EXPECT_EQ(1, called);
    engine.shutdown();
}
