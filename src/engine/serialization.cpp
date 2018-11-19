#include "engine/serialization.h"

#include <boost/lexical_cast.hpp>
#include <boost/variant.hpp>
#include <cfloat>
#include <stdexcept>
#include <unordered_map>

using namespace thrive;


namespace {

using TypeId = uint16_t;

using Variant = boost::variant<bool,
    char,
    int8_t,
    int16_t,
    int32_t,
    int64_t,
    uint8_t,
    uint16_t,
    uint32_t,
    uint64_t,
    float,
    double,
    std::string,
    StorageContainer,
    StorageList>;

struct StoredValue {
    TypeId typeId;
    Variant value;
};

/**
 * @brief Information about a storable type
 *
 * @tparam Type
 *   The serialized type
 */
template<typename Type> struct TypeInfo {

    /**
     * @brief The type that is actually put into the storage container
     */
    using StoredType = bool;

    /**
     * @brief Type id
     *
     * Must remain constant through all versions
     */
    static const TypeId Id = 0;

    /**
     * @brief Converts the stored type to the actual type
     *
     * @param storedValue
     *   The stored value
     *
     * @return The actual value
     */
    static Type
        convertFromStoredType(const StoredType& storedValue)
    {
        return storedValue;
    }

    /**
     * @brief Converts the actual type to its stored type
     *
     * @param value
     *   The value to convert
     *
     * @return The stored value
     */
    static StoredType
        convertToStoredType(const Type& value)
    {
        return value;
    }
};

template<TypeId> struct IdToType {
    using Type = void;
};

#define TYPE_INFO(type, storedType, typeId)                  \
    template<> struct TypeInfo<type> {                       \
        using StoredType = storedType;                       \
        static const TypeId Id = typeId;                     \
                                                             \
        static type                                          \
            convertFromStoredType(const StoredType& stored); \
                                                             \
        static StoredType                                    \
            convertToStoredType(const type& value);          \
    };                                                       \
                                                             \
    template<> struct IdToType<typeId> {                     \
        using Type = type;                                   \
    };


TYPE_INFO(bool, bool, 16)
TYPE_INFO(char, char, 32)
TYPE_INFO(int8_t, int8_t, 48)
TYPE_INFO(int16_t, int16_t, 64)
TYPE_INFO(int32_t, int32_t, 80)
TYPE_INFO(int64_t, int64_t, 96)
TYPE_INFO(uint8_t, uint8_t, 112)
TYPE_INFO(uint16_t, uint16_t, 128)
TYPE_INFO(uint32_t, uint32_t, 144)
TYPE_INFO(uint64_t, uint64_t, 160)
TYPE_INFO(float, float, 176)
TYPE_INFO(double, double, 192)
TYPE_INFO(std::string, std::string, 208)
TYPE_INFO(StorageContainer, StorageContainer, 224)
TYPE_INFO(StorageList, StorageList, 240)

// Compound types
TYPE_INFO(Ogre::Degree, float, 272)
TYPE_INFO(Ogre::Plane, StorageContainer, 288)
TYPE_INFO(Ogre::Vector3, StorageContainer, 304)
TYPE_INFO(Ogre::Quaternion, StorageContainer, 320)
TYPE_INFO(Ogre::ColourValue, uint32_t, 336)
} // namespace

/*
#define TO_LUA_CASE(typeName)                                       \
case TypeInfo<typeName>::Id:                                        \
{                                                                   \
    using Info = TypeInfo<typeName>;                                \
    auto storedValue = boost::get<Info::StoredType>(value.value);   \
    auto value = Info::convertFromStoredType(storedValue);          \
    return sol::make_object(lua, value);                            \
}
*/

