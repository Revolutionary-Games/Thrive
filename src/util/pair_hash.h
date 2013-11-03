#pragma once

namespace std {

/**
 * @brief Template specialization for std::pair
 *
 */
template<typename A, typename B>
struct hash<std::pair<A, B>> {

    /**
     * @brief Computes a combined hash from the pair's elements and returns it
     *
     * @param pair
     *   The pair to hash
     *
     * @return
     *   The hash
     */
    std::size_t
    operator() (
        const std::pair<A, B>& pair
    ) const {
        std::size_t hashA = std::hash<A>()(pair.first);
        std::size_t hashB = std::hash<B>()(pair.second);
        return hashA ^ (hashB + 0x9e3779b9 + (hashA << 6) + (hashA >> 2));
    }
};

}
