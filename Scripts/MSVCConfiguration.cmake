# MSVC-specific configurations
if(NOT WIN32 OR NOT MSVC)
  return()
endif()

# Force MDd runtime for debug
string(REPLACE "/MTd" "/MDd" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE}")
string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_RELWITHDEBINFO "${CMAKE_CXX_FLAGS_RELWITHDEBINFO}")
string(REPLACE "/MT" "/MD" CMAKE_CXX_FLAGS_MINSIZEREL "${CMAKE_CXX_FLAGS_MINSIZEREL}")

# CPU core optimization
include(ProcessorCount)
ProcessorCount(MSVC_CPU_CORES)
if(MSVC_CPU_CORES EQUAL 0)
  set(MSVC_CPU_CORES 1)
elseif(MSVC_CPU_CORES GREATER 4)
  math(EXPR MSVC_CPU_CORES "${MSVC_CPU_CORES} - 1")
endif()

# Warning suppressions
set(THRIVE_MSVC_WARNING_SUPPRESS
  /wd4005  # Macro redefinition
  /wd4273  # Inconsistent dll linkage
  /wd4141  # 'inline' used more than once
  /wd4068  # Unknown pragma
  /wd4100  # Unreferenced parameter
  /wd4127  # Conditional expression is constant
  /wd4189  # Local variable initialized but not referenced
  /wd4201  # Nonstandard extension: nameless struct/union
  /wd4244  # Conversion: possible loss of data
  /wd4245  # Conversion: signed/unsigned mismatch
  /wd4267  # Conversion from size_t
  /wd4305  # Truncation
  /wd4310  # Cast truncates constant value
  /wd4311  # Pointer truncation
  /wd4324  # Structure was padded
  /wd4458  # Declaration hides class member
  /wd4464  # Relative include path contains '..'
  /wd4505  # Unreferenced local function removed
  /wd4514  # Unreferenced inline function removed
  /wd4530  # C++ exception handler used
  /wd4574  # Macro defined to be 0
  /wd4611  # Interaction between '_setjmp' and C++ object destruction
  /wd4702  # Unreachable code
  /wd4710  # Function not inlined
  /wd4711  # Function selected for inline expansion
  /wd4820  # Padding added
  /wd4996  # Deprecated functions
)

function(configure_msvc_target target)
  target_compile_options(${target} PRIVATE
    /bigobj
    /GR 
    /EHsc
    /nologo
    /Zc:preprocessor
    /Zc:__cplusplus
    /Zc:externConstexpr
    /Zc:throwingNew
    /Zc:rvalueCast
    /permissive-
    ${THRIVE_MSVC_WARNING_SUPPRESS}
    $<$<CONFIG:Debug>:/MDd /Od /Ob1 /RTC1>
    $<$<NOT:$<CONFIG:Debug>>:/MD /O2 /Ob2 /DNDEBUG>
  )

  # Only add /MP if not using PCH
  get_target_property(uses_pch ${target} MSVC_PRECOMPILE_HEADERS)
  if(NOT uses_pch)
    target_compile_options(${target} PRIVATE /MP${MSVC_CPU_CORES})
  endif()

  target_compile_definitions(${target} PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    _CRT_SECURE_NO_WARNINGS
    _CRT_NONSTDC_NO_WARNINGS
    _SCL_SECURE_NO_WARNINGS
    _HAS_EXCEPTIONS=1
  )

  set_target_properties(${target} PROPERTIES
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
    VS_DEBUGGER_WORKING_DIRECTORY "${CMAKE_BINARY_DIR}/bin/$<CONFIG>"
  )

  if(MSVC_CPU_CORES LESS 4)
    set(UNITY_BATCH_SIZE 10)
  elseif(MSVC_CPU_CORES LESS 8)
    set(UNITY_BATCH_SIZE 20)
  else()
    set(UNITY_BATCH_SIZE 30)
  endif()

  set_target_properties(${target} PROPERTIES
    UNITY_BUILD ON
    UNITY_BUILD_MODE GROUP
    UNITY_BUILD_BATCH_SIZE ${UNITY_BATCH_SIZE}
    VS_GLOBAL_EnableUnitySupport "true"
    VS_GLOBAL_UseUnitySupport "true"
    VS_GLOBAL_UnitySupport "true"
    VS_GLOBAL_CLToolChain "v143"
    VS_GLOBAL_PlatformToolset "v143"
    VS_GLOBAL_DisableSpecificWarnings "MSB8027;MSB8065;4244;4267;4996;4273;4141;4005;4574"
    VS_GLOBAL_TreatWarningAsError "false"
    VS_GLOBAL_WarningLevel "3"
  )
endfunction()