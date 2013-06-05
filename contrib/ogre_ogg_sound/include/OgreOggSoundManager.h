/**
* @file OgreOggSoundManager.h
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
* Manages the audio library
*/

#pragma once

#include "OgreOggSoundPrereqs.h"
#include "OgreOggSound.h"
#include "LocklessQueue.h"

#include <map>
#include <string>

#if OGGSOUND_THREADED
#	ifdef POCO_THREAD
#		include "Poco/ScopedLock.h"
#		include "Poco/Thread.h"
#		include "Poco/Mutex.h"
#	else 
#		include <boost/thread/thread.hpp>
#		include <boost/function/function0.hpp>
#		include <boost/thread/recursive_mutex.hpp>
#		include <boost/thread/xtime.hpp>
#	endif
#endif

namespace OgreOggSound
{
	typedef std::map<std::string, OgreOggISound*> SoundMap;
	typedef std::map<std::string, ALuint> EffectList;
	typedef std::map<ALenum, bool> FeatureList;
	typedef std::list<OgreOggISound*> ActiveList;
	typedef std::deque<ALuint> SourceList;
	
	//! Various sound commands
	enum SOUND_ACTION
	{
		LQ_PLAY,
		LQ_STOP,
		LQ_PAUSE,
		LQ_LOAD,
		LQ_GLOBAL_PITCH,
		LQ_STOP_ALL,
		LQ_PAUSE_ALL,
		LQ_RESUME_ALL,
		LQ_REACTIVATE,
		LQ_DESTROY_TEMPORARY,
		LQ_ATTACH_EFX,
		LQ_DETACH_EFX,
		LQ_SET_EFX_PROPERTY
	};

	//! Holds information about a sound action
	struct SoundAction
	{
		Ogre::String	mSound;
		SOUND_ACTION	mAction;
		bool			mImmediately;
		void*			mParams;
	};

	//! Holds information about a create sound request.
	struct cSound
	{
		bool mPrebuffer;
		Ogre::String mFileName;
		ALuint mBuffer;
		Ogre::DataStreamPtr mStream;
	};

	//! Holds information about a EFX effect.
	struct efxProperty
	{
		Ogre::String mEffectName;
		Ogre::String mFilterName;
		float mAirAbsorption;
		float mRolloff;
		float mConeHF;
		ALuint mSlotID;
	};

	//! Holds information about a static shared audio buffer.
	struct sharedAudioBuffer
	{
		ALuint mAudioBuffer;
		unsigned int mRefCount;

	};

	typedef std::map<std::string, sharedAudioBuffer*> SharedBufferList;

	//! Sound Manager: Manages all sounds for an application
	class _OGGSOUND_EXPORT OgreOggSoundManager : public Ogre::Singleton<OgreOggSoundManager>
	{

	public:

		// Version string
		static const Ogre::String OGREOGGSOUND_VERSION_STRING;

