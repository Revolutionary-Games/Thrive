/**
* @file OgreOggSoundFactory.h
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
* @section DESCRIPTION
* 
* Factory manager for creation of sound objects
*/

#ifndef __OGREOGGSOUNDFactory_H__
#define __OGREOGGSOUNDFactory_H__

#include <Ogre.h>
#include "OgreOggSound.h"

namespace OgreOggSound 
{
	//! MovableFactory for creating Sound instances 
	class _OGGSOUND_EXPORT OgreOggSoundFactory : public Ogre::MovableObjectFactory
	{

	protected:
		Ogre::MovableObject* createInstanceImpl( const Ogre::String& name, const Ogre::NameValuePairList* params);

	public:
		OgreOggSoundFactory() {}
		~OgreOggSoundFactory() {}

		static Ogre::String FACTORY_TYPE_NAME;

		const Ogre::String& getType(void) const;
		void destroyInstance( Ogre::MovableObject* obj);

	};
}

#endif
