#include "engine/serialization.h"

#include "scripting/luabind.h"

#include <boost/variant.hpp>
#include <cfloat>
#include <luabind/iterator_policy.hpp>
#include <stdexcept>
#include <unordered_map>

using namespace thrive;


namespace {

using TypeId = uint16_t;

using Variant = boost::variant<
    bool,
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
    StorageList
>;

struct StoredValue {
    TypeId typeId;
    Variant value;
};

template<typename Type>
struct TypeInfo {

    using StoredType = void;

    static const TypeId Id = 0;
};

template<TypeId>
struct IdToType {
    using Type = void;
};

#define TYPE_INFO(type, storedType, typeId) \
    template<> \
    struct TypeInfo<type> { \
        using StoredType = storedType; \
        static const TypeId Id = typeId; \
    }; \
    \
    template<> \
    struct IdToType<typeId> { \
        using Type = type; \
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

template<typename T>
luabind::object
toLua(
    lua_State* L,
    const T& value
) {
    return luabind::object(L, value);
}

#define TO_LUA_CASE(typeName) \
    case TypeInfo<typeName>::Id: \
        return toLua<typeName>(L, boost::get<typeName>(value.value));

static luabind::object
toLua(
    lua_State* L,
    const StoredValue& value
) {
    switch(value.typeId) {
        TO_LUA_CASE(bool);
        TO_LUA_CASE(char);
        TO_LUA_CASE(int8_t);
        TO_LUA_CASE(int16_t);
        TO_LUA_CASE(int32_t);
        TO_LUA_CASE(int64_t);
        TO_LUA_CASE(uint8_t);
        TO_LUA_CASE(uint16_t);
        TO_LUA_CASE(uint32_t);
        TO_LUA_CASE(uint64_t);
        TO_LUA_CASE(float);
        TO_LUA_CASE(double);
        TO_LUA_CASE(std::string);
        TO_LUA_CASE(StorageContainer);
        TO_LUA_CASE(StorageList);
        default:
            return luabind::object();
    }
}

struct StorageContainer::Implementation {

    template<typename T>
    bool
    rawContains(
        const std::string& key
    ) const {
        auto iter = m_content.find(key);
        return (
            iter != m_content.end() and
            iter->second.typeId == TypeInfo<T>::Id
        );
    }

    template<typename T>
    typename TypeInfo<T>::StoredType
    rawGet(
        const std::string& key,
        const typename TypeInfo<T>::StoredType& defaultValue = typename TypeInfo<T>::StoredType()
    ) const {
        auto iter = m_content.find(key);
        if (iter == m_content.end()) {
            return defaultValue;
        }
        else if (iter->second.typeId != TypeInfo<T>::Id){
            return defaultValue;
        }
        else {
            return boost::get<typename TypeInfo<T>::StoredType>(iter->second.value);
        }
    }

    template<typename T>
    void
    rawSet(
        const std::string& key,
        typename TypeInfo<T>::StoredType value
    ) {
        m_content[key] = StoredValue{
            TypeInfo<T>::Id, 
            std::move(value)
        };
    }

