#pragma once

#include "luajit.h"

#include <vector>

//! \file Utility functions for helping interacting with Lua

namespace thrive{

/**
* @brief Creates an std::vector from a Lua table. Ignores invalid types
*
* @note The values will be returned in random order
*/
template<class T>
auto
createVectorFromLuaTable(sol::table array){
    
    std::vector<T> result;

    for(const auto& pair : array){

        if(pair.second.is<T>())
            result.push_back(pair.second.as<T>());
    }

    return result;
}
    



}

