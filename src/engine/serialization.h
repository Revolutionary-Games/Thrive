#pragma once

#include "scripting/luabind.h"

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


class StorageContainer {

public:

    static luabind::scope
    luaBindings();

    StorageContainer();

    StorageContainer(
        const StorageContainer& other
    );

    StorageContainer(
        StorageContainer&& other
    );

    ~StorageContainer();

    StorageContainer&
    operator = (
        const StorageContainer& other
    );

    StorageContainer&
    operator = (
        StorageContainer&& other
    );

    bool
    contains(
        const std::string& key
    ) const;

    template<typename T>
    bool
    contains(
        const std::string& key
    ) const;

    template<typename T>
    T
    get(
        const std::string& key,
        const T& defaultValue = T()
    ) const;

    luabind::object
    luaGet(
        const std::string& key,
        luabind::object defaultValue
    ) const;

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

std::ostream&
operator << (
    std::ostream& stream,
    const StorageContainer& storage
);

std::istream&
operator >> (
    std::istream& stream,
    StorageContainer& storage
);


class StorageList : public std::vector<StorageContainer> {

public:

    static luabind::scope
    luaBindings();

    StorageList();

    StorageList(
        const StorageList& other
    );

    StorageList(
        StorageList&& other
    );

    StorageList&
    operator = (
        const StorageList& other
    );

    StorageList&
    operator = (
        StorageList&& other
    );

    void
    append(
        StorageContainer element
    );

    StorageContainer&
    get(
        size_t index
    );

};

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
