/**
* @file OgreOggISound.cpp
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

#include "OgreOggISound.h"
#include "OgreOggSound.h"
#include <OgreMovableObject.h>

namespace OgreOggSound
{
	/*
	** These next four methods are custom accessor functions to allow the Ogg Vorbis
	** libraries to be able to stream audio data directly from an Ogre::DataStreamPtr
	*/
	size_t	OOSStreamRead(void *ptr, size_t size, size_t nmemb, void *datasource)
	{
		Ogre::DataStreamPtr dataStream = *reinterpret_cast<Ogre::DataStreamPtr*>(datasource);
		return dataStream->read(ptr, size);
	}

	int		OOSStreamSeek(void *datasource, ogg_int64_t offset, int whence)
	{
		Ogre::DataStreamPtr dataStream = *reinterpret_cast<Ogre::DataStreamPtr*>(datasource);
		switch(whence)
		{
		case SEEK_SET:
			dataStream->seek(offset);
			break;
		case SEEK_END:
			dataStream->seek(dataStream->size());
			// Falling through purposefully here
		case SEEK_CUR:
			dataStream->skip(offset);
			break;
		}

		return 0;
	}

	int		OOSStreamClose(void *datasource)
	{
		return 0;
	}

	long	OOSStreamTell(void *datasource)
	{
		Ogre::DataStreamPtr dataStream = *reinterpret_cast<Ogre::DataStreamPtr*>(datasource);
		return static_cast<long>(dataStream->tell());
	}

	/*/////////////////////////////////////////////////////////////////*/
	OgreOggISound::OgreOggISound(const Ogre::String& name, const Ogre::SceneManager& scnMgr) : 
	 mName(name)
	,mSource(0) 
	,mLoop(false) 
	,mPlay(false) 
	,mPosition(0,0,0) 
	,mReferenceDistance(1.0f) 
	,mDirection(0,0,0) 
	,mVelocity(0,0,0) 
	,mGain(1.0f) 
	,mMaxDistance(1E10) 
	,mMaxGain(1.0f) 
	,mMinGain(0.0f)
	,mPitch(1.0f) 
	,mRolloffFactor(1.0f) 
	,mInnerConeAngle(360.0f) 
	,mOuterConeAngle(360.0f) 
	,mOuterConeGain(0.0f) 
	,mPlayTime(0.0f) 
	,mFadeTimer(0.0f) 
	,mFadeTime(1.0f) 
	,mFadeInitVol(0) 
	,mFadeEndVol(1) 
	,mFade(false) 
	,mFadeEndAction(OgreOggSound::FC_NONE)  
	,mStream(false) 
	,mGiveUpSource(false)  
	,mPlayPosChanged(false)  
	,mPlayPos(0.f) 
	,mPriority(0)
	,mScnMan(const_cast<Ogre::SceneManager&>(scnMgr))
	,mAudioOffset(0)
	,mAudioEnd(0)
	,mLoopOffset(0)
	,mLocalTransformDirty(true)
	,mDisable3D(false)
	,mSeekable(true)
	,mSourceRelative(false)
	,mTemporary(false)
	,mInitialised(false)
	,mAwaitingDestruction(0)
	,mSoundListener(0)
	{
		// Init some oggVorbis callbacks
		mOggCallbacks.read_func	= OOSStreamRead;
		mOggCallbacks.close_func= OOSStreamClose;
		mOggCallbacks.seek_func	= OOSStreamSeek;
		mOggCallbacks.tell_func	= OOSStreamTell;
	}
	/*/////////////////////////////////////////////////////////////////*/
	OgreOggISound::~OgreOggISound() 
	{
		mAudioStream.setNull();
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::play(bool immediate)
	{
#if OGGSOUND_THREADED
		SoundAction action;
		action.mSound = mName;
		action.mAction = LQ_PLAY;
		action.mImmediately = immediate;
		action.mParams = 0;
		OgreOggSoundManager::getSingletonPtr()->_requestSoundAction(action);
#else
		_playImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::stop(bool immediate)
	{
#if OGGSOUND_THREADED
		SoundAction action;
		action.mSound = mName;
		action.mAction = LQ_STOP;
		action.mImmediately = immediate;
		action.mParams = 0;
		OgreOggSoundManager::getSingletonPtr()->_requestSoundAction(action);
#else
		_stopImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::pause(bool immediate)
	{
#if OGGSOUND_THREADED
		SoundAction action;
		action.mSound = mName;
		action.mAction = LQ_PAUSE;
		action.mImmediately = immediate;
		action.mParams = 0;
		OgreOggSoundManager::getSingletonPtr()->_requestSoundAction(action);
#else
		_pauseImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::disable3D(bool disable)
	{
		// Set flag
		mDisable3D = disable;

		/** Disable 3D
		@remarks
			Requires setting listener relative to AL_TRUE
			Position to ZERO.
			Reference distance is set to 1.
			If Source available then settings are applied immediately
			else they are applied next time the sound is initialised.
		*/
		if ( mDisable3D )
		{
			mSourceRelative = true;
			mReferenceDistance = 1.f;
			mPosition = Ogre::Vector3::ZERO;

			if ( mSource!=AL_NONE ) 
			{
				alSourcei(mSource, AL_SOURCE_RELATIVE, mSourceRelative);
				alSourcef(mSource, AL_REFERENCE_DISTANCE, mReferenceDistance);
				alSource3f(mSource, AL_POSITION, mPosition.x, mPosition.y, mPosition.z);
			}
		}
		/** Enable 3D
		@remarks
			Set listener relative to AL_FALSE
			If Source available then settings are applied immediately
			else they are applied next time the sound is initialised.
			NOTE:- If previously disabled, Reference distance will still be set to 1.
			Should be reset as required by user AFTER calling this function.
		*/
		else
		{
			mSourceRelative = false;

			if ( mSource!=AL_NONE ) 
			{
				alSourcei(mSource, AL_SOURCE_RELATIVE, mSourceRelative);
			}
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setPosition(float posx,float posy, float posz)
	{
		mPosition.x = posx;
		mPosition.y = posy;
		mPosition.z = posz;	
		mLocalTransformDirty = true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setPosition(const Ogre::Vector3 &pos)
	{
		mPosition = pos;   
		mLocalTransformDirty = true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setDirection(float dirx, float diry, float dirz)
	{
		mDirection.x = dirx;
		mDirection.y = diry;
		mDirection.z = dirz;
		mLocalTransformDirty = true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setDirection(const Ogre::Vector3 &dir)
	{
		mDirection = dir;  
		mLocalTransformDirty = true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setVelocity(float velx, float vely, float velz)
	{
		mVelocity.x = velx;
		mVelocity.y = vely;
		mVelocity.z = velz;

		if(mSource != AL_NONE)
		{
			alSource3f(mSource, AL_VELOCITY, velx, vely, velz);
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setVelocity(const Ogre::Vector3 &vel)
	{
		mVelocity = vel;	

		if(mSource != AL_NONE)
		{
			alSource3f(mSource, AL_VELOCITY, vel.x, vel.y, vel.z);
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setVolume(float gain)
	{
		if(gain < 0) return;

		mGain = gain;

		if(mSource != AL_NONE)
		{
			alSourcef(mSource, AL_GAIN, mGain);		
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setMaxVolume(float maxGain)
	{
		if(maxGain < 0 || maxGain > 1) return;

		mMaxGain = maxGain;

		if(mSource != AL_NONE)
		{
			alSourcef(mSource, AL_MAX_GAIN, mMaxGain);	
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setMinVolume(float minGain)
	{
		if(minGain < 0 || minGain > 1) return;

		mMinGain = minGain;

		if(mSource != AL_NONE)
		{
			alSourcef(mSource, AL_MIN_GAIN, mMinGain);		
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setConeAngles(float insideAngle, float outsideAngle)
	{
		if(insideAngle < 0 || insideAngle > 360)	return;
		if(outsideAngle < 0 || outsideAngle > 360)	return;

		mInnerConeAngle = insideAngle;
		mOuterConeAngle = outsideAngle;

		if(mSource != AL_NONE)
		{
			alSourcef (mSource, AL_CONE_INNER_ANGLE,	mInnerConeAngle);
			alSourcef (mSource, AL_CONE_OUTER_ANGLE,	mOuterConeAngle);
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setOuterConeVolume(float gain)
	{
		if(gain < 0 || gain > 1)	return;

		mOuterConeGain = gain;

		if(mSource != AL_NONE)
		{
			alSourcef (mSource, AL_CONE_OUTER_GAIN, mOuterConeGain);
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setMaxDistance(float maxDistance)
	{
		if(maxDistance < 0) return;

		mMaxDistance = maxDistance;

		if(mSource != AL_NONE)
		{
			alSourcef(mSource, AL_MAX_DISTANCE, mMaxDistance);		
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	const float OgreOggISound::getMaxDistance() const
	{
		float val=-1.f;
		if(mSource != AL_NONE)
		{
			alGetSourcef(mSource, AL_MAX_DISTANCE, &val);		
		}
		return val;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setRolloffFactor(float rolloffFactor)
	{
		if(rolloffFactor < 0) return;

		mRolloffFactor = rolloffFactor;

		if(mSource != AL_NONE)
		{
			alSourcef(mSource, AL_ROLLOFF_FACTOR, mRolloffFactor);		
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	const float OgreOggISound::getRolloffFactor() const
	{
		float val=-1.f;
		if(mSource != AL_NONE)
		{
			alGetSourcef(mSource, AL_ROLLOFF_FACTOR, &val);		
		}
		return val;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setReferenceDistance(float referenceDistance)
	{
		if(referenceDistance < 0) return;

		mReferenceDistance = referenceDistance;

		if(mSource != AL_NONE)
		{
			alSourcef(mSource, AL_REFERENCE_DISTANCE, mReferenceDistance);		
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	const float OgreOggISound::getReferenceDistance() const
	{
		float val=-1.f;

		if(mSource != AL_NONE)
		{
			alGetSourcef(mSource, AL_REFERENCE_DISTANCE, &val);		
		}
		return val;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setPitch(float pitch)
	{
		if ( pitch<=0.f ) return;

		mPitch = pitch;

		if(mSource != AL_NONE)
		{
			alSourcef(mSource, AL_PITCH, mPitch);		
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	const float OgreOggISound::getPitch() const
	{
		float val=-1.f;
		if(mSource != AL_NONE)
		{
			alGetSourcef(mSource, AL_PITCH, &val);		
		}
		return val;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::_initSource()
	{
		//'reset' the source properties 		
		if(mSource != AL_NONE)
		{
			alSourcef (mSource, AL_GAIN, mGain);
			alSourcef (mSource, AL_MAX_GAIN, mMaxGain);
			alSourcef (mSource, AL_MIN_GAIN, mMinGain);
			alSourcef (mSource, AL_MAX_DISTANCE, mMaxDistance);	
			alSourcef (mSource, AL_ROLLOFF_FACTOR, mRolloffFactor);
			alSourcef (mSource, AL_REFERENCE_DISTANCE, mReferenceDistance);
			alSourcef (mSource, AL_CONE_OUTER_GAIN, mOuterConeGain);
			alSourcef (mSource, AL_CONE_INNER_ANGLE,	mInnerConeAngle);
			alSourcef (mSource, AL_CONE_OUTER_ANGLE,	mOuterConeAngle);
			alSource3f(mSource, AL_POSITION, mPosition.x, mPosition.y, mPosition.z);
			alSource3f(mSource, AL_DIRECTION, mDirection.x, mDirection.y, mDirection.z);
			alSource3f(mSource, AL_VELOCITY, mVelocity.x, mVelocity.y, mVelocity.z);
			alSourcef (mSource, AL_PITCH, mPitch);
			alSourcei (mSource, AL_SOURCE_RELATIVE, mSourceRelative);
			alSourcei (mSource, AL_LOOPING, mStream ? AL_FALSE : mLoop);
			alSourcei (mSource, AL_SOURCE_STATE, AL_INITIAL);
			mInitialised = true;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	float OgreOggISound::getVolume() const
	{
		float vol=0.f;

		// Check if a source is available
		if(mSource != AL_NONE)
		{
			alGetSourcef(mSource, AL_GAIN, &vol);		
			return vol;
		}
		// If not - return requested setting.
		else
			return mGain;
	}

	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::startFade(bool fDir, float fadeTime, FadeControl actionOnComplete)
	{
		mFade			= true;
	    mFadeInitVol	= getVolume();
		mFadeEndVol		= fDir ? mMaxGain : 0.f;
		mFadeTimer		= 0.f;
		mFadeEndAction	= actionOnComplete;
		mFadeTime		= fadeTime;
		// Automatically start if not currently playing
		if ( mFadeEndVol==mMaxGain )
			if ( !isPlaying() )
				this->play();
	}

	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::_updateFade(float fTime)
	{
		if (mFade)
		{
			if ( (mFadeTimer+=fTime) >= mFadeTime )
			{
				setVolume(mFadeEndVol);
				mFade = false;
				// Perform requested action on completion
				// NOTE:- Must go through SoundManager when using threads to avoid any corruption/mutex issues.
				switch ( mFadeEndAction ) 
				{
				case FC_PAUSE: 
					{ 
						pause(); 
					} 
					break;
				case FC_STOP: 
					{ 
						stop(); 
					} 
					break;
				}
			}
			else
			{
				float vol = (mFadeEndVol-mFadeInitVol)*(mFadeTimer/mFadeTime);
				setVolume(mFadeInitVol + vol);
			}
		} 
	}

	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::_markPlayPosition()
	{
		/** Ignore if no source available.
			With stream sounds the buffers will hold the audio data
			at the position it is kicked off at, although there is potential
			to be 1/4 second out, so may need to look at this..
			for now just re-use buffers
		*/
		if ( !mSeekable || (mSource==AL_NONE) || mStream )
			return;

		alSourcePause(mSource);
		alGetSourcef(mSource, AL_SEC_OFFSET, &mPlayPos);
	}

	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::_recoverPlayPosition()
	{
		if ( !mSeekable || (mSource==AL_NONE) || mStream )
			return;

		alGetError();
		alSourcef(mSource, AL_SEC_OFFSET, mPlayPos);
		if (alGetError())
		{
			Ogre::LogManager::getSingleton().logMessage("***--- OgreOggISound::_recoverPlayPosition() - Unable to set play position", Ogre::LML_CRITICAL);
		}
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggISound::isPlaying() const
	{
		if(mSource != AL_NONE)
		{
			ALenum state;
			alGetError();    
			alGetSourcei(mSource, AL_SOURCE_STATE, &state);
			return (state == AL_PLAYING);
		}

		// May have been kicked off and is currently waiting to be reactivated
		// Return its previous status..
		return false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggISound::isPaused() const
	{
		if(mSource != AL_NONE)
		{
			ALenum state;
			alGetError();    
			alGetSourcei(mSource, AL_SOURCE_STATE, &state);
			return (state == AL_PAUSED);
		}

		return false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggISound::isStopped() const
	{
		if(mSource != AL_NONE)
		{
			ALenum state;
			alGetError();    
			alGetSourcei(mSource, AL_SOURCE_STATE, &state);
			return (!mPlay && (state == AL_STOPPED));
		}

		return false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setPlayPosition(float seconds)
	{
		// Invalid time - exit
		if ( !mSeekable || seconds<0.f ) 
			return;

		// Wrap time
		if ( seconds > mPlayTime ) 
		{
			do		{ seconds-=mPlayTime; }
			while	( seconds>mPlayTime );
		}

		// Set offset if source available
		if ( mSource!=AL_NONE )
		{
			alGetError();
			alSourcef(mSource, AL_SEC_OFFSET, seconds);
			if (alGetError())
			{
				Ogre::LogManager::getSingleton().logMessage("***--- OgreOggISound::setPlayPosition() - Error setting play position", Ogre::LML_CRITICAL);
			}
			// Reset flag
			mPlayPosChanged = false;
		}
		// Mark it so it can be applied when sound receives a source
		else
		{
			mPlayPosChanged = true;
			mPlayPos = seconds;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	float OgreOggISound::getPlayPosition()
	{
		// Invalid time - exit
		if ( !mSeekable || !mSource ) 
			return -1.f;

		// Set offset if source available
		ALfloat offset=-1.f;
		alGetError();
		alGetSourcef(mSource, AL_SEC_OFFSET, &offset);
		if (alGetError())
		{
			Ogre::LogManager::getSingleton().logMessage("***--- OgreOggISound::setPlayPosition() - Error getting play position", Ogre::LML_CRITICAL);
			return -1.f;
		}
			
		return offset;
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggISound::addCuePoint(float seconds)
	{
		// Valid time?
		if ( seconds > 0.f )
		{
			mCuePoints.push_back(seconds);
			return true;
		}
		else
			return false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::removeCuePoint(unsigned short index)
	{
		if ( mCuePoints.empty() || ((int)mCuePoints.size()<=index) )
			return;

		// Erase element
		mCuePoints.erase(mCuePoints.begin()+index);
	}

	/*/////////////////////////////////////////////////////////////////*/
	float OgreOggISound::getCuePoint(unsigned short index)
	{
		if ( mCuePoints.empty() || ((int)mCuePoints.size()<=index) )
			return -1.f;

		// get element
		return mCuePoints.at(index);
	}

	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setCuePoint(unsigned short index)
	{
		if ( mCuePoints.empty() || ((int)mCuePoints.size()<=index) )
			return;

		// set cue point
		setPlayPosition(mCuePoints.at(index));
	}

	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::setRelativeToListener(bool relative)
	{
		mSourceRelative = relative;
		
		if(mSource != AL_NONE)
		{
			alSourcei(mSource, AL_SOURCE_RELATIVE, mSourceRelative);
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::update(float fTime)
	{
		if (mLocalTransformDirty)
		{
			if (!mDisable3D && mParentNode)
			{
				mPosition = mParentNode->_getDerivedPosition();
				mDirection = -mParentNode->_getDerivedOrientation().zAxis();
			}

			if(mSource != AL_NONE)
			{
				alSource3f(mSource, AL_POSITION, mPosition.x, mPosition.y, mPosition.z);
				alSource3f(mSource, AL_DIRECTION, mDirection.x, mDirection.y, mDirection.z);
				mLocalTransformDirty = false;
			}
		}	

		_updateFade(fTime);
	}
	/*/////////////////////////////////////////////////////////////////*/
	const Ogre::String& OgreOggISound::getMovableType(void) const
	{
		return OgreOggSoundFactory::FACTORY_TYPE_NAME;
	}
	/*/////////////////////////////////////////////////////////////////*/
	const Ogre::AxisAlignedBox& OgreOggISound::getBoundingBox(void) const
	{
		static Ogre::AxisAlignedBox aab;
		return aab;
	}
	/*/////////////////////////////////////////////////////////////////*/
	float OgreOggISound::getBoundingRadius(void) const
	{
		return 0;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::_updateRenderQueue(Ogre::RenderQueue *queue)
	{
		return;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::_notifyAttached(Ogre::Node* node, bool isTagPoint)
	{
		// Call base class notify
		Ogre::MovableObject::_notifyAttached(node, isTagPoint);

		// Immediately set position/orientation when attached
		if (mParentNode)
		{
			mPosition = mParentNode->_getDerivedPosition();
			mDirection = -mParentNode->_getDerivedOrientation().zAxis();
		}

		// Set tarnsform immediately if possible
		if(mSource != AL_NONE)
		{
			alSource3f(mSource, AL_POSITION, mPosition.x, mPosition.y, mPosition.z);
			alSource3f(mSource, AL_DIRECTION, mDirection.x, mDirection.y, mDirection.z);
		}

		return;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::_notifyMoved(void) 
	{ 
		// Call base class notify
		Ogre::MovableObject::_notifyMoved();

		mLocalTransformDirty=true; 
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggISound::visitRenderables(Ogre::Renderable::Visitor* visitor, bool debugRenderables)
	{
		return;
	}
}