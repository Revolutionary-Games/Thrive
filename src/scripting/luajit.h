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
#endif


#include "sol.hpp"

#ifdef __GNUC__
#pragma GCC diagnostic pop
#endif

