
function(SeparateLibrariesByBuildType LIB_LIST OUT_DEBUG OUT_RELEASE)
    GetLibraryForBuildType(
        "${LIB_LIST}"
        "Release"
        RELEASE_LIB
    )
    set(${OUT_RELEASE} ${RELEASE_LIB} PARENT_SCOPE)
    GetLibraryForBuildType(
        "${LIB_LIST}"
        "Debug"
        DEBUG_LIB
    )
    set(${OUT_DEBUG} ${DEBUG_LIB} PARENT_SCOPE)
endfunction()



function(GetLibraryForBuildType LIB_LIST BUILD_TYPE OUT_LIB)
    if(BUILD_TYPE STREQUAL "Debug")
        list(FIND LIB_LIST "debug" LIB_INDEX_MINUS_ONE)
    else()
        list(FIND LIB_LIST "optimized" LIB_INDEX_MINUS_ONE)
    endif()
    if(LIB_INDEX_MINUS_ONE LESS 0)
        set(${OUT_LIB} ${LIB_LIST} PARENT_SCOPE)
    else()
        math(EXPR LIB_INDEX "${LIB_INDEX_MINUS_ONE} + 1")
        list(GET LIB_LIST ${LIB_INDEX} LIB_NAME)
        set(${OUT_LIB} ${LIB_NAME} PARENT_SCOPE)
    endif()
endfunction()


function(InstallFollowingSymlink FILE_LIST DESTINATION CONFIGURATIONS RENAME)
    foreach(FILE_NAME ${FILE_LIST})
        get_filename_component(RESOLVED_FILE ${FILE_NAME} REALPATH)
        if(EXISTS ${RESOLVED_FILE})
            if(RENAME)
                get_filename_component(NAME ${FILE_NAME} NAME)
                install(FILES 
                    ${RESOLVED_FILE} 
                    DESTINATION ${DESTINATION} 
                    CONFIGURATIONS ${CONFIGURATIONS} 
                    RENAME ${NAME}
                )
            else()
                install(FILES 
                    ${RESOLVED_FILE} 
                    DESTINATION ${DESTINATION}
                    CONFIGURATIONS ${CONFIGURATIONS} 
                )
            endif()
        endif()
    endforeach()
endfunction()