		/** Creates a manager for all sounds within the application.
		 */
		OgreOggSoundManager();
		/** Gets a singleton reference.
		 */
		static OgreOggSoundManager& getSingleton(void);
		/** Gets a singleton reference.
		 */
		static OgreOggSoundManager* getSingletonPtr(void);
		/** Destroys this manager.
		@remarks
			Destroys all sound objects and thread if defined. Cleans up
			all OpenAL objects, buffers and devices and closes down the
			audio device.
		 */
		~OgreOggSoundManager();
		/** Creates a listener object for the system
		@remarks
			Only needed when clearScene or similar is used which destroys listener object automatically 
			without the manager knowing. You can therefore use this function to recreate a listener
			object for the system.
		 */
		bool createListener();
		/** Initialises the audio device.
		@remarks
			Attempts to initialise the audio device for sound playback.
			Internally some logging is done to list features supported as
			well as creating a pool of sources from which sounds can be
			attached and played.
			@param deviceName
				Audio device string to open, will use default device if not found.
			@param maxSources
				maximum number of sources to allocate (optional)
			@param queueListSize
				Desired size of queue list (optional | Multi-threaded ONLY)
		 */
		bool init(const std::string &deviceName = "", unsigned int maxSources=100, unsigned int queueListSize=100, Ogre::SceneManager* sMan=0);
		/** Sets the global volume for all sounds
			@param vol 
				global attenuation for all sounds.
		 */
		void setMasterVolume(ALfloat vol);
		/** Sets the default SceneManager for creation of sound objects
		 */
		void setSceneManager(Ogre::SceneManager* sMan) { mSceneMgr=sMan; }
		/** Gets the default SceneManager for creation of sound objects
		 */
		Ogre::SceneManager* getSceneManager() { return mSceneMgr; }
		/** Gets number of currently created sounds
		 */
		unsigned int getNumSounds() const { return static_cast<unsigned int>(mSoundMap.size()); }
		/** Gets the current global volume for all sounds
		 */
		ALfloat getMasterVolume();
		/** Creates a single sound object.
		@remarks
			Plugin specific version of createSound, uses createMovableObject() to instantiate
			a sound automatically registered with the supplied SceneManager, allows OGRE to automatically
			cleanup/manage this sound.
			Each sound must have a unique name within the manager.
			@param name 
				Unique name of sound
			@param file 
				Audio file path string
			@param stream 
				Flag indicating if the sound sound be streamed.
			@param loop 
				Flag indicating if the file should loop.
			@param preBuffer 
				Flag indicating if a source should be attached at creation.
			@param scnMgr
				Pointer to SceneManager this sound belongs - 0 defaults to first SceneManager defined.
			@param immediately
				Optional flag to indicate creation should occur immediately and not be passed to background thread
				for queueing. Can be used to overcome the random creation time which might not be acceptable (MULTI-THREADED ONLY)
		 */
		OgreOggISound* createSound(const std::string& name,const std::string& file, bool stream = false, bool loop = false, bool preBuffer=false, Ogre::SceneManager* scnMgr=0, bool immediate=false);
		/** Gets a named sound.
		@remarks
			Returns a named sound object if defined, NULL otherwise.
			@param name 
				Sound name.
		 */
		OgreOggISound *getSound(const std::string& name);
		/** Gets list of created sounds.
		@remarks
			Returns a vector of sound name strings.
		 */
		const Ogre::StringVector getSoundList() const;
		/** Returns whether named sound exists.
		@remarks
			Checks sound map for a named sound.
			@param name 
				Sound name.
		 */
		bool hasSound(const std::string& name);
		/** Sets the pitch of all sounds.
		@remarks
			Sets the pitch modifier applied to all sounds.
			@param pitch
				new pitch for all sounds (positive value)
		 */
		void setGlobalPitch(float pitch);
		/** Gets the current global pitch.
		 */
		const float getGlobalPitch() const { return mGlobalPitch; }
		/** Stops all currently playing sounds.
		 */
		void stopAllSounds();
		/** Pauses all currently playing sounds.
		 */
		void pauseAllSounds();
		/** Mutes all sounds.
		 */
		void muteAllSounds();
		/** Un mutes all sounds.
		 */
		void unmuteAllSounds();
		/** Resumes all previously playing sounds.
		 */
		void resumeAllPausedSounds();
		/** Destroys all sounds within manager.
		 */
		void destroyAllSounds();
		/** Destroys a single sound.
		@remarks
			Destroys a single sound object.
			@param name 
				Sound name to destroy.
		 */
		void destroySound(const std::string& name="");
		/** Destroys a single sound.
		@remarks
			Destroys a single sound object.
			@param name 
				Sound name to destroy.
		 */
		void destroySound(OgreOggISound* sound);
		/** Destroys a temporary sound implementation
		@remarks
			Internal use only.
			@param sound
				Sound to destroy.
		 */
		void _destroyTemporarySoundImpl(OgreOggISound* sound);
		/** Destroys a temporary sound.
		@remarks
			Internal use only.
			@param sound
				Sound to destroy.
		 */
		void _destroyTemporarySound(OgreOggISound* sound);
		/** Requests a free source object.
		@remarks
			Internal function - SHOULD NOT BE CALLED BY USER CODE
			Retrieves a free source object and attaches it to the
			specified sound object. Internally checks for any currently
			available sources, then checks stopped sounds and finally
			prioritised sounds.
			@param sound 
				Sound pointer.
		 */
		bool _requestSoundSource(OgreOggISound* sound=0);
		/** Release a sounds source.
		@remarks
			Internal function - SHOULD NOT BE CALLED BY USER CODE
			Releases a specified sounds source object back to the system,
			allowing it to be re-used by another sound.
			@param sound 
				Sound pointer.
		 */
		bool _releaseSoundSource(OgreOggISound* sound=0);
		/** Releases a shared audio buffer
		@remarks
			Internal function - SHOULD NOT BE CALLED BY USER CODE
			Each shared audio buffer is reference counted so destruction is handled correctly,
			this function merely decrements the reference count, only destroying when no sounds
			are referencing buffer.
			@param sName
				Name of audio file
			@param buffer
				buffer id
		*/
		bool _releaseSharedBuffer(const Ogre::String& sName, ALuint& buffer);
		/** Registers a shared audio buffer
		@remarks
			Internal function - SHOULD NOT BE CALLED BY USER CODE
			Its possible to share audio buffer data among many sources so this function
			registers an audio buffer as 'sharable', meaning if a the same audio file is
			created more then once, it will simply use the original buffer data instead of
			creating/loading the same data again.
			@param sName
				Name of audio file
			@param buffer
				OpenAL buffer ID holding audio data
		 */
		bool _registerSharedBuffer(const Ogre::String& sName, ALuint& buffer);
		/** Sets distance model.
		@remarks
			Sets the global distance attenuation algorithm used by all
			sounds in the system.
			@param value 
				ALenum value of distance model.
		 */
		void setDistanceModel(ALenum value);
		/** Sets doppler factor.
		@remarks
			Sets the global doppler factor which affects attenuation for
			all sounds
			@param factor 
				Factor scale (>0).
		 */
		void setDopplerFactor(float factor=1.f);
		/** Sets speed of sound.
		@remarks
			Sets the global speed of sound used in the attenuation algorithm,
			affects all sounds.
			@param speed 
				Speed (m/s).
		 */
		void setSpeedOfSound(float speed=363.f);
		/** Fades master volume in/out
		@remarks
			Allows fading of in/out of alls sounds
		 */
		void fadeMasterVolume(float time, bool fadeIn);
		/** Gets a list of device strings
		@remarks
			Creates a list of available audio device strings
		 */
		const Ogre::StringVector getDeviceList() const;
		/** Returns pointer to listener.
		 */
		OgreOggListener* getListener() { return mListener; }
		/** Returns number of sources created.
		 */
		int getNumSources() const { return mNumSources; }
		/** Updates system.
		@remarks
			Iterates all sounds and updates them.
			@param fTime 
				Elapsed frametime.
		 */
		void update(float fTime=0.f);
		/** Sets a resource group name to search for all sounds first.
		@remarks
			A speed improvement to skip the cost of searching all resource locations/groups when creating sounds.
			Will default to searching all groups if sound is not found.
			@param group
				Name of OGRE ResourceGroup.
		 */
		void setResourceGroupName(const Ogre::String& group) { mResourceGroupName=group; }
		/** Returns user defined search group name
		 */
		const Ogre::String& getResourceGroupName() const { return mResourceGroupName; }
#if HAVE_EFX
		/** Returns XRAM support status.
		 */
		bool hasXRamSupport() { return mXRamSupport; }
		/** Returns EFX support status.
		 */
		bool hasEFXSupport() { return mEFXSupport; }
		/** Returns EAX support status.
		 */
		bool hasEAXSupport() { return mEAXSupport; }
		/** Sets XRam buffers.
		@remarks
			Currently defaults to AL_STORAGE_AUTO.
		 */
		void setXRamBuffer(ALsizei numBuffers, ALuint* buffers);
		/** Sets XRam buffers storage mode.
		@remarks
			Should be called before creating any sounds
			Options: AL_STORAGE_AUTOMATIC | AL_STORAGE_HARDWARE | AL_STORAGE_ACCESSIBLE
		 */
		void setXRamBufferMode(ALenum mode);
		/** Sets the distance units of measurement for EFX effects.
		@remarks
			@param unit 
				units(meters).
		 */
		void setEFXDistanceUnits(float unit=3.3f);
		/** Creates a specified EFX filter
		@remarks
			Creates a specified EFX filter if hardware supports it.
			@param eName 
				name for filter.
			@param type 
				see OpenAL docs for available filters.
			@param gain
				see OpenAL docs for available filters.
			@param hfgain 
				see OpenAL docs for available filters.
		 */
		bool createEFXFilter(const std::string& eName, ALint type, ALfloat gain=1.0, ALfloat hfGain=1.0);
		/** Creates a specified EFX effect
		@remarks
			Creates a specified EFX effect if hardware supports it. Optional reverb
			preset structure can be passed which will be applied to the effect. See
			eax-util.h for list of presets.
			@param eName 
				name for effect.
			@param type 
				see OpenAL docs for available effects.
			@param props	
				legacy structure describing a preset reverb effect.
		 */
		bool createEFXEffect(const std::string& eName, ALint type, EAXREVERBPROPERTIES* props=0);
		/** Sets extended properties on a specified sounds source
		@remarks
			Tries to set EFX extended source properties.
			@param sName 
				name of sound.
			@param airAbsorption 
				absorption factor for air.
			@param roomRolloff 
				room rolloff factor.
			@param coneOuterHF 
				cone outer gain factor for High frequencies.
		 */
		bool setEFXSoundProperties(const std::string& sName, float airAbsorption=0.f, float roomRolloff=0.f, float coneOuterHF=0.f);
		/** Sets extended properties on a specified sounds source
		@remarks
			Tries to set EFX extended source properties.
			@param sound
				name of sound.
			@param airAbsorption 
				absorption factor for air.
			@param roomRolloff 
				room rolloff factor.
			@param coneOuterHF 
				cone outer gain factor for High frequencies.
		 */
		bool _setEFXSoundPropertiesImpl(OgreOggISound* sound=0, float airAbsorption=0.f, float roomRolloff=0.f, float coneOuterHF=0.f);
		/** Sets a specified paremeter on an effect
		@remarks
			Tries to set a parameter value on a specified effect. Returns true/false.
			@param eName 
				name of effect.
			@param effectType 
				see OpenAL docs for available effects.
			@param attrib 
				parameter value to alter.
			@param param 
				float value to set.
		 */
		bool setEFXEffectParameter(const std::string& eName, ALint effectType, ALenum attrib, ALfloat param);
		/** Sets a specified paremeter on an effect
		@remarks
			Tries to set a parameter value on a specified effect. Returns true/false.
			@param eName 
				name of effect.
			@param type 
				see OpenAL docs for available effects.
			@param attrib 
				parameter value to alter.
			@param params 
				vector pointer of float values to set.
		 */
		bool setEFXEffectParameter(const std::string& eName, ALint type, ALenum attrib, ALfloat* params=0);
		/** Sets a specified paremeter on an effect
		@remarks
			Tries to set a parameter value on a specified effect. Returns true/false.
			@param eName 
				name of effect.
			@param type 
				see OpenAL docs for available effects.
			@param attrib 
				parameter value to alter.
			@param param 
				integer value to set.
		 */
		bool setEFXEffectParameter(const std::string& eName, ALint type, ALenum attrib, ALint param);
		/** Sets a specified paremeter on an effect
		@remarks
			Tries to set a parameter value on a specified effect. Returns true/false.
			@param eName 
				name of effect.
			@param type 
				see OpenAL docs for available effects.
			@param attrib 
				parameter value to alter.
			@param params 
				vector pointer of integer values to set.
		 */
		bool setEFXEffectParameter(const std::string& eName, ALint type, ALenum attrib, ALint* params=0);
		/** Gets the maximum number of Auxiliary Effect slots per source
		@remarks
			Determines how many simultaneous effects can be applied to
			any one source object
		 */
		int getNumberOfSupportedEffectSlots();
		/** Gets the number of currently created Auxiliary Effect slots
		@remarks
			Returns number of slots craeted and available for effects/filters.
		 */
		int getNumberOfCreatedEffectSlots();
		/** Creates a specified EFX filter
		@remarks
			Creates a specified EFX filter if hardware supports it.
			@param eName 
				name for filter. type see OpenAL docs for available filter types.
		 */
		bool createEFXSlot();
		/** Attaches an effect to a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sName 
				name of sound
			@param slot 
				slot ID
			@param effect 
				name of effect as defined when created
			@param filter 
				name of filter as defined when created
		 */
		bool attachEffectToSound(const std::string& sName, ALuint slot, const Ogre::String& effect="", const Ogre::String& filter="");
		/** Attaches a filter to a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sName 
				name of sound
			@param filter 
				name of filter as defined when created
		 */
		bool attachFilterToSound(const std::string& sName, const Ogre::String& filter="");
		/** Detaches all effects from a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sName 
				name of sound
			@param slotID 
				slot ID
		 */
		bool detachEffectFromSound(const std::string& sName, ALuint slotID);
		/** Detaches all filters from a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sName 
				name of sound
		 */
		bool detachFilterFromSound(const std::string& sName);
		/** Attaches an effect to a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sound 
				sound pointer
			@param slot 
				slot ID
			@param effect 
				name of effect as defined when created
			@param filter 
				name of filter as defined when created
		 */
		bool _attachEffectToSoundImpl(OgreOggISound* sound=0, ALuint slot=255, const Ogre::String& effect="", const Ogre::String& filter="");
		/** Attaches a filter to a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sound 
				sound pointer
			@param filter 
				name of filter as defined when created
		 */
		bool _attachFilterToSoundImpl(OgreOggISound* sound=0, const Ogre::String& filter="");
		/** Detaches all effects from a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sound
				sound pointer
			@param slotID 
				slot ID
		 */
		bool _detachEffectFromSoundImpl(OgreOggISound* sound=0, ALuint slotID=255);
		/** Detaches all filters from a sound
		@remarks
			Currently sound must have a source attached prior to this call.
			@param sound
				sound pointer
		 */
		bool _detachFilterFromSoundImpl(OgreOggISound* sound=0);
		/** Returns whether a specified effect is supported
			@param effectID 
				OpenAL effect/filter id. (AL_EFFECT... | AL_FILTER...)
		 */
		bool isEffectSupported(ALint effectID);
#endif

#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
		OgreOggSoundRecord* createRecorder();
		/** Gets recording device
		 */
		OgreOggSoundRecord* getRecorder() { return mRecorder; }
		/** Returns whether a capture device is available
		 */
		bool isRecordingAvailable() const;
		/** Creates a recordable object
		 */
#endif

#if OGGSOUND_THREADED
#	if POCO_THREAD
		static Poco::Mutex mMutex;
#	else
		static boost::recursive_mutex mMutex;
#	endif

