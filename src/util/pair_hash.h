#pragma once

namespace std {

template<typename A, typename B>
struct hash<std::pair<A, B>> {

    std::size_t
    operator() (
        const std::pair<A, B>& pair
    ) const {
        std::size_t hashA = std::hash<A>()(pair.first);
        std::size_t hashB = std::hash<B>()(pair.first);
        return hashA ^ (hashB + 0x9e3779b9 + (hashA << 6) + (hashA >> 2));
    }
};

}
