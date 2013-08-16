#pragma once

#include <random>

template<typename T>
T
randomFromRange(
    const T& min,
    const T& max
) {
    return min + (max - min) * float(std::rand()) / float(RAND_MAX);
}
