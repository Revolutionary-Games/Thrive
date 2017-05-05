#pragma once

#include "scripting/luajit.h"

#include <cstdint>
#include <OgreColourValue.h>
#include <OgreMath.h>
#include <OgrePlane.h>
#include <OgreQuaternion.h>
#include <OgreVector3.h>
#include <string>
#include <map>
#include <vector>

static_assert(
    CHAR_BIT == 8,
    "char must be 8 bit long for properly portable serialization."
);


namespace Ogre {
    class ColourValue;
    class Degree;
    class Plane;
    class Quaternion;
    class Radian;
    class Vector3;
}

namespace thrive {


/**
* @brief A key-value storage for serialization
*/
class StorageContainer {

public:

    /**
    * @brief Lua bindings
    *
    * - StorageContainer::contains
    * - StorageContainer::get
    * - StorageContainer::set
    *
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    StorageContainer();

    /**
    * @brief Copy-constructor
    *
    * @param other
    */
    StorageContainer(
        const StorageContainer& other
    );

    /**
    * @brief Move-constructor
    *
    * @param other
    */
    StorageContainer(
        StorageContainer&& other
    );

    /**
    * @brief Destructor
    */
    ~StorageContainer();

    /**
    * @brief Copy-assignment
    *
    * @param other
    *
    */
    StorageContainer&
    operator = (
        const StorageContainer& other
    );

    /**
    * @brief Move assignment
    *
    * @param other
    *
    */
    StorageContainer&
    operator = (
        StorageContainer&& other
    );

    /**
    * @brief Required for lua bindings
    */
    bool operator ==(
        const StorageContainer &other
    ) const {
        return this == &other;
    }

    /**
    * @brief Required for lua bindings
    */
    bool operator <(
        const StorageContainer &other
    ) const {
        (void)other;
        return false;
    }

    /**
    * @brief Checks for a key
    *
    * @param key
    *   The key to check for
    *
    * @return \c true if the key is present in this container, \c false otherwise
    */
    bool
    contains(
        const std::string& key
    ) const;

    /**
    * @brief Checks for a key together with type
    *
    * @tparam T
    *   The expected type of the key's associated value
    * @param key
    *   The key to check for
    *
    * @return
    *   \c true if the key is present and associated with a value of type \a T,
    *   \c false otherwise
    */
    template<typename T>
    bool
    contains(
        const std::string& key
    ) const;

    /**
    * @brief Retrieves a value from the container
    *
    * @tparam T
    *   The value's type
    * @param key
    *   The key to retrieve
    * @param defaultValue
    *   The value to return when the key is not present (or the value has the
    *   wrong type)
    *
    * @return
    *   The value associated with \a key or \a defaultValue if the key could
    *   not be found or has a value associated with it that is not \a T.
    */
    template<typename T>
    T
    get(
        const std::string& key,
        const T& defaultValue = T()
    ) const;

    /**
    * @brief Returns a list of all keys in this container
    *
    */
    std::list<std::string>
    keys() const;

    /**
    * @brief Lua version of StorageContainer::get
    *
    * @param key
    * @param defaultValue
    *
    * @return
    */
    sol::object luaGet(
        const std::string& key,
        sol::object defaultValue,
        sol::this_state s
    ) const;

    /**
    * @brief Sets a value in this container
    *
    * If \a key is already associated with a value, it is overwritten.
    *
    * @tparam T
    *   The type of \a value
    * @param key
    *   The key to associate with the value
    * @param value
    *   The value to insert
    */
    template<typename T>
    void
    set(
        const std::string& key,
        T value
    );

    friend std::ostream&
    operator << (
        std::ostream& stream,
        const StorageContainer& storage
    );

    friend std::istream&
    operator >> (
        std::istream& stream,
        StorageContainer& storage
    );

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

/**
* @brief Output stream operator for StorageContainer
*
* @param stream
* @param storage
*
* @return
*/
std::ostream&
operator << (
    std::ostream& stream,
    const StorageContainer& storage
);

/**
* @brief Input stream operator for StorageContainer
*
* @param stream
* @param storage
*
* @return
*/
std::istream&
operator >> (
    std::istream& stream,
    StorageContainer& storage
);


/**
* @brief A list of StorageContainers
*/
class StorageList : public std::vector<StorageContainer> {

public:

    /**
    * @brief Lua bindings
    *
    * - StorageList::append
    * - StorageList::get
    * - StorageList::size
    *
    * @return
    */
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    StorageList();

    /**
    * @brief Copy Constructor
    *
    * @param other
    */
    StorageList(
        const StorageList& other
    );

    /**
    * @brief Move constructor
    *
    * @param other
    */
    StorageList(
        StorageList&& other
    );

    /**
    * @brief Copy assignment
    *
    * @param other
    *
    */
    StorageList&
    operator = (
        const StorageList& other
    );

    /**
    * @brief Move assignment
    *
    * @param other
    *
    */
    StorageList&
    operator = (
        StorageList&& other
    );

    /**
    * @brief Appends a StorageContainer to this list
    *
    * @param element
    *   The container to append
    */
    void
    append(
        StorageContainer element
    );

    /**
    * @brief Retrieves an element by index
    *
    * @param index
    *   The index to retrieve
    *
    * @return The element at \a index
    *
    * @throws std::out_of_range if \a index is out of range
    */
    StorageContainer&
    get(
        size_t index
    );

};

/**
* @brief Macro for declaring a new storable type
*
* @param typeName
*   The name of the storable type
*
*/
#define STORABLE_TYPE(typeName) \
    template<> \
    bool \
    StorageContainer::contains<typeName>( \
        const std::string& key \
    ) const; \
    \
    template<> \
    typeName \
    StorageContainer::get<typeName>( \
        const std::string& key, \
        const typeName& defaultValue \
    ) const; \
    \
    template<> \
    void \
    StorageContainer::set<typeName>( \
        const std::string& key, \
        typeName value \
    );

// Native types
STORABLE_TYPE(bool)
STORABLE_TYPE(char)
STORABLE_TYPE(int8_t)
STORABLE_TYPE(int16_t)
STORABLE_TYPE(int32_t)
STORABLE_TYPE(int64_t)
STORABLE_TYPE(uint8_t)
STORABLE_TYPE(uint16_t)
STORABLE_TYPE(uint32_t)
STORABLE_TYPE(uint64_t)
STORABLE_TYPE(float)
STORABLE_TYPE(double)
STORABLE_TYPE(std::string)
STORABLE_TYPE(StorageContainer)
STORABLE_TYPE(StorageList)

// Compound types
STORABLE_TYPE(Ogre::Degree)
STORABLE_TYPE(Ogre::Plane)
STORABLE_TYPE(Ogre::Vector3)
STORABLE_TYPE(Ogre::Quaternion)
STORABLE_TYPE(Ogre::ColourValue)
}
