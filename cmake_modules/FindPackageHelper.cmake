# Copyright (c) 2010 Xynilex Project
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,  
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.

# This macro uses the following variable for input:
# ${name}_HOME - hint for finding the path
# ${name}_INTERNAL_HOME - internal hint for finding path (this is used first)
# ${name}_FIND_REQUIRED - should we exit if it is not found
# ${name}_FIND_QUIETLY - disable status messages
# ${name}_INCLUDE_NAMES - files to look for in the include directory
# ${name}_LIBRARY_NAMES - libraries to look for in the library directory
# ${name}_PREFIX_PATH - directories to look in
# ${name}_INCLUDE_PATH_SUFFIXES - suffixes to look in
# ${name}_LIBRARY_PATH_SUFFIXES - suffixes to look in
# ${name}_ONLY_STATIC - only look for static libraries
# ${name}_ONLY_SHARED - only look for shared libraries
# ${name}_PARENTS - parent names (use these variables for HOME, INTERNAL_HOME, etc)
#
# Outputs the following
# ${name}_FOUND - true if package was found
# ${name}_INCLUDE_DIRS - include directories
# ${name}_LIBRARIES - libraries needed to link
# ${name}_SHARED_LIBRARIES - libraries that are shared
# ${name}_INCLUDES_FOUND - list of files specified in ${name}_INCLUDE_NAMES found
# ${name}_LIBRARIES_FOUND - list of libraries specified in ${name}_LIBRARY_NAMES found

include(LibraryPathHints)

