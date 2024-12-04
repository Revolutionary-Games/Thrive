# CPU core detection for parts of build that need to know that beforehand
include(ProcessorCount)
processorcount(CPU_CORES)
if(CPU_CORES EQUAL 0)
  set(CPU_CORES 1)
elseif(CPU_CORES GREATER 4)
  math(EXPR CPU_CORES "${CPU_CORES} - 1")
endif()

# Print source files for a target
function(print_source_files target)
  get_target_property(target_sources ${target} SOURCES)
  if(target_sources)
    set(cpp_files "")
    foreach(source ${target_sources})
      if(source MATCHES ".*\\.(cpp|cxx|cc)$")
        get_filename_component(filename ${source} NAME)
        list(APPEND cpp_files ${filename})
      endif()
    endforeach()

    list(LENGTH cpp_files total_files)
    if(total_files GREATER 0)
      list(SORT cpp_files)
      message(STATUS "Files for ${target}:")
      foreach(filename ${cpp_files})
        message(STATUS "  ${filename}")
      endforeach()
    endif()
  endif()
endfunction()

# Unity build configuration based on CPU cores
function(configure_unity_build target)
  if(WIN32 AND MSVC)
    if(CPU_CORES LESS 4)
      set(UNITY_BATCH_SIZE 10)
    elseif(CPU_CORES LESS 8)
      set(UNITY_BATCH_SIZE 20)
    else()
      set(UNITY_BATCH_SIZE 30)
    endif()

    set_target_properties(${target} PROPERTIES
      UNITY_BUILD ON
      UNITY_BUILD_MODE GROUP
      UNITY_BUILD_BATCH_SIZE ${UNITY_BATCH_SIZE}
    )
  endif()
endfunction()