		/** Pushes a sound action request onto the queue
		@remarks
			Internal function - SHOULD NOT BE CALLED BY USER CODE!
			Sound actions are queued through the manager to be operated on in an efficient and
			non-blocking manner, this function adds a request to the list to be processed.
			@param sound
				Sound object to perform action upon
			@param action
				Action to perform.
		*/
		void _requestSoundAction(const SoundAction& action);
		/** Sets the mForceMutex flag for switching between non-blocking/blocking action calls.
		@remarks
			@param on
				Flag indicating status of mForceMutex var.
		*/
		void setForceMutex(bool on) { mForceMutex=on; }
#endif
 
	private:

		LocklessQueue<OgreOggISound*>* mSoundsToDestroy;

#if OGGSOUND_THREADED
		/** Processes queued sound actions.
		@remarks
			Presently executes a maximum of 5 actions in a single iteration.
		 */
		void _processQueuedSounds(void);
		/** Updates all sound buffers.
		@remarks
			Iterates all sounds and updates their buffers.
		 */
		void _updateBuffers();

		LocklessQueue<SoundAction>* mActionsList;

#ifdef POCO_THREAD
		static Poco::Thread* mUpdateThread;
		class Updater : public Poco::Runnable
		{
		public:
			virtual void run();
		};
		friend class Updater;
		static Updater* mUpdater;
#else
		static boost::thread* mUpdateThread;
#endif
		static bool mShuttingDown;

