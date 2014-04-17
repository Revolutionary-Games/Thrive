    # Locate CEGUI (Made for CEGUI 7.5)
    #
    # This module defines
    # CEGUI_FOUND, if false, do not try to link to CEGUI
    # CEGUI_LIBRARY, where to find the librarys
    # CEGUI_INCLUDE_DIR, where to find the headers
    #
    # $CEGUIDIR is an environment variable that would
    # correspond to the ./configure --prefix=$CEGUIDIR
    #
    # There are several COMPONENTS that can be included:
    # NULL, OPENGL, DIRECT3D9, DIRECT3D10, DIRECT3D11, DIRECTFB, OGRE, IRRLICHT
    # Selecting no render as COMPONENT will create a error massage!
    #
    # 2011-07-21 Created by Frederik vom Hofe using the findSFML.cmake versions from David Guthrie with code from Robert Osfield.

    SET(CEGUI_FOUND "YES")
    SET(CEGUI_LIBRARY "")
    SET(CEGUI_INCLUDE_DIR "")

    SET( CEGUIDIR $ENV{CEGUIDIR} )
    IF((WIN32 OR WIN64) AND NOT(CYGWIN))
       # Convert backslashes to slashes
       STRING(REGEX REPLACE "\\\\" "/" CEGUIDIR "${CEGUIDIR}")
    ENDIF()


    #To always have the right case sensitive name we use this list and a helper macro:
    SET(RENDER_NAME
       Null
       OpenGL
       Direct3D9
       Direct3D10
       Direct3D11
       DirectFB
       Ogre
       Irrlicht
    )

    MACRO(HELPER_GET_CASE_FROM_LIST SEARCHSTR LOOKUPLIST RESULTSTR)
       SET(${RESULTSTR} ${SEARCHSTR}) #default return value if nothing is found
       FOREACH(LOOP_S IN LISTS ${LOOKUPLIST})
          string(TOLOWER ${LOOP_S} LOOP_S_LOWER)
          string(TOLOWER ${SEARCHSTR} LOOP_SEARCHSTR_LOWER)
          string(COMPARE EQUAL ${LOOP_S_LOWER} ${LOOP_SEARCHSTR_LOWER} LOOP_STR_COMPARE)
          IF(LOOP_STR_COMPARE)
             SET(${RESULTSTR} ${LOOP_S})
          ENDIF()
       ENDFOREACH()
    ENDMACRO()

    #********** First we locate the include directorys ********** ********** ********** **********
    SET( CEGUI_INCLUDE_SEARCH_DIR
       ${CEGUI_ROOT}/include
       ${CEGUIDIR}/include
       ${CEGUIDIR}/cegui/include
       ~/Library/Frameworks
       /Library/Frameworks
       /usr/local/include
       /usr/include
       /sw/include # Fink
       /opt/local/include # DarwinPorts
       /opt/csw/include # Blastwave
       /opt/include
       /usr/freeware/include
    )

    #helper
    MACRO(FIND_PATH_HELPER FILENAME DIR SUFFIX)
       FIND_PATH(${FILENAME}_DIR ${FILENAME} PATHS ${${DIR}} PATH_SUFFIXES ${SUFFIX})
       IF(NOT ${FILENAME}_DIR)
          MESSAGE("Could not located ${FILENAME}")
          SET(CEGUI_FOUND "NO")
       ELSE()
          LIST(APPEND CEGUI_INCLUDE_DIR ${${FILENAME}_DIR})
       ENDIF()
    ENDMACRO()

    FIND_PATH_HELPER(CEGUI.h CEGUI_INCLUDE_SEARCH_DIR CEGUI)

    IF("${CEGUI_FIND_COMPONENTS}" STREQUAL "")
       MESSAGE("ERROR: No CEGUI renderer selected. \n\nSelect a renderer by including it's name in the component list:\n\ne.g. Find_Package(CEGUI REQUIRED COMPONENTS OPENGL)\n\nCEGUI renderers:")
       FOREACH(LOOP_S IN LISTS RENDER_NAME)
          MESSAGE("${LOOP_S}")
       ENDFOREACH()
       MESSAGE("\n")
       MESSAGE(SEND_ERROR "Select at last one renderer!" )
    ENDIF()

    FOREACH(COMPONENT ${CEGUI_FIND_COMPONENTS})
       HELPER_GET_CASE_FROM_LIST( ${COMPONENT} RENDER_NAME COMPONENT_CASE)
       FIND_PATH_HELPER( "Renderer.h" "CEGUI_INCLUDE_SEARCH_DIR" "CEGUI/RendererModules/${COMPONENT_CASE}/;RendererModules/${COMPONENT_CASE}/" )
    ENDFOREACH(COMPONENT)

    IF (APPLE)
       FIND_PATH(CEGUI_FRAMEWORK_DIR CEGUI.h
         PATHS
           ~/Library/Frameworks/CEGUI.framework/Headers
           /Library/Frameworks/CEGUI.framework/Headers
           ${DELTA3D_EXT_DIR}/Frameworks/CEGUI.framework/Headers
    )
    ENDIF (APPLE)

    IF(CEGUI_FRAMEWORK_DIR)
       LIST(APPEND CEGUI_INCLUDE_DIR ${CEGUI_FRAMEWORK_DIR})
    ELSE()
       LIST(APPEND CEGUI_INCLUDE_DIR ${CEGUI_FRAMEWORK_DIR}/CEGUI)
    ENDIF()


    #********** Then we locate the Librarys ********** ********** ********** **********
    SET( CEGUI_LIBRARY_SEARCH_DIR
       ${CEGUIDIR}/lib
            ${CEGUI_ROOT}/lib
            ${CEGUIDIR}
            ~/Library/Frameworks
            /Library/Frameworks
            /usr/local/lib
            /usr/lib
            /sw/lib
            /opt/local/lib
            /opt/csw/lib
            /opt/lib
            [HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Session\ Manager\\Environment;CEGUI_ROOT]/lib
            /usr/freeware/lib64
    )

    #helper
    MACRO(FIND_LIBRARY_HELPER FILENAME DIR)
       FIND_LIBRARY(${FILENAME}_DIR NAMES ${FILENAME} PATHS ${${DIR}})
       IF(NOT ${FILENAME}_DIR)
          MESSAGE("Could not located ${FILENAME}")
          SET(CEGUI_FOUND "NO")
       ELSE()
          LIST(APPEND CEGUI_LIBRARY ${${FILENAME}_DIR})
       ENDIF()
    ENDMACRO()

    IF(CMAKE_BUILD_TYPE MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
        FIND_LIBRARY_HELPER( CEGUIBase-0_d CEGUI_LIBRARY_SEARCH_DIR )
    ELSE()
        FIND_LIBRARY_HELPER( CEGUIBase-0 CEGUI_LIBRARY_SEARCH_DIR )
    ENDIF()
    
    
    FOREACH(COMPONENT ${CEGUI_FIND_COMPONENTS})
       HELPER_GET_CASE_FROM_LIST( ${COMPONENT} RENDER_NAME COMPONENT_CASE)
       IF(CMAKE_BUILD_TYPE MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
           FIND_LIBRARY_HELPER( CEGUI${COMPONENT_CASE}Renderer-0_d "CEGUI_LIBRARY_SEARCH_DIR" CEGUI)
       ELSE()
           FIND_LIBRARY_HELPER( CEGUI${COMPONENT_CASE}Renderer-0 "CEGUI_LIBRARY_SEARCH_DIR" CEGUI)
       ENDIF()
       
    ENDFOREACH(COMPONENT)

    #********** And we are done ********** ********** ********** ********** ********** ********** ********** **********

    IF(NOT CEGUI_FOUND)
       MESSAGE(SEND_ERROR "Error(s) during CEGUI dedection!")
    ENDIF()

    INCLUDE(FindPackageHandleStandardArgs)
    FIND_PACKAGE_HANDLE_STANDARD_ARGS(CEGUI DEFAULT_MSG CEGUI_LIBRARY CEGUI_INCLUDE_DIR)