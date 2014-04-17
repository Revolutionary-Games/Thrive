# OGG CMake build file
cmake_minimum_required(VERSION 2.6)
project(OGG)	
set(OGG_SRC_DIR 
    "${CMAKE_CURRENT_SOURCE_DIR}/libogg-1.3.1"
    CACHE STRING "Path to unpacked libogg source archive"
)
string(REGEX REPLACE "\\\\" "/" OGG_SRC_DIR "${OGG_SRC_DIR}") 

message(${OGG_SRC_DIR})
set(OGG_HEADER_FILES
    ${OGG_SRC_DIR}/include/ogg/ogg.h
    ${OGG_SRC_DIR}/include/ogg/os_types.h
)

set(OGG_SRC_FILES
    ${OGG_SRC_DIR}/src/bitwise.c
    ${OGG_SRC_DIR}/src/framing.c
    ${OGG_SRC_DIR}/win32/ogg.def
)

include_directories(${OGG_SRC_DIR}/include)

add_library(ogg STATIC ${OGG_SRC_FILES} ${OGG_HEADER_FILES})

install(TARGETS ogg
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION lib
)

install(FILES ${OGG_HEADER_FILES}
    DESTINATION include/ogg
)
