/**
* @file OgreOggSoundPluginDllStart.cpp
* @author  Ian Stangoe
* @version v1.24
*
* @section LICENSE
* 
* This source file is part of OgreOggSound, an OpenAL wrapper library for   
* use with the Ogre Rendering Engine.										 
*                                                                           
* Copyright (c) 2013 Ian Stangoe
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
*/

#include <OgreRoot.h>

#include "OgreOggSound.h"
#include "OgreOggSoundPlugin.h"

using namespace Ogre;
using namespace OgreOggSound;

OgreOggSoundPlugin* mOgreOggSoundPlugin;

//----------------------------------------------------------------------------------
// register SecureArchive with Archive Manager
//----------------------------------------------------------------------------------
extern "C" void _OGGSOUND_EXPORT dllStartPlugin( void )
{
	// Create new plugin
	mOgreOggSoundPlugin = OGRE_NEW_T(OgreOggSoundPlugin, Ogre::MEMCATEGORY_GENERAL)();

	// Register
	Root::getSingleton().installPlugin(mOgreOggSoundPlugin);
}

extern "C" void _OGGSOUND_EXPORT dllStopPlugin( void )
{
	Root::getSingleton().uninstallPlugin(mOgreOggSoundPlugin);

	OGRE_DELETE_T(mOgreOggSoundPlugin, OgreOggSoundPlugin, Ogre::MEMCATEGORY_GENERAL);
	mOgreOggSoundPlugin = 0;
}