// static sol::object
// toLua(
//     sol::state_view &lua,
//     const StoredValue& value
// ) {
//     switch(value.typeId) {
//         TO_LUA_CASE(bool);
//         TO_LUA_CASE(char);
//         TO_LUA_CASE(int8_t);
//         TO_LUA_CASE(int16_t);
//         TO_LUA_CASE(int32_t);
//         TO_LUA_CASE(int64_t);
//         TO_LUA_CASE(uint8_t);
//         TO_LUA_CASE(uint16_t);
//         TO_LUA_CASE(uint32_t);
//         TO_LUA_CASE(uint64_t);
//         TO_LUA_CASE(float);
//         TO_LUA_CASE(double);
//         TO_LUA_CASE(std::string);
//         TO_LUA_CASE(StorageContainer);
//         TO_LUA_CASE(StorageList);
//         // Compound types
//         TO_LUA_CASE(Ogre::Degree);
//         TO_LUA_CASE(Ogre::Plane);
//         TO_LUA_CASE(Ogre::Vector3);
//         TO_LUA_CASE(Ogre::Quaternion);
//         TO_LUA_CASE(Ogre::ColourValue);
//         default:
//             return sol::nil;
//     }
// }

struct StorageContainer::Implementation {

    template<typename T>
    bool
        rawContains(const std::string& key) const
    {
        auto iter = m_content.find(key);
        return (
            iter != m_content.end() && iter->second.typeId == TypeInfo<T>::Id);
    }

    template<typename T>
    typename TypeInfo<T>::StoredType
        rawGet(const std::string& key,
            const typename TypeInfo<T>::StoredType& defaultValue =
#ifdef _MSC_VER // Microsoft, why?
                TypeInfo<T>::StoredType()
#else
                typename TypeInfo<T>::StoredType()
#endif
                ) const
    {
        auto iter = m_content.find(key);
        if(iter == m_content.end()) {
            return defaultValue;
        } else if(iter->second.typeId != TypeInfo<T>::Id) {
            return defaultValue;
        } else {
            return boost::get<typename TypeInfo<T>::StoredType>(
                iter->second.value);
        }
    }

    template<typename T>
    void
        rawSet(const std::string& key, typename TypeInfo<T>::StoredType value)
    {
        m_content[key] = StoredValue{TypeInfo<T>::Id, std::move(value)};
    }

    std::unordered_map<std::string, StoredValue> m_content;
};

#define GET_SET_CONTAINS(type)                                           \
                                                                         \
    template<>                                                           \
    bool StorageContainer::contains<type>(const std::string& key) const  \
    {                                                                    \
        return m_impl->rawContains<type>(key);                           \
    }                                                                    \
                                                                         \
    template<>                                                           \
    type StorageContainer::get<type>(                                    \
        const std::string& key, const type& defaultValue) const          \
    {                                                                    \
        if(!this->contains<type>(key)) {                                 \
            return defaultValue;                                         \
        }                                                                \
        auto storedValue = m_impl->rawGet<type>(key);                    \
        return TypeInfo<type>::convertFromStoredType(storedValue);       \
    }                                                                    \
                                                                         \
    template<>                                                           \
    void StorageContainer::set<type>(const std::string& key, type value) \
    {                                                                    \
        auto storedValue = TypeInfo<type>::convertToStoredType(value);   \
        m_impl->rawSet<type>(key, std::move(storedValue));               \
    }


GET_SET_CONTAINS(bool)
GET_SET_CONTAINS(char)
GET_SET_CONTAINS(int8_t)
GET_SET_CONTAINS(int16_t)
GET_SET_CONTAINS(int32_t)
GET_SET_CONTAINS(int64_t)
GET_SET_CONTAINS(uint8_t)
GET_SET_CONTAINS(uint16_t)
GET_SET_CONTAINS(uint32_t)
GET_SET_CONTAINS(uint64_t)
GET_SET_CONTAINS(float)
GET_SET_CONTAINS(double)
GET_SET_CONTAINS(std::string)
GET_SET_CONTAINS(StorageContainer)
GET_SET_CONTAINS(StorageList)
// Compound types
GET_SET_CONTAINS(Ogre::Degree)
GET_SET_CONTAINS(Ogre::Plane)
GET_SET_CONTAINS(Ogre::Vector3)
GET_SET_CONTAINS(Ogre::Quaternion)
GET_SET_CONTAINS(Ogre::ColourValue)


// void StorageContainer::luaBindings(
//     sol::state &lua
// ){
//     lua.new_usertype<StorageContainer>("StorageContainer",

//         sol::constructors<sol::types<>>(),

//         "get", sol::overload([](StorageContainer &self, const std::string
//         &key,
//                 sol::this_state s)
//             {
//                 return self.luaGet(key, sol::nil, s);

