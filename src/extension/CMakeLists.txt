# GDExtension for Thrive using C++.
# The non-Godot using C++ is in the "native" sibling folder.

# Note when the source code is packaged, godot-cpp (and other native)
# libraries are not included and need to be separately downloaded
# (for example from our official Thrive git repository)

# Use the absolute path
set(EXTENSION_DIR ${CMAKE_CURRENT_SOURCE_DIR})

file(GLOB_RECURSE EXTENSION_SOURCES 
    ${EXTENSION_DIR}/*.cpp
    ${EXTENSION_DIR}/*.hpp
)


message(STATUS "Extension directory: ${EXTENSION_DIR}")
message(STATUS "Extension sources: ${EXTENSION_SOURCES}")

if(NOT EXTENSION_SOURCES)
    message(FATAL_ERROR "No .cpp or .hpp files found in ${EXTENSION_DIR} or its subdirectories. Please ensure your source files are in this directory tree.")
endif()

add_library(thrive_extension SHARED ${EXTENSION_SOURCES})

target_include_directories(thrive_extension PRIVATE
  ${CMAKE_CURRENT_SOURCE_DIR}
  ${CMAKE_CURRENT_SOURCE_DIR}/../native
  ${CMAKE_CURRENT_SOURCE_DIR}/../../third_party/godot-cpp/include
  ${CMAKE_CURRENT_SOURCE_DIR}/../../third_party/godot-cpp/gen/include
  ${GODOT_GDEXTENSION_DIR}
)

target_link_libraries(thrive_extension
  PRIVATE
    thrive_native
    godot-cpp
)

target_compile_definitions(thrive_extension PRIVATE 
  THRIVE_EXTENSION_VERSION=${THRIVE_EXTENSION_VERSION}
  THRIVE_LIBRARY_VERSION=${NATIVE_LIBRARY_VERSION}
)

set_target_properties(thrive_extension PROPERTIES
  CXX_STANDARD 20
  CXX_STANDARD_REQUIRED ON
  CXX_EXTENSIONS OFF
  MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>"
)

if(THRIVE_LTO)
  message(STATUS "Enabling LTO for thrive_extension")
  set_target_properties(thrive_extension PROPERTIES INTERPROCEDURAL_OPTIMIZATION ON)
endif()

if(MSVC)
  target_compile_options(thrive_extension PRIVATE /W4 /WX /wd4068)
else()
  target_compile_options(thrive_extension PRIVATE -Wall -Wextra -Werror -Wno-unknown-pragmas)
endif()

install(TARGETS thrive_extension)
