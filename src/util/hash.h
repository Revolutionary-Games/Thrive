#pragma once

#include <functional>

namespace std {

/**
 * @brief Template specialization for hashing a pair of integers
 */
template<> struct hash<std::pair<int, int>> {
public:
    /**
     * @brief Computes a hash for a pair of ints
     *
     * @param pair
     *
     * @return
     */
    std::size_t
        operator()(const std::pair<int, int>& pair) const
    {
        // See stackoverflow.com/a/738234/1184818
        size_t seed = intHash(pair.first);
        return intHash(pair.second) + 0x9e3779b9 + (seed << 6) + (seed >> 2);
    }

private:
    const std::hash<int> intHash;
};

} // namespace std
