# - Try to find Ogg
# Once done, this will define
#
#  Ogg_FOUND - system has Ogg
#  Ogg_INCLUDE_DIRS - the Ogg include directories
#  Ogg_LIBRARIES - link these to use Ogg

include(LibFindMacros)

# Use pkg-config to get hints about paths
libfind_pkg_check_modules(Ogg_PKGCONF Ogg)

# Include dir
find_path(Ogg_INCLUDE_DIR
    NAMES ogg/ogg.h
    PATHS ${Ogg_PKGCONF_INCLUDE_DIRS} ${OGG_ROOT}/include
)

# Finally the library itself
find_library(Ogg_LIBRARY
    NAMES ogg
    PATHS ${Ogg_PKGCONF_LIBRARY_DIRS} ${OGG_ROOT}/lib
)

# Set the include dir variables and the libraries and let libfind_process do the rest.
# NOTE: Singular variables for this library, plural for libraries this this lib depends on.
set(Ogg_PROCESS_INCLUDES Ogg_INCLUDE_DIR Ogg_INCLUDE_DIRS)
set(Ogg_PROCESS_LIBS Ogg_LIBRARY Ogg_LIBRARIES)
libfind_process(Ogg)