		/** Flag indicating that a mutex should be used whenever an action is requested.
		@remarks
			In certain instances user may require that an action is performed inline,
			rather then the default: queued and performed asynchronously. This global flag
			will affect all subsequent asynchronous calls to execute immediately, with the
			disadvantage that it will block the main thread. NOTE:- this doesn't affect
			buffer updates, which will still be handled asynchronously.
		*/
		bool mForceMutex;			 
									
		/** Performs a requested action.
		@param act
			Action struct describing action to perform
		 */
		void _performAction(const SoundAction& act);

		/** Threaded function for streaming updates
		@remarks
			Optional threading function specified in OgreOggPreReqs.h.
			Implemented to handle updating of streamed audio buffers
			independently of main game thread, unthreaded streaming
			would be disrupted by any pauses or large frame lags, due to
			the fact that OpenAL itself runs in its own thread. If the audio
			buffers aren't constantly re-filled the sound will be automatically
			stopped by OpenAL. Static sounds do not suffer this problem because all the
			audio data is preloaded into memory.
		 */
		static void threadUpdate()
		{
			while(!mShuttingDown)
			{	
				{
#ifdef POCO_THREAD
					Poco::Mutex::ScopedLock l(OgreOggSoundManager::getSingletonPtr()->mMutex);
#else
					boost::recursive_mutex::scoped_lock lock(OgreOggSoundManager::getSingletonPtr()->mMutex);
#endif
					OgreOggSoundManager::getSingletonPtr()->_updateBuffers();
					OgreOggSoundManager::getSingletonPtr()->_processQueuedSounds();
				}
#ifdef POCO_THREAD
				Poco::Thread::sleep(10);
#else
				boost::this_thread::sleep(boost::posix_time::milliseconds(10));
#endif
			}
		}
#endif
		/** Creates a single sound object (implementation).
		@remarks
			Creates and inits a single sound object, depending on passed
			parameters this function will create a static/streamed sound.
			Each sound must have a unique name within the manager.
			@param scnMgr
				Reference to creator
			@param name 
				Unique name of sound
			@param file 
				Audio file path string
			@param stream 
				Flag indicating if the sound sound be streamed.
			@param loop 
				Flag indicating if the file should loop.
			@param preBuffer 
				Flag indicating if a source should be attached at creation.
		 */
		OgreOggISound* _createSoundImpl(const Ogre::SceneManager& scnMgr, const std::string& name,const std::string& file, bool stream = false, bool loop = false, bool preBuffer=false, bool immediate=false);
		/** Implementation of sound loading
		@param sound
			sound pointer.
		@param file
			name of sound file.
		@param stream
			DataStreamPtr (optional).
		@param prebuffer
			Prebuffer flag.
		*/
		void _loadSoundImpl(OgreOggISound* sound, const Ogre::String& file, Ogre::DataStreamPtr stream, bool prebuffer);
		/** Destroys a single sound.
		@remarks
			Destroys a single sound object.
			@param sound
				Sound object to destroy.
		 */
		void _destroySoundImpl(OgreOggISound* sound=0);
		/** Destroys a single sound.
		@remarks
			Destroys a single sound object.
			@param sound
				Sound to destroy.
		 */
		void _releaseSoundImpl(OgreOggISound* sound=0);
		/** Removes references of a sound from all possible internal lists.
		@remarks
			Various lists exist to manage numerous states of a sound, this
			function exists to remove a sound object from any/all lists it has
			previously been added to. 
			@param sound
				Sound to destroy.
		 */
		void _removeFromLists(OgreOggISound* sound=0);
		/** Stops all currently playing sounds.
		 */
		void _stopAllSoundsImpl();
		/** Applys global pitch.
		 */
		void _setGlobalPitchImpl();
		/** Pauses all currently playing sounds.
		 */
		void _pauseAllSoundsImpl();
		/** Resumes all previously playing sounds.
		 */
		void _resumeAllPausedSoundsImpl();
		/** Destroys all sounds.
		 */
		void _destroyAllSoundsImpl();
		/** Creates a pool of OpenAL sources for playback.
		@remarks
			Attempts to create a pool of source objects which allow
			simultaneous audio playback. The number of sources will be
			clamped to either the hardware maximum or [mMaxSources]
			whichever comes first.
		 */
		int _createSourcePool();
		/** Gets a shared audio buffer
		@remarks
			Returns a previously loaded shared buffer reference if available.
			NOTE:- Increments a reference count so releaseSharedBuffer() must be called
			when buffer is no longer used.
			@param sName
				Name of audio file
		 */
		ALuint _getSharedBuffer(const Ogre::String& sName);
		/** Releases all sounds and buffers
		@remarks
			Release all sounds and their associated OpenAL objects
			from the system.
		 */
		void _releaseAll();
		/** Checks and Logs a supported feature list
		@remarks
			Queries OpenAL for various supported features and lists
			them with the LogManager.
		 */
		void _checkFeatureSupport();
#if HAVE_EFX
		/** Checks for EFX hardware support
		 */
		bool _checkEFXSupport();
		/** Checks for XRAM hardware support
		 */
		bool _checkXRAMSupport();
		/** Checks for EAX effect support
		 */
		void _determineAuxEffectSlots();
		/** Gets a specified EFX filter
		@param fName 
			name of filter as defined when created.
		 */
		ALuint _getEFXFilter(const std::string& fName);
		/** Gets a specified EFX Effect
		@param eName 
			name of effect as defined when created.
		 */
		ALuint _getEFXEffect(const std::string& eName);
		/** Gets a specified EFX Effect slot
		@param slotID 
			index of auxiliary effect slot
		 */
		ALuint _getEFXSlot(int slotID=0);
		/** Sets EAX reverb properties using a specified present
		@param pEFXEAXReverb 
			pointer to converted EFXEAXREVERBPROPERTIES structure object
		@param uiEffect 
			effect ID
		 */
		bool _setEAXReverbProperties(EFXEAXREVERBPROPERTIES *pEFXEAXReverb, ALuint uiEffect);
		/** Attaches a created effect to an Auxiliary slot
		@param slot 
			slot ID
		@param effect 
			effect ID
		 */
		bool _attachEffectToSlot(ALuint slot, ALuint effect);
#endif
		/** Re-activates any sounds which had their source stolen.
		 */
		void _reactivateQueuedSounds();
		/** Re-activates any sounds which had their source stolen, implementation methods.
		@remarks
			When all sources are in use the sounds begin to give up
			their source objects to higher priority sounds. When this
			happens the lower priority sound is queued ready to re-play
			when a source becomes available again, this function checks
			this queue and tries to re-play those sounds. Only affects
			sounds which were originally playing when forced to give up
			their source object.
		 */
		void _reactivateQueuedSoundsImpl();
		/** Enumerates audio devices.
		@remarks
			Gets a list of audio device available.
		 */
		void _enumDevices();
		/** Creates a listener object.
		 */
		OgreOggListener* _createListener();
		/** Destroys a listener object.
		 */
		void _destroyListener();

