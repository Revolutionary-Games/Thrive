/**
* @file OgreOggSoundPlugin.cpp
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
*/

#include "OgreOggSoundPlugin.h"

using namespace Ogre;
using namespace OgreOggSound;

const String sPluginName = "OgreOggSound";

//---------------------------------------------------------------------
OgreOggSoundPlugin::OgreOggSoundPlugin() : 
 mOgreOggSoundFactory(0)
,mOgreOggSoundManager(0)
{

}
//---------------------------------------------------------------------
const String& OgreOggSoundPlugin::getName() const
{
	return sPluginName;
}
//---------------------------------------------------------------------
void OgreOggSoundPlugin::install()
{
	if ( mOgreOggSoundFactory ) return;

	// Create new factory
	mOgreOggSoundFactory = OGRE_NEW_T(OgreOggSoundFactory, Ogre::MEMCATEGORY_GENERAL)();

	// Register
	Root::getSingleton().addMovableObjectFactory(mOgreOggSoundFactory, true);
}
//---------------------------------------------------------------------
void OgreOggSoundPlugin::initialise()
{
	if ( mOgreOggSoundManager ) return;

	//initialise OgreOggSoundManager here
	mOgreOggSoundManager = OGRE_NEW_T(OgreOggSoundManager, Ogre::MEMCATEGORY_GENERAL)();
}
//---------------------------------------------------------------------
void OgreOggSoundPlugin::shutdown()
{
	if ( !mOgreOggSoundManager ) return;

	// shutdown OgreOggSoundManager here
	OGRE_DELETE_T(mOgreOggSoundManager, OgreOggSoundManager, Ogre::MEMCATEGORY_GENERAL);
	mOgreOggSoundManager = 0;
}
//---------------------------------------------------------------------
void OgreOggSoundPlugin::uninstall()
{
	if ( !mOgreOggSoundFactory ) return;

	// unregister
	Root::getSingleton().removeMovableObjectFactory(mOgreOggSoundFactory);

	OGRE_DELETE_T(mOgreOggSoundFactory, OgreOggSoundFactory, Ogre::MEMCATEGORY_GENERAL);
	mOgreOggSoundFactory = 0;
}