    std::unordered_map<std::string, StoredValue> m_content;

};


luabind::object
StorageContainer::luaGet(
    const std::string& key,
    luabind::object defaultValue
) const {
    auto iter = m_impl->m_content.find(key);
    if (iter == m_impl->m_content.end()) {
        return defaultValue;
    }
    else {
        luabind::object obj = toLua(defaultValue.interpreter(), iter->second);
        if (obj) {
            return obj;
        }
        else {
            return defaultValue;
        }
    }
}


luabind::scope
StorageContainer::luaBindings() {
    using namespace luabind;
    return 
        class_<StorageContainer>("StorageContainer")
            .def(constructor<>())
            .def("contains", static_cast<bool(StorageContainer::*)(const std::string&) const>(&StorageContainer::contains))
            .def("get", &StorageContainer::luaGet)
            .def("set", &StorageContainer::set<bool>)
            .def("set", &StorageContainer::set<double>)
            .def("set", &StorageContainer::set<std::string>)
            .def("set", &StorageContainer::set<StorageContainer>)
            .def("set", &StorageContainer::set<StorageList>)
            // Compound types
            .def("set", &StorageContainer::set<Ogre::Degree>)
            .def("set", &StorageContainer::set<Ogre::Plane>)
            .def("set", &StorageContainer::set<Ogre::Vector3>)
            .def("set", &StorageContainer::set<Ogre::Quaternion>)
            .def("set", &StorageContainer::set<Ogre::ColourValue>)
    ;
}


StorageContainer::StorageContainer()
  : m_impl(new Implementation())
{
}


StorageContainer::StorageContainer(
    const StorageContainer& other
) : m_impl(new Implementation())
{
    *this = other;
}


StorageContainer::StorageContainer(
    StorageContainer&& other
) : m_impl(std::move(other.m_impl))
{
}


StorageContainer::~StorageContainer() {}


StorageContainer&
StorageContainer::operator = (
    const StorageContainer& other
) {
    if (this != &other) {
        m_impl->m_content = other.m_impl->m_content;
    }
    return *this;
}


StorageContainer&
StorageContainer::operator = (
    StorageContainer&& other
) {
    assert(this != &other);
    m_impl = std::move(other.m_impl);
    return *this;
}


bool
StorageContainer::contains(
    const std::string& key
) const {
    return m_impl->m_content.find(key) != m_impl->m_content.cend();
}


#define NATIVE_TYPE(typeName) \
    template<> \
    bool \
    StorageContainer::contains<typeName>( \
        const std::string& key \
    ) const { \
        return m_impl->rawContains<typeName>(key); \
    } \
    \
    template<> \
    typeName \
    StorageContainer::get<typeName>( \
        const std::string& key, \
        const typeName& defaultValue \
    ) const { \
        return m_impl->rawGet<typeName>(key, defaultValue); \
    } \
    \
    template <> \
    void \
    StorageContainer::set<typeName>( \
        const std::string& key, \
        typeName value \
    ) { \
        m_impl->rawSet<typeName>(key, value); \
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


#define CONTAINS(typeName) \
    template<> \
    bool \
    StorageContainer::contains<typeName>( \
        const std::string& key \
    ) const { \
        return m_impl->rawContains<typeName>(key); \
    }

////////////////////////////////////////////////////////////////////////////////
// Ogre::Degree
////////////////////////////////////////////////////////////////////////////////

CONTAINS(Ogre::Degree)

template<>
Ogre::Degree
StorageContainer::get<Ogre::Degree>(
    const std::string& key,
    const Ogre::Degree& defaultValue
) const {
    if (not this->contains<Ogre::Degree>(key)) {
        return defaultValue;
    }
    float value = m_impl->rawGet<Ogre::Degree>(key);
    return Ogre::Degree(value);
}


template<>
void
StorageContainer::set<Ogre::Degree>(
    const std::string& key,
    Ogre::Degree value
) {
    m_impl->rawSet<Ogre::Degree>(key, value.valueDegrees());
}


////////////////////////////////////////////////////////////////////////////////
// Ogre::Plane
////////////////////////////////////////////////////////////////////////////////

CONTAINS(Ogre::Plane)

template<>
Ogre::Plane
StorageContainer::get<Ogre::Plane>(
    const std::string& key,
    const Ogre::Plane& defaultValue
) const {
    if (not this->contains<Ogre::Plane>(key)) {
        return defaultValue;
    }
    StorageContainer storage = m_impl->rawGet<Ogre::Plane>(key);
    Ogre::Vector3 normal = storage.get<Ogre::Vector3>("normal", defaultValue.normal);
    Ogre::Real d = storage.get<Ogre::Real>("d", defaultValue.d);
    return Ogre::Plane(normal, d);
}


template<>
void
StorageContainer::set<Ogre::Plane>(
    const std::string& key,
    Ogre::Plane value
) {
    StorageContainer storage;
    storage.set<Ogre::Vector3>("normal", value.normal);
    storage.set<Ogre::Real>("d", value.d);
    m_impl->rawSet<Ogre::Plane>(key, storage);
}



////////////////////////////////////////////////////////////////////////////////
// Ogre::Vector3
////////////////////////////////////////////////////////////////////////////////

CONTAINS(Ogre::Vector3)

template<>
Ogre::Vector3
StorageContainer::get<Ogre::Vector3>(
    const std::string& key,
    const Ogre::Vector3& defaultValue
) const {
    if (not this->contains<Ogre::Vector3>(key)) {
        return defaultValue;
    }
    StorageContainer storage = m_impl->rawGet<Ogre::Vector3>(key);
    std::array<Ogre::Real, 3> elements {{
        storage.get<Ogre::Real>("x", defaultValue.x),
        storage.get<Ogre::Real>("y", defaultValue.y),
        storage.get<Ogre::Real>("z", defaultValue.z)
    }};
    return Ogre::Vector3(elements.data());
}


template<>
void
StorageContainer::set<Ogre::Vector3>(
    const std::string& key,
    Ogre::Vector3 value
) {
    StorageContainer storage;
    storage.set<Ogre::Real>("x", value.x);
    storage.set<Ogre::Real>("y", value.y);
    storage.set<Ogre::Real>("z", value.z);
    m_impl->rawSet<Ogre::Vector3>(key, storage);
}



////////////////////////////////////////////////////////////////////////////////
// Ogre::Quaternion
////////////////////////////////////////////////////////////////////////////////

CONTAINS(Ogre::Quaternion)

template<>
Ogre::Quaternion
StorageContainer::get<Ogre::Quaternion>(
    const std::string& key,
    const Ogre::Quaternion& defaultValue
) const {
    if (not this->contains<Ogre::Quaternion>(key)) {
        return defaultValue;
    }
    StorageContainer storage = m_impl->rawGet<Ogre::Quaternion>(key);
    std::array<Ogre::Real, 4> elements {{
        storage.get<Ogre::Real>("w", defaultValue.w),
        storage.get<Ogre::Real>("x", defaultValue.x),
        storage.get<Ogre::Real>("y", defaultValue.y),
        storage.get<Ogre::Real>("z", defaultValue.z)
    }};
    return Ogre::Quaternion(elements.data());
}


template<>
void
StorageContainer::set<Ogre::Quaternion>(
    const std::string& key,
    Ogre::Quaternion value
) {
    StorageContainer storage;
    storage.set<Ogre::Real>("w", value.w);
    storage.set<Ogre::Real>("x", value.x);
    storage.set<Ogre::Real>("y", value.y);
    storage.set<Ogre::Real>("z", value.z);
    m_impl->rawSet<Ogre::Quaternion>(key, storage);
}



////////////////////////////////////////////////////////////////////////////////
// Ogre::ColourValue
////////////////////////////////////////////////////////////////////////////////

CONTAINS(Ogre::ColourValue)

template<>
Ogre::ColourValue
StorageContainer::get<Ogre::ColourValue>(
    const std::string& key,
    const Ogre::ColourValue& defaultValue
) const {
    if (not this->contains<Ogre::ColourValue>(key)) {
        return defaultValue;
    }
    uint32_t rgba = m_impl->rawGet<Ogre::ColourValue>(key);
    Ogre::ColourValue value = defaultValue;
    value.setAsRGBA(rgba);
    return value;
}


template<>
void
StorageContainer::set<Ogre::ColourValue>(
    const std::string& key,
    Ogre::ColourValue value
) {
    m_impl->rawSet<Ogre::ColourValue>(key, value.getAsRGBA());
}


////////////////////////////////////////////////////////////////////////////////
// StorageList
////////////////////////////////////////////////////////////////////////////////

luabind::scope
StorageList::luaBindings() {
    using namespace luabind;
    return class_<StorageList>("StorageList")
        .def(constructor<>())
        .def("append", &StorageList::append)
        .def("get", &StorageList::get)
        .def("size", &StorageList::size)
    ;
}


StorageList::StorageList() {}

StorageList::StorageList(
    const StorageList& other
) : std::vector<StorageContainer>(other)
{
}


StorageList::StorageList(
    StorageList&& other
) : std::vector<StorageContainer>(other)
{
}


StorageList&
StorageList::operator = (
    const StorageList& other
) {
    std::vector<StorageContainer>::operator=(other);
    return *this;
}


StorageList&
StorageList::operator = (
    StorageList&& other
) {
    std::vector<StorageContainer>::operator=(other);
    return *this;
}


void
StorageList::append(
    StorageContainer element
) {
    this->emplace_back(std::move(element));
}


StorageContainer&
StorageList::get(
    size_t index
) {
    assert(index > 0);
    return this->at(index - 1);
}


////////////////////////////////////////////////////////////////////////////////
// Serialization
////////////////////////////////////////////////////////////////////////////////

namespace {

template<typename T>
struct TypeHandler {

    static T
    deserialize(
        std::istream& stream
    );

    static void
    serialize(
        std::ostream& stream,
        const T& value
    );

};


////////////////////////////////////////////////////////////////////////////////
// Portable float serialization
// Source: http://beej.us/guide/bgnet/output/html/singlepage/bgnet.html#serialization
////////////////////////////////////////////////////////////////////////////////
#define pack754_32(f) (pack754((f), 32, 8))
#define pack754_64(f) (pack754((f), 64, 11))
#define unpack754_32(i) (unpack754((i), 32, 8))
#define unpack754_64(i) (unpack754((i), 64, 11))

static uint64_t 
pack754(
    long double f, 
    unsigned bits, 
    unsigned expbits
) {
    long double fnorm;
    int shift;
    long long sign, exp, significand;
    unsigned significandbits = bits - expbits - 1; // -1 for sign bit

    if (std::abs(f) < LDBL_EPSILON) return 0; // get this special case out of the way

    // check sign and begin normalization
    if (f < 0) { sign = 1; fnorm = -f; }
    else { sign = 0; fnorm = f; }

    // get the normalized form of f and track the exponent
    shift = 0;
    while(fnorm >= 2.0) { fnorm /= 2.0; shift++; }
    while(fnorm < 1.0) { fnorm *= 2.0; shift--; }
    fnorm = fnorm - 1.0;

    // calculate the binary form (non-float) of the significand data
    significand = fnorm * ((1LL<<significandbits) + 0.5f);

    // get the biased exponent
    exp = shift + ((1<<(expbits-1)) - 1); // shift + bias

    // return the final answer
    return (sign<<(bits-1)) | (exp<<(bits-expbits-1)) | significand;
}

static long double 
unpack754(
    uint64_t i, 
    unsigned bits, 
    unsigned expbits
) {
    long double result;
    long long shift;
    unsigned bias;
    unsigned significandbits = bits - expbits - 1; // -1 for sign bit

    if (i == 0) return 0.0;

    // pull the significand
    result = (i&((1LL<<significandbits)-1)); // mask
    result /= (1LL<<significandbits); // convert back to float
    result += 1.0f; // add the one back on

    // deal with the exponent
    bias = (1<<(expbits-1)) - 1;
    shift = ((i>>significandbits)&((1LL<<expbits)-1)) - bias;
    while(shift > 0) { result *= 2.0; shift--; }
    while(shift < 0) { result /= 2.0; shift++; }

    // sign it
    result *= (i>>(bits-1))&1? -1.0: 1.0;

    return result;
}


////////////////////////////////////////////////////////////////////////////////
// Integrals
////////////////////////////////////////////////////////////////////////////////

template<typename T>
struct IntegralTypeHandler {

    static T
    deserialize(
        std::istream& stream
    ) {
        T value = 0;
        stream.read(
            reinterpret_cast<char*>(&value),
            sizeof(T)
        );
        assert(not stream.fail());
        return value;
    }

    static void
    serialize(
        std::ostream& stream,
        T value
    ) {
        stream.write(
            reinterpret_cast<char*>(&value), 
            sizeof(T)
        );
    }
};

#define INTEGRAL_TYPE_HANDLER(typeName) \
    template<> struct TypeHandler<typeName> : public IntegralTypeHandler<typeName> {};

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

template<>
struct TypeHandler<bool> {

    static bool
    deserialize(
        std::istream& stream
    ) {
        auto value = TypeHandler<uint8_t>::deserialize(stream);
        return value > 0;
    }

    static void
    serialize(
        std::ostream& stream,
        const bool& value
    ) {
        uint8_t encoded = value ? 1 : 0;
        TypeHandler<uint8_t>::serialize(stream, encoded);
    }

};


////////////////////////////////////////////////////////////////////////////////
// Char
////////////////////////////////////////////////////////////////////////////////

template<>
struct TypeHandler<char> {

    static char
    deserialize(
        std::istream& stream
    ) {
        char value = 0;
        stream.read(&value, 1);
        assert(not stream.fail());
        return value;
    }

    static void
    serialize(
        std::ostream& stream,
        const char& value
    ) {
        stream.write(&value, 1);
    }
};


////////////////////////////////////////////////////////////////////////////////
// Float
////////////////////////////////////////////////////////////////////////////////

template<>
struct TypeHandler<float> {

    static float
    deserialize(
        std::istream& stream
    ) {
        uint64_t packed = TypeHandler<uint64_t>::deserialize(stream);
        return unpack754_32(packed);
    }

    static void
    serialize(
        std::ostream& stream,
        const float& value
    ) {
        uint64_t packed = pack754_32(value);
        TypeHandler<uint64_t>::serialize(stream, packed);
    }

};


////////////////////////////////////////////////////////////////////////////////
// Double
////////////////////////////////////////////////////////////////////////////////

template<>
struct TypeHandler<double> {

    static double
    deserialize(
        std::istream& stream
    ) {
        uint64_t packed = TypeHandler<uint64_t>::deserialize(stream);
        return unpack754_64(packed);
    }

    static void
    serialize(
        std::ostream& stream,
        const double& value
    ) {
        uint64_t packed = pack754_64(value);
        TypeHandler<uint64_t>::serialize(stream, packed);
    }

};


////////////////////////////////////////////////////////////////////////////////
// String
////////////////////////////////////////////////////////////////////////////////

template<>
struct TypeHandler<std::string> {

    static std::string
    deserialize(
        std::istream& stream
    ) {
        uint64_t size = TypeHandler<uint64_t>::deserialize(stream);
        std::vector<char> buffer(size, '\0');
        stream.read(&buffer[0], size);
        assert(not stream.fail());
        return std::string(buffer.begin(), buffer.end());
    }

    static void
    serialize(
        std::ostream& stream,
        const std::string& string
    ) {
        uint64_t size = string.size();
        TypeHandler<uint64_t>::serialize(stream, size);
        stream.write(string.data(), size);
    }

};


////////////////////////////////////////////////////////////////////////////////
// StorageContainer
////////////////////////////////////////////////////////////////////////////////

template<>
struct TypeHandler<StorageContainer> {

    static StorageContainer
    deserialize(
        std::istream& stream
    ) {
        StorageContainer value;
        stream >> value;
        return value;
    }


    static void
    serialize(
        std::ostream& stream,
        const StorageContainer& value
    ) {
        stream << value;
    }

};


////////////////////////////////////////////////////////////////////////////////
// StorageList
////////////////////////////////////////////////////////////////////////////////

template<>
struct TypeHandler<StorageList> {

    static StorageList
    deserialize(
        std::istream& stream
    ) {
        StorageList list;
        uint64_t size = TypeHandler<uint64_t>::deserialize(stream);
        list.reserve(size);
        for (size_t i=0; i < size; ++i) {
            list.append(TypeHandler<StorageContainer>::deserialize(stream));
        }
        return list;
    }


    static void
    serialize(
        std::ostream& stream,
        const StorageList& list
    ) {
        uint64_t size = list.size();
        TypeHandler<uint64_t>::serialize(stream, size);
        for (const auto& storageContainer : list) {
            TypeHandler<StorageContainer>::serialize(stream, storageContainer);
        }
    }

};


struct SerializationVisitor : public boost::static_visitor<> {

    SerializationVisitor(
        std::ostream& stream
    ) : m_stream(stream)
    {
    }

    template<typename T>
    void
    operator () (
        const T& value
    ) const {
        TypeHandler<T>::serialize(m_stream, value);
    }

    std::ostream& m_stream;
};

#define DESERIALIZE_CASE(typeName) \
    case TypeInfo<typeName>::Id: \
        return TypeHandler<typeName>::deserialize(stream)

static Variant
deserialize(
    TypeId typeId,
    std::istream& stream
) {
    switch (typeId) {
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
        default:
            return TypeHandler<StorageContainer>::deserialize(stream);
    }
}


} // namespace

std::ostream&
thrive::operator << (
    std::ostream& stream,
    const StorageContainer& storage
) {
    SerializationVisitor visitor(stream);
    const auto& content = storage.m_impl->m_content;
    TypeHandler<uint64_t>::serialize(stream, content.size());
    for (const auto& pair : content) {
        TypeHandler<std::string>::serialize(stream, pair.first);    
        TypeHandler<TypeId>::serialize(stream, pair.second.typeId);    
        boost::apply_visitor(visitor, pair.second.value);
    }
    return stream;
}


std::istream&
thrive::operator >> (
    std::istream& stream,
    StorageContainer& storage
) {
    uint64_t size = TypeHandler<uint64_t>::deserialize(stream);
    storage.m_impl->m_content.clear();
    for (size_t i = 0; i < size; ++i) {
        std::string key = TypeHandler<std::string>::deserialize(stream);
        TypeId typeId = TypeHandler<TypeId>::deserialize(stream);
        storage.m_impl->m_content[key] = StoredValue {
            typeId,
            deserialize(typeId, stream)
        };
    }
    return stream;
}





