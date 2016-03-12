#pragma once

// This file suppresses warnings/errors that compiling cAudio with C++11 causes

#define CAUDIO_REROUTE_STRING_ALLOCATIONS 0

#ifdef __GNUC__
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wignored-qualifiers"
#pragma GCC diagnostic ignored "-Wold-style-cast"
#pragma GCC diagnostic ignored "-Wcast-qual"
#pragma GCC diagnostic ignored "-Wunused-parameter"
#pragma GCC diagnostic ignored "-Wunused-function"
#pragma GCC diagnostic ignored "-Wunused-variable"
#pragma GCC diagnostic ignored "-Wsequence-point"
#pragma GCC diagnostic ignored "-Wconversion-null"
#pragma GCC diagnostic ignored "-Wunused"
#pragma GCC diagnostic ignored "-Wall"

#include <cAudio.h>



__attribute__((unused)) static void toUTF8NotUnused(){

    cAudio::toUTF8(cAudio::cAudioString());
}

__attribute__((unused)) static void fromUTF8NotUnused(){

    cAudio::fromUTF8(NULL);
}


#pragma GCC diagnostic pop
#else

#include <cAudio.h>

#endif

