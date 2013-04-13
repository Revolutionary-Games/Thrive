cmake_minimum_required(VERSION 2.8)

project(lua CXX)

set(SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/lua/src)

set(LIB_SOURCES
    ${SOURCE_DIR}/lapi.c
    ${SOURCE_DIR}/lauxlib.c
    ${SOURCE_DIR}/lbaselib.c
    ${SOURCE_DIR}/lbitlib.c
    ${SOURCE_DIR}/lcode.c
    ${SOURCE_DIR}/lcorolib.c
    ${SOURCE_DIR}/lctype.c
    ${SOURCE_DIR}/ldblib.c
    ${SOURCE_DIR}/ldebug.c
    ${SOURCE_DIR}/ldo.c
    ${SOURCE_DIR}/ldump.c
    ${SOURCE_DIR}/lfunc.c
    ${SOURCE_DIR}/lgc.c
    ${SOURCE_DIR}/linit.c
    ${SOURCE_DIR}/liolib.c
    ${SOURCE_DIR}/llex.c
    ${SOURCE_DIR}/lmathlib.c
    ${SOURCE_DIR}/lmem.c
    ${SOURCE_DIR}/loadlib.c
    ${SOURCE_DIR}/lobject.c
    ${SOURCE_DIR}/lopcodes.c
    ${SOURCE_DIR}/loslib.c
    ${SOURCE_DIR}/lparser.c
    ${SOURCE_DIR}/lstate.c
    ${SOURCE_DIR}/lstring.c
    ${SOURCE_DIR}/lstrlib.c
    ${SOURCE_DIR}/ltable.c
    ${SOURCE_DIR}/ltablib.c
    ${SOURCE_DIR}/ltm.c
    ${SOURCE_DIR}/lundump.c
    ${SOURCE_DIR}/lvm.c
    ${SOURCE_DIR}/lzio.c
)

set(LIB_HEADERS
    ${SOURCE_DIR}/lapi.h
    ${SOURCE_DIR}/lauxlib.h
    ${SOURCE_DIR}/lcode.h
    ${SOURCE_DIR}/lctype.h
    ${SOURCE_DIR}/ldebug.h
    ${SOURCE_DIR}/ldo.h
    ${SOURCE_DIR}/lfunc.h
    ${SOURCE_DIR}/lgc.h
    ${SOURCE_DIR}/llex.h
    ${SOURCE_DIR}/llimits.h
    ${SOURCE_DIR}/lmem.h
    ${SOURCE_DIR}/lobject.h
    ${SOURCE_DIR}/lopcodes.h
    ${SOURCE_DIR}/lparser.h
    ${SOURCE_DIR}/lstate.h
    ${SOURCE_DIR}/lstring.h
    ${SOURCE_DIR}/ltable.h
    ${SOURCE_DIR}/ltm.h
    ${SOURCE_DIR}/luaconf.h
    ${SOURCE_DIR}/lualib.h
    ${SOURCE_DIR}/lundump.h
    ${SOURCE_DIR}/lvm.h
    ${SOURCE_DIR}/lzio.h
)

set_source_files_properties(
    ${LIB_SOURCES}
    PROPERTIES LANGUAGE CXX
)

include_directories(
    ${SOURCE_DIR}
)

add_library(lua SHARED 
    ${LIB_SOURCES} 
    ${LIB_HEADERS}
)

