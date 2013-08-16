#pragma once

#include <functional>

namespace std {

template<>
struct hash<std::pair<int, int>> {
public:

    std::size_t operator() (const std::pair<int, int>& pair) const {
        // See stackoverflow.com/a/738234/1184818
        size_t seed = intHash(pair.first);
        return intHash(pair.second) + 0x9e3779b9 + (seed << 6) + (seed >> 2);
    }

private:

    const std::hash<int> intHash;

};

}
