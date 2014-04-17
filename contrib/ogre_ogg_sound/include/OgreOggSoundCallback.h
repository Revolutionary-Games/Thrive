/**
* @file OgreOggSoundCallback.h
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
* Callbacks for detecting various states
*/

#ifndef _OGREOGGSOUND_CALLBACK_H_
#define _OGREOGGSOUND_CALLBACK_H_

#include "OgreOggSoundPrereqs.h"

namespace OgreOggSound
{
	class OgreOggISound;

	//! Callbacks for sound states.
	/** Template class for implementing callbacks which can be attached to sounds.
	@remarks
		Allows member functions to be used as callbacks. Amended from OgreAL, 
		originally written by CaseyB.
	**/
	class _OGGSOUND_EXPORT OOSCallback
	{
	
	public:
	
		virtual ~OOSCallback(){};
		virtual void execute(OgreOggISound* sound) = 0;

	};

	//! Callback template
	template<typename T>
	class OSSCallbackPointer : public OOSCallback
	{

	public:

		typedef void (T::*MemberFunction)(OgreOggISound* sound);

		OSSCallbackPointer() : mUndefined(true){}

		OSSCallbackPointer(MemberFunction func, T* obj) :
			mFunction(func),
			mObject(obj),
			mUndefined(false)
		{}

		virtual ~OSSCallbackPointer(){}

		void execute(OgreOggISound* sound)
		{
			if(!mUndefined)
				(mObject->*mFunction)(sound);
		}

	protected:

		MemberFunction mFunction;
		T* mObject;
		bool mUndefined;

	}; 

} 

#endif	/* _OGREOGGSOUND_CALLBACK_H_ */
