# TinyXML CMake build file
cmake_minimum_required(VERSION 2.6)
project(TinyXML)	
set(TinyXML_SRC_DIR 
    "${CMAKE_CURRENT_SOURCE_DIR}/tinyxml"
    CACHE STRING "Path to unpacked tinyxml source archive"
)
string(REGEX REPLACE "\\\\" "/" TinyXML_SRC_DIR "${TinyXML_SRC_DIR}") 

set(TinyXML_HEADER_FILES
    ${TinyXML_SRC_DIR}/tinyxml.h
    ${TinyXML_SRC_DIR}/tinystr.h
)

set(TinyXML_SRC_FILES
    ${TinyXML_SRC_DIR}/tinyxml.cpp
    ${TinyXML_SRC_DIR}/tinystr.cpp
    ${TinyXML_SRC_DIR}/tinyxmlparser.cpp
    ${TinyXML_SRC_DIR}/tinyxmlerror.cpp
)

include_directories(${TinyXML_SRC_DIR}/include)

add_library(tinyxml STATIC ${TinyXML_SRC_FILES} ${TinyXML_HEADER_FILES})

install(TARGETS tinyxml
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION lib
)

install(FILES ${TinyXML_HEADER_FILES}
    DESTINATION include/tinyxml
)
