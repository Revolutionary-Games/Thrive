#pragma once

#include "engine/component.h"
#include "util/make_unique.h"

namespace sol {
class state;
}

namespace thrive {

class StorageContainer;

/**
* @brief A factory for components
*
* Duh.
*/
class ComponentFactory {

public:

    /**
    * @brief Typedef for a component factory function
    *
    * Takes a storage container and returns a std::unique_ptr to the new 
    * component.
    */
    using ComponentLoader = std::function<
        std::unique_ptr<Component>(const StorageContainer& storage)
    >;

    /**
    * @brief Lua bindings
    *
    * - ComponentFactory::registerComponentType
    *
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    *
    * Usually only used by the Engine.
    */
    ComponentFactory();

    /**
    * @brief Destructor
    */
    ~ComponentFactory();

    /**
    * @brief Registers a component type for all factories
    *
    * @tparam C
    *   The subclass of Component.
    *
    * @return The type's unique id
    *
    * @note
    *   You should probably use the REGISTER_COMPONENT macro instead of 
    *   calling this directly.
    *
    */
    template<typename C>
    static ComponentTypeId
    registerGlobalComponentType() {
        return ComponentFactory::registerGlobalComponentType(
            C::TYPE_NAME(),
            [](const StorageContainer& storage) {
                std::unique_ptr<Component> component = make_unique<C>();
                component->load(storage);
                return component;
            }
        );
    }

    /**
    * @brief Looks up a component type name and returns its id
    *
    * @param name
    *   The component type name
    *
    * @return 
    *   The component type's unique id or NULL_COMPONENT_TYPE if the type name 
    *   is not registered.
    *
    */
    ComponentTypeId
    getTypeId(
        const std::string& name
    ) const;

    /**
    * @brief Looks up a component type id and returns its name
    *
    * @param typeId
    *   The component type id
    *
    * @return
    *   The component type name or an empty string if the type id is unknown.
    */
    std::string
    getTypeName(
        ComponentTypeId typeId
    ) const;

    /**
    * @brief Loads a component from storage
    *
    * @param typeName
    *   The name of the component type
    *
    * @param storage
    *   The component's storage container. May be empty.
    *
    * @return 
    *   A new component
    */
    std::unique_ptr<Component>
    load(
        const std::string& typeName,
        const StorageContainer& storage
    ) const;

    /**
    * @brief Registers a component type with this factory
    *
    * @param name
    *   The type name
    * @param loader
    *   The function used for loading this component type
    *
    * @return 
    *   The type's unique id
    */
    ComponentTypeId
    registerComponentType(
        const std::string& name,
        ComponentLoader loader
    );

    /**
    * @brief Unregisters a component type
    *
    * @param name
    *   The type's name
    */
    void
    unregisterComponentType(
        const std::string& name
    );

private:

    static ComponentTypeId
    registerGlobalComponentType(
        const std::string& name,
        ComponentLoader loader
    );

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

/**
 * @brief Registers a component class with the ComponentFactory
 *
 * Use this in the component's source file.
 */
#define REGISTER_COMPONENT(cls) \
    const ComponentTypeId cls::TYPE_ID = thrive::ComponentFactory::registerGlobalComponentType<cls>();

}
