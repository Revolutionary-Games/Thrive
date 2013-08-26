#pragma once

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
    T
    get(
        const std::string& key,
        const T& defaultValue = T()
    ) const;

    template<typename T>
    void
    set(
        const std::string& key,
        const T& value
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

using StorageVector = std::vector<StorageContainer>;

using StorageMap = std::map<std::string, StorageContainer>;

using StorableTypeId = uint16_t;

template<typename T>
struct StorableTypeToId {
    static const StorableTypeId Id = 0;
};

template<StorableTypeId>
struct IdToStorableType {
    using Type = void;
};

#define STORABLE_TYPE(typeName, typeId) \
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
        const typeName& value \
    ); \
    \
    template<> \
    struct StorableTypeToId<typeName> { \
        static const StorableTypeId Id = typeId; \
    }; \
    \
    template<> \
    struct IdToStorableType<typeId> { \
        using Type = typeName; \
    };

STORABLE_TYPE(bool,              16)
STORABLE_TYPE(char,              32)
STORABLE_TYPE(int8_t,            48)
STORABLE_TYPE(int16_t,           64)
STORABLE_TYPE(int32_t,           80)
STORABLE_TYPE(int64_t,           96)
STORABLE_TYPE(uint8_t,          112)
STORABLE_TYPE(uint16_t,         128)
STORABLE_TYPE(uint32_t,         144)
STORABLE_TYPE(uint64_t,         160)
STORABLE_TYPE(float,            176)
STORABLE_TYPE(double,           192)
STORABLE_TYPE(std::string,      208)
STORABLE_TYPE(StorageContainer, 224)
STORABLE_TYPE(StorageVector,    240)
STORABLE_TYPE(StorageMap,       256)

}
