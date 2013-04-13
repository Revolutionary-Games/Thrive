#pragma once

template<typename Collection, typename Key>
bool
contains(
    const Collection& collection,
    const Key& key
) {
    auto iter = collection.find(key);
    return iter != collection.end();
}

