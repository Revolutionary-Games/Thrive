#pragma once

#include "engine/component.h"

#include <memory>

namespace thrive {

class ComponentFactory {

public:

    using ComponentConstructor = std::function<std::shared_ptr<Component>(void)>;

    static ComponentFactory&
    instance();

    std::shared_ptr<Component>
    create(
        const std::string& name
    );

    std::shared_ptr<Component>
    create(
        Component::TypeId typeId
    );

    template<typename C>
    bool
    registerClass() {
        return this->registerComponent(
            C::TYPE_ID(),
            C::TYPE_NAME(),
            []() -> std::shared_ptr<Component> {
                return std::make_shared<C>();
            }
        );
    }

    bool
    registerComponent(
        Component::TypeId typeId,
        const std::string& name,
        ComponentConstructor constructor
    );

    Component::TypeId
    typeNameToId(
        const std::string& name
    );

    std::string
    typeIdToName(
        Component::TypeId typeId
    );

private:

    ComponentFactory();

    ~ComponentFactory();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

#define REGISTER_COMPONENT(cls) \
    static const bool cls ## _REGISTERED = thrive::ComponentFactory::instance().registerClass<cls>();

}