		/**
		 * OpenAL device objects
		 */
		ALCdevice* mDevice;						// OpenAL device
		ALCcontext* mContext;					// OpenAL context

		ALfloat	mOrigVolume;					// Used to revert volume after a mute

		/** Sound lists
		 */
		SoundMap mSoundMap;						// Map of all sounds
		ActiveList mActiveSounds;				// list of sounds currently active
		ActiveList mPausedSounds;				// list of sounds currently paused
		ActiveList mSoundsToReactivate;			// list of sounds that need re-activating when sources become available
		ActiveList mWaitingSounds;				// list of sounds that need playing when sources become available
		SourceList mSourcePool;					// List of available sources
		FeatureList mEFXSupportList;			// List of supported EFX effects by OpenAL ID
		SharedBufferList mSharedBuffers;		// List of shared static buffers

		/** Fading vars
		*/																  
		Ogre::Real mFadeTime;					// Time over which to fade
		Ogre::Real mFadeTimer;					// Timer for fade
		bool mFadeIn;							// Direction fade in/out
		bool mFadeVolume;						// Flag for fading

		ALCchar* mDeviceStrings;				// List of available devices strings
		unsigned int mNumSources;				// Number of sources available for sounds
		unsigned int mMaxSources;				// Maximum Number of sources to allocate