//             }, &StorageContainer::luaGet),

//         "contains", static_cast<bool(StorageContainer::*)(
//             const std::string&) const>(
//                 (&StorageContainer::contains)),

//         // Overridden set method //

//         "set", sol::overload(
//             &StorageContainer::set<bool>,
//             &StorageContainer::set<double>,
//             &StorageContainer::set<std::string>,
//             &StorageContainer::set<StorageContainer>,
//             &StorageContainer::set<StorageList>,
//             // Compound types
//             &StorageContainer::set<Ogre::Degree>,
//             &StorageContainer::set<Ogre::Plane>,
//             &StorageContainer::set<Ogre::Vector3>,
//             &StorageContainer::set<Ogre::Quaternion>,
//             &StorageContainer::set<Ogre::ColourValue>
//             // Extra wrappers
//             // ,[](StorageContainer &self, const std::string &key, const
//             StorageContainer &value){

//             //     self.set(key, value);
//             // }
//         )
//     );
// }

StorageContainer::StorageContainer() : m_impl(new Implementation()) {}


StorageContainer::StorageContainer(const StorageContainer& other) :
    m_impl(new Implementation())
{
    *this = other;
}


StorageContainer::StorageContainer(StorageContainer&& other) :
    m_impl(std::move(other.m_impl))
{}


StorageContainer::~StorageContainer() {}


StorageContainer&
    StorageContainer::operator=(const StorageContainer& other)
{
    if(this != &other) {
        m_impl->m_content = other.m_impl->m_content;
    }
    return *this;
}


StorageContainer&
    StorageContainer::operator=(StorageContainer&& other)
{
    assert(this != &other);
    m_impl = std::move(other.m_impl);
    return *this;
}


bool
    StorageContainer::contains(const std::string& key) const
{
    return m_impl->m_content.find(key) != m_impl->m_content.cend();
}


// sol::object
// StorageContainer::luaGet(
//     const std::string& key,
//     sol::object defaultValue,
//     sol::this_state s
// ) const {
//     auto iter = m_impl->m_content.find(key);
//     if (iter == m_impl->m_content.end()) {
//         return defaultValue;
//     }
//     else {
//         sol::state_view lua(s);
//         sol::object obj = toLua(lua, iter->second);
//         if (obj.valid()) {
//             return obj;
//         }
//         else {
//             return defaultValue;
//         }
//     }
// }


std::list<std::string>
    StorageContainer::keys() const
{
    std::list<std::string> keys;
    for(const auto& pair : m_impl->m_content) {
        keys.push_back(pair.first);
    }
    return keys;
}


#define NATIVE_TYPE(typeName)                                               \
    typeName TypeInfo<typeName>::convertFromStoredType(                     \
        const typeName& storedValue)                                        \
    {                                                                       \
        return storedValue;                                                 \
    }                                                                       \
                                                                            \
    typeName TypeInfo<typeName>::convertToStoredType(const typeName& value) \
    {                                                                       \
        return value;                                                       \
    }

NATIVE_TYPE(bool)
NATIVE_TYPE(char)
NATIVE_TYPE(int8_t)
NATIVE_TYPE(int16_t)
NATIVE_TYPE(int32_t)
NATIVE_TYPE(int64_t)
NATIVE_TYPE(uint8_t)
NATIVE_TYPE(uint16_t)
NATIVE_TYPE(uint32_t)
NATIVE_TYPE(uint64_t)
NATIVE_TYPE(float)
NATIVE_TYPE(double)
NATIVE_TYPE(std::string)
NATIVE_TYPE(StorageContainer)
NATIVE_TYPE(StorageList)


////////////////////////////////////////////////////////////////////////////////
// Ogre::Degree
////////////////////////////////////////////////////////////////////////////////

Ogre::Degree
    TypeInfo<Ogre::Degree>::convertFromStoredType(const float& value)
{
    return Ogre::Degree(value);
}


float
    TypeInfo<Ogre::Degree>::convertToStoredType(const Ogre::Degree& value)
{
    return value.valueDegrees();
}


////////////////////////////////////////////////////////////////////////////////
// Ogre::Plane
////////////////////////////////////////////////////////////////////////////////

