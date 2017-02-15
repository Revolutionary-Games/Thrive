//! \file This file contains classes that are "inheritable" in Lua
#pragma once

#include "scripting/script_wrapper.h"


#include "engine/component.h"
#include "engine/system.h"


namespace CEGUI {

class StandardItem;
}

namespace thrive {
/**
* @brief Wrapper class to enable subclassing Component in Lua
*/
class ComponentWrapper : public Component, public ScriptWrapper {
public:
    
    ComponentWrapper(
        sol::table obj
    );

    void load(
        const StorageContainer& storage
    ) override;

    static void default_load(
        Component* self, 
        const StorageContainer& storage
    );

    ComponentTypeId
        typeId() const override;

    std::string
        typeName() const override;

    StorageContainer
        storage() const override;

    static StorageContainer default_storage(
        const Component* self
    );
};

/**
* @brief Wrapper class to enable subclassing System in Lua
*/
struct SystemWrapper : public System, public ScriptWrapper {

    SystemWrapper(sol::table obj);

    void init(
        GameState* gameState
    ) override;

    void initNamed(
        const std::string &name,
        GameState* gameState
    ) override;

    static void default_init(
        System* self,
        GameState* gameState
    ) {
        self->System::init(gameState);
    }

    static void default_initNamed(
        System* self,
        std::string name,
        GameState* gameState
    ) {
        self->System::initNamed(name, gameState);
    }

    void shutdown() override;

    static void default_shutdown(
        System* self
    ) {
        self->System::shutdown();
    }

    static void default_activate(
        System* self
    ) {
        self->System::activate();
    }

    void activate() override;

    static void default_deactivate(
        System* self
    ) {
        self->System::deactivate();
    }

    void deactivate() override;

    void update(
        int renderTime,
        int logicTime
    ) override;
};

}
