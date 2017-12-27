#pragma once

// Do not include this in headers!
// Use a forward declaration like this:
// namespace sol {
// class state;
// }

#include "luajit/src/lua.hpp"

#ifdef __GNUC__
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wredundant-decls"
#pragma GCC diagnostic ignored "-Wfloat-equal"
#pragma GCC diagnostic ignored "-Wcast-qual"
#pragma GCC diagnostic ignored "-Wold-style-cast"
#pragma GCC diagnostic ignored "-Wctor-dtor-privacy"
#pragma GCC diagnostic ignored "-Wswitch-default"
#endif

#include "sol.hpp"

#ifdef __GNUC__
#pragma GCC diagnostic pop
#endif

// Helper macros

// For boost iterators
// TODO: check  http://sol2.readthedocs.io/en/latest/api/containers.html#for-handling-std-vector-map-set-and-others
// for alternate solutions
#define THRIVE_BIND_ITERATOR_TO_TABLE(getList) sol::state_view lua(s);  \
 const auto list = getList;                                             \
 sol::table table = lua.create_table();                                 \
                                                                        \
 auto iter = list.begin();                                              \
 for(int i = 1; iter != list.end(); ++i, ++iter){                       \
     table[i] = *iter;                                                  \
 }                                                                      \
                                                                        \
 return table;                                                          \
 

// For "castFrom" functions
#define LUA_CAST_FROM(className, baseType)                       \
"castFrom", [](baseType* baseptr){                               \
    return dynamic_cast<className*>(baseptr);                    \
}

