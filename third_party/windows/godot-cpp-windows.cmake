# Windows-specific godot-cpp configuration
cmake_minimum_required(VERSION 3.13)
project(godot-cpp LANGUAGES CXX)

include(ProcessorCount)
ProcessorCount(CPU_CORES)
if(CPU_CORES EQUAL 0)
  set(CPU_CORES 1)
elseif(CPU_CORES GREATER 4)
  math(EXPR CPU_CORES "${CPU_CORES} - 1")
endif()

option(GENERATE_TEMPLATE_GET_NODE "Generate template get_node" ON)
option(GODOT_CPP_SYSTEM_HEADERS "Expose headers as SYSTEM." ON)
option(GODOT_CPP_WARNING_AS_ERROR "Treat warnings as errors" OFF)

# Add path to modules
list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake/")

if("${CMAKE_BUILD_TYPE}" STREQUAL "")
  set(CMAKE_BUILD_TYPE Debug)
endif()

# Hot reload is enabled by default in Debug-builds should be further tested
if("${CMAKE_BUILD_TYPE}" STREQUAL "Debug")
  option(GODOT_ENABLE_HOT_RELOAD "Build with hot reload support" ON)
else()
  option(GODOT_ENABLE_HOT_RELOAD "Build with hot reload support" OFF)
endif()

# Detect bits (32/64) if not defined
if(NOT DEFINED BITS)
  set(BITS 32)
    if(CMAKE_SIZEOF_VOID_P EQUAL 8)
      set(BITS 64)
  endif()
endif()

# Input from user for GDExtension interface header and the API JSON file
set(GODOT_GDEXTENSION_DIR "gdextension" CACHE STRING "")
set(GODOT_CUSTOM_API_FILE "" CACHE STRING "")

set(GODOT_GDEXTENSION_API_FILE "${GODOT_GDEXTENSION_DIR}/extension_api.json")
if(NOT "${GODOT_CUSTOM_API_FILE}" STREQUAL "")  # User-defined override.
  set(GODOT_GDEXTENSION_API_FILE "${GODOT_CUSTOM_API_FILE}")
endif()

set(FLOAT_PRECISION "single" CACHE STRING "")
if ("${FLOAT_PRECISION}" STREQUAL "double")
  add_definitions(-DREAL_T_IS_DOUBLE)
endif()

set(GODOT_COMPILE_FLAGS "")

