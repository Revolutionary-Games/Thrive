################################################################################
# Join function to make a string out of the lists
################################################################################

function(join TARGET)
  set(_list)
  foreach(_element ${ARGN})
    set(_list "${_list} ${_element}")
  endforeach()
  string(STRIP ${_list} _list)
  set(${TARGET} ${_list} PARENT_SCOPE)
endfunction()


################################################################################
# GCC warning flags
################################################################################

if(CMAKE_COMPILER_IS_GNUCC OR CMAKE_COMPILER_IS_GNUCXX)

    join(WARNING_FLAGS
        -Werror
        -Wall
        -Wextra
        # Miscellaneous warnings:
        -Wcast-align
        -Wcast-qual
        -Wdisabled-optimization
        -Wfloat-equal
        -Wformat=2
        -Winit-self
        #-Winline    # Generates many useless warnings about destructors
        -Wlogical-op
        -Wmissing-declarations
        -Wmissing-include-dirs
        -Wpointer-arith
        -Wredundant-decls
        -Wstrict-overflow=2
        -Wswitch-default
        -Wswitch-enum
        -Wundef
        -Wunreachable-code
        # C++ specific
        -Wctor-dtor-privacy
        #-Weffc++               # Annoying member initialization
        -Wold-style-cast
        -Woverloaded-virtual
        -Wsign-promo
        -Wstrict-null-sentinel
    )

endif()

