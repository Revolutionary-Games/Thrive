#pragma once

#include "engine/component.h"
#include "util/make_unique.h"

namespace thrive {

class StorageContainer;

class ComponentFactory {

public:

    using ComponentLoader = std::function<
        std::unique_ptr<Component>(const StorageContainer& storage)
    >;

    template<typename C>
    static typename std::enable_if<
        std::is_base_of<Component, C>::value, 
        bool
    >::type
    registerComponentType() {
        bool isNew = false;
        Registry& registry = ComponentFactory::registry();
        std::tie(std::ignore, isNew) = registry.insert({
            C::TYPE_NAME(),
            [](const StorageContainer& storage) {
                auto component = make_unique<C>();
                component->load(storage);
                return component;
            }
        });
        return isNew;
    }

    static std::unique_ptr<Component>
    load(
        const std::string& typeName,
        const StorageContainer& storage
    );

private:

    using Registry = std::unordered_map<std::string, ComponentLoader>;

    static Registry&
    registry();

};

/**
 * @brief Registers a component class with the ComponentFactory
 *
 * Use this in the component's source file.
 */
#define REGISTER_COMPONENT(cls) \
    static const bool cls ## _REGISTERED = thrive::ComponentFactory::registerComponentType<cls>();

}