if ("${CMAKE_CXX_COMPILER_ID}" STREQUAL "MSVC")
  set(GODOT_COMPILE_FLAGS "/utf-8 /nologo") 

  if(CMAKE_BUILD_TYPE MATCHES Debug)
    set(GODOT_COMPILE_FLAGS "${GODOT_COMPILE_FLAGS} /MDd")
  else()
    set(GODOT_COMPILE_FLAGS "${GODOT_COMPILE_FLAGS} /MD /O2")
    STRING(REGEX REPLACE "/RTC(su|[1su])" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
    string(REPLACE "/RTC1" "" CMAKE_CXX_FLAGS_DEBUG ${CMAKE_CXX_FLAGS_DEBUG})
  endif()

  add_definitions(-DNOMINMAX)
endif()

# Generate source from the bindings file
find_package(Python3 3.4 REQUIRED)
if(GENERATE_TEMPLATE_GET_NODE)
  set(GENERATE_BINDING_PARAMETERS "True")
else()
  set(GENERATE_BINDING_PARAMETERS "False")
endif()

execute_process(
  COMMAND "${Python3_EXECUTABLE}" "-c"
  "import binding_generator; binding_generator.print_file_list(\"${GODOT_GDEXTENSION_API_FILE}\", \"${CMAKE_CURRENT_BINARY_DIR}\", headers=True, sources=True)"
  WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
  OUTPUT_VARIABLE GENERATED_FILES_LIST
  OUTPUT_STRIP_TRAILING_WHITESPACE
)

add_custom_command(
  OUTPUT ${GENERATED_FILES_LIST}
  COMMAND "${Python3_EXECUTABLE}" "-c" "import binding_generator; binding_generator.generate_bindings(\"${GODOT_GDEXTENSION_API_FILE}\", \"${GENERATE_BINDING_PARAMETERS}\", \"${BITS}\", \"${FLOAT_PRECISION}\", \"${CMAKE_CURRENT_BINARY_DIR}\")"
  VERBATIM
  WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
  MAIN_DEPENDENCY ${GODOT_GDEXTENSION_API_FILE}
  DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/binding_generator.py
  COMMENT "Generating bindings"
)

# Get Sources using GLOB
file(GLOB_RECURSE SOURCES CONFIGURE_DEPENDS src/*.c**)
file(GLOB_RECURSE HEADERS CONFIGURE_DEPENDS include/*.h**)

# Configure object.cpp 
if(MSVC)
  set_source_files_properties(
      "${CMAKE_CURRENT_SOURCE_DIR}/src/core/object.cpp"
      "${CMAKE_CURRENT_BINARY_DIR}/gen/src/classes/object.cpp"
      PROPERTIES 
      SKIP_UNITY_BUILD_INCLUSION ON
      COMPILE_FLAGS "/nologo"
  )
endif()

add_library(${PROJECT_NAME} STATIC
  ${SOURCES}
  ${HEADERS}
  ${GENERATED_FILES_LIST}
)

add_library(godot::cpp ALIAS ${PROJECT_NAME})

# Configure compiler options for MSVC
if(MSVC)
  string(REGEX REPLACE "/W[0-4]" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")

# These are the survivors that earned their right to silence
  target_compile_options(${PROJECT_NAME} PRIVATE
    /MP${CPU_CORES}
    /bigobj
    /GR 
    /EHsc
    /nologo
    $<$<CONFIG:Debug>:/MDd /Od /Ob1 /RTC1>
    $<$<NOT:$<CONFIG:Debug>>:/MD /O2 /Ob2 /DNDEBUG>
    # Critical warning suppressions
    /wd4005  # Macro redefinition
    /wd4273  # Inconsistent dll linkage
    /wd4141  # 'inline' used more than once
    # Other warning suppressions
    /wd4068  # Unknown pragma
    /wd4100  # Unreferenced formal parameter
    /wd4127  # Conditional expression is constant
    /wd4189  # Local variable initialized but not referenced
    /wd4201  # Nonstandard extension: nameless struct/union
    /wd4244  # Conversion from 'type1' to 'type2', possible loss of data
    /wd4245  # Conversion from 'type1' to 'type2', signed/unsigned mismatch
    /wd4267  # Conversion from 'size_t' to 'type', possible loss of data
    /wd4305  # Truncation from 'type1' to 'type2'
    /wd4310  # Cast truncates constant value
    /wd4311  # Pointer truncation
    /wd4458  # Declaration hides class member
    /wd4505  # Unreferenced local function removed
    /wd4702  # Unreachable code
    /wd4996  # Deprecated functions
    /wd4514  # Unreferenced inline function removed
    /wd4710  # Function not inlined
    /wd4711  # Function selected for inline expansion
    /wd4820  # Padding added
    /wd4464  # Relative include path contains '..'
    /vmg  # Use full generality for member pointers
  )

  target_compile_definitions(${PROJECT_NAME} PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    _CRT_SECURE_NO_WARNINGS
    _HAS_EXCEPTIONS=1
    TYPED_METHOD_BIND
  )

# CPU core optimization
  if(CPU_CORES LESS 4)
    set(UNITY_BATCH_SIZE 10)
    elseif(CPU_CORES LESS 8)
      set(UNITY_BATCH_SIZE 20)
        else()
          set(UNITY_BATCH_SIZE 30)
  endif()

  set_target_properties(${PROJECT_NAME} PROPERTIES
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
    UNITY_BUILD ON
    UNITY_BUILD_MODE GROUP
    UNITY_BUILD_BATCH_SIZE ${UNITY_BATCH_SIZE}
    VS_GLOBAL_EnableUnitySupport "true"
    VS_GLOBAL_UseUnitySupport "true"
    VS_GLOBAL_UnitySupport "true"
    VS_GLOBAL_CLToolChain "v143"
    VS_GLOBAL_PlatformToolset "v143"
    VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273;4141;4005;4141"
    VS_GLOBAL_TreatWarningAsError "false"
    VS_GLOBAL_WarningLevel "0"
  )
endif()

target_compile_definitions(${PROJECT_NAME} PUBLIC
  $<$<CONFIG:Debug>:DEBUG_ENABLED DEBUG_METHODS_ENABLED>
)

target_include_directories(${PROJECT_NAME} PUBLIC
  include
  ${CMAKE_CURRENT_BINARY_DIR}/gen/include
  ${GODOT_GDEXTENSION_DIR}
)

# Add the compile flags
set_property(TARGET ${PROJECT_NAME} APPEND_STRING PROPERTY COMPILE_FLAGS ${GODOT_COMPILE_FLAGS})

# Create the correct name
string(TOLOWER "${CMAKE_SYSTEM_NAME}" SYSTEM_NAME)
string(TOLOWER "${CMAKE_BUILD_TYPE}" BUILD_TYPE)

set(OUTPUT_NAME "godot-cpp.${SYSTEM_NAME}.${BUILD_TYPE}.${BITS}")

set_target_properties(${PROJECT_NAME}
  PROPERTIES
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
    POSITION_INDEPENDENT_CODE ON
    OUTPUT_NAME "${OUTPUT_NAME}"
)
