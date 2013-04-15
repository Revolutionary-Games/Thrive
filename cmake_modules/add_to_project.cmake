

################################################################################
# Add to source files
################################################################################

# Adds all arguments to the global SOURCE_FILES property.
#
# Usage:
#
#    add_source_files(main.cpp class.cpp class.hpp)
#
function(add_sources)
    # make absolute paths
    set(ABSOLUTE_FILENAMES)
    foreach(FILENAME IN LISTS ARGN)
        get_filename_component(FILENAME "${FILENAME}" ABSOLUTE)
        list(APPEND ABSOLUTE_FILENAMES "${FILENAME}")
    endforeach()
  # append to global list
  set_property(GLOBAL APPEND PROPERTY SOURCE_FILES "${ABSOLUTE_FILENAMES}")
endfunction()

# A bit of documentation for the SOURCE_FILES property
define_property(GLOBAL PROPERTY SOURCE_FILES
    BRIEF_DOCS "List of source files"
    FULL_DOCS "List of source files to be compiled in one library"
)


################################################################################
# Add to test files
################################################################################

# Adds all arguments to the global TEST_SOURCE_FILES property.
#
# Usage:
#
#    add_test_sources(test.cpp test_class.cpp)
#
function(add_test_sources)
    # make absolute paths
    set(ABSOLUTE_FILENAMES)
    foreach(FILENAME IN LISTS ARGN)
        get_filename_component(FILENAME "${FILENAME}" ABSOLUTE)
        list(APPEND ABSOLUTE_FILENAMES "${FILENAME}")
    endforeach()
  # append to global list
  set_property(GLOBAL APPEND PROPERTY TEST_SOURCE_FILES "${ABSOLUTE_FILENAMES}")
endfunction()

# A bit of documentation for the TEST_SOURCE_FILES property
define_property(GLOBAL PROPERTY TEST_SOURCE_FILES
    BRIEF_DOCS "List of test source files"
    FULL_DOCS "List of test source files to be compiled."
)