		float mGlobalPitch;						// Global pitch modifier

		OgreOggSoundRecord* mRecorder;			// recorder object

		//! sorts sound list by distance
		struct _sortNearToFar;
		//! sorts sound list by distance
		struct _sortFarToNear;

#if HAVE_EFX
		/**	EFX Support
		*/
		bool mEFXSupport;						// EFX present flag

		// Effect objects
		LPALGENEFFECTS alGenEffects;
		LPALDELETEEFFECTS alDeleteEffects;
		LPALISEFFECT alIsEffect;
		LPALEFFECTI alEffecti;
		LPALEFFECTIV alEffectiv;
		LPALEFFECTF alEffectf;
		LPALEFFECTFV alEffectfv;
		LPALGETEFFECTI alGetEffecti;
		LPALGETEFFECTIV alGetEffectiv;
		LPALGETEFFECTF alGetEffectf;
		LPALGETEFFECTFV alGetEffectfv;

		//Filter objects
		LPALGENFILTERS alGenFilters;
		LPALDELETEFILTERS alDeleteFilters;
		LPALISFILTER alIsFilter;
		LPALFILTERI alFilteri;
		LPALFILTERIV alFilteriv;
		LPALFILTERF alFilterf;
		LPALFILTERFV alFilterfv;
		LPALGETFILTERI alGetFilteri;
		LPALGETFILTERIV alGetFilteriv;
		LPALGETFILTERF alGetFilterf;
		LPALGETFILTERFV alGetFilterfv;

