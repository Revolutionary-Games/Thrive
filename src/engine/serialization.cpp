#include "engine/serialization.h"

#include <boost/variant.hpp>
#include <cfloat>
#include <stdexcept>
#include <unordered_map>

using namespace thrive;


namespace {

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
    StorageVector,
    StorageMap
>;

struct StoredValue {
    StorableTypeId typeId;
    Variant value;
};

} // namespace


struct StorageContainer::Implementation {

    template<typename T>
    T
    rawGet(
        const std::string& name,
        const T& defaultValue
    ) const {
        auto iter = m_content.find(name);
        if (iter == m_content.end()) {
            return defaultValue;
        }
        else if (iter->second.typeId != StorableTypeToId<T>::Id){
            return defaultValue;
        }
        else {
            return boost::get<T>(iter->second.value);
        }
    }

    template<typename T>
    void
    rawSet(
        const std::string& name,
        const T& value
    ) {
        m_content[name] = StoredValue{StorableTypeToId<T>::Id, value};
    }

    std::unordered_map<std::string, StoredValue> m_content;

};


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


#define NATIVE_STORABLE_TYPE(typeName) \
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
        const typeName& value \
    ) { \
        m_impl->rawSet<typeName>(key, value); \
    }

NATIVE_STORABLE_TYPE(bool)
NATIVE_STORABLE_TYPE(char)
NATIVE_STORABLE_TYPE(int8_t)
NATIVE_STORABLE_TYPE(int16_t)
NATIVE_STORABLE_TYPE(int32_t)
NATIVE_STORABLE_TYPE(int64_t)
NATIVE_STORABLE_TYPE(uint8_t)
NATIVE_STORABLE_TYPE(uint16_t)
NATIVE_STORABLE_TYPE(uint32_t)
NATIVE_STORABLE_TYPE(uint64_t)
NATIVE_STORABLE_TYPE(float)
NATIVE_STORABLE_TYPE(double)
NATIVE_STORABLE_TYPE(std::string)
NATIVE_STORABLE_TYPE(StorageContainer)
NATIVE_STORABLE_TYPE(StorageVector)
NATIVE_STORABLE_TYPE(StorageMap)



////////////////////////////////////////////////////////////////////////////////
// Serialization
////////////////////////////////////////////////////////////////////////////////

namespace {

template<typename T>
struct Serializer {

    static void
    serialize(
        std::ostream& stream,
        const T& value
    );

