# - Try to find OpenAL
# Once done, this will define
#
#  OpenAL_FOUND - system has OpenAL
#  OpenAL_INCLUDE_DIRS - the OpenAL include directories
#  OpenAL_LIBRARIES - link these to use OpenAL

if(NOT OPENAL_ROOT)
    set(OPENAL_ROOT $ENV{OPENAL_ROOT})
endif()

include(LibFindMacros)

# Use pkg-config to get hints about paths
libfind_pkg_check_modules(OpenAL_PKGCONF OpenAL)

# Include dir
find_path(OpenAL_INCLUDE_DIR
    NAMES al.h
    PATHS ${OpenAL_PKGCONF_INCLUDE_DIRS} ${OPENAL_ROOT}/include
    PATH_SUFFIXES AL
)

# OpenAL library
find_library(OpenAL_LIBRARY
    NAMES OpenAL al openal OpenAL32
    PATHS ${OpenAL_PKGCONF_LIBRARY_DIRS} ${OPENAL_ROOT}/lib
)

# Set the include dir variables and the libraries and let libfind_process do the rest.
# NOTE: Singular variables for this library, plural for libraries this this lib depends on.
set(OpenAL_PROCESS_INCLUDES OpenAL_INCLUDE_DIR OpenAL_INCLUDE_DIRS)
set(OpenAL_PROCESS_LIBS OpenAL_LIBRARY OpenAL_LIBRARIES)
libfind_process(OpenAL)

