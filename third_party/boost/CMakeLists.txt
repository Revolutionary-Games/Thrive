# Copyright 2019, 2021 Peter Dimov
# Distributed under the Boost Software License, Version 1.0.
# See accompanying file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt

# Customized for Thrive to just include the needed info

cmake_minimum_required(VERSION 3.5...3.16)

# The default build type must be set before project()
if(CMAKE_SOURCE_DIR STREQUAL CMAKE_CURRENT_SOURCE_DIR AND NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
  set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type" FORCE)
  set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "Debug" "Release" "MinSizeRel" "RelWithDebInfo")
endif()

project(Boost VERSION 1.82.0 LANGUAGES CXX)

set(BOOST_SUPERPROJECT_VERSION ${PROJECT_VERSION})
set(BOOST_SUPERPROJECT_SOURCE_DIR ${PROJECT_SOURCE_DIR})

list(APPEND CMAKE_MODULE_PATH ${PROJECT_SOURCE_DIR}/tools/cmake/include)

add_subdirectory(libs/assert)
add_subdirectory(libs/circular_buffer)
add_subdirectory(libs/concept_check)
add_subdirectory(libs/config)
add_subdirectory(libs/container_hash)
add_subdirectory(libs/core)
add_subdirectory(libs/describe)
add_subdirectory(libs/intrusive)
add_subdirectory(libs/move)
add_subdirectory(libs/mp11)
add_subdirectory(libs/preprocessor)
add_subdirectory(libs/static_assert)
add_subdirectory(libs/throw_exception)
add_subdirectory(libs/type_traits)
