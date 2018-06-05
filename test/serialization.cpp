#include "engine/serialization.h"

#include <gtest/gtest.h>

using namespace thrive;


template<typename T>
static T
    copy(const T& value)
{
    // Prepare
    StorageContainer container;
    container.set("value", value);
    // Serialize
    std::ostringstream outputStream(std::ios_base::out | std::ios_base::binary);
    outputStream << container;
    // Deserialize
    auto data = outputStream.str();
    StorageContainer copy;
    std::istringstream inputStream(
        outputStream.str(), std::ios_base::in | std::ios_base::binary);
    inputStream >> copy;
    EXPECT_TRUE(copy.contains("value"));
    return copy.get<T>("value");
}

template<typename T>
static void
    testSerialization(const T& value)
{
    EXPECT_TRUE(value == copy(value));
}

TEST(Serialization, bool)
{
    testSerialization(true);
    testSerialization(false);
}


TEST(Serialization, float)
{
    std::vector<float> floats = {0.0f, 3.1415f, -18.0f};
    for(float f : floats) {
        EXPECT_FLOAT_EQ(f, copy(f));
    }
}


TEST(Serialization, double)
{
    std::vector<double> doubles = {0.0, 3.1415, -18.0};
    for(double d : doubles) {
        EXPECT_DOUBLE_EQ(d, copy(d));
    }
}


TEST(Serialization, integer)
{
    testSerialization(2001);
    testSerialization(-18000);
}


TEST(Serialization, string)
{
    std::vector<std::string> strings{"thrive", ""};
    for(const std::string& string : strings) {
        testSerialization(string);
    }
}


TEST(Serialization, StorageContainer)
{
    StorageContainer inner;
    inner.set<std::string>("value", "thrive");
    StorageContainer outer;
    outer.set<StorageContainer>("container", inner);
    StorageContainer outerCopy = copy(outer);
    EXPECT_TRUE(outerCopy.contains("container"));
    StorageContainer innerCopy = outerCopy.get<StorageContainer>("container");
    EXPECT_TRUE(innerCopy.contains("value"));
}


TEST(Serialization, ColourValue)
{
    Ogre::ColourValue colour = Ogre::ColourValue::White;
    testSerialization(colour);
}



TEST(Serialization, Vector3)
{
    Ogre::Vector3 vector(1, 2, 3);
    testSerialization(vector);
}
