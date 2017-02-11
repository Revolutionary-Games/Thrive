//! \file This file contains classes that are "inheritable" in Lua
#pragma once

#include "scripting/script_wrapper.h"


#include "engine/component.h"


namespace CEGUI {

class StandardItem;
}

namespace thrive {
/**
* @brief Wrapper class to enable subclassing Component in Lua
*
* \cond
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