    static T
    deserialize(
        std::istream& stream
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
struct IntegralSerializer {

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

#define INTEGRAL_SERIALIZER(typeName) \
    template<> struct Serializer<typeName> : public IntegralSerializer<typeName> {};

INTEGRAL_SERIALIZER(int8_t)
INTEGRAL_SERIALIZER(int16_t)
INTEGRAL_SERIALIZER(int32_t)
INTEGRAL_SERIALIZER(int64_t)
INTEGRAL_SERIALIZER(uint8_t)
INTEGRAL_SERIALIZER(uint16_t)
INTEGRAL_SERIALIZER(uint32_t)
INTEGRAL_SERIALIZER(uint64_t)


////////////////////////////////////////////////////////////////////////////////
// Bool
////////////////////////////////////////////////////////////////////////////////

template<>
struct Serializer<bool> {

    static bool
    deserialize(
        std::istream& stream
    ) {
        auto value = Serializer<uint8_t>::deserialize(stream);
        return value > 0;
    }

    static void
    serialize(
        std::ostream& stream,
        const bool& value
    ) {
        uint8_t encoded = value ? 1 : 0;
        Serializer<uint8_t>::serialize(stream, encoded);
    }

};


////////////////////////////////////////////////////////////////////////////////
// Char
////////////////////////////////////////////////////////////////////////////////

template<>
struct Serializer<char> {

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
struct Serializer<float> {

    static float
    deserialize(
        std::istream& stream
    ) {
        uint64_t packed = Serializer<uint64_t>::deserialize(stream);
        return unpack754_32(packed);
    }

    static void
    serialize(
        std::ostream& stream,
        const float& value
    ) {
        uint64_t packed = pack754_32(value);
        Serializer<uint64_t>::serialize(stream, packed);
    }

};


////////////////////////////////////////////////////////////////////////////////
// Double
////////////////////////////////////////////////////////////////////////////////

template<>
struct Serializer<double> {

    static double
    deserialize(
        std::istream& stream
    ) {
        uint64_t packed = Serializer<uint64_t>::deserialize(stream);
        return unpack754_64(packed);
    }

    static void
    serialize(
        std::ostream& stream,
        const double& value
    ) {
        uint64_t packed = pack754_64(value);
        Serializer<uint64_t>::serialize(stream, packed);
    }

};


////////////////////////////////////////////////////////////////////////////////
// String
////////////////////////////////////////////////////////////////////////////////

template<>
struct Serializer<std::string> {

    static std::string
    deserialize(
        std::istream& stream
    ) {
        uint64_t size = Serializer<uint64_t>::deserialize(stream);
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
        Serializer<uint64_t>::serialize(stream, size);
        stream.write(string.data(), size);
    }

};


////////////////////////////////////////////////////////////////////////////////
// StorageContainer
////////////////////////////////////////////////////////////////////////////////

template<>
struct Serializer<StorageContainer> {

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
// StorageVector
////////////////////////////////////////////////////////////////////////////////

template<>
struct Serializer<StorageVector> {

    static StorageVector
    deserialize(
        std::istream& stream
    ) {
        StorageVector vector;
        uint64_t size = Serializer<uint64_t>::deserialize(stream);
        vector.reserve(size);
        for (size_t i=0; i < size; ++i) {
            vector[i] = Serializer<StorageContainer>::deserialize(stream);
        }
        return vector;
    }


    static void
    serialize(
        std::ostream& stream,
        const StorageVector& vector
    ) {
        uint64_t size = vector.size();
        Serializer<uint64_t>::serialize(stream, size);
        for (const auto& storageContainer : vector) {
            Serializer<StorageContainer>::serialize(stream, storageContainer);
        }
    }

};


////////////////////////////////////////////////////////////////////////////////
// StorageMap
////////////////////////////////////////////////////////////////////////////////

template<>
struct Serializer<StorageMap> {

    static StorageMap
    deserialize(
        std::istream& stream
    ) {
        StorageMap map;
        uint64_t size = Serializer<uint64_t>::deserialize(stream);
        std::string key;
        for (size_t i=0; i < size; ++i) {
            key = Serializer<std::string>::deserialize(stream);
            map[key] = Serializer<StorageContainer>::deserialize(stream);
        }
        return map;
    }


    static void
    serialize(
        std::ostream& stream,
        const StorageMap& map
    ) {
        uint64_t size = map.size();
        Serializer<uint64_t>::serialize(stream, size);
        for (const auto& pair : map) {
            Serializer<std::string>::serialize(stream, pair.first);
            Serializer<StorageContainer>::serialize(stream, pair.second);
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
        Serializer<T>::serialize(m_stream, value);
    }

    std::ostream& m_stream;
};

#define DESERIALIZE_CASE(typeName) \
    case StorableTypeToId<typeName>::Id: \
        return Serializer<typeName>::deserialize(stream)

static Variant
deserialize(
    StorableTypeId typeId,
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
        DESERIALIZE_CASE(StorageVector);
        DESERIALIZE_CASE(StorageMap);
        default:
            return Serializer<StorageContainer>::deserialize(stream);
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
    Serializer<uint64_t>::serialize(stream, content.size());
    for (const auto& pair : content) {
        Serializer<std::string>::serialize(stream, pair.first);    
        Serializer<StorableTypeId>::serialize(stream, pair.second.typeId);    
        boost::apply_visitor(visitor, pair.second.value);
    }
    return stream;
}


std::istream&
thrive::operator >> (
    std::istream& stream,
    StorageContainer& storage
) {
    uint64_t size = Serializer<uint64_t>::deserialize(stream);
    storage.m_impl->m_content.clear();
    for (size_t i = 0; i < size; ++i) {
        std::string key = Serializer<std::string>::deserialize(stream);
        StorableTypeId typeId = Serializer<StorableTypeId>::deserialize(stream);
        storage.m_impl->m_content[key] = StoredValue {
            typeId,
            deserialize(typeId, stream)
        };
    }
    return stream;
}




