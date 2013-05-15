# - Try to find irrKlang
# Once done this will define
#
# IRRKLANG_FOUND - system has irrKlang
# IRRKLANG_INCLUDE_DIRS - the irrKlang include directory
# IRRKLANG_LIBRARIES - Link these to use irrKlang
# IRRKLANG_DEFINITIONS - Compiler switches required for using irrKlang
#
# Copyright (c) 2006 Andreas Schneider <mail@cynapses.org>
#
# Redistribution and use is allowed according to the terms of the New
# BSD license.
# For details see the accompanying COPYING-CMAKE-SCRIPTS file.
#


if (IRRKLANG_LIBRARIES AND IRRKLANG_INCLUDE_DIRS)
  # in cache already
  set(IRRKLANG_FOUND TRUE)
else (IRRKLANG_LIBRARIES AND IRRKLANG_INCLUDE_DIRS)

  find_path(IRRKLANG_INCLUDE_DIR
    NAMES
      irrKlang.h
  )

  find_library(IRRKLANG_LIBRARY
    NAMES
      IrrKlang
  )

  find_library(IRRKLANG_BASIC_LIBRARY
    NAMES
      irrKlang.dll
  )

  find_library(IRRKLANG_ikpMP3_LIBRARY
    NAMES
      ikpMP3.dll
  )

  find_library(IRRKLANG_ikpFlac_LIBRARY
    NAMES
      ikpFlac.dll
  )
  set(IRRKLANG_LIBRARIES_DLL
    ${IRRKLANG_BASIC_LIBRARY};
    ${IRRKLANG_ikpMP3_LIBRARY};
    ${IRRKLANG_ikpFlac_LIBRARY}
  )

  if (IRRKLANG_LIBRARY)
    set(IRRKLANG_FOUND TRUE)
  endif (IRRKLANG_LIBRARY)

  set(IRRKLANG_INCLUDE_DIRS
    ${IRRKLANG_INCLUDE_DIR}
  )

  if (IRRKLANG_FOUND)
    set(IRRKLANG_LIBRARIES
      ${IRRKLANG_LIBRARIES}
      ${IRRKLANG_LIBRARY}
    )
  endif (IRRKLANG_FOUND)

  if (IRRKLANG_INCLUDE_DIRS AND IRRKLANG_LIBRARIES)
     set(IRRKLANG_FOUND TRUE)
  endif (IRRKLANG_INCLUDE_DIRS AND IRRKLANG_LIBRARIES)

  if (IRRKLANG_FOUND)
    if (NOT IRRKLANG_FIND_QUIETLY)
      message(STATUS "Found irrKlang: ${IRRKLANG_LIBRARIES}")
    endif (NOT IRRKLANG_FIND_QUIETLY)
  else (IRRKLANG_FOUND)
    if (IRRKLANG_FIND_REQUIRED)
      message(FATAL_ERROR "Could not find irrKlang")
    endif (IRRKLANG_FIND_REQUIRED)
  endif (IRRKLANG_FOUND)

  # show the IRRKLANG_INCLUDE_DIRS and IRRKLANG_LIBRARIES variables only in the advanced view
  mark_as_advanced(IRRKLANG_INCLUDE_DIRS IRRKLANG_LIBRARIES IRRKLANG_BASIC_LIBRARY IRRKLANG_ikpMP3_LIBRARY IRRKLANG_ikpFlac_LIBRARY IRRKLANG_LIBRARIES_DLL)

endif (IRRKLANG_LIBRARIES AND IRRKLANG_INCLUDE_DIRS)
