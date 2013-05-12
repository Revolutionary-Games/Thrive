#pragma once

#include "engine/component.h"

#include <memory>

namespace thrive {

/**
* @brief Produces components by name
*/
class ComponentFactory {

public:

    /**
    * @brief Produces shared pointers of components
    *
    * shared_ptr was selected over unique_ptr here because the EntityManager
    * and the ComponentCollection instances will need shared pointers. 
    * Constructing a shared pointer from the start will allow us to use
    * std::make_shared, improving cache coherency.
    */
    using ComponentConstructor = std::function<std::shared_ptr<Component>(void)>;

    /**
    * @brief Returns the singleton instance
    */
    static ComponentFactory&
    instance();

    /**
    * @brief Creates a component by name
    *
    * @param name The component's type name
    *
    * @return A new component
    *
    * @throws std::invalid_argument if the name is unknown
    *
    * @note The type id overload should be preferred for performance reasons
    */
    std::shared_ptr<Component>
    create(
        const std::string& name
    );

    /**
    * @brief Creates a component by type id
    *
    * @param name The component's type id
    *
    * @return A new component
    *
    * @throws std::invalid_argument if the id is unknown
    */
    std::shared_ptr<Component>
    create(
        Component::TypeId typeId
    );

    /**
    * @brief Registers a component by class
    *
    * @tparam C
    *   A class derived from Component
    *
    * @return 
    *   \c true if the registration was successful, \c false otherwise
    */
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

    /**
    * @brief Registers a component type
    *
    * @param typeId
    *   The component's type id
    * @param name
    *   The component's name (e.g. for scripts)
    * @param constructor
    *   An \c std::function taking \c void and returning a shared
    *   pointer to a new component.
    *
    * @return \c true if the registration was successful, false otherwise
    *
    * @note
    *   Use the REGISTER_COMPONENT macro for easier registration
    */
    bool
    registerComponent(
        Component::TypeId typeId,
        const std::string& name,
        ComponentConstructor constructor
    );

    /**
    * @brief Converts a component type name to the type id
    *
    * @param name
    *   The component type name
    *
    * @return 
    *   The component type id
    *
    * @throws
    *   std::invalid_argument if the name could not be found
    */
    Component::TypeId
    typeNameToId(
        const std::string& name
    );

    /**
    * @brief Converts a component type id to the type name
    *
    * @param typeId
    *   The component type id
    *
    * @return 
    *   The component type name
    *
    * @throws
    *   std::invalid_argument if the typeId could not be found
    */
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

/**
 * @brief Registers a component class with the ComponentFactory
 *
 * Use this in the component's source file.
 */
#define REGISTER_COMPONENT(cls) \
    static const bool cls ## _REGISTERED = thrive::ComponentFactory::instance().registerClass<cls>();

}
