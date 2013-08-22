/**
* @brief 
*/
#pragma once

#include "engine/typedefs.h"

#include <memory>
#include <string>
#include <unordered_map>

namespace luabind {
class scope;
}

namespace thrive {

/**
* @brief Base class for components
*
* Components are mainly data classes that "tag" entities with certain
* properties.
*
* Good introductions to entity / component models:
* - <a href="http://piemaster.net/2011/07/entity-component-primer/">Entity Component Primer</a>
* - <a href="http://www.gamasutra.com/blogs/MeganFox/20101208/88590/Game_Engines_101_The_EntityComponent_Model.php">Game Engines 101</a>
* - <a href="http://www.richardlord.net/blog/what-is-an-entity-framework">What is an entity system?</a>
*/
class Component {

public:

    /**
    * @brief Typedef for component type ids
    *
    * A component type id is a unique identifier, different for each component 
    * class. We could use the component name (a string) for this, but using
    * an integer is more performant.
    */
    using TypeId = size_t;

    /**
    * @brief Lua bindings
    *
    * Exposes:
    *
    * - Component::typeId()
    * - Component::typeName()
    * - Component::touch()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Destructor
    */
    virtual ~Component() = 0;

    /**
    * @brief The component's type id
    */
    virtual TypeId
    typeId() const = 0;

    /**
    * @brief The component's type name
    */
    virtual const std::string&
    typeName() const = 0;

protected:

    /**
    * @brief Generates a new type id
    *
    * You usually don't have to call this directly. Use the COMPONENT macro 
    * instead.
    *
    * @return A unique type id
    */
    static TypeId
    generateTypeId();

private:

    bool m_hasChanges = true;

};

}

/**
* @brief Fills in basic functions for derived components
*
* This macro creates the following functions:
* - \c TYPE_ID: Static function that returns the component's type id. The id 
*   is generated automatically the first time this is called.
* - \c typeId: Overrides Component::typeId() and returns the type id
*   returned by \c TYPE_ID.
* - \c TYPE_NAME: Static function that returns \a name as a string. To avoid
*   copying, the string is returned as a const reference to a local static
*   variable.
* - \c typeName: Overrides Component::typeName() and returns the name returned
*   by \c TYPE_NAME.
*
* @param name 
*   The component's name
*
* Example:
* \code
* class MyComponent : public Component {
*     COMPONENT(MyComponent)
* public:
*     // ...
* };
* \endcode
*/
#define COMPONENT(name)  \
    public: \
        \
        static TypeId TYPE_ID() { \
            static TypeId id = Component::generateTypeId(); \
            return id; \
        } \
        \
        TypeId typeId() const override { \
            return TYPE_ID(); \
        } \
        \
        static const std::string& TYPE_NAME() { \
            static std::string string(#name); \
            return string; \
        } \
        \
        const std::string& typeName() const override { \
            return TYPE_NAME(); \
        } \
        \
    private: \


