# - Try to find Vorbis
# Once done, this will define
#
#  Vorbis_FOUND - system has Vorbis
#  Vorbis_INCLUDE_DIRS - the Vorbis include directories
#  Vorbis_LIBRARIES - link these to use Vorbis

include(LibFindMacros)

# Use pkg-config to get hints about paths
libfind_pkg_check_modules(Vorbis_PKGCONF Vorbis)

# Include dir
find_path(Vorbis_INCLUDE_DIR
    NAMES vorbisfile.h
    PATHS ${Vorbis_PKGCONF_INCLUDE_DIRS} ${VORBIS_ROOT}/include
    PATH_SUFFIXES vorbis
)

# Vorbis library
find_library(Vorbis_LIBRARY
    NAMES vorbis
    PATHS ${Vorbis_PKGCONF_LIBRARY_DIRS} ${VORBIS_ROOT}/lib
)

# Vorbis file library
find_library(VorbisFile_LIBRARY
    NAMES vorbisfile
    PATHS ${Vorbis_PKGCONF_LIBRARY_DIRS} ${VORBIS_ROOT}/lib
)

# Set the include dir variables and the libraries and let libfind_process do the rest.
# NOTE: Singular variables for this library, plural for libraries this this lib depends on.
set(Vorbis_PROCESS_INCLUDES Vorbis_INCLUDE_DIR Vorbis_INCLUDE_DIRS)
set(Vorbis_PROCESS_LIBS Vorbis_LIBRARY VorbisFile_LIBRARY Vorbis_LIBRARIES)
libfind_process(Vorbis)