		// Auxiliary slot object
		LPALGENAUXILIARYEFFECTSLOTS alGenAuxiliaryEffectSlots;
		LPALDELETEAUXILIARYEFFECTSLOTS alDeleteAuxiliaryEffectSlots;
		LPALISAUXILIARYEFFECTSLOT alIsAuxiliaryEffectSlot;
		LPALAUXILIARYEFFECTSLOTI alAuxiliaryEffectSloti;
		LPALAUXILIARYEFFECTSLOTIV alAuxiliaryEffectSlotiv;
		LPALAUXILIARYEFFECTSLOTF alAuxiliaryEffectSlotf;
		LPALAUXILIARYEFFECTSLOTFV alAuxiliaryEffectSlotfv;
		LPALGETAUXILIARYEFFECTSLOTI alGetAuxiliaryEffectSloti;
		LPALGETAUXILIARYEFFECTSLOTIV alGetAuxiliaryEffectSlotiv;
		LPALGETAUXILIARYEFFECTSLOTF alGetAuxiliaryEffectSlotf;
		LPALGETAUXILIARYEFFECTSLOTFV alGetAuxiliaryEffectSlotfv;

		/**	XRAM Support
		*/
		typedef ALboolean (__cdecl *LPEAXSETBUFFERMODE)(ALsizei n, ALuint *buffers, ALint value);
		typedef ALenum    (__cdecl *LPEAXGETBUFFERMODE)(ALuint buffer, ALint *value);

		LPEAXSETBUFFERMODE mEAXSetBufferMode;
		LPEAXGETBUFFERMODE mEAXGetBufferMode;

		/**	EAX Support
		*/
		bool mEAXSupport;						// EAX present flag
		int mEAXVersion;						// EAX version ID

		bool mXRamSupport;

		EffectList mFilterList;					// List of EFX filters
		EffectList mEffectList;					// List of EFX effects
		SourceList mEffectSlotList;				// List of EFX effect slots

		ALint mNumEffectSlots;					// Number of effect slots available
		ALint mNumSendsPerSource;				// Number of aux sends per source

		ALenum	mXRamSize,
				mXRamFree,
				mXRamAuto,
				mXRamHardware,
				mXRamAccessible,
				mCurrentXRamMode;

		ALint	mXRamSizeMB,
				mXRamFreeMB;
#endif

		Ogre::String mResourceGroupName;		// Resource group name to search for all sounds

		Ogre::SceneManager* mSceneMgr;			// Default SceneManager to use to create sound objects

		/**	Listener pointer
		 */
		OgreOggListener *mListener;				// Listener object

		friend class OgreOggSoundFactory;
	};
}
