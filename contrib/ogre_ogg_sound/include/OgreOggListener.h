/**
* @file OgreOggSoundListener.h
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
* Listener object (The users 'ears')
*/

#pragma once

#include "OgreOggSoundPrereqs.h"
#include <OgreVector3.h>
#include <OgreMovableObject.h>

namespace OgreOggSound
{
	//! Listener object (Users ears)
	/** Handles properties associated with the listener.
	*/
	class  _OGGSOUND_EXPORT OgreOggListener : public Ogre::MovableObject
	{

	public:		

		/** Constructor
		@remarks
			Creates a listener object to act as the ears of the user. 
		 */
		OgreOggListener(): mPosition(Ogre::Vector3::ZERO)
			, mVelocity(Ogre::Vector3::ZERO)
			, mLocalTransformDirty(false)
			, mSceneMgr(0)
		{
			for (int i=0; i<6; ++i ) mOrientation[i]=0.f;	
			mName = "OgreOggListener";
		};
		/** Sets the position of the listener.
		@remarks
			Sets the 3D position of the listener. This is a manual method,
			if attached to a SceneNode this will automatically be handled 
			for you.
			@param
				x/y/z position.
		*/
		void setPosition(ALfloat x,ALfloat y, ALfloat z);
		/** Sets the position of the listener.
		@remarks
			Sets the 3D position of the listener. This is a manual method,
			if attached to a SceneNode this will automatically be handled 
			for you.
			@param
				pos Vector position.
		*/
		void setPosition(const Ogre::Vector3 &pos);
		/** Gets the position of the listener.
		*/
		const Ogre::Vector3& getPosition() { return mPosition; }
		/** Sets the orientation of the listener.
		@remarks
			Sets the 3D orientation of the listener. This is a manual method,
			if attached to a SceneNode this will automatically be handled 
			for you.
			@param
				x/y/z direction.
			@param
				upx/upy/upz up.
		 */
		void setOrientation(ALfloat x, ALfloat y, ALfloat z, ALfloat upx, ALfloat upy, ALfloat upz);
		/** Sets the orientation of the listener.
		@remarks
			Sets the 3D orientation of the listener. This is a manual method,
			if attached to a SceneNode this will automatically be handled 
			for you.
			@param
				q Orientation quaternion.
		 */
		void setOrientation(const Ogre::Quaternion &q);
		/** Gets the orientation of the listener.
		*/
		Ogre::Vector3 getOrientation() { return Ogre::Vector3(mOrientation[0],mOrientation[1],mOrientation[2]); }
		/** Sets sounds velocity.
		@param
			vel 3D x/y/z velocity
		 */
		void setVelocity(float velx, float vely, float velz);
		/** Sets sounds velocity.
		@param
			vel 3D vector velocity
		 */
		void setVelocity(const Ogre::Vector3 &vel);
		/** Updates the listener.
		@remarks
			Handles positional updates to the listener either automatically
			through the SceneGraph attachment or manually using the 
			provided functions.
		 */
		void update();
		/** Gets the movable type string for this object.
		@remarks
			Overridden function from MovableObject, returns a 
			Sound object string for identification.
		 */
		virtual const Ogre::String& getMovableType(void) const;
		/** Gets the bounding box of this object.
		@remarks
			Overridden function from MovableObject, provides a
			bounding box for this object.
		 */
		virtual const Ogre::AxisAlignedBox& getBoundingBox(void) const;
		/** Gets the bounding radius of this object.
		@remarks
			Overridden function from MovableObject, provides the
			bounding radius for this object.
		 */
		virtual float getBoundingRadius(void) const;
		/** Updates the RenderQueue for this object
		@remarks
			Overridden function from MovableObject.
		 */
		virtual void _updateRenderQueue(Ogre::RenderQueue *queue);
		/** Renderable callback
		@remarks
			Overridden function from MovableObject.
		 */
		virtual void visitRenderables(Ogre::Renderable::Visitor* visitor, bool debugRenderables);
		/** Attach callback
		@remarks
			Overridden function from MovableObject.
		 */
		virtual void _notifyAttached(Ogre::Node* node, bool isTagPoint=false);
		/** Moved callback
		@remarks
			Overridden function from MovableObject.
		 */
		virtual void _notifyMoved(void);
		/** Returns scenemanager which created this listener.
		 */
		Ogre::SceneManager* getSceneManager() { return mSceneMgr; }
		/** Sets scenemanager which created this listener.
		 */
		void setSceneManager(Ogre::SceneManager& m) { mSceneMgr=&m; }
		
	private:

		/**
		 * Positional variables
		 */
		Ogre::Vector3 mPosition;		// 3D position
		Ogre::Vector3 mVelocity;		// 3D velocity
		float mOrientation[6];			// 3D orientation
		bool mLocalTransformDirty;		// Dirty transforms flag
		Ogre::SceneManager* mSceneMgr;	// Creator 

	};
}