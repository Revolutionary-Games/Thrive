#pragma once

#include "engine/component.h"
#include "util/make_unique.h"

namespace luabind {
    class scope;
}

namespace thrive {

class StorageContainer;

class ComponentFactory {

public:

    using ComponentLoader = std::function<
        std::unique_ptr<Component>(const StorageContainer& storage)
    >;

    static luabind::scope
    luaBindings();

    ComponentFactory();

    ~ComponentFactory();

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

    ComponentTypeId
    getTypeId(
        const std::string& name
    ) const;

    std::string
    getTypeName(
        ComponentTypeId typeId
    ) const;

    std::unique_ptr<Component>
    load(
        const std::string& typeName,
        const StorageContainer& storage
    ) const;

    ComponentTypeId
    registerComponentType(
        const std::string& name,
        ComponentLoader loader
    );

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
