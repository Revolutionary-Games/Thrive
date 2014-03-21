/**
* @file OgreOggSoundPrereqs.h
* @author  Ian Stangoe
* @version v1.23
*
* @section LICENSE
* 
* This source file is part of OgreOggSound, an OpenAL wrapper library for   
* use with the Ogre Rendering Engine.										 
*                                                                           
* Copyright (c) 2013 <Ian Stangoe>
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE. 
*
* @section DESCRIPTION
* 
* Pre-requisites for building lib
*/

#include <OgreVector3.h>
#include <OgreDataStream.h>
#include <OgreMovableObject.h>
#include <OgreLogManager.h>

#   if OGRE_PLATFORM == OGRE_PLATFORM_WIN32

#	pragma once
#	pragma warning( disable : 4244 )

/**
 * Specifies whether EFX enhancements are supported
 * 0 - EFX not supported
 * 1 - Enable EFX suport
 */
#	ifndef HAVE_EFX
#		define HAVE_EFX 1
#	endif

#	include "al.h"
#	include "alc.h"
#	if HAVE_EFX
#		include "efx.h"
#		include "efx-util.h"
#		include "efx-creative.h"
#		include "xram.h"
#	endif
#	if OGRE_COMPILER == OGRE_COMPILER_MSVC
#		ifdef OGGSOUND_EXPORT
#			define _OGGSOUND_EXPORT __declspec(dllexport)
#		else
#			define _OGGSOUND_EXPORT __declspec(dllimport)
#		endif
#	else
#		define _OGGSOUND_EXPORT
#	endif
#elif OGRE_COMPILER == OGRE_COMPILER_GNUC
#   if OGRE_PLATFORM == OGRE_PLATFORM_APPLE
#		include <al.h>
#		include <alc.h>
#   else
#		include <AL/al.h>
#		include <AL/alc.h>
#	endif
#	if defined(OGGSOUND_EXPORT) && OGRE_COMP_VER >= 400
#		define _OGGSOUND_EXPORT __attribute__ ((visibility("default")))
#	else
#		define _OGGSOUND_EXPORT
#	endif
#else // Other Compilers
#	include <OpenAL/al.h>
#	include <OpenAL/alc.h>
#	include "xram.h"
#	define _OGGSOUND_EXPORT
#endif

/**
 * Specifies whether to use threads for streaming
 * 0 - No multithreading
 * 1 - BOOST multithreaded
 */
#ifndef OGGSOUND_THREADED
	#define OGGSOUND_THREADED 1
#endif


