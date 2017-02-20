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

}