macro(FindPackage name)

  set(EVN_${name}_HOME $ENV{${name}_HOME})

  # Saves some typing
  set(CUR_HOME ${${name}_HOME})
  set(CUR_ENV_HOME ${ENV_${name}_HOME})
  set(CUR_INTERNAL_HOME ${${name}_INTERNAL_HOME})
  set(CUR_FIND_REQUIRED ${${name}_FIND_REQUIRED})
  set(CUR_FIND_QUIETLY ${${name}_FIND_QUIETLY})
  set(CUR_INCLUDE_NAMES ${${name}_INCLUDE_NAMES})
  set(CUR_LIBRARY_NAMES ${${name}_LIBRARY_NAMES})
  set(CUR_PREFIX_PATH ${${name}_PREFIX_PATH})
  set(CUR_INCLUDE_PATH_SUFFIXES ${${name}_INCLUDE_PATH_SUFFIXES})
  set(CUR_LIBRARY_PATH_SUFFIXES ${${name}_LIBRARY_PATH_SUFFIXES})
  set(CUR_ONLY_STATIC ${${name}_ONLY_STATIC})
  set(CUR_ONLY_SHARED ${${name}_ONLY_SHARED})
  set(CUR_STATIC_PERFERRED ${${name}_STATIC_PERFERRED})
  set(CUR_SHARED_PERFERRED ${${name}_SHARED_PERFERRED})

  foreach(parent ${${name}_PARENTS})
    if(${parent}_HOME)
      set(CUR_HOME ${${parent}_HOME})
    endif()
    if(ENV_${parent}_HOME)
      set(CUR_ENV_HOME $ENV{${name}_HOME})
    endif()
    if(${parent}_INTERNAL_HOME)
      set(CUR_INTERNAL_HOME ${${parent}_INTERNAL_HOME})
    endif()
    if(${parent}_FIND_QUIETLY)
      set(CUR_FIND_QUIETLY ${${parent}_FIND_QUIETLY})
    endif()
    if(${parent}_FIND_REQUIRED)
      set(CUR_FIND_REQUIRED ${${parent}_FIND_REQUIRED})
    endif()

    set(CUR_PREFIX_PATH ${CUR_PREFIX_PATH} ${${parent}_PREFIX_PATH})
    set(CUR_INCLUDE_PATH_SUFFIXES ${CUR_INCLUDE_PATH_SUFFIXES} ${${parent}_INCLUDE_PATH_SUFFIXES})
    set(CUR_LIBRARY_PATH_SUFFIXES ${CUR_LIBRARY_PATH_SUFFIXES} ${${parent}_LIBRARY_PATH_SUFFIXES})
  endforeach()

  set(${name}_HOME ${CUR_HOME} CACHE PATH "${name} installation directory.")

  # Output status message
  if(NOT CUR_FIND_QUIETLY)
    message(STATUS "Check for library '${name}'")
  endif()

  # If this package is already found, skip it
  if(${name}_FOUND)
    if(NOT CUR_FIND_QUIETLY)
      message(STATUS "Check for library '${name}' -- found")
    endif()
    return()
  endif()

  set(${name}_FOUND TRUE) # Found unless proven otherwise

  # Some hardcoded hints
  GetLibraryPathHints(${name})

  set(CUR_SEARCH_DIRS
    "${CUR_INTERNAL_HOME}"
    "${CUR_HOME}"
    "${CUR_ENV_HOME}"
    "${CUR_PREFIX_PATH}"
    ${${name}_PATH_HINTS}
  )
  set(CUR_INCLUDE_PATH_SUFFIXES ${CUR_INCLUDE_PATH_SUFFIXES} ${${name}_PATH_SUFFIX_HINTS})
  set(CUR_LIBRARY_PATH_SUFFIXES ${CUR_LIBRARY_PATH_SUFFIXES} ${${name}_PATH_SUFFIX_HINTS})

  # Look through all the include names and see if they are available
  set(CUR_INCLUDE_DIRS "")
  set(CUR_INCLUDES_FOUND "")

  foreach(inc ${CUR_INCLUDE_NAMES})
    if(NOT CUR_FIND_QUIETLY)
      message(STATUS "  Check for ${inc}")
    endif()

    # Try to find the path
    find_path(${inc}_PATH
      NAMES "${inc}"
      PATHS ${CUR_SEARCH_DIRS}
      PATH_SUFFIXES ${CUR_INCLUDE_PATH_SUFFIXES}
    )

    # If we can't find the path, end the search.
    if(NOT ${inc}_PATH)
      if(NOT CUR_FIND_QUIETLY)
        message(STATUS "  Check for ${inc} -- missing")
      endif()
      set(${name}_FOUND FALSE)
    else()
      if(NOT CUR_FIND_QUIETLY)
        message(STATUS "  Check for ${inc} -- found")
      endif()
      set(CUR_INCLUDE_DIRS ${CUR_INCLUDE_DIRS} ${${inc}_PATH})
      set(CUR_INCLUDES_FOUND ${CUR_INCLUDES_FOUND} ${inc})
    endif()

    # Cleanup
    mark_as_advanced(${inc}_PATH)
  endforeach()

  # Check all library names
  set(CUR_LIBRARIES "")
  set(CUR_SHARED_LIBRARIES "")
  set(CUR_LIBRARIES_FOUND "")

  foreach(rawlib ${CUR_LIBRARY_NAMES})
    FindLibraryHelper(${name} ${rawlib} "${CUR_SEARCH_DIRS}")
    set(libname LIB_${rawlib})

    if(${libname}_FOUND)
      set(CUR_LIBRARIES ${CUR_LIBRARIES} ${${libname}_PATH})
      set(CUR_LIBRARIES_FOUND ${rawlib})

      if(${libname}_SHARED AND NOT MSYS)
        set(CUR_SHARED_LIBRARIES ${CUR_SHARED_LIBRARIES} ${${libname}_PATH})
      endif()
    else()
      set(${name}_FOUND FALSE)
    endif()
  endforeach()

  if(${name}_FOUND)
    if(NOT CUR_FIND_QUIETLY)
      message(STATUS "Check for library '${name}' -- found")
    endif()
    foreach(parent ${${name}_PARENTS})
      set(${parent}_LIBRARIES ${${parent}_LIBRARIES} ${CUR_LIBRARIES})
      set(${parent}_SHARED_LIBRARIES ${${parent}_SHARED_LIBRARIES} ${CUR_SHARED_LIBRARIES})
      set(${parent}_INCLUDE_DIRS ${${parent}_INCLUDE_DIRS} ${CUR_INCLUDE_DIRS})
    endforeach()
  else()
    if(CUR_FIND_REQUIRED)
      foreach(parent ${${name}_PARENTS})
        set(${parent}_FOUND FALSE)
      endforeach()
    endif()
  endif()

  if(${name}_PARENTS)
    mark_as_advanced(${name}_HOME)
  endif()

  set(${name}_INCLUDE_DIRS ${CUR_INCLUDE_DIRS})
  set(${name}_LIBRARIES ${CUR_LIBRARIES})
  set(${name}_SHARED_LIBRARIES ${CUR_SHARED_LIBRARIES})
  set(${name}_INCLUDES_FOUND ${CUR_INCLUDES_FOUND})
  set(${name}_LIBRARIES_FOUND ${CUR_LIBRARIES_FOUND})
endmacro()

