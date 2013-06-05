/**
* @file OgreOggListener.cpp
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

#include "OgreOggListener.h"
#include "OgreOggSound.h"
#include <OgreMovableObject.h>

namespace OgreOggSound
{
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::setPosition(ALfloat x, ALfloat y, ALfloat z)
	{
		mPosition.x = x;
		mPosition.y = y;
		mPosition.z = z;
		alListener3f(AL_POSITION,x,y,z);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::setPosition(const Ogre::Vector3 &pos)
	{
		mPosition = pos;
		alListener3f(AL_POSITION,pos.x,pos.y,pos.z);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::setVelocity(float velx, float vely, float velz)
	{
		mVelocity.x = velx;
		mVelocity.y = vely;
		mVelocity.z = velz;
		alListener3f(AL_VELOCITY, velx, vely, velz);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::setVelocity(const Ogre::Vector3 &vel)
	{
		mVelocity = vel;	
		alListener3f(AL_VELOCITY, vel.x, vel.y, vel.z);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::setOrientation(ALfloat x,ALfloat y,ALfloat z,ALfloat upx,ALfloat upy,ALfloat upz)
	{
		mOrientation[0] = x;
		mOrientation[1] = y;
		mOrientation[2] = z;
		mOrientation[3] = upx;
		mOrientation[4] = upy;
		mOrientation[5] = upz;	
		alListenerfv(AL_ORIENTATION,mOrientation);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::setOrientation(const Ogre::Quaternion &q)
	{
		Ogre::Vector3 vDirection = q.zAxis();
		Ogre::Vector3 vUp = q.yAxis();

		mOrientation[0] = -vDirection.x;
		mOrientation[1] = -vDirection.y;
		mOrientation[2] = -vDirection.z;
		mOrientation[3] = vUp.x;
		mOrientation[4] = vUp.y;
		mOrientation[5] = vUp.z;	
		alListenerfv(AL_ORIENTATION,mOrientation);	
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::update()
	{
		if(mLocalTransformDirty)
		{
			if ( mParentNode )
			{
				setPosition(mParentNode->_getDerivedPosition());
				setOrientation(mParentNode->_getDerivedOrientation());			 
			}
			mLocalTransformDirty=false;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	const Ogre::AxisAlignedBox& OgreOggListener::getBoundingBox(void) const
	{
		static Ogre::AxisAlignedBox aab;
		return aab;
	}
	/*/////////////////////////////////////////////////////////////////*/
	float OgreOggListener::getBoundingRadius(void) const
	{
		return 0;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::_updateRenderQueue(Ogre::RenderQueue *queue)
	{
		return;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::visitRenderables(Ogre::Renderable::Visitor* visitor, bool debugRenderables)
	{
		return;
	}
	/*/////////////////////////////////////////////////////////////////*/
	const Ogre::String& OgreOggListener::getMovableType(void) const
	{
		return OgreOggSoundFactory::FACTORY_TYPE_NAME;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::_notifyAttached(Ogre::Node* node, bool isTagPoint)
	{
		// Call base class notify
		Ogre::MovableObject::_notifyAttached(node, isTagPoint);

		// Immediately set position/orientation when attached
		if (mParentNode)
		{
			setPosition(mParentNode->_getDerivedPosition());
			setOrientation(mParentNode->_getDerivedOrientation());
		}

		return;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggListener::_notifyMoved(void) 
	{ 
		// Call base class notify
		Ogre::MovableObject::_notifyMoved();

		mLocalTransformDirty=true; 
	}
}

	