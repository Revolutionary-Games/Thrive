# - Try to find cAudio
# Once done, this will define
#
# cAudio_FOUND - system has cAudio
# cAudio_INCLUDE_DIRS - the cAudio include directories
# cAudio_LIBRARIES - link these to use cAudio
#
# Accepted Inputs:
# Those defined in FindPackageHelper.cmake (use cAudio for ${name})
include(FindPackageHelper)
set(cAudio_INCLUDE_NAMES
  cAudio.h
  )

set(cAudio_LIBRARY_NAMES
  cAudio
  )

set(cAudio_INCLUDE_PATH_SUFFIXES
  include
  cAudio
  include/cAudio
  )

set(cAudio_LIBRARY_PATH_SUFFIXES
  bin/linux-x86
  )

set(cAudio_ONLY_SHARED TRUE) # Require shared linking

FindPackage(cAudio)
