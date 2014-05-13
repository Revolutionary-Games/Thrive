# - Find TinyXML
# Find the native TinyXML includes and library
#
# TINYXML_FOUND - True if TinyXML found.
# TINYXML_INCLUDE_DIR - where to find tinyxml.h, etc.
# TINYXML_LIBRARIES - List of libraries when using TinyXML.
#

SET( TINYXML_INCLUDE_SEARCH_DIR
       ${TINYXML_ROOT}/include
       ${TINYXML_ROOT}/include/tinyxml
       ${TINYXMLDIR}/include
       ${TINYXMLDIR}/tinyxml/include
       /usr/local/include
       /usr/include
       /sw/include # Fink
       /opt/local/include # DarwinPorts
       /opt/csw/include # Blastwave
       /opt/include
       /usr/freeware/include
    )

SET( TINYXML_LIBRARY_SEARCH_DIR
        ${CEGUIDIR}/lib
        ${CEGUI_ROOT}/lib
        ${CEGUIDIR}
        /usr/local/lib
        /usr/lib
        /sw/lib
        /opt/local/lib
        /opt/csw/lib
        /opt/lib
        [HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Session\ Manager\\Environment;CEGUI_ROOT]/lib
        /usr/freeware/lib64
    )
    
IF( TINYXML_INCLUDE_DIR )
# Already in cache, be silent
SET( TinyXML_FIND_QUIETLY TRUE )
ENDIF( TINYXML_INCLUDE_DIR )

FIND_PATH( TINYXML_INCLUDE_DIR "tinyxml.h"
PATHS ${TINYXML_INCLUDE_SEARCH_DIR})

FIND_LIBRARY( TINYXML_LIBRARIES
NAMES "tinyxml"
PATHS ${TINYXML_LIBRARY_SEARCH_DIR})


# handle the QUIETLY and REQUIRED arguments and set TINYXML_FOUND to TRUE if
# all listed variables are TRUE
INCLUDE( "FindPackageHandleStandardArgs" )
FIND_PACKAGE_HANDLE_STANDARD_ARGS( "TinyXML" DEFAULT_MSG TINYXML_INCLUDE_DIR TINYXML_LIBRARIES )

MARK_AS_ADVANCED( TINYXML_INCLUDE_DIR TINYXML_LIBRARIES )

