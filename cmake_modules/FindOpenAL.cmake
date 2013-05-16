# Locate OpenAL
# This module defines
# OPENAL_LIBRARY
# OPENAL_FOUND, if false, do not try to link to OpenAL 
# OPENAL_INCLUDE_DIR, where to find the headers

if (OPENAL_LIBRARY AND OPENAL_INCLUDE_DIR)
  # in cache already
  set(OPENAL_FOUND TRUE)
else (OPENAL_LIBRARY AND OPENAL_INCLUDE_DIR)


  FIND_PATH(OPENAL_INCLUDE_DIR 
    NAMES
      al.h
)

  FIND_LIBRARY(OPENAL_LIBRARY 
    NAMES 
      OpenAL al openal OpenAL32
)
endif (OPENAL_LIBRARY AND OPENAL_INCLUDE_DIR)