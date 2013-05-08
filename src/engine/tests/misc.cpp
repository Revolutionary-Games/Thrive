#include <array>
#include <chrono>
#include <gtest/gtest.h>
#include <iostream>
#include <unordered_map>

using Clock = std::chrono::high_resolution_clock;

static void
print(
    const std::string& caption,
    const Clock::duration& duration
) {
    auto microseconds = std::chrono::duration_cast<std::chrono::microseconds>(duration);
    std::cout << caption << ": " << microseconds.count() << " us" << std::endl;
}

static const size_t N = 1000;

TEST(MapPerformance, KeyIsSize_t) {
    Clock::time_point begin, end;
    // Map building
    std::unordered_map<size_t, std::string> fastMap;
    begin = Clock::now();
    for (size_t i = 0; i < N; ++i) {
        fastMap.insert(std::make_pair(i, std::string("test")));
    }
    end = Clock::now();
    print("Time for insertion:", end - begin);
    // Retrieval
    begin = Clock::now();
    for (size_t i = N-1; i!=0; --i) {
        EXPECT_EQ("test", fastMap.at(i));
    }
    end = Clock::now();
    std::cout << fastMap.at(1500);
    print("Time for retrieval:", end - begin);
}


TEST(MapPerformance, KeyIsString) {
    Clock::time_point begin, end;
    // Build keys
    std::array<std::string, N> keys;
    for (size_t i = 0; i < N; ++i) {
        keys[i] = std::to_string(i);
    }
    // Map building
    std::unordered_map<std::string, std::string> fastMap;
    begin = Clock::now();
    for (size_t i = 0; i < N; ++i) {
        fastMap.insert(std::make_pair(keys[i], std::string("test")));
    }
    end = Clock::now();
    print("Time for insertion:", end - begin);
    // Retrieval
    begin = Clock::now();
    for (size_t i = N-1; i!=0; --i) {
        EXPECT_EQ("test", fastMap.at(keys[i]));
    }
    end = Clock::now();
    print("Time for retrieval:", end - begin);
}





