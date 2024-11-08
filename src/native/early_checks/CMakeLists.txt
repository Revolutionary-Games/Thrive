# Early load library that allows checking things before loading the main library
add_library(early_checks SHARED
  EarlyInterop.h 
  EarlyInterop.cpp
  ../helpers/CPUCheck.hpp
  ../helpers/CPUCheckResult.h
)

if(PRINT_SOURCE_FILES)
  print_source_files(early_checks)
endif()

configure_target(early_checks)

target_compile_definitions(early_checks PRIVATE 
  EARLY_CHECK_BUILD
  _HAS_EXCEPTIONS=1
)

# CPU compatibility settings
if(MSVC)
  # MSVC-specific old CPU support
  target_compile_options(early_checks PRIVATE
    /arch:IA32  # Base x86 instruction set
    /Qspectre-  # Disable Spectre mitigations for older CPUs
  )
else()
  # For GCC/Clang, use baseline CPU support
  target_compile_options(early_checks PRIVATE
    -march=sandybridge  # Minimum CPU support for required instructions
    -Wall 
    -Wextra 
    -Wpedantic 
    -Wno-unknown-pragmas
    $<$<CONFIG:Release>:-O3>
    $<$<CONFIG:Distribution>:-O3>
  )
endif()

set_target_properties(early_checks PROPERTIES
  CXX_STANDARD 20
  CXX_STANDARD_REQUIRED ON
  CXX_EXTENSIONS OFF
  OUTPUT_NAME "libearly_checks"
  ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/${CMAKE_BUILD_TYPE}"
  LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
  RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
  PDB_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}"
)

if(THRIVE_LTO)
  set_target_properties(early_checks PROPERTIES 
    INTERPROCEDURAL_OPTIMIZATION ON
  )
endif()

# Installation configuration
install(TARGETS early_checks
  RUNTIME DESTINATION "${INSTALL_BUILD_TYPE}/bin"
  LIBRARY DESTINATION "${INSTALL_BUILD_TYPE}/lib"
  ARCHIVE DESTINATION "${INSTALL_BUILD_TYPE}/lib"
)

if(MSVC AND CMAKE_BUILD_TYPE STREQUAL "Debug")
  install(FILES "$<TARGET_PDB_FILE:early_checks>"
    DESTINATION "${INSTALL_BUILD_TYPE}/bin"
    OPTIONAL
  )
endif()

# Create post-build directory structure
add_custom_command(TARGET early_checks POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E make_directory 
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin"
  COMMAND ${CMAKE_COMMAND} -E make_directory 
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/lib"
)

add_custom_command(TARGET early_checks POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "$<TARGET_FILE:early_checks>"
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin/"
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "$<TARGET_LINKER_FILE:early_checks>"
    "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/lib/"
)

if(MSVC AND CMAKE_BUILD_TYPE STREQUAL "Debug")
  add_custom_command(TARGET early_checks POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
      "$<TARGET_PDB_FILE:early_checks>"
      "${CMAKE_BINARY_DIR}/install/${THRIVE_OS}/${INSTALL_BUILD_TYPE}/bin/"
  )
endif()