# Returns ${libname}_PATH (with libname.whatever in the path)
macro(FindLibraryHelper name rawlib CUR_SEARCH_DIRS)
  set(shared_lib ${CMAKE_SHARED_LIBRARY_PREFIX}${rawlib}${CMAKE_SHARED_LIBRARY_SUFFIX})
  set(static_lib ${CMAKE_STATIC_LIBRARY_PREFIX}${rawlib}${CMAKE_STATIC_LIBRARY_SUFFIX})
  set(base_shared_lib ${rawlib}${CMAKE_SHARED_LIBRARY_SUFFIX})
  set(base_static_lib ${rawlib}${CMAKE_STATIC_LIBRARY_SUFFIX})

  set(libname LIB_${rawlib})

  if(CUR_ONLY_STATIC)
    set(${libname}_NAME ${static_lib})
  else()
    set(${libname}_NAME ${shared_lib})
  endif()

  if(NOT CUR_FIND_QUIETLY)
    message(STATUS "  Check for ${${libname}_NAME}")
  endif()

  # Try to find the library
  find_path(${libname}_STATIC_PATH
    NAMES "${static_lib}"
    PATHS ${CUR_SEARCH_DIRS} ${CMAKE_SYSTEM_LIBRARY_PATH}
    PATH_SUFFIXES ${CUR_LIBRARY_PATH_SUFFIXES}
  )

  find_path(${libname}_BASE_STATIC_PATH
    NAMES "${base_static_lib}"
    PATHS ${CUR_SEARCH_DIRS} ${CMAKE_SYSTEM_LIBRARY_PATH}
    PATH_SUFFIXES ${CUR_LIBRARY_PATH_SUFFIXES}
  )

  find_path(${libname}_SHARED_PATH
    NAMES "${shared_lib}"
    PATHS ${CUR_SEARCH_DIRS} ${CMAKE_SYSTEM_LIBRARY_PATH}
    PATH_SUFFIXES ${CUR_LIBRARY_PATH_SUFFIXES}
  )

  find_path(${libname}_BASE_SHARED_PATH
    NAMES "${base_shared_lib}"
    PATHS ${CUR_SEARCH_DIRS} ${CMAKE_SYSTEM_LIBRARY_PATH}
    PATH_SUFFIXES ${CUR_LIBRARY_PATH_SUFFIXES}
  )

  # Choose which one is the best
  set(${libname}_PATH FALSE)
  set(${libname}_SHARED TRUE)

  if(CUR_ONLY_STATIC)
    if(${libname}_STATIC_PATH)
      set(${libname}_PATH ${${libname}_STATIC_PATH})
      set(${libname}_NAME ${static_lib})
      set(${libname}_SHARED FALSE)
    elseif(${libname}_BASE_STATIC_PATH)
      set(${libname}_PATH ${${libname}_BASE_STATIC_PATH})
      set(${libname}_NAME ${base_static_lib})
      set(${libname}_SHARED FALSE)
    endif()
  elseif(CUR_ONLY_SHARED)
    if(${libname}_SHARED_PATH)
      set(${libname}_PATH ${${libname}_SHARED_PATH})
      set(${libname}_NAME ${shared_lib})
    elseif(${libname}_BASE_SHARED_PATH)
      set(${libname}_PATH ${${libname}_BASE_SHARED_PATH})
      set(${libname}_NAME ${base_shared_lib})
    endif()
  else()
    # Choose whichever one we can find
    if(${libname}_SHARED_PATH)
      set(${libname}_PATH ${${libname}_SHARED_PATH})
      set(${libname}_NAME ${shared_lib})
    elseif(${libname}_BASE_SHARED_PATH)
      set(${libname}_PATH ${${libname}_BASE_SHARED_PATH})
      set(${libname}_NAME ${base_shared_lib})
    elseif(${libname}_STATIC_PATH)
      set(${libname}_PATH ${${libname}_STATIC_PATH})
      set(${libname}_NAME ${static_lib})
      set(${libname}_SHARED FALSE)
    elseif(${libname}_BASE_STATIC_PATH)
      set(${libname}_PATH ${${libname}_BASE_STATIC_PATH})
      set(${libname}_NAME ${base_static_lib})
      set(${libname}_SHARED FALSE)
    else()
      # Not found
    endif()
  endif()

  if(${libname}_PATH)
    # Found
    if(NOT CUR_FIND_QUIETLY)
      message(STATUS "  Check for ${${libname}_NAME} -- found")
    endif()

    set(${libname}_PATH "${${libname}_PATH}/${${libname}_NAME}")
    set(${libname}_FOUND TRUE)
  else()
    # Missing
    if(NOT CUR_FIND_QUIETLY)
      message(STATUS "  Check for ${${libname}_NAME} -- missing")
    endif()

    set(${libname}_FOUND FALSE)
  endif()

  # Cleanup
  mark_as_advanced(${libname}_STATIC_PATH)
  mark_as_advanced(${libname}_BASE_STATIC_PATH)
  mark_as_advanced(${libname}_SHARED_PATH)
  mark_as_advanced(${libname}_BASE_SHARED_PATH)
endmacro()
