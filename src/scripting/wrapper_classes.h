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
        Component* self
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

    static void default_update(
        System*,
        int,
        int
    ) {
        throw std::runtime_error("System::update has no default implementation");
    }
};

// StandardItemWrapper
class StandardItemWrapper{
public:
    //! @brief Constructs a wrapper around CEGUI::StandardItem(text, id)
    StandardItemWrapper(
        const std::string &text,
        int id
    );

    //! @brief Destroys the CEGUI object if it hasn't been attached
    ~StandardItemWrapper();

    //! @brief Returns the underlying CEGUI object
    CEGUI::StandardItem*
        getItem();

    //! Once attached CEGUI will handle deleting so this stops the desctructor from deleting
    void markAttached();

    
private:

    bool m_attached;
    CEGUI::StandardItem* m_item;
};

}
