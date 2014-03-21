# Vorbis CMake build file
cmake_minimum_required(VERSION 2.6)
project(VORBIS)

set(VORBIS_SRC_DIR 
    "${CMAKE_CURRENT_SOURCE_DIR}/libvorbis-1.3.1"
    CACHE STRING "Path to unpacked libvorbis source archive"
)
string(REGEX REPLACE "\\\\" "/" VORBIS_SRC_DIR "${VORBIS_SRC_DIR}") 

set(VORBIS_SRC_FILES
	${VORBIS_SRC_DIR}/lib/analysis.c
	${VORBIS_SRC_DIR}/lib/bitrate.c
	${VORBIS_SRC_DIR}/lib/block.c
	${VORBIS_SRC_DIR}/lib/codebook.c
	${VORBIS_SRC_DIR}/lib/envelope.c
	${VORBIS_SRC_DIR}/lib/floor0.c
	${VORBIS_SRC_DIR}/lib/floor1.c
	${VORBIS_SRC_DIR}/lib/info.c
	${VORBIS_SRC_DIR}/lib/lookup.c
	${VORBIS_SRC_DIR}/lib/lpc.c
	${VORBIS_SRC_DIR}/lib/lsp.c
	${VORBIS_SRC_DIR}/lib/mapping0.c
	${VORBIS_SRC_DIR}/lib/mdct.c
	${VORBIS_SRC_DIR}/lib/psy.c
	${VORBIS_SRC_DIR}/lib/registry.c
	${VORBIS_SRC_DIR}/lib/res0.c
	${VORBIS_SRC_DIR}/lib/sharedbook.c
	${VORBIS_SRC_DIR}/lib/smallft.c
	${VORBIS_SRC_DIR}/lib/synthesis.c
	${VORBIS_SRC_DIR}/win32/vorbis.def
	${VORBIS_SRC_DIR}/lib/vorbisenc.c
	${VORBIS_SRC_DIR}/lib/window.c
	${VORBIS_SRC_DIR}/lib/backends.h
	${VORBIS_SRC_DIR}/lib/bitrate.h
	${VORBIS_SRC_DIR}/lib/codebook.h
	${VORBIS_SRC_DIR}/lib/codec_internal.h
	${VORBIS_SRC_DIR}/lib/envelope.h
	${VORBIS_SRC_DIR}/lib/modes/floor_all.h
	${VORBIS_SRC_DIR}/lib/books/floor/floor_books.h
	${VORBIS_SRC_DIR}/lib/highlevel.h
	${VORBIS_SRC_DIR}/lib/lookup.h
	${VORBIS_SRC_DIR}/lib/lookup_data.h
	${VORBIS_SRC_DIR}/lib/lpc.h
	${VORBIS_SRC_DIR}/lib/lsp.h
	${VORBIS_SRC_DIR}/lib/masking.h
	${VORBIS_SRC_DIR}/lib/mdct.h
	${VORBIS_SRC_DIR}/lib/misc.h
	${VORBIS_SRC_DIR}/lib/os.h
	${VORBIS_SRC_DIR}/lib/psy.h
	${VORBIS_SRC_DIR}/lib/modes/psych_11.h
	${VORBIS_SRC_DIR}/lib/modes/psych_16.h
	${VORBIS_SRC_DIR}/lib/modes/psych_44.h
	${VORBIS_SRC_DIR}/lib/modes/psych_8.h
	${VORBIS_SRC_DIR}/lib/registry.h
	${VORBIS_SRC_DIR}/lib/books/coupled/res_books_stereo.h
	${VORBIS_SRC_DIR}/lib/books/uncoupled/res_books_uncoupled.h
	${VORBIS_SRC_DIR}/lib/modes/residue_16.h
	${VORBIS_SRC_DIR}/lib/modes/residue_44.h
	${VORBIS_SRC_DIR}/lib/modes/residue_44u.h
	${VORBIS_SRC_DIR}/lib/modes/residue_8.h
	${VORBIS_SRC_DIR}/lib/scales.h
	${VORBIS_SRC_DIR}/lib/modes/setup_11.h
	${VORBIS_SRC_DIR}/lib/modes/setup_16.h
	${VORBIS_SRC_DIR}/lib/modes/setup_22.h
	${VORBIS_SRC_DIR}/lib/modes/setup_32.h
	${VORBIS_SRC_DIR}/lib/modes/setup_44.h
	${VORBIS_SRC_DIR}/lib/modes/setup_44u.h
	${VORBIS_SRC_DIR}/lib/modes/setup_8.h
	${VORBIS_SRC_DIR}/lib/modes/setup_X.h
	${VORBIS_SRC_DIR}/lib/smallft.h
	${VORBIS_SRC_DIR}/lib/window.h
)

set(VORBIS_HEADER_FILES
	${VORBIS_SRC_DIR}/include/vorbis/vorbisenc.h
	${VORBIS_SRC_DIR}/include/vorbis/codec.h
)

set(VORBIS_FILE_HEADER_FILES
	${VORBIS_SRC_DIR}/include/vorbis/vorbisfile.h
)

set(VORBIS_FILE_SRC_FILES
	${VORBIS_SRC_DIR}/lib/vorbisfile.c
)

find_path(OGG_INCLUDE_DIR ogg/ogg.h
)

include_directories(
    ${OGG_INCLUDE_DIR}
    ${VORBIS_SRC_DIR}/include
    ${VORBIS_SRC_DIR}/lib
)

add_library(vorbis STATIC ${VORBIS_SRC_FILES} ${VORBIS_HEADER_FILES})
add_library(vorbisfile STATIC ${VORBIS_FILE_SRC_FILES} ${VORBIS_FILE_HEADER_FILES})

target_link_libraries(vorbis ogg)
target_link_libraries(vorbisfile vorbis)

install(TARGETS vorbis vorbisfile
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION lib
)

install(FILES ${VORBIS_HEADER_FILES} ${VORBIS_FILE_HEADER_FILES}
    DESTINATION include/vorbis
)