Ogre::Plane
    TypeInfo<Ogre::Plane>::convertFromStoredType(
        const StorageContainer& storage)
{
    Ogre::Vector3 normal = storage.get<Ogre::Vector3>("normal");
    Ogre::Real d = storage.get<Ogre::Real>("d");
    Ogre::Plane plane(normal, -d); // See the constructor definition in
                                   // OgrePlane.cpp for the minus sign
    return plane;
}


StorageContainer
    TypeInfo<Ogre::Plane>::convertToStoredType(const Ogre::Plane& value)
{
    StorageContainer storage;
    storage.set<Ogre::Vector3>("normal", value.normal);
    storage.set<Ogre::Real>("d", value.d);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// Ogre::Vector3
////////////////////////////////////////////////////////////////////////////////

Ogre::Vector3
    TypeInfo<Ogre::Vector3>::convertFromStoredType(
        const StorageContainer& storage)
{
    std::array<Ogre::Real, 3> elements{{storage.get<Ogre::Real>("x"),
        storage.get<Ogre::Real>("y"), storage.get<Ogre::Real>("z")}};
    return Ogre::Vector3(elements.data());
}


StorageContainer
    TypeInfo<Ogre::Vector3>::convertToStoredType(const Ogre::Vector3& value)
{
    StorageContainer storage;
    storage.set<Ogre::Real>("x", value.x);
    storage.set<Ogre::Real>("y", value.y);
    storage.set<Ogre::Real>("z", value.z);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// Ogre::Quaternion
////////////////////////////////////////////////////////////////////////////////

Ogre::Quaternion
    TypeInfo<Ogre::Quaternion>::convertFromStoredType(
        const StorageContainer& storage)
{
    std::array<Ogre::Real, 4> elements{
        {storage.get<Ogre::Real>("w"), storage.get<Ogre::Real>("x"),
            storage.get<Ogre::Real>("y"), storage.get<Ogre::Real>("z")}};
    return Ogre::Quaternion(elements.data());
}


StorageContainer
    TypeInfo<Ogre::Quaternion>::convertToStoredType(
        const Ogre::Quaternion& value)
{
    StorageContainer storage;
    storage.set<Ogre::Real>("w", value.w);
    storage.set<Ogre::Real>("x", value.x);
    storage.set<Ogre::Real>("y", value.y);
    storage.set<Ogre::Real>("z", value.z);
    return storage;
}



////////////////////////////////////////////////////////////////////////////////
// Ogre::ColourValue
////////////////////////////////////////////////////////////////////////////////

Ogre::ColourValue
    TypeInfo<Ogre::ColourValue>::convertFromStoredType(const uint32_t& rgba)
{
    Ogre::ColourValue value;
    value.setAsRGBA(rgba);
    return value;
}


uint32_t
    TypeInfo<Ogre::ColourValue>::convertToStoredType(
        const Ogre::ColourValue& value)
{
    return value.getAsRGBA();
}


////////////////////////////////////////////////////////////////////////////////
// StorageList
////////////////////////////////////////////////////////////////////////////////

// void StorageList::luaBindings(
//     sol::state &lua
// ){

//     lua.new_usertype<StorageList>("StorageList",

//         sol::constructors<sol::types<>>(),

//         "append", &StorageList::append,
//         "get", &StorageList::get,
//         "size", &StorageList::size
//     );
// }


StorageList::StorageList() {}

StorageList::StorageList(const StorageList& other) :
    std::vector<StorageContainer>(other)
{}


StorageList::StorageList(StorageList&& other) :
    std::vector<StorageContainer>(other)
{}


StorageList&
    StorageList::operator=(const StorageList& other)
{
    std::vector<StorageContainer>::operator=(other);
    return *this;
}


StorageList&
    StorageList::operator=(StorageList&& other)
{
    std::vector<StorageContainer>::operator=(other);
    return *this;
}


void
    StorageList::append(StorageContainer element)
{
    this->emplace_back(std::move(element));
}


StorageContainer&
    StorageList::get(size_t index)
{
    assert(index > 0);
    return this->at(index - 1);
}


////////////////////////////////////////////////////////////////////////////////
// Serialization
////////////////////////////////////////////////////////////////////////////////

namespace {

template<typename T> struct TypeHandler {

    static T
        deserialize(std::istream& stream);

    static void
        serialize(std::ostream& stream, const T& value);
};


////////////////////////////////////////////////////////////////////////////////
// Integrals
////////////////////////////////////////////////////////////////////////////////

template<typename T> struct IntegralTypeHandler {

    static T
        deserialize(std::istream& stream)
    {
        T value = 0;
        stream.read(reinterpret_cast<char*>(&value), sizeof(T));
        assert(not stream.fail());
        return value;
    }

    static void
        serialize(std::ostream& stream, T value)
    {
        stream.write(reinterpret_cast<char*>(&value), sizeof(T));
    }
};

#define INTEGRAL_TYPE_HANDLER(typeName) \
    template<>                          \
    struct TypeHandler<typeName> : public IntegralTypeHandler<typeName> {};

INTEGRAL_TYPE_HANDLER(int8_t)
INTEGRAL_TYPE_HANDLER(int16_t)
INTEGRAL_TYPE_HANDLER(int32_t)
INTEGRAL_TYPE_HANDLER(int64_t)
INTEGRAL_TYPE_HANDLER(uint8_t)
INTEGRAL_TYPE_HANDLER(uint16_t)
INTEGRAL_TYPE_HANDLER(uint32_t)
INTEGRAL_TYPE_HANDLER(uint64_t)


////////////////////////////////////////////////////////////////////////////////
// Bool
////////////////////////////////////////////////////////////////////////////////

template<> struct TypeHandler<bool> {

    static bool
        deserialize(std::istream& stream)
    {
        auto value = TypeHandler<uint8_t>::deserialize(stream);
        return value > 0;
    }

    static void
        serialize(std::ostream& stream, const bool& value)
    {
        uint8_t encoded = value ? 1 : 0;
        TypeHandler<uint8_t>::serialize(stream, encoded);
    }
};


////////////////////////////////////////////////////////////////////////////////
// Char
////////////////////////////////////////////////////////////////////////////////

template<> struct TypeHandler<char> {

    static char
        deserialize(std::istream& stream)
    {
        char value = 0;
        stream.read(&value, 1);
        assert(not stream.fail());
        return value;
    }

    static void
        serialize(std::ostream& stream, const char& value)
    {
        stream.write(&value, 1);
    }
};


////////////////////////////////////////////////////////////////////////////////
// String
////////////////////////////////////////////////////////////////////////////////

template<> struct TypeHandler<std::string> {

    static std::string
        deserialize(std::istream& stream)
    {
        uint64_t size = TypeHandler<uint64_t>::deserialize(stream);
        std::vector<char> buffer(size, '\0');
        stream.read(&buffer[0], size);
        assert(not stream.fail());
        return std::string(buffer.begin(), buffer.end());
    }

    static void
        serialize(std::ostream& stream, const std::string& string)
    {
        uint64_t size = string.size();
        TypeHandler<uint64_t>::serialize(stream, size);
        stream.write(string.data(), size);
    }
};


////////////////////////////////////////////////////////////////////////////////
// Float
////////////////////////////////////////////////////////////////////////////////

template<> struct TypeHandler<float> {

    static float
        deserialize(std::istream& stream)
    {
        std::string asString = TypeHandler<std::string>::deserialize(stream);
        return boost::lexical_cast<float>(asString);
    }

    static void
        serialize(std::ostream& stream, const float& value)
    {
        std::string asString = boost::lexical_cast<std::string>(value);
        TypeHandler<std::string>::serialize(stream, asString);
    }
};


////////////////////////////////////////////////////////////////////////////////
// Double
////////////////////////////////////////////////////////////////////////////////

template<> struct TypeHandler<double> {

    static double
        deserialize(std::istream& stream)
    {
        std::string asString = TypeHandler<std::string>::deserialize(stream);
        return boost::lexical_cast<double>(asString);
    }

    static void
        serialize(std::ostream& stream, const double& value)
    {
        std::string asString = boost::lexical_cast<std::string>(value);
        TypeHandler<std::string>::serialize(stream, asString);
    }
};


////////////////////////////////////////////////////////////////////////////////
// StorageContainer
////////////////////////////////////////////////////////////////////////////////

template<> struct TypeHandler<StorageContainer> {

    static StorageContainer
        deserialize(std::istream& stream)
    {
        StorageContainer value;
        stream >> value;
        return value;
    }


    static void
        serialize(std::ostream& stream, const StorageContainer& value)
    {
        stream << value;
    }
};


////////////////////////////////////////////////////////////////////////////////
// StorageList
////////////////////////////////////////////////////////////////////////////////

template<> struct TypeHandler<StorageList> {

    static StorageList
        deserialize(std::istream& stream)
    {
        StorageList list;
        uint64_t size = TypeHandler<uint64_t>::deserialize(stream);
        list.reserve(size);
        for(size_t i = 0; i < size; ++i) {
            list.append(TypeHandler<StorageContainer>::deserialize(stream));
        }
        return list;
    }


    static void
        serialize(std::ostream& stream, const StorageList& list)
    {
        uint64_t size = list.size();
        TypeHandler<uint64_t>::serialize(stream, size);
        for(const auto& storageContainer : list) {
            TypeHandler<StorageContainer>::serialize(stream, storageContainer);
        }
    }
};


struct SerializationVisitor : public boost::static_visitor<> {

    SerializationVisitor(std::ostream& stream) : m_stream(stream) {}

    template<typename T>
    void
        operator()(const T& value) const
    {
        TypeHandler<T>::serialize(m_stream, value);
    }

    std::ostream& m_stream;
};

#define DESERIALIZE_CASE(typeName) \
    case TypeInfo<typeName>::Id:   \
        return TypeHandler<TypeInfo<typeName>::StoredType>::deserialize(stream)

static Variant
    deserialize(TypeId typeId, std::istream& stream)
{
    switch(typeId) {
        DESERIALIZE_CASE(bool);
        DESERIALIZE_CASE(char);
        DESERIALIZE_CASE(int8_t);
        DESERIALIZE_CASE(int16_t);
        DESERIALIZE_CASE(int32_t);
        DESERIALIZE_CASE(int64_t);
        DESERIALIZE_CASE(uint8_t);
        DESERIALIZE_CASE(uint16_t);
        DESERIALIZE_CASE(uint32_t);
        DESERIALIZE_CASE(uint64_t);
        DESERIALIZE_CASE(float);
        DESERIALIZE_CASE(double);
        DESERIALIZE_CASE(std::string);
        DESERIALIZE_CASE(StorageContainer);
        DESERIALIZE_CASE(StorageList);
        // Compound types
        DESERIALIZE_CASE(Ogre::Degree);
        DESERIALIZE_CASE(Ogre::Plane);
        DESERIALIZE_CASE(Ogre::Vector3);
        DESERIALIZE_CASE(Ogre::Quaternion);
        DESERIALIZE_CASE(Ogre::ColourValue);
    default:
        assert(false && "Unknown type id. Did you add a new STORABLE_TYPE, but "
                        "forgot the DESERIALIZE_CASE?");
    }
    return Variant(); // Should never be reached, but mingw complains without it
}


} // namespace

std::ostream&
    thrive::operator<<(std::ostream& stream, const StorageContainer& storage)
{
    SerializationVisitor visitor(stream);
    const auto& content = storage.m_impl->m_content;
    TypeHandler<uint64_t>::serialize(stream, content.size());
    for(const auto& pair : content) {
        TypeHandler<std::string>::serialize(stream, pair.first);
        TypeHandler<TypeId>::serialize(stream, pair.second.typeId);
        boost::apply_visitor(visitor, pair.second.value);
    }
    return stream;
}


std::istream&
    thrive::operator>>(std::istream& stream, StorageContainer& storage)
{
    uint64_t size = TypeHandler<uint64_t>::deserialize(stream);
    storage.m_impl->m_content.clear();
    for(size_t i = 0; i < size; ++i) {
        std::string key = TypeHandler<std::string>::deserialize(stream);
        TypeId typeId = TypeHandler<TypeId>::deserialize(stream);
        storage.m_impl->m_content[key] =
            StoredValue{typeId, deserialize(typeId, stream)};
    }
    return stream;
}
