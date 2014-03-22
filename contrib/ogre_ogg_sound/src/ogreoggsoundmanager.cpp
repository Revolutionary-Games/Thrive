/**
* @file OgreOggSoundManager.cpp
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

#include "OgreOggSoundManager.h"
#include "OgreOggSound.h"

#include <string>

#if OGGSOUND_THREADED
#	ifdef POCO_THREAD
		Poco::Thread *OgreOggSound::OgreOggSoundManager::mUpdateThread = 0;
		OgreOggSound::OgreOggSoundManager::Updater* OgreOggSound::OgreOggSoundManager::mUpdater = 0;
		Poco::Mutex OgreOggSound::OgreOggSoundManager::mMutex;
#	else
		boost::thread *OgreOggSound::OgreOggSoundManager::mUpdateThread = 0;
		boost::recursive_mutex OgreOggSound::OgreOggSoundManager::mMutex;
#	endif
	bool OgreOggSound::OgreOggSoundManager::mShuttingDown = false;
#endif

#if OGRE_VERSION_MAJOR == 1 && OGRE_VERSION_MINOR <= 7    
	template<> OgreOggSound::OgreOggSoundManager* Ogre::Singleton<OgreOggSound::OgreOggSoundManager>::ms_Singleton = 0;
#else
	template<> OgreOggSound::OgreOggSoundManager* Ogre::Singleton<OgreOggSound::OgreOggSoundManager>::msSingleton = 0;
#endif

namespace OgreOggSound
{
	using namespace Ogre;

	const Ogre::String OgreOggSoundManager::OGREOGGSOUND_VERSION_STRING = "OgreOggSound v1.22";

	/*/////////////////////////////////////////////////////////////////*/
	OgreOggSoundManager::OgreOggSoundManager() :
		mNumSources(0)
		,mOrigVolume(1.f)
		,mDevice(0)
		,mContext(0)
		,mListener(0)
#if HAVE_EFX
		,mEAXSupport(false)
		,mEFXSupport(false)
		,mXRamSupport(false)
		,mXRamSize(0)
		,mXRamFree(0)
		,mXRamAuto(0)
		,mXRamHardware(0)
		,mXRamAccessible(0)
		,mCurrentXRamMode(0)
		,mEAXVersion(0)
#endif
		,mRecorder(0)
		,mDeviceStrings(0)
		,mMaxSources(100)
		,mResourceGroupName("")
		,mGlobalPitch(1.f)
		,mSoundsToDestroy(0)
		,mFadeVolume(false)
		,mFadeIn(false)
		,mFadeTime(0.f)
		,mFadeTimer(0.f)
#if OGGSOUND_THREADED
		,mActionsList(0)
		,mForceMutex(false)
#endif
		{
#if HAVE_EFX
			// Effect objects
			alGenEffects = NULL;
			alDeleteEffects = NULL;
			alIsEffect = NULL;
			alEffecti = NULL;
			alEffectiv = NULL;
			alEffectf = NULL;
			alEffectfv = NULL;
			alGetEffecti = NULL;
			alGetEffectiv = NULL;
			alGetEffectf = NULL;
			alGetEffectfv = NULL;

			//Filter objects
			alGenFilters = NULL;
			alDeleteFilters = NULL;
			alIsFilter = NULL;
			alFilteri = NULL;
			alFilteriv = NULL;
			alFilterf = NULL;
			alFilterfv = NULL;
			alGetFilteri = NULL;
			alGetFilteriv = NULL;
			alGetFilterf = NULL;
			alGetFilterfv = NULL;

			// Auxiliary slot object
			alGenAuxiliaryEffectSlots = NULL;
			alDeleteAuxiliaryEffectSlots = NULL;
			alIsAuxiliaryEffectSlot = NULL;
			alAuxiliaryEffectSloti = NULL;
			alAuxiliaryEffectSlotiv = NULL;
			alAuxiliaryEffectSlotf = NULL;
			alAuxiliaryEffectSlotfv = NULL;
			alGetAuxiliaryEffectSloti = NULL;
			alGetAuxiliaryEffectSlotiv = NULL;
			alGetAuxiliaryEffectSlotf = NULL;
			alGetAuxiliaryEffectSlotfv = NULL;

			mNumEffectSlots = 0;
			mNumSendsPerSource = 0;
#endif
		}
	/*/////////////////////////////////////////////////////////////////*/
	OgreOggSoundManager::~OgreOggSoundManager()
	{
#if OGGSOUND_THREADED
		mShuttingDown = true;
		if ( mUpdateThread )
		{
			mUpdateThread->join();
			OGRE_FREE(mUpdateThread, Ogre::MEMCATEGORY_GENERAL);
			mUpdateThread = 0;
			mShuttingDown=false;
#ifdef POCO_THREAD
			OGRE_FREE(mUpdater, Ogre::MEMCATEGORY_GENERAL);
			mUpdater = 0;
#endif
		}
		if ( mActionsList )
		{
			if ( !mActionsList->empty() )
			{
				SoundAction obj;
				// Clear out action list
				while (mActionsList->pop(obj))
				{
					// If parameters specified delete structure
					if (obj.mParams)
					{
						switch ( obj.mAction )
						{			
						case LQ_LOAD:
							{
								cSound* params = static_cast<cSound*>(obj.mParams);
								params->mStream.setNull();	
								OGRE_DELETE_T(params, cSound, Ogre::MEMCATEGORY_GENERAL);
							}
							break;	   
						case LQ_ATTACH_EFX:
						case LQ_DETACH_EFX:
						case LQ_SET_EFX_PROPERTY:
							{
								OGRE_DELETE_T(static_cast<efxProperty*>(obj.mParams), efxProperty, Ogre::MEMCATEGORY_GENERAL);
							}
							break;	 
						default:
							{
								OGRE_FREE(obj.mParams, Ogre::MEMCATEGORY_GENERAL);
							}
							break;
						}
					}
				}
			}
			delete mActionsList;
			mActionsList=0;
		}
#endif
		if ( mSoundsToDestroy )
		{
			delete mSoundsToDestroy;
			mSoundsToDestroy=0;
		}

		_releaseAll();

		if ( mRecorder ) { OGRE_DELETE_T(mRecorder, OgreOggSoundRecord, Ogre::MEMCATEGORY_GENERAL); mRecorder=0; }

		alcMakeContextCurrent(0);
		alcDestroyContext(mContext);
		mContext=0;
		alcCloseDevice(mDevice);
		mDevice=0;

		if ( mListener )
		{
			Ogre::SceneManager* s = mListener->getSceneManager();
			s->destroyAllMovableObjectsByType("OgreOggISound");
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	OgreOggSoundManager* OgreOggSoundManager::getSingletonPtr(void)
	{
#if OGRE_VERSION_MAJOR == 1 && OGRE_VERSION_MINOR <= 7    
		return ms_Singleton;
#else
		return msSingleton;
#endif	
	}
	/*/////////////////////////////////////////////////////////////////*/
	OgreOggSoundManager& OgreOggSoundManager::getSingleton(void)
	{
#if OGRE_VERSION_MAJOR == 1 && OGRE_VERSION_MINOR <= 7    
		if ( !ms_Singleton ) 
#else
		if ( !msSingleton )
#endif
			OGRE_EXCEPT( Ogre::Exception::ERR_ITEM_NOT_FOUND, "'OgreOggSound[_d]' plugin NOT loaded! - use loadPlugin()", "OgreOggSoundManager::getSingleton()");  
#if OGRE_VERSION_MAJOR == 1 && OGRE_VERSION_MINOR <= 7    
		return ( *ms_Singleton );
#else
		return ( *msSingleton );
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::init(	const std::string &deviceName, 
									unsigned int maxSources, 
									unsigned int queueListSize, 
									SceneManager* scnMgr)
	{
		if (mDevice) return true;

		Ogre::LogManager::getSingleton().logMessage("*****************************************", Ogre::LML_NORMAL);
		Ogre::LogManager::getSingleton().logMessage("*** --- Initialising OgreOggSound --- ***", Ogre::LML_NORMAL);
		Ogre::LogManager::getSingleton().logMessage("*** ---     "+OGREOGGSOUND_VERSION_STRING+"    --- ***", Ogre::LML_NORMAL);
		Ogre::LogManager::getSingleton().logMessage("*****************************************", Ogre::LML_NORMAL);

		// Set source limit
		mMaxSources = maxSources;

		// Get an internal list of audio device strings
		_enumDevices();

		int majorVersion;
		int minorVersion;
#if OGRE_PLATFORM != OGRE_PLATFORM_WIN32
		ALCdevice* device = alcOpenDevice(NULL);
#endif
		// Version Info
#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
        ALenum error = 0;
		alcGetError(NULL);
	    alcGetIntegerv(NULL, ALC_MINOR_VERSION, sizeof(minorVersion), &minorVersion);
        if ((error = alcGetError(NULL))!=AL_NO_ERROR)
		{
			switch (error)
			{
			case AL_INVALID_NAME:		{ LogManager::getSingleton().logMessage("Invalid Name when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }		break;
			case AL_INVALID_ENUM:		{ LogManager::getSingleton().logMessage("Invalid Enum when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }		break;
			case AL_INVALID_VALUE:		{ LogManager::getSingleton().logMessage("Invalid Value when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL);}		break;
			case AL_INVALID_OPERATION:	{ LogManager::getSingleton().logMessage("Invalid Operation when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }break;
			case AL_OUT_OF_MEMORY:		{ LogManager::getSingleton().logMessage("Out of memory when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }	break;
			}
			LogManager::getSingleton().logMessage("Unable to get OpenAL Minor Version number", Ogre::LML_CRITICAL);
			return false;
		}
		alcGetError(NULL);
		alcGetIntegerv(NULL, ALC_MAJOR_VERSION, sizeof(majorVersion), &majorVersion);
        if ((error = alcGetError(NULL))!=AL_NO_ERROR)
		{
			switch (error)
			{
			case AL_INVALID_NAME:		{ LogManager::getSingleton().logMessage("Invalid Name when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }		break;
			case AL_INVALID_ENUM:		{ LogManager::getSingleton().logMessage("Invalid Enum when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }		break;
			case AL_INVALID_VALUE:		{ LogManager::getSingleton().logMessage("Invalid Value when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL);}		break;
			case AL_INVALID_OPERATION:	{ LogManager::getSingleton().logMessage("Invalid Operation when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }break;
			case AL_OUT_OF_MEMORY:		{ LogManager::getSingleton().logMessage("Out of memory when attempting: OpenAL Minor Version number", Ogre::LML_CRITICAL); }	break;
			}
			LogManager::getSingleton().logMessage("Unable to get OpenAL Major Version number", Ogre::LML_CRITICAL);
			return false;
		}
#else
        alcGetIntegerv(device, ALC_MINOR_VERSION, sizeof(minorVersion), &minorVersion);
        ALCenum error = alcGetError(device);
        if (error != ALC_NO_ERROR)
		{
			LogManager::getSingleton().logMessage("Unable to get OpenAL Minor Version number", Ogre::LML_CRITICAL);
			return false;
		}
		alcGetIntegerv(device, ALC_MAJOR_VERSION, sizeof(majorVersion), &majorVersion);
		error = alcGetError(device);
        if (error != ALC_NO_ERROR)
		{
			LogManager::getSingleton().logMessage("Unable to get OpenAL Major Version number", Ogre::LML_CRITICAL);
			return false;
		}
		alcCloseDevice(device);
#endif
		Ogre::String msg="*** --- OpenAL version " + Ogre::StringConverter::toString(majorVersion) + "." + Ogre::StringConverter::toString(minorVersion);
		Ogre::LogManager::getSingleton().logMessage(msg, Ogre::LML_NORMAL);

		/*
		** OpenAL versions prior to 1.0 DO NOT support device enumeration, so we
		** need to test the current version and decide if we should try to find
		** an appropriate device or if we should just open the default device.
		*/
		bool deviceInList = false;
		if(majorVersion >= 1 && minorVersion >= 1)
		{
			Ogre::LogManager::getSingleton().logMessage("*** --- AVAILABLE DEVICES --- ***");

			// List devices in log and see if the sugested device is in the list
			Ogre::StringVector deviceList = getDeviceList();
			std::stringstream ss;
			Ogre::StringVector::iterator deviceItr;
			for(deviceItr = deviceList.begin(); deviceItr != deviceList.end() && (*deviceItr).compare("") != 0; ++deviceItr)
			{
				deviceInList |= (*deviceItr).compare(deviceName) == 0;
				ss << "*** --- " << (*deviceItr);
				Ogre::LogManager::getSingleton().logMessage(ss.str());
				ss.clear(); ss.str("");
			}
		}

		// If the suggested device is in the list we use it, otherwise select the default device
		mDevice = alcOpenDevice(deviceInList ? deviceName.c_str() : NULL);
		if (!mDevice)
		{
			Ogre::LogManager::getSingletonPtr()->logMessage("OgreOggSoundManager::init() ERROR - Unable to open audio device", Ogre::LML_CRITICAL);
			return false;
		}
		
		if (!deviceInList)
			Ogre::LogManager::getSingleton().logMessage("*** --- Choosing: " + Ogre::String(alcGetString(mDevice, ALC_DEVICE_SPECIFIER))+" (Default device)");
		else
			Ogre::LogManager::getSingleton().logMessage("*** --- Choosing: " + Ogre::String(alcGetString(mDevice, ALC_DEVICE_SPECIFIER)));

		Ogre::LogManager::getSingleton().logMessage("*** --- OpenAL Device successfully created");

#if HAVE_EFX
        ALint attribs[2] = {ALC_MAX_AUXILIARY_SENDS, 4};
#else
        ALint attribs[1] = {4};
#endif

		mContext = alcCreateContext(mDevice, attribs);
		if (!mContext)
		{
			Ogre::LogManager::getSingletonPtr()->logMessage("OgreOggSoundManager::init() ERROR - Unable to create a context", Ogre::LML_CRITICAL);
			return false;
		}

		Ogre::LogManager::getSingleton().logMessage("*** --- OpenAL Context successfully created");

		if (!alcMakeContextCurrent(mContext))
		{
			Ogre::LogManager::getSingletonPtr()->logMessage("OgreOggSoundManager::init() ERROR - Unable to set context", Ogre::LML_CRITICAL);
			return false;
		}

		_checkFeatureSupport();

		// If no manager specified - grab first one 
		if ( !scnMgr )
		{
			Ogre::SceneManagerEnumerator::SceneManagerIterator it=Ogre::Root::getSingletonPtr()->getSceneManagerIterator();

			if ( it.hasMoreElements() ) 
				mSceneMgr = it.getNext(); 
			else
			{
				OGRE_EXCEPT(Exception::ERR_INTERNAL_ERROR, "No SceneManager's created - a valid SceneManager is required to create sounds", "OgreOggSoundManager::init()");
				return false;
			}
		}
		else
			mSceneMgr = scnMgr;

		if ( !createListener() ) 
		{
			OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Unable to create a listener object", "OgreOggSoundManager::init()");
			return false;
		}

		mNumSources = _createSourcePool();

		msg="*** --- Created " + Ogre::StringConverter::toString(mNumSources) + " sources for simultaneous sounds";
		Ogre::LogManager::getSingleton().logMessage(msg, Ogre::LML_NORMAL);

		mSoundsToDestroy = new LocklessQueue<OgreOggISound*>(100);
#if OGGSOUND_THREADED
		if (queueListSize)
		{
			mActionsList = new LocklessQueue<SoundAction>(queueListSize);
		}
#	ifdef POCO_THREAD
		mUpdateThread = OGRE_NEW_T(Poco::Thread, Ogre::MEMCATEGORY_GENERAL)();
		mUpdater = OGRE_NEW_T(Updater, Ogre::MEMCATEGORY_GENERAL)();
		mUpdateThread->start(*mUpdater);
		Ogre::LogManager::getSingleton().logMessage("*** --- Using POCO threads for streaming", Ogre::LML_NORMAL);
#	else
		mUpdateThread = OGRE_NEW_T(boost::thread, Ogre::MEMCATEGORY_GENERAL)(boost::function0<void>(&OgreOggSoundManager::threadUpdate, this));
		Ogre::LogManager::getSingleton().logMessage("*** --- Using BOOST threads for streaming", Ogre::LML_NORMAL);
#	endif	
#endif

#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32
		// Recording
		if (alcIsExtensionPresent(mDevice, "ALC_EXT_CAPTURE") == AL_FALSE)
			Ogre::LogManager::getSingleton().logMessage("*** --- Recording devices NOT detected!", Ogre::LML_NORMAL);
		else
		{
			LogManager::getSingleton().logMessage("*** --- Recording devices available:", Ogre::LML_NORMAL);
			OgreOggSoundRecord* r=0;
			if ( r=createRecorder() )
			{
				OgreOggSoundRecord::RecordDeviceList list=r->getCaptureDeviceList();
				for ( OgreOggSoundRecord::RecordDeviceList::iterator iter=list.begin(); iter!=list.end(); ++iter )
					Ogre::LogManager::getSingleton().logMessage("***--- '"+(*iter)+"'", Ogre::LML_NORMAL);
				OGRE_DELETE_T(r, OgreOggSoundRecord, Ogre::MEMCATEGORY_GENERAL);
			}
		}
#endif
		Ogre::LogManager::getSingleton().logMessage("*****************************************", Ogre::LML_NORMAL);
		Ogre::LogManager::getSingleton().logMessage("*** ---  OgreOggSound Initialised --- ***", Ogre::LML_NORMAL);
		Ogre::LogManager::getSingleton().logMessage("*****************************************", Ogre::LML_NORMAL);

		return true;
	}

	/*/////////////////////////////////////////////////////////////////*/
	const StringVector OgreOggSoundManager::getDeviceList() const
	{
		const ALCchar* deviceList = alcGetString(NULL, ALC_DEVICE_SPECIFIER);

		Ogre::StringVector deviceVector;
		/*
		** The list returned by the call to alcGetString has the names of the
		** devices seperated by NULL characters and the list is terminated by
		** two NULL characters, so we can cast the list into a string and it
		** will automatically stop at the first NULL that it sees, then we
		** can move the pointer ahead by the lenght of that string + 1 and we
		** will be at the begining of the next string.  Once we hit an empty
		** string we know that we've found the double NULL that terminates the
		** list and we can stop there.
		*/
		while(*deviceList != 0)
		{
			try
			{
				ALCdevice *device = alcOpenDevice(deviceList);
				if (alcGetError(device)) throw std::string("Unable to open device");

				if(device)
				{
					// Device seems to be valid
					ALCcontext *context = alcCreateContext(device, NULL);
					if (alcGetError(device)) throw std::string("Unable to create context");
					if(context)
					{
						// Context seems to be valid
						alcMakeContextCurrent(context);
						if(alcGetError(device)) throw std::string("Unable to make context current");
						deviceVector.push_back(alcGetString(device, ALC_DEVICE_SPECIFIER));
						alcMakeContextCurrent(NULL);
						if(alcGetError(device)) throw std::string("Unable to clear current context");
						alcDestroyContext(context);
						if(alcGetError(device)) throw std::string("Unable to destroy current context");
					}
					alcCloseDevice(device);
				}
			}
			catch(...)
			{
				// Don't die here, we'll just skip this device.
			}

			deviceList += strlen(deviceList) + 1;
		}

		return deviceVector;
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::createListener() 
	{
		if ( mListener ) return true;

		// Create a listener
		return ( (mListener = dynamic_cast<OgreOggListener*>(mSceneMgr->createMovableObject("OgreOggSoundListener", OgreOggSoundFactory::FACTORY_TYPE_NAME, 0)))!=0);
	}
	/*/////////////////////////////////////////////////////////////////*/
	const StringVector OgreOggSoundManager::getSoundList() const
	{
		StringVector list;
		for ( SoundMap::const_iterator iter=mSoundMap.begin(); iter!=mSoundMap.end(); ++iter )
			list.push_back((*iter).first);
		return list;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setMasterVolume(ALfloat vol)
	{
		if ( (vol>=0.f) && (vol<=1.f) )
			alListenerf(AL_GAIN, vol);
	}		 
	/*/////////////////////////////////////////////////////////////////*/
	ALfloat OgreOggSoundManager::getMasterVolume()
	{
		ALfloat vol=0.0;
		alGetListenerf(AL_GAIN, &vol);
		return vol;
	}
	/*/////////////////////////////////////////////////////////////////*/
	OgreOggISound* OgreOggSoundManager::_createSoundImpl(	const SceneManager& scnMgr, 
															const std::string& name, 
															const std::string& file, 
															bool stream, 
															bool loop, 
															bool preBuffer,
															bool immediate)
	{
		Ogre::ResourceGroupManager* groupManager = 0;
		Ogre::String group;
		Ogre::DataStreamPtr soundData;
		OgreOggISound* sound = 0;

		try
		{
			if ( groupManager = Ogre::ResourceGroupManager::getSingletonPtr() )
			{
				if ( !mResourceGroupName.empty() )
				{
					soundData = groupManager->openResource(file, mResourceGroupName);
				}
				else
				{
					group = groupManager->findGroupContainingResource(file);
					soundData = groupManager->openResource(file, group);
				}
			}
			else
			{
				OGRE_EXCEPT(Exception::ERR_FILE_NOT_FOUND, "Unable to find Ogre::ResourceGroupManager", "OgreOggSoundManager::createSound()");
				return 0;
			}
		}
		catch (Ogre::Exception& e)
		{
			OGRE_EXCEPT(Exception::ERR_FILE_NOT_FOUND, e.getFullDescription(), "OgreOggSoundManager::_createSoundImpl()");
			return 0;
		}

		if		( file.find(".ogg")!=file.npos || file.find(".OGG")!=file.npos )
		{
			// MUST be unique
			if ( hasSound(name) )
			{
				Ogre::String msg="*** OgreOggSoundManager::createSound() - Sound with name: "+name+" already exists!";
				Ogre::LogManager::getSingleton().logMessage(msg);
				return 0;
			}

			if(stream)
				sound = OGRE_NEW_T(OgreOggStreamSound, Ogre::MEMCATEGORY_GENERAL)(name, scnMgr);
			else
				sound = OGRE_NEW_T(OgreOggStaticSound, Ogre::MEMCATEGORY_GENERAL)(name, scnMgr);

			// Set loop flag
			sound->loop(loop);

			// Add to list
			mSoundMap[name]=sound;

#if OGGSOUND_THREADED

			SoundAction action;
			cSound* c		= OGRE_NEW_T(cSound, Ogre::MEMCATEGORY_GENERAL);
			c->mFileName	= file;
			c->mPrebuffer	= preBuffer;
			c->mStream		= soundData;
			action.mAction	= LQ_LOAD;
			action.mParams	= c;
			action.mImmediately = immediate;
			action.mSound	= sound->getName();
			_requestSoundAction(action);
#else
			// load audio data
			_loadSoundImpl(sound, file, soundData, preBuffer);
#endif
			return sound;
		}
		else if	( file.find(".wav")!=file.npos || file.find(".WAV")!=file.npos )
		{
			// MUST be unique
			if ( hasSound(name) )
			{
				Ogre::String msg="*** OgreOggSoundManager::createSound() - Sound with name: "+name+" already exists!";
				Ogre::LogManager::getSingleton().logMessage(msg);
				return 0;
			}

			if(stream)
				sound = OGRE_NEW_T(OgreOggStreamWavSound, Ogre::MEMCATEGORY_GENERAL)(name, scnMgr);
			else
				sound = OGRE_NEW_T(OgreOggStaticWavSound, Ogre::MEMCATEGORY_GENERAL)(name, scnMgr);

			// Set loop flag
			sound->loop(loop);

			// Add to list
			mSoundMap[name]=sound;

#if OGGSOUND_THREADED
			SoundAction action;
			cSound* c		= OGRE_NEW_T(cSound, Ogre::MEMCATEGORY_GENERAL);
			c->mFileName	= file;
			c->mPrebuffer	= preBuffer; 
			c->mStream		= soundData;
			action.mAction	= LQ_LOAD;
			action.mParams	= c;
			action.mImmediately = immediate;
			action.mSound	= sound->getName();
			_requestSoundAction(action);
#else
			// Load audio file
			_loadSoundImpl(sound, file, soundData, preBuffer);
#endif
			return sound;
		}
		else
		{
			Ogre::String msg="*** OgreOggSoundManager::createSound() - Sound does not have (.ogg | .wav) extension: "+name;
			Ogre::LogManager::getSingleton().logMessage(msg);
			return 0;
		}
	}

	/*/////////////////////////////////////////////////////////////////*/
	OgreOggISound* OgreOggSoundManager::createSound(const std::string& name, 
													const std::string& file, 
													bool stream, 
													bool loop, 
													bool preBuffer, 
													SceneManager* scnMgr,
													bool immediate)
	{
		Ogre::NameValuePairList params;
		OgreOggISound* sound = 0;

		params["fileName"]	= file;
		params["stream"]	= stream	? "true" : "false";
		params["loop"]		= loop		? "true" : "false";
		params["preBuffer"]	= preBuffer ? "true" : "false";
		params["immediate"]	= immediate ? "true" : "false";

		// Get first SceneManager if defined
		if ( !scnMgr ) 
		{
			if ( mSceneMgr ) 
				scnMgr = mSceneMgr;
			else
			{
				OGRE_EXCEPT(Exception::ERR_ITEM_NOT_FOUND, "No SceneManager defined!", "OgreOggSoundManager::createSound()");
				return 0;
			}
		}

		// Catch exception when plugin hasn't been registered
		try
		{
			params["sceneManagerName"]=scnMgr->getName();
			sound = static_cast<OgreOggISound*>(scnMgr->createMovableObject( name, OgreOggSoundFactory::FACTORY_TYPE_NAME, &params ));
		}
		catch (Exception& e)
		{
			OGRE_EXCEPT(Exception::ERR_INTERNAL_ERROR, e.getFullDescription(), "OgreOggSoundManager::createSound()");
		}
		// create Movable Sound
		return sound;
	}

	/*/////////////////////////////////////////////////////////////////*/
	OgreOggListener* OgreOggSoundManager::_createListener()
	{
		OgreOggListener* l = OGRE_NEW_T(OgreOggListener, Ogre::MEMCATEGORY_GENERAL)();
		l->setSceneManager(*mSceneMgr);
		return l;
	}		
	/*/////////////////////////////////////////////////////////////////*/
	OgreOggISound* OgreOggSoundManager::getSound(const std::string& name)
	{
		SoundMap::iterator i = mSoundMap.find(name);
		if(i == mSoundMap.end()) return 0;
#if OGGSOUND_THREADED
		if ( !i->second->_isDestroying() )
			return i->second;
		else
			return 0;
#else
		return i->second;
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::hasSound(const std::string& name)
	{
		SoundMap::iterator i = mSoundMap.find(name);
		if(i == mSoundMap.end())
			return false;
#if OGGSOUND_THREADED
		if ( !i->second->_isDestroying() )
			return true;
		else
			return false;
#else
		return true;
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::destroyAllSounds()
	{
		_destroyAllSoundsImpl();
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::stopAllSounds()
	{
#if OGGSOUND_THREADED 
		SoundAction action;
		action.mAction	= LQ_STOP_ALL;
		action.mImmediately = false;
		action.mParams	= 0;
		action.mSound	= "";
		_requestSoundAction(action);
#else
		_stopAllSoundsImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setGlobalPitch(float pitch)
	{
		if ( pitch<=0.f ) return;

		mGlobalPitch=pitch;
#if OGGSOUND_THREADED 
		SoundAction action;
		action.mAction	= LQ_GLOBAL_PITCH;
		action.mParams	= 0;
		action.mImmediately = false;
		action.mSound	= "";
		_requestSoundAction(action);
#else
		_setGlobalPitchImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::pauseAllSounds()
	{
#if OGGSOUND_THREADED
		SoundAction action;
		action.mAction = LQ_PAUSE_ALL;
		action.mSound = "";
		action.mParams = 0;
		action.mImmediately = false;
		_requestSoundAction(action);
#else
		_pauseAllSoundsImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::fadeMasterVolume(float time, bool fadeIn)
	{
		mFadeVolume = true;
		mFadeTime = time;
		mFadeIn = fadeIn;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::resumeAllPausedSounds()
	{
#if OGGSOUND_THREADED
		SoundAction action;
		action.mAction = LQ_RESUME_ALL;
		action.mSound = "";
		action.mParams = 0;
		action.mImmediately = false;
		_requestSoundAction(action);
#else
		_resumeAllPausedSoundsImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::muteAllSounds()
	{
		alGetListenerf(AL_GAIN, &mOrigVolume);
		alListenerf(AL_GAIN, 0.f);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::unmuteAllSounds()
	{
		alListenerf(AL_GAIN, mOrigVolume);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::destroySound(const Ogre::String& sName)
	{
		OgreOggISound* sound=0;
		if ( !(sound = getSound(sName)) ) return;
		_destroySoundImpl(sound);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::destroySound(OgreOggISound* sound)
	{
		if ( !sound ) return;

		_destroySoundImpl(sound);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setDistanceModel(ALenum value)
	{
		alDistanceModel(value);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setSpeedOfSound(float speed)
	{
		alSpeedOfSound(speed);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setDopplerFactor(float factor)
	{
		alDopplerFactor(factor);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::update(float fTime)
	{
#if OGGSOUND_THREADED == 0
		static float rTime=0.f;
	
		if ( !mActiveSounds.empty() )
		{
			// Update ALL active sounds
			ActiveList::const_iterator i=mActiveSounds.begin(); 
			ActiveList::const_iterator end(mActiveSounds.end()); 
			while ( i!=end )
			{
				(*i)->update(fTime);
				(*i)->_updateAudioBuffers();
				// Update recorder
				if ( mRecorder ) mRecorder->_updateRecording();
				++i;
			}
		}

		// Update listener
		mListener->update();

		// Limit re-activation
		if ( (rTime+=fTime) > 0.05 )
		{
			// try to reactivate any
			_reactivateQueuedSounds();

			// Reset timer
			rTime=0.f;
		}

#endif
		// Fade volume
		if ( mFadeVolume )
		{
			mFadeTimer+=fTime;

			if ( mFadeTimer > mFadeTime )
			{
				mFadeVolume = false;
				setMasterVolume(mFadeIn ? 1.f : 0.f);
			}
			else
			{
				ALfloat vol = 1.f;
				if ( mFadeIn )
					vol = (mFadeTimer/mFadeTime);
				else
					vol = 1.f - (mFadeTimer/mFadeTime);

				setMasterVolume(vol);
			}
		}

		// Destroy sounds
		if ( mSoundsToDestroy )
		{
			if (!mSoundsToDestroy->empty() )
			{
				OgreOggISound* s=0;
				if ( mSoundsToDestroy->pop(s) )
					_destroySoundImpl(s);
			}
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	struct OgreOggSoundManager::_sortNearToFar
	{
		bool operator()(OgreOggISound*& sound1, OgreOggISound*& sound2)
		{
			float	d1=0.f,
					d2=0.f;
			Vector3	lPos=OgreOggSoundManager::getSingleton().getListener()->getPosition();

			if ( !sound1->isMono() ) return false;

			if ( sound1->isRelativeToListener() )
				d1 = sound1->getPosition().length();
			else
				d1 = sound1->getPosition().distance(lPos);

			if ( sound2->isRelativeToListener() )
				d2 = sound2->getPosition().length();
			else
				d2 = sound2->getPosition().distance(lPos);

			// Check sort order
			if ( !sound1->isMono() && sound2->isMono() ) return false;
			if ( d1<d2 )	return true;
			if ( d1>d2 )	return false;

			// Equal - don't sort
			return false;
		}
	};
	/*/////////////////////////////////////////////////////////////////*/
	struct OgreOggSoundManager::_sortFarToNear
	{
		bool operator()(OgreOggISound*& sound1, OgreOggISound*& sound2)
		{
			float	d1=0.f,
					d2=0.f;
			Vector3	lPos=OgreOggSoundManager::getSingleton().getListener()->getPosition();

			if ( !sound1->isMono() ) return false;

			if ( sound1->isRelativeToListener() )
				d1 = sound1->getPosition().length();
			else
				d1 = sound1->getPosition().distance(lPos);

			if ( sound2->isRelativeToListener() )
				d2 = sound2->getPosition().length();
			else
				d2 = sound2->getPosition().distance(lPos);

			// Check sort order
			if ( !sound1->isMono() && sound2->isMono() ) return true;
			if ( d1>d2 )	return true;
			if ( d1<d2 )	return false;

			// Equal - don't sort
			return false;
		}
	};
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_requestSoundSource(OgreOggISound* sound)
	{
		// Does sound need a source?
		if (!sound) return false;

		if (sound->getSource()!=AL_NONE) return true;
		
		ALuint src = AL_NONE;

		// If there are still sources available
		// Pop next available off list
		if ( !mSourcePool.empty() )
		{
			// Get next available source
			src = static_cast<ALuint>(mSourcePool.back());
			// Remove from available list
			mSourcePool.pop_back();
			// Set sounds source
			sound->setSource(src);
			// Remove from reactivate list if reactivating..
			if ( !mSoundsToReactivate.empty() )
			{
				ActiveList::iterator rIter=mSoundsToReactivate.begin(); 
				while ( rIter!=mSoundsToReactivate.end() )
				{
					if ( (*rIter)==sound )
						rIter = mSoundsToReactivate.erase(rIter);
					else
						++rIter;
				}
			}
			// Add new sound to active list
			mActiveSounds.push_back(sound);
			return true;
		}
		// All sources in use
		// Re-use an active source
		// Use either a non-playing source or a lower priority source
		else
		{
			// Get iterator for list
			ActiveList::iterator iter = mActiveSounds.begin();

			// Search for a stopped sound
			while ( iter!=mActiveSounds.end() )
			{
				// Find a stopped sound - reuse its source
				if ( (*iter)->isStopped() )
				{
					ALuint src = (*iter)->getSource();
					ALuint nullSrc = AL_NONE;
					// Remove source
					(*iter)->setSource(nullSrc);
					// Attach source to new sound
					sound->setSource(src);
					// Add new sound to active list
					mActiveSounds.erase(iter);
					// Add new sound to active list
					mActiveSounds.push_back(sound);
					// Return success
					return true;
				}
				else
					++iter;
			}

			// Check priority...
			Ogre::uint8 priority = sound->getPriority();
			iter = mActiveSounds.begin();

			// Search for a lower priority sound
			while ( iter!=mActiveSounds.end() )
			{
				// Find a stopped sound - reuse its source
				if ( (*iter)->getPriority()<sound->getPriority() )
				{
					ALuint src = (*iter)->getSource();
					ALuint nullSrc = AL_NONE;
					// Pause sounds
					(*iter)->pause();
					// Remove source
					(*iter)->setSource(nullSrc);
					// Attach source to new sound
					sound->setSource(src);
					// Add to reactivate list
					mSoundsToReactivate.push_back((*iter));
					// Remove relinquished sound from active list
					mActiveSounds.erase(iter);
					// Add new sound to active list
					mActiveSounds.push_back(sound);
					// Return success
					return true;
				}
				else
					++iter;
			}

			// Sort by distance
			float	d1 = 0.f,
					d2 = 0.f;

			// Sort list by distance
			mActiveSounds.sort(_sortFarToNear());

			// Lists should be sorted:	Active-->furthest to Nearest
			//							Reactivate-->Nearest to furthest
			OgreOggISound* snd1 = mActiveSounds.front();

			if ( snd1->isRelativeToListener() )
				d1 = snd1->getPosition().length();
			else
				d1 = snd1->getPosition().distance(mListener->getPosition());

			if ( sound->isRelativeToListener() )
				d1 = sound->getPosition().length();
			else
				d1 = sound->getPosition().distance(mListener->getPosition());

			// Needs swapping?
			if ( d1>d2 )
			{
				ALuint src = snd1->getSource();
				ALuint nullSrc = AL_NONE;
				// Pause sounds
				snd1->pause();
				snd1->_markPlayPosition();
				// Remove source
				snd1->setSource(nullSrc);
				// Attach source to new sound
				sound->setSource(src);
				sound->_recoverPlayPosition();
				// Add to reactivate list
				mSoundsToReactivate.push_back(snd1);
				// Remove relinquished sound from active list
				mActiveSounds.erase(mActiveSounds.begin());					 
				// Add new sound to active list
				mActiveSounds.push_back(sound);
				// Return success
				return true;
			}
		}

		// If no opportunity to grab a source add to queue
		if ( !mWaitingSounds.empty() )
		{
			// Check not already in list
			for ( ActiveList::iterator iter=mWaitingSounds.begin(); iter!=mWaitingSounds.end(); ++iter )
				if ( (*iter)==sound )
					return false;
		}

		// Add to list
		mWaitingSounds.push_back(sound);

		// Uh oh - won't be played
		return false;
	}	  
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_releaseSoundSource(OgreOggISound* sound)
	{
		if (!sound) return false;

		if (sound->getSource()==AL_NONE) return true;

		// Get source
		ALuint src = sound->getSource();

		// Valid source?
		if(src!=AL_NONE)
		{
			ALuint source=AL_NONE;

			// Detach source from sound
			sound->setSource(source);

			// Make source available
			mSourcePool.push_back(src);

			// Remove from actives list
			ActiveList::iterator iter=mActiveSounds.begin(); 
			while ( iter!=mActiveSounds.end() )
			{
				// Find sound in actives list
				if ( (*iter)==sound )
					iter = mActiveSounds.erase(iter);
				else
					++iter;
			}
			return true;
		}

		return false;
	}	 	
#if OGRE_PLATFORM == OGRE_PLATFORM_WIN32

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::isRecordingAvailable() const
	{
		if ( mRecorder ) return mRecorder->isCaptureAvailable();
		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	OgreOggSoundRecord* OgreOggSoundManager::createRecorder()
	{
		if ( mDevice )
			return (OGRE_NEW_T(OgreOggSoundRecord, Ogre::MEMCATEGORY_GENERAL)(*mDevice));
		else
			return 0;
	}

#endif																				  	
#if HAVE_EFX
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::isEffectSupported(ALint effectID)
	{
		if ( mEFXSupportList.find(effectID)!=mEFXSupportList.end() )
			return mEFXSupportList[effectID];
		else
			Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::isEffectSupported() - Invalid effectID!");

		return false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setXRamBuffer(ALsizei numBuffers, ALuint* buffer)
	{
		if ( buffer && mEAXSetBufferMode )
			mEAXSetBufferMode(numBuffers, buffer, mCurrentXRamMode);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setXRamBufferMode(ALenum mode)
	{
		mCurrentXRamMode = mXRamAuto;
		if		( mode==mXRamAuto ) mCurrentXRamMode = mXRamAuto;
		else if ( mode==mXRamHardware ) mCurrentXRamMode = mXRamHardware;
		else if ( mode==mXRamAccessible ) mCurrentXRamMode = mXRamAccessible;
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::setEFXEffectParameter(const std::string& eName, ALint effectType, ALenum attrib, ALfloat param)
	{
		if ( !hasEFXSupport() && eName.empty() ) return false;

		ALuint effect;

		// Get effect id's
		if ( (effect = _getEFXEffect(eName) ) != AL_EFFECT_NULL )
		{
			alGetError();
			alEffecti(effectType, attrib, static_cast<ALint>(param));
			if ( alGetError()!=AL_NO_ERROR )
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::setEFXEffectParameter() - Unable to change effect parameter!");
				return false;
			}
		}

		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::setEFXEffectParameter(const std::string& eName, ALint effectType, ALenum attrib, ALfloat* params)
	{
		if ( !hasEFXSupport() && eName.empty() || !params ) return false;

		ALuint effect;

		// Get effect id's
		if ( (effect = _getEFXEffect(eName) ) != AL_EFFECT_NULL )
		{
			alGetError();
			alEffectfv(effectType, attrib, params);
			if ( alGetError()!=AL_NO_ERROR )
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::setEFXEffectParameter() - Unable to change effect parameters!");
				return false;
			}
		}

		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::setEFXEffectParameter(const std::string& eName, ALint effectType, ALenum attrib, ALint param)
	{
		if ( !hasEFXSupport() && eName.empty() ) return false;

		ALuint effect;

		// Get effect id's
		if ( (effect = _getEFXEffect(eName) ) != AL_EFFECT_NULL )
		{
			alGetError();
			alEffecti(effectType, attrib, param);
			if ( alGetError()!=AL_NO_ERROR )
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::setEFXEffectParameter() - Unable to change effect parameter!");
				return false;
			}
		}

		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::setEFXEffectParameter(const std::string& eName, ALint effectType, ALenum attrib, ALint* params)
	{
		if ( !hasEFXSupport() && eName.empty() || !params ) return false;

		ALuint effect;

		// Get effect id's
		if ( (effect = _getEFXEffect(eName) ) != AL_EFFECT_NULL )
		{
			alGetError();
			alEffectiv(effectType, attrib, params);
			if ( alGetError()!=AL_NO_ERROR )
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::setEFXEffectParameter() - Unable to change effect parameters!");
				return false;
			}
		}

		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::attachEffectToSound(const std::string& sName, ALuint slotID, const Ogre::String& effectName, const Ogre::String& filterName)
	{
		OgreOggISound* sound=0;
		if ( !(sound = getSound(sName)) ) return false;

#if OGGSOUND_THREADED
		SoundAction action;
		efxProperty* e	= OGRE_NEW_T(efxProperty, Ogre::MEMCATEGORY_GENERAL);
		e->mEffectName	= effectName;
		e->mFilterName	= filterName;
		e->mSlotID		= slotID;
		action.mAction	= LQ_ATTACH_EFX;
		action.mParams	= e;
		action.mSound	= sound->getName();
		action.mImmediately = false;
		_requestSoundAction(action);
		return true;
#else
		// load audio data
		return _attachEffectToSoundImpl(sound, slotID, effectName, filterName);
#endif
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::attachFilterToSound(const std::string& sName, const Ogre::String& filterName)
	{
		OgreOggISound* sound=0;
		if ( !(sound = getSound(sName)) ) return false;

#if OGGSOUND_THREADED
		SoundAction action;
		efxProperty* e	= OGRE_NEW_T(efxProperty, Ogre::MEMCATEGORY_GENERAL);
		e->mEffectName	= "";
		e->mFilterName	= filterName;
		e->mSlotID		= 255;
		action.mAction	= LQ_ATTACH_EFX;
		action.mParams	= e;
		action.mSound	= sound->getName();
		action.mImmediately = false;
		_requestSoundAction(action);
		return true;
#else
		// load audio data
		return _attachFilterToSoundImpl(sound, filterName);
#endif
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_attachEffectToSoundImpl(OgreOggISound* sound, ALuint slotID, const Ogre::String& effectName, const Ogre::String& filterName)
	{
		if ( !hasEFXSupport() || !sound ) return false;

		ALuint effect;
		ALuint filter;
		ALuint slot;

		// Get effect id's
		slot	= _getEFXSlot(slotID);
		effect	= _getEFXEffect(effectName);
		filter	= _getEFXFilter(filterName);

		// Attach effect and filter to slot
		if ( _attachEffectToSlot(slot, effect) )
		{
			ALuint src = sound->getSource();
			if ( src!=AL_NONE )
			{
				alSource3i(src, AL_AUXILIARY_SEND_FILTER, effect, slotID, filter);
				if (alGetError() == AL_NO_ERROR)
				{
					return true;
				}
				else
				{
					Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::attachEffectToSound() - Unable to attach effect to source!");
					return false;
				}
			}
			else
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::attachEffectToSound() - sound has no source!");
				return false;
			}
		}
		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_attachFilterToSoundImpl(OgreOggISound* sound, const Ogre::String& filterName)
	{
		if ( !hasEFXSupport() || !sound ) return false;

		ALuint filter = _getEFXFilter(filterName);

		if ( filter!=AL_FILTER_NULL )
		{
			ALuint src = sound->getSource();
			if ( src!=AL_NONE )
			{
				alSourcei(src, AL_DIRECT_FILTER, filter);
				if (alGetError() == AL_NO_ERROR)
				{
					return true;
				}
				else
				{
					Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::attachFilterToSound() - Unable to attach filter to source!");
					return false;
				}
			}
			else
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::attachFilterToSound() - sound has no source!");
				return false;
			}
		}
		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_detachEffectFromSoundImpl(OgreOggISound* sound, ALuint slotID)
	{
		if ( !hasEFXSupport() || !sound ) return false;

		ALuint slot;

		// Get slot
		slot = _getEFXSlot(slotID);

		// Detach effect from sound
		if ( slot!=AL_NONE )
		{
			ALuint src = sound->getSource();
			if ( src!=AL_NONE )
			{
				alSource3i(src, AL_AUXILIARY_SEND_FILTER, AL_EFFECT_NULL, slot, AL_FILTER_NULL);
				if (alGetError() == AL_NO_ERROR)
				{
					return true;
				}
				else
				{
					Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::detachEffectFromSound() - Unable to detach effect from source!");
					return false;
				}
			}
			else
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::detachEffectFromSound() - sound has no source!");
				return false;
			}
		}
		return false;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_detachFilterFromSoundImpl(OgreOggISound* sound)
	{
		if ( !hasEFXSupport() || !sound ) return false;

		ALuint src = sound->getSource();
		if ( src!=AL_NONE )
		{
			alSourcei(src, AL_DIRECT_FILTER, AL_FILTER_NULL);
			if (alGetError() != AL_NO_ERROR)
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::dettachFilterToSound() - Unable to detach filter from source!");
				return false;
			}
		}
		else
		{
			Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::detachFilterFromSound() - sound has no source!");
			return false;
		}

		return true;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::detachEffectFromSound(const std::string& sName, ALuint slotID)
	{
		OgreOggISound* sound=0;
		if ( !(sound = getSound(sName)) ) return false;

#if OGGSOUND_THREADED
		SoundAction action;
		efxProperty* e	= OGRE_NEW_T(efxProperty, Ogre::MEMCATEGORY_GENERAL);
		e->mEffectName	= "";
		e->mFilterName	= "";
		e->mSlotID		= slotID;
		e->mAirAbsorption= 0.f;
		e->mRolloff		= 0.f;
		e->mConeHF		= 0.f;
		action.mAction	= LQ_DETACH_EFX;
		action.mParams	= e;
		action.mSound	= sound->getName();
		action.mImmediately = false;
		_requestSoundAction(action);
		return true;
#else
		// load audio data
		return _detachEffectFromSoundImpl(sound, slotID);
#endif
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::detachFilterFromSound(const std::string& sName)
	{
		OgreOggISound* sound=0;
		if ( !(sound = getSound(sName)) ) return false;

#if OGGSOUND_THREADED
		SoundAction action;
		efxProperty* e	= OGRE_NEW_T(efxProperty, Ogre::MEMCATEGORY_GENERAL);
		e->mEffectName	= "";
		e->mFilterName	= "";
		e->mSlotID		= 255;
		e->mAirAbsorption= 0.f;
		e->mRolloff		= 0.f;
		e->mConeHF		= 0.f;
		action.mAction	= LQ_DETACH_EFX;
		action.mParams	= e;
		action.mSound	= sound->getName();
		action.mImmediately = false;
		_requestSoundAction(action);
		return true;
#else
		// load audio data
		return _detachFilterFromSoundImpl(sound);
#endif
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::createEFXFilter(const std::string& fName, ALint filterType, ALfloat gain, ALfloat hfGain)
	{
		if ( !hasEFXSupport() || fName.empty() || !isEffectSupported(filterType) )
		{
			Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::createEFXFilter() - Unsupported filter!");
			return false;
		}

		ALuint filter;

		alGenFilters(1, &filter);
		if (alGetError() != AL_NO_ERROR)
		{
			Ogre::LogManager::getSingleton().logMessage("*** Cannot create EFX Filter!");
			return false;
		}

		if (alIsFilter(filter) && ((filterType==AL_FILTER_LOWPASS) || (filterType==AL_FILTER_HIGHPASS) || (filterType==AL_FILTER_BANDPASS) ))
		{
			alFilteri(filter, AL_FILTER_TYPE, filterType);
			if (alGetError() != AL_NO_ERROR)
			{
				Ogre::LogManager::getSingleton().logMessage("*** Filter not supported!");
				return false;
			}
			else
			{
				// Set properties
				alFilterf(filter, AL_LOWPASS_GAIN, gain);
				alFilterf(filter, AL_LOWPASS_GAINHF, hfGain);
				mFilterList[fName]=filter;
			}
		}
		return true;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::createEFXEffect(const std::string& eName, ALint effectType, EAXREVERBPROPERTIES* props)
	{
		if ( !hasEFXSupport() || eName.empty() || !isEffectSupported(effectType) )
		{
			Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::createEFXEffect() - Unsupported effect!");
			return false;
		}

		ALuint effect;

		alGenEffects(1, &effect);
		if (alGetError() != AL_NO_ERROR)
		{
			Ogre::LogManager::getSingleton().logMessage("*** Cannot create EFX effect!");
			return false;
		}

		if (alIsEffect(effect))
		{
			alEffecti(effect, AL_EFFECT_TYPE, effectType);
			if (alGetError() != AL_NO_ERROR)
			{
				Ogre::LogManager::getSingleton().logMessage("*** Effect not supported!");
				return false;
			}
			else
			{
				// Apply some preset reverb properties
				if ( effectType==AL_EFFECT_EAXREVERB  && props )
				{
					EFXEAXREVERBPROPERTIES eaxProps;
					ConvertReverbParameters(props, &eaxProps);
					_setEAXReverbProperties(&eaxProps, effect);
				}

				// Add to list
				mEffectList[eName]=effect;
			}
		}
		return true;
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::createEFXSlot()
	{
		if ( !hasEFXSupport() )
		{
			Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::createEFXFilter() - No EFX support!");
			return false;
		}

		ALuint slot;

		alGenAuxiliaryEffectSlots(1, &slot);
		if (alGetError() != AL_NO_ERROR)
		{
			Ogre::LogManager::getSingleton().logMessage("*** Cannot create Auxiliary effect slot!");
			return false;
		}
		else
		{
			mEffectSlotList.push_back(slot);
		}

		return true;
	}

	/*/////////////////////////////////////////////////////////////////*/
	int OgreOggSoundManager::getNumberOfSupportedEffectSlots()
	{
		if ( !hasEFXSupport() ) return 0;

		ALint auxSends=0;
		alcGetIntegerv(mDevice, ALC_MAX_AUXILIARY_SENDS, 1, &auxSends);

		return auxSends;
	}

	/*/////////////////////////////////////////////////////////////////*/
	int OgreOggSoundManager::getNumberOfCreatedEffectSlots()
	{
		if ( mEffectSlotList.empty() ) return 0;

		return static_cast<int>(mEffectSlotList.size());
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_setEFXSoundPropertiesImpl(OgreOggISound* sound, float airAbsorption, float roomRolloff, float coneOuterHF)
	{
		if (!sound) return false;

		ALuint src = sound->getSource();

		if ( src!=AL_NONE )
		{
			alGetError();

			alSourcef(src, AL_AIR_ABSORPTION_FACTOR, airAbsorption);
			alSourcef(src, AL_ROOM_ROLLOFF_FACTOR, roomRolloff);
			alSourcef(src, AL_CONE_OUTER_GAINHF, coneOuterHF);

			if ( alGetError()==AL_NO_ERROR )
			{
				return true;
			}
			else
			{
				Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::setEFXSoundProperties() - Unable to set EFX sound properties!");
				return false;
			}
		}
		else
		{
			Ogre::LogManager::getSingleton().logMessage("*** OgreOggSoundManager::setEFXSoundProperties() - No source attached to sound!");
			return false;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::setEFXSoundProperties(const std::string& sName, float airAbsorption, float roomRolloff, float coneOuterHF)
	{
		OgreOggISound* sound=0;
		if ( !(sound = getSound(sName)) ) return false;

#if OGGSOUND_THREADED
		SoundAction action;
		efxProperty* e	= OGRE_NEW_T(efxProperty, Ogre::MEMCATEGORY_GENERAL);
		e->mEffectName	= "";
		e->mFilterName	= "";
		e->mSlotID		= 255;
		e->mAirAbsorption= airAbsorption;
		e->mRolloff		= roomRolloff;
		e->mConeHF		= coneOuterHF;
		action.mAction	= LQ_SET_EFX_PROPERTY;
		action.mParams	= e;
		action.mSound	= sound->getName();
		action.mImmediately = false;
		_requestSoundAction(action);
		return true;
#else
		// load audio data
		return _setEFXSoundPropertiesImpl(sound);
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::setEFXDistanceUnits(float units)
	{
		if ( !hasEFXSupport() || units<=0 ) return;

		alListenerf(AL_METERS_PER_UNIT, units);
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_setEAXReverbProperties(EFXEAXREVERBPROPERTIES *pEFXEAXReverb, ALuint uiEffect)
	{
		if (pEFXEAXReverb)
		{
			// Clear AL Error code
			alGetError();

			// Determine type of 'Reverb' effect and apply correct settings
			ALint type;
			alGetEffecti(uiEffect, AL_EFFECT_TYPE, &type);

			// Apply selected presets to normal reverb
			if ( type==AL_EFFECT_REVERB )
			{
				alEffectf(uiEffect, AL_REVERB_DENSITY, pEFXEAXReverb->flDensity);
				alEffectf(uiEffect, AL_REVERB_DIFFUSION, pEFXEAXReverb->flDiffusion);
				alEffectf(uiEffect, AL_REVERB_GAIN, pEFXEAXReverb->flGain);
				alEffectf(uiEffect, AL_REVERB_GAINHF, pEFXEAXReverb->flGainHF);
				alEffectf(uiEffect, AL_REVERB_DECAY_TIME, pEFXEAXReverb->flDecayTime);
				alEffectf(uiEffect, AL_REVERB_DECAY_HFRATIO, pEFXEAXReverb->flDecayHFRatio);
				alEffectf(uiEffect, AL_REVERB_REFLECTIONS_GAIN, pEFXEAXReverb->flReflectionsGain);
				alEffectf(uiEffect, AL_REVERB_REFLECTIONS_DELAY, pEFXEAXReverb->flReflectionsDelay);
				alEffectf(uiEffect, AL_REVERB_LATE_REVERB_GAIN, pEFXEAXReverb->flLateReverbGain);
				alEffectf(uiEffect, AL_REVERB_LATE_REVERB_DELAY, pEFXEAXReverb->flLateReverbDelay);
				alEffectf(uiEffect, AL_REVERB_AIR_ABSORPTION_GAINHF, pEFXEAXReverb->flAirAbsorptionGainHF);
				alEffectf(uiEffect, AL_REVERB_ROOM_ROLLOFF_FACTOR, pEFXEAXReverb->flRoomRolloffFactor);
				alEffecti(uiEffect, AL_REVERB_DECAY_HFLIMIT, pEFXEAXReverb->iDecayHFLimit);
			}
			// Apply full EAX reverb settings
			else
			{
				alEffectf(uiEffect, AL_EAXREVERB_DENSITY, pEFXEAXReverb->flDensity);
				alEffectf(uiEffect, AL_EAXREVERB_DIFFUSION, pEFXEAXReverb->flDiffusion);
				alEffectf(uiEffect, AL_EAXREVERB_GAIN, pEFXEAXReverb->flGain);
				alEffectf(uiEffect, AL_EAXREVERB_GAINHF, pEFXEAXReverb->flGainHF);
				alEffectf(uiEffect, AL_EAXREVERB_GAINLF, pEFXEAXReverb->flGainLF);
				alEffectf(uiEffect, AL_EAXREVERB_DECAY_TIME, pEFXEAXReverb->flDecayTime);
				alEffectf(uiEffect, AL_EAXREVERB_DECAY_HFRATIO, pEFXEAXReverb->flDecayHFRatio);
				alEffectf(uiEffect, AL_EAXREVERB_DECAY_LFRATIO, pEFXEAXReverb->flDecayLFRatio);
				alEffectf(uiEffect, AL_EAXREVERB_REFLECTIONS_GAIN, pEFXEAXReverb->flReflectionsGain);
				alEffectf(uiEffect, AL_EAXREVERB_REFLECTIONS_DELAY, pEFXEAXReverb->flReflectionsDelay);
				alEffectfv(uiEffect, AL_EAXREVERB_REFLECTIONS_PAN, pEFXEAXReverb->flReflectionsPan);
				alEffectf(uiEffect, AL_EAXREVERB_LATE_REVERB_GAIN, pEFXEAXReverb->flLateReverbGain);
				alEffectf(uiEffect, AL_EAXREVERB_LATE_REVERB_DELAY, pEFXEAXReverb->flLateReverbDelay);
				alEffectfv(uiEffect, AL_EAXREVERB_LATE_REVERB_PAN, pEFXEAXReverb->flLateReverbPan);
				alEffectf(uiEffect, AL_EAXREVERB_ECHO_TIME, pEFXEAXReverb->flEchoTime);
				alEffectf(uiEffect, AL_EAXREVERB_ECHO_DEPTH, pEFXEAXReverb->flEchoDepth);
				alEffectf(uiEffect, AL_EAXREVERB_MODULATION_TIME, pEFXEAXReverb->flModulationTime);
				alEffectf(uiEffect, AL_EAXREVERB_MODULATION_DEPTH, pEFXEAXReverb->flModulationDepth);
				alEffectf(uiEffect, AL_EAXREVERB_AIR_ABSORPTION_GAINHF, pEFXEAXReverb->flAirAbsorptionGainHF);
				alEffectf(uiEffect, AL_EAXREVERB_HFREFERENCE, pEFXEAXReverb->flHFReference);
				alEffectf(uiEffect, AL_EAXREVERB_LFREFERENCE, pEFXEAXReverb->flLFReference);
				alEffectf(uiEffect, AL_EAXREVERB_ROOM_ROLLOFF_FACTOR, pEFXEAXReverb->flRoomRolloffFactor);
				alEffecti(uiEffect, AL_EAXREVERB_DECAY_HFLIMIT, pEFXEAXReverb->iDecayHFLimit);
			}
			if (alGetError() == AL_NO_ERROR)
				return true;
		}

		return false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_attachEffectToSlot(ALuint slot, ALuint effect)
	{
		if ( !hasEFXSupport() || slot==AL_NONE ) return false;

		/* Attach Effect to Auxiliary Effect Slot */
		/* slot is the ID of an Aux Effect Slot */
		/* effect is the ID of an Effect */
		alAuxiliaryEffectSloti(slot, AL_EFFECTSLOT_EFFECT, effect);
		if (alGetError() != AL_NO_ERROR)
		{
			Ogre::LogManager::getSingleton().logMessage("*** Cannot attach effect to slot!");
			return false;
		}
		return true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	ALuint OgreOggSoundManager::_getEFXFilter(const std::string& fName)
	{
		if ( mFilterList.empty() || !hasEFXSupport() || fName.empty() ) return AL_FILTER_NULL;

		EffectList::iterator filter=mFilterList.find(fName);
		if ( filter==mFilterList.end() )
			return AL_FILTER_NULL;
		else
			return filter->second;
	}

	/*/////////////////////////////////////////////////////////////////*/
	ALuint OgreOggSoundManager::_getEFXEffect(const std::string& eName)
	{
		if ( mEffectList.empty() || !hasEFXSupport() || eName.empty() ) return AL_EFFECT_NULL;

		EffectList::iterator effect=mEffectList.find(eName);
		if ( effect==mEffectList.end() )
			return AL_EFFECT_NULL;
		else
			return effect->second;
	}

	/*/////////////////////////////////////////////////////////////////*/
	ALuint OgreOggSoundManager::_getEFXSlot(int slotID)
	{
		if ( mEffectSlotList.empty() || !hasEFXSupport() || (slotID>=static_cast<int>(mEffectSlotList.size())) ) return AL_NONE;

		return static_cast<ALuint>(mEffectSlotList[slotID]);
	}

	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_determineAuxEffectSlots()
	{
		ALuint		uiEffectSlots[128] = { 0 };
		ALuint		uiEffects[1] = { 0 };
		ALuint		uiFilters[1] = { 0 };
		Ogre::String msg="";

		// To determine how many Auxiliary Effects Slots are available, create as many as possible (up to 128)
		// until the call fails.
		for (mNumEffectSlots = 0; mNumEffectSlots < 128; mNumEffectSlots++)
		{
			alGenAuxiliaryEffectSlots(1, &uiEffectSlots[mNumEffectSlots]);
			if (alGetError() != AL_NO_ERROR)
				break;
		}

		msg="*** --- "+Ogre::StringConverter::toString(mNumEffectSlots)+ " Auxiliary Effect Slot(s)";
		Ogre::LogManager::getSingleton().logMessage(msg);

		// Retrieve the number of Auxiliary Effect Slots Sends available on each Source
		alcGetIntegerv(mDevice, ALC_MAX_AUXILIARY_SENDS, 1, &mNumSendsPerSource);
		msg="*** --- "+Ogre::StringConverter::toString(mNumSendsPerSource)+" Auxiliary Send(s) per Source";
		Ogre::LogManager::getSingleton().logMessage(msg);

		Ogre::LogManager::getSingleton().logMessage("*** --- EFFECTS SUPPORTED:");
		alGenEffects(1, &uiEffects[0]);
		if (alGetError() == AL_NO_ERROR)
		{
			// Try setting Effect Type to known Effects
			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_REVERB);
			if ( mEFXSupportList[AL_EFFECT_REVERB] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Reverb' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Reverb' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_EAXREVERB);
			if ( mEFXSupportList[AL_EFFECT_EAXREVERB] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'EAX Reverb' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'EAX Reverb' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_CHORUS);
			if ( mEFXSupportList[AL_EFFECT_CHORUS] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Chorus' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Chorus' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_DISTORTION);
			if ( mEFXSupportList[AL_EFFECT_DISTORTION] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Distortion' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Distortion' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_ECHO);
			if ( mEFXSupportList[AL_EFFECT_ECHO] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Echo' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Echo' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_FLANGER);
			if ( mEFXSupportList[AL_EFFECT_FLANGER] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Flanger' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Flanger' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_FREQUENCY_SHIFTER);
			if ( mEFXSupportList[AL_EFFECT_FREQUENCY_SHIFTER] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Frequency shifter' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Frequency shifter' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_VOCAL_MORPHER);
			if ( mEFXSupportList[AL_EFFECT_VOCAL_MORPHER] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Vocal Morpher' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Vocal Morpher' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_PITCH_SHIFTER);
			if ( mEFXSupportList[AL_EFFECT_PITCH_SHIFTER] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Pitch shifter' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Pitch shifter' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_RING_MODULATOR);
			if ( mEFXSupportList[AL_EFFECT_RING_MODULATOR] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Ring modulator' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Ring modulator' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_AUTOWAH);
			if ( mEFXSupportList[AL_EFFECT_AUTOWAH] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Autowah' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Autowah' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_COMPRESSOR);
			if ( mEFXSupportList[AL_EFFECT_COMPRESSOR] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Compressor' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Compressor' Support: NO");

			alEffecti(uiEffects[0], AL_EFFECT_TYPE, AL_EFFECT_EQUALIZER);
			if ( mEFXSupportList[AL_EFFECT_EQUALIZER] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Equalizer' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Equalizer' Support: NO");
		}


		// To determine which Filters are supported, generate a Filter Object, and try to set its type to
		// the various Filter enum values
		Ogre::LogManager::getSingleton().logMessage("*** --- FILTERS SUPPORTED: ");

		// Generate a Filter to use to determine what Filter Types are supported
		alGenFilters(1, &uiFilters[0]);
		if (alGetError() == AL_NO_ERROR)
		{
			// Try setting the Filter type to known Filters
			alFilteri(uiFilters[0], AL_FILTER_TYPE, AL_FILTER_LOWPASS);
			if ( mEFXSupportList[AL_FILTER_LOWPASS] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Low Pass' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Low Pass' Support: NO");

			alFilteri(uiFilters[0], AL_FILTER_TYPE, AL_FILTER_HIGHPASS);
			if ( mEFXSupportList[AL_FILTER_HIGHPASS] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'High Pass' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'High Pass' Support: NO");

			alFilteri(uiFilters[0], AL_FILTER_TYPE, AL_FILTER_BANDPASS);
			if ( mEFXSupportList[AL_FILTER_BANDPASS] = (alGetError() == AL_NO_ERROR) )
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Band Pass' Support: YES");
			else
				Ogre::LogManager::getSingleton().logMessage("*** --- 'Band Pass' Support: NO");
		}

		// Delete Filter
		alDeleteFilters(1, &uiFilters[0]);

		// Delete Effect
		alDeleteEffects(1, &uiEffects[0]);

		// Delete Auxiliary Effect Slots
		alDeleteAuxiliaryEffectSlots(mNumEffectSlots, uiEffectSlots);
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_checkEFXSupport()
	{
		if (alcIsExtensionPresent(mDevice, "ALC_EXT_EFX"))
		{
			// Get function pointers
			alGenEffects = (LPALGENEFFECTS)alGetProcAddress("alGenEffects");
			alDeleteEffects = (LPALDELETEEFFECTS )alGetProcAddress("alDeleteEffects");
			alIsEffect = (LPALISEFFECT )alGetProcAddress("alIsEffect");
			alEffecti = (LPALEFFECTI)alGetProcAddress("alEffecti");
			alEffectiv = (LPALEFFECTIV)alGetProcAddress("alEffectiv");
			alEffectf = (LPALEFFECTF)alGetProcAddress("alEffectf");
			alEffectfv = (LPALEFFECTFV)alGetProcAddress("alEffectfv");
			alGetEffecti = (LPALGETEFFECTI)alGetProcAddress("alGetEffecti");
			alGetEffectiv = (LPALGETEFFECTIV)alGetProcAddress("alGetEffectiv");
			alGetEffectf = (LPALGETEFFECTF)alGetProcAddress("alGetEffectf");
			alGetEffectfv = (LPALGETEFFECTFV)alGetProcAddress("alGetEffectfv");
			alGenFilters = (LPALGENFILTERS)alGetProcAddress("alGenFilters");
			alDeleteFilters = (LPALDELETEFILTERS)alGetProcAddress("alDeleteFilters");
			alIsFilter = (LPALISFILTER)alGetProcAddress("alIsFilter");
			alFilteri = (LPALFILTERI)alGetProcAddress("alFilteri");
			alFilteriv = (LPALFILTERIV)alGetProcAddress("alFilteriv");
			alFilterf = (LPALFILTERF)alGetProcAddress("alFilterf");
			alFilterfv = (LPALFILTERFV)alGetProcAddress("alFilterfv");
			alGetFilteri = (LPALGETFILTERI )alGetProcAddress("alGetFilteri");
			alGetFilteriv= (LPALGETFILTERIV )alGetProcAddress("alGetFilteriv");
			alGetFilterf = (LPALGETFILTERF )alGetProcAddress("alGetFilterf");
			alGetFilterfv= (LPALGETFILTERFV )alGetProcAddress("alGetFilterfv");
			alGenAuxiliaryEffectSlots = (LPALGENAUXILIARYEFFECTSLOTS)alGetProcAddress("alGenAuxiliaryEffectSlots");
			alDeleteAuxiliaryEffectSlots = (LPALDELETEAUXILIARYEFFECTSLOTS)alGetProcAddress("alDeleteAuxiliaryEffectSlots");
			alIsAuxiliaryEffectSlot = (LPALISAUXILIARYEFFECTSLOT)alGetProcAddress("alIsAuxiliaryEffectSlot");
			alAuxiliaryEffectSloti = (LPALAUXILIARYEFFECTSLOTI)alGetProcAddress("alAuxiliaryEffectSloti");
			alAuxiliaryEffectSlotiv = (LPALAUXILIARYEFFECTSLOTIV)alGetProcAddress("alAuxiliaryEffectSlotiv");
			alAuxiliaryEffectSlotf = (LPALAUXILIARYEFFECTSLOTF)alGetProcAddress("alAuxiliaryEffectSlotf");
			alAuxiliaryEffectSlotfv = (LPALAUXILIARYEFFECTSLOTFV)alGetProcAddress("alAuxiliaryEffectSlotfv");
			alGetAuxiliaryEffectSloti = (LPALGETAUXILIARYEFFECTSLOTI)alGetProcAddress("alGetAuxiliaryEffectSloti");
			alGetAuxiliaryEffectSlotiv = (LPALGETAUXILIARYEFFECTSLOTIV)alGetProcAddress("alGetAuxiliaryEffectSlotiv");
			alGetAuxiliaryEffectSlotf = (LPALGETAUXILIARYEFFECTSLOTF)alGetProcAddress("alGetAuxiliaryEffectSlotf");
			alGetAuxiliaryEffectSlotfv = (LPALGETAUXILIARYEFFECTSLOTFV)alGetProcAddress("alGetAuxiliaryEffectSlotfv");

			if (alGenEffects &&	alDeleteEffects && alIsEffect && alEffecti && alEffectiv &&	alEffectf &&
				alEffectfv && alGetEffecti && alGetEffectiv && alGetEffectf && alGetEffectfv &&	alGenFilters &&
				alDeleteFilters && alIsFilter && alFilteri && alFilteriv &&	alFilterf && alFilterfv &&
				alGetFilteri &&	alGetFilteriv && alGetFilterf && alGetFilterfv && alGenAuxiliaryEffectSlots &&
				alDeleteAuxiliaryEffectSlots &&	alIsAuxiliaryEffectSlot && alAuxiliaryEffectSloti &&
				alAuxiliaryEffectSlotiv && alAuxiliaryEffectSlotf && alAuxiliaryEffectSlotfv &&
				alGetAuxiliaryEffectSloti && alGetAuxiliaryEffectSlotiv && alGetAuxiliaryEffectSlotf &&
				alGetAuxiliaryEffectSlotfv)
				return true;
		}

		return false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_checkXRAMSupport()
	{
		// Check for X-RAM extension
		if(alIsExtensionPresent("EAX-RAM") == AL_TRUE)
		{
			// Get X-RAM Function pointers
			mEAXSetBufferMode = (EAXSetBufferMode)alGetProcAddress("EAXSetBufferMode");
			mEAXGetBufferMode = (EAXGetBufferMode)alGetProcAddress("EAXGetBufferMode");

			if (mEAXSetBufferMode && mEAXGetBufferMode)
			{
				mXRamSize = alGetEnumValue("AL_EAX_RAM_SIZE");
				mXRamFree = alGetEnumValue("AL_EAX_RAM_FREE");
				mXRamAuto = alGetEnumValue("AL_STORAGE_AUTOMATIC");
				mXRamHardware = alGetEnumValue("AL_STORAGE_HARDWARE");
				mXRamAccessible = alGetEnumValue("AL_STORAGE_ACCESSIBLE");

				if (mXRamSize && mXRamFree && mXRamAuto && mXRamHardware && mXRamAccessible)
				{
					// Support available
					mXRamSizeMB = alGetInteger(mXRamSize) / (1024*1024);
					mXRamFreeMB = alGetInteger(mXRamFree) / (1024*1024);
					return true;
				}
			}
		}
		return false;
	}
#endif
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_destroyTemporarySound(OgreOggISound* sound)
	{
		if (!sound) return;

		mSoundsToDestroy->push(sound);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_destroyAllSoundsImpl()
	{
#if OGGSOUND_THREADED
		/** Mutex lock to avoid potential thread crashes. 
		*/
#	ifdef POCO_THREAD
		Poco::Mutex::ScopedLock l(mMutex);
#else
		boost::recursive_mutex::scoped_lock lock(mMutex);
#	endif
#endif
		// Destroy all sounds
		StringVector soundList;

		// Get a list of all sound names
		for ( SoundMap::iterator i=mSoundMap.begin(); i!=mSoundMap.end(); ++i )
			soundList.push_back(i->first);

		// Destroy individually outside mSoundMap iteration
		for ( StringVector::iterator i=soundList.begin(); i!=soundList.end(); ++i )
		{
			OgreOggISound* sound=0;
			if ( sound=getSound((*i)) ) 
				_destroySoundImpl(sound);
		}
		soundList.clear();

		// Shared buffers
		SharedBufferList::iterator b = mSharedBuffers.begin();
		while (b != mSharedBuffers.end())
		{
			if ( b->second->mRefCount>0 )
				alDeleteBuffers(1, &b->second->mAudioBuffer);
			OGRE_FREE(b->second, Ogre::MEMCATEGORY_GENERAL);
			++b;
		}

		mSharedBuffers.clear();

		// Clear queues
		mActiveSounds.clear();
		mPausedSounds.clear();
		mSoundsToReactivate.clear();
		mWaitingSounds.clear();
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_stopAllSoundsImpl()
	{
		if (mActiveSounds.empty()) return;

		for (ActiveList::const_iterator iter=mActiveSounds.begin(); iter!=mActiveSounds.end(); ++iter)
			(*iter)->_stopImpl();
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_setGlobalPitchImpl()
	{
		if (mSoundMap.empty() ) return;

		// Affect all sounds
		for (SoundMap::const_iterator iter=mSoundMap.begin(); iter!=mSoundMap.end(); ++iter)
			iter->second->setPitch(mGlobalPitch);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_pauseAllSoundsImpl()
	{
		if (mActiveSounds.empty()) return;

		for (ActiveList::const_iterator iter=mActiveSounds.begin(); iter!=mActiveSounds.end(); ++iter)
		{
			if ( (*iter)->isPlaying() && !(*iter)->isPaused() )
			{
				// Pause sound
				(*iter)->_pauseImpl();

				// Add to list to allow resuming
				mPausedSounds.push_back((*iter));
			}
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_resumeAllPausedSoundsImpl()
	{
		if (mPausedSounds.empty()) return;

		for (ActiveList::const_iterator iter=mPausedSounds.begin(); iter!=mPausedSounds.end(); ++iter)
			(*iter)->_playImpl();

		mPausedSounds.clear();
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_loadSoundImpl(	OgreOggISound* sound, 
												const String& file, 
												DataStreamPtr stream, 
												bool prebuffer)
	{
		if ( !sound ) return;

		sharedAudioBuffer* buffer=0;

		if ( !sound->mStream )
			// Is there a shared buffer?
			buffer = _getSharedBuffer(file);

		if (!buffer)
		{
			// Load audio file
			sound->_openImpl(stream);
		}
		else
		{
			// Use shared buffer if available
			sound->_openImpl(file, buffer);
		}

		// If requested to preBuffer - grab free source and init
		if (prebuffer)
		{
			if ( !_requestSoundSource(sound) )
			{
				Ogre::String msg="*** OgreOggSoundManager::createSound() - Failed to preBuffer sound: "+sound->getName();
				Ogre::LogManager::getSingleton().logMessage(msg);
			}
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_removeFromLists(OgreOggSound::OgreOggISound *sound)
	{
		// Remove from reactivate list
		if ( !mSoundsToReactivate.empty() )
		{
			// Remove ALL referneces to this sound..
			ActiveList::iterator iter=mSoundsToReactivate.begin(); 
			while ( iter!=mSoundsToReactivate.end() )
			{
				if ( sound==(*iter) )
					iter=mSoundsToReactivate.erase(iter);
				else
					++iter;
			}
		}

		/** Paused sound list - created by a call to pauseAllSounds()
		*/
		if ( !mPausedSounds.empty() )
		{
			// Remove ALL referneces to this sound..
			ActiveList::iterator iter=mPausedSounds.begin(); 
			while ( iter!=mPausedSounds.end() )
			{
				if ( sound==(*iter) )
					iter=mPausedSounds.erase(iter);
				else
					++iter;
			}
		}
		/** Waiting sound list
		*/
		if ( !mWaitingSounds.empty() )
		{
			// Remove ALL referneces to this sound..
			ActiveList::iterator iter=mWaitingSounds.begin(); 
			while ( iter!=mWaitingSounds.end() )
			{
				if ( sound==(*iter) )
					iter=mWaitingSounds.erase(iter);
				else
					++iter;
			}
		}
		/** Active sound list
		*/
		if ( !mActiveSounds.empty() )
		{
			// Remove ALL references to this sound..
			ActiveList::iterator iter=mActiveSounds.begin(); 
			while ( iter!=mActiveSounds.end() )
			{
				if ( sound==(*iter) )
					iter=mActiveSounds.erase(iter);
				else
					++iter;
			}
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_releaseSoundImpl(OgreOggISound* sound)
	{
		if (!sound) return;

#if OGGSOUND_THREADED
#	ifdef POCO_THREAD
		Poco::Mutex::ScopedLock l(mMutex);
#	else
		boost::recursive_mutex::scoped_lock lock(mMutex);
#	endif
#endif
		// Delete sound buffer
		ALuint src = sound->getSource();
		if ( src!=AL_NONE ) _releaseSoundSource(sound);

		// Remove references from lists
		_removeFromLists(sound);

		// Find sound in map
		SoundMap::iterator i = mSoundMap.find(sound->getName());
		mSoundMap.erase(i);

		// Delete sound
		OGRE_DELETE_T(sound, OgreOggISound, Ogre::MEMCATEGORY_GENERAL);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_destroySoundImpl(OgreOggISound* sound)
	{
		if ( !sound ) return;

		// Get SceneManager
		Ogre::SceneManager* s = sound->getSceneManager();
		s->destroyMovableObject(sound);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_destroyListener()
	{
		if ( !mListener ) return;

#if OGGSOUND_THREADED
		/** Dumb check to catch external destruction of sounds to avoid potential
			thread crashes. (manager issued destruction sets this flag)
		*/
#	ifdef POCO_THREAD
		Poco::Mutex::ScopedLock l(mMutex);
#else
		boost::recursive_mutex::scoped_lock lock(mMutex);
#	endif
#endif

		OGRE_DELETE_T(mListener, OgreOggListener, Ogre::MEMCATEGORY_GENERAL);
		mListener = 0;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_checkFeatureSupport()
	{
		Ogre::String msg="";
		// Supported Formats Info
		Ogre::LogManager::getSingleton().logMessage("*** --- SUPPORTED FORMATS");
		ALenum eBufferFormat = 0;
		eBufferFormat = alcGetEnumValue(mDevice, "AL_FORMAT_MONO16");
		if(eBufferFormat)
		{
			msg="*** --- AL_FORMAT_MONO16 -- Monophonic Sound";
			Ogre::LogManager::getSingleton().logMessage(msg);
		}
		eBufferFormat = alcGetEnumValue(mDevice, "AL_FORMAT_STEREO16");
		if(eBufferFormat)
		{
			msg="*** --- AL_FORMAT_STEREO16 -- Stereo Sound";
			Ogre::LogManager::getSingleton().logMessage(msg);
		}
		eBufferFormat = alcGetEnumValue(mDevice, "AL_FORMAT_QUAD16");
		if(eBufferFormat)
		{
			msg="*** --- AL_FORMAT_QUAD16 -- 4 Channel Sound";
			Ogre::LogManager::getSingleton().logMessage(msg);
		}
		eBufferFormat = alcGetEnumValue(mDevice, "AL_FORMAT_51CHN16");
		if(eBufferFormat)
		{
			msg="*** --- AL_FORMAT_51CHN16 -- 5.1 Surround Sound";
			Ogre::LogManager::getSingleton().logMessage(msg);
		}
		eBufferFormat = alcGetEnumValue(mDevice, "AL_FORMAT_61CHN16");
		if(eBufferFormat)
		{
			msg="*** --- AL_FORMAT_61CHN16 -- 6.1 Surround Sound";
			Ogre::LogManager::getSingleton().logMessage(msg);
		}
		eBufferFormat = alcGetEnumValue(mDevice, "AL_FORMAT_71CHN16");
		if(eBufferFormat)
		{
			msg="*** --- AL_FORMAT_71CHN16 -- 7.1 Surround Sound";
			Ogre::LogManager::getSingleton().logMessage(msg);
		}

#if HAVE_EFX
		// EFX
		mEFXSupport = _checkEFXSupport();
		if (mEFXSupport)
		{
			Ogre::LogManager::getSingleton().logMessage("*** --- EFX Detected");
			_determineAuxEffectSlots();
		}
		else
			Ogre::LogManager::getSingleton().logMessage("*** --- EFX NOT Detected");

		// XRAM
		mXRamSupport = _checkXRAMSupport();
		if (mXRamSupport)
		{
			// Log message
			Ogre::LogManager::getSingleton().logMessage("*** --- X-RAM Detected");
			Ogre::LogManager::getSingleton().logMessage("*** --- X-RAM Size(MB): " + Ogre::StringConverter::toString(mXRamSizeMB) +
				" Free(MB):" + Ogre::StringConverter::toString(mXRamFreeMB));
		}
		else
			Ogre::LogManager::getSingleton().logMessage("*** --- XRAM NOT Detected");

		// EAX
		for(int version = 5; version >= 2; version--)
		{
			Ogre::String eaxName="EAX"+Ogre::StringConverter::toString(version)+".0";
			if(alIsExtensionPresent(eaxName.c_str()) == AL_TRUE)
			{
				mEAXSupport = true;
				mEAXVersion = version;
				eaxName="*** --- EAX "+Ogre::StringConverter::toString(version)+".0 Detected";
				Ogre::LogManager::getSingleton().logMessage(eaxName);
				break;
			}
		}
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_enumDevices()
	{
		mDeviceStrings = const_cast<ALCchar*>(alcGetString(0,ALC_DEVICE_SPECIFIER));
	}					  
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_releaseAll()
	{
		stopAllSounds();
		_destroyAllSoundsImpl();

		// Delete sources
		SourceList::iterator it = mSourcePool.begin();
		while (it != mSourcePool.end())
		{
#if HAVE_EFX
			if ( hasEFXSupport() )
			{
				// Remove filters/effects
				alSourcei(static_cast<ALuint>((*it)), AL_DIRECT_FILTER, AL_FILTER_NULL);
				alSource3i(static_cast<ALuint>((*it)), AL_AUXILIARY_SEND_FILTER, AL_EFFECTSLOT_NULL, 0, AL_FILTER_NULL);
 			}
#endif
			alDeleteSources(1,&(*it));
			++it;
		}

		mSourcePool.clear();

#if HAVE_EFX
		// clear EFX effect lists
		if ( !mFilterList.empty() )
		{
			EffectList::iterator iter=mFilterList.begin();
			for ( ; iter!=mFilterList.end(); ++iter )
			    alDeleteEffects( 1, &iter->second);
			mFilterList.clear();
		}

		if ( !mEffectList.empty() )
		{
			EffectList::iterator iter=mEffectList.begin();
			for ( ; iter!=mEffectList.end(); ++iter )
			    alDeleteEffects( 1, &iter->second);
			mEffectList.clear();
		}

		if ( !mEffectSlotList.empty() )
		{
			SourceList::iterator iter=mEffectSlotList.begin();
			for ( ; iter!=mEffectSlotList.end(); ++iter )
			    alDeleteEffects( 1, &(*iter));
			mEffectSlotList.clear();
		}
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	int OgreOggSoundManager::_createSourcePool()
	{
		ALuint source;
		unsigned int numSources = 0;

		while(alGetError() == AL_NO_ERROR && numSources < mMaxSources)
		{
			source = 0;
			alGenSources(1,&source);
			if(source != 0)
			{
				mSourcePool.push_back(source);
				numSources++;
			}
			else
			{
				alGetError();
				break;
			}
		}

		return static_cast<int>(mSourcePool.size());
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_reactivateQueuedSoundsImpl()
	{
		// Pump waiting sounds first..
		if (!mWaitingSounds.empty())
		{
			OgreOggISound* sound = mWaitingSounds.front();

			// Grab a source
			if ( _requestSoundSource(sound) )
			{
				// Play
				sound->_playImpl();

				// Remove
				mWaitingSounds.erase(mWaitingSounds.begin());

				return;
			}
			else
				// Non available - quit
				return;
		}

		// Any sounds to re-activate?
		if (mSoundsToReactivate.empty()) return;

		// Sort list by distance
		mActiveSounds.sort(_sortNearToFar());

		// Get sound object from front of list
		OgreOggISound* snd = mSoundsToReactivate.front();

		// Check sound hasn't been stopped whilst in list
		if ( !snd->isPlaying() )
		{
			// Try to request a source for sound
			if (_requestSoundSource(snd))
			{
				// play sound
				snd->_playImpl();
			}
		}
		// Else - kick off list
		else
		{
			mSoundsToReactivate.erase(mSoundsToReactivate.begin());
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_reactivateQueuedSounds()
	{
		if ( mWaitingSounds.empty() && mSoundsToReactivate.empty() ) return;

#if OGGSOUND_THREADED
		SoundAction action;
		action.mAction	= LQ_REACTIVATE;
		action.mParams	= 0;
		action.mSound	= "";
		action.mImmediately = false;
		_requestSoundAction(action);
#else
		_reactivateQueuedSoundsImpl();
#endif
	}
	/*/////////////////////////////////////////////////////////////////*/
	sharedAudioBuffer* OgreOggSoundManager::_getSharedBuffer(const String& sName)
	{
		if ( sName.empty() ) return AL_NONE;

		SharedBufferList::iterator f;
		if ( ( f = mSharedBuffers.find(sName) ) != mSharedBuffers.end() )
			return f->second;

		return AL_NONE;
	}	 
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_releaseSharedBuffer(const String& sName, ALuint& buffer)
	{
		if ( sName.empty() ) return false;

		SharedBufferList::iterator f;
		if ( ( f = mSharedBuffers.find(sName) ) != mSharedBuffers.end() )
		{
			// Is it sharing buffer?
			if ( buffer == f->second->mAudioBuffer )
			{
				// Decrement
				f->second->mRefCount--;
				if ( f->second->mRefCount==0 )
				{
					// Delete buffer object
					alDeleteBuffers(1, &f->second->mAudioBuffer);

					// Delete struct
					OGRE_FREE(f->second, Ogre::MEMCATEGORY_GENERAL);

					// Remove from list
					mSharedBuffers.erase(f);
				}
				return true;
			}
		}
		return false;
	}	
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggSoundManager::_registerSharedBuffer(const String& sName, ALuint& buffer, OgreOggISound* parent)
	{
		if ( sName.empty() ) return false;

		SharedBufferList::iterator f;
		if ( ( f = mSharedBuffers.find(sName) ) == mSharedBuffers.end() )
		{
			// Create struct
			sharedAudioBuffer* buf = OGRE_ALLOC_T(sharedAudioBuffer, 1, Ogre::MEMCATEGORY_GENERAL);

			// Set buffer
			buf->mAudioBuffer = buffer;

			// Set ref count
			buf->mRefCount = 1;

			// Set parent ptr
			buf->mParent = parent;

			// Add to list
			mSharedBuffers[sName] = buf;
		}
		return true;
	}
#if OGGSOUND_THREADED
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_updateBuffers()
	{
		static Ogre::uint32 cTime;
		static Ogre::uint32 pTime=0;
		static Ogre::Timer timer;
		static float rTime=0.f;

		// Get frame time
		cTime = timer.getMillisecondsCPU();
		float fTime = (cTime-pTime) * 0.001f;

		// update Listener
		if ( mListener ) 
			mListener->update();

		// Loop all active sounds
		ActiveList::const_iterator i = mActiveSounds.begin();
		while( i != mActiveSounds.end())
		{
			// update pos/fade
			(*i)->update(fTime);

			// Update buffers
			(*i)->_updateAudioBuffers();

			// Update recorder
			if ( mRecorder ) mRecorder->_updateRecording();

			// Next..
			++i;
		}

		// Reactivate 10fps
		if ( (rTime+=fTime) > 0.1f )
		{
			_reactivateQueuedSoundsImpl();
			rTime=0.f;
		}

		// Reset timer
		pTime=cTime;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_performAction(const SoundAction& act)
	{
		switch (act.mAction)
		{
		case LQ_PLAY:			
			{ 
				if ( hasSound(act.mSound) )
					getSound(act.mSound)->_playImpl(); 
			} 
			break;
		case LQ_PAUSE:			
			{ 
				if ( hasSound(act.mSound) )
					getSound(act.mSound)->_pauseImpl(); 
			} 
			break;
		case LQ_STOP:			
			{ 
				if ( hasSound(act.mSound) )
					getSound(act.mSound)->_stopImpl(); 
			} 
			break;
		case LQ_REACTIVATE:		
			{ 
				_reactivateQueuedSoundsImpl(); 
			} 
			break;
		case LQ_GLOBAL_PITCH:	
			{ 
				_setGlobalPitchImpl(); 
			} 
			break;
		case LQ_STOP_ALL:		
			{ 
				_stopAllSoundsImpl(); 
			} 
			break;
		case LQ_PAUSE_ALL:		
			{ 
				_pauseAllSoundsImpl(); 
			} 
			break;
		case LQ_RESUME_ALL:		
			{ 
				_resumeAllPausedSoundsImpl(); 
			} 
			break;
		case LQ_LOAD:
			{
				cSound* c = static_cast<cSound*>(act.mParams);
				if ( hasSound(act.mSound) )
				{
					OgreOggISound* s = getSound(act.mSound); 
					_loadSoundImpl(s, c->mFileName, c->mStream, c->mPrebuffer);

					// Cleanup..
					c->mStream.setNull();
				}

				// Delete
				OGRE_DELETE_T(c, cSound, Ogre::MEMCATEGORY_GENERAL);
			}
			break;
#if HAVE_EFX
		case LQ_ATTACH_EFX:
			{
				efxProperty* e = static_cast<efxProperty*>(act.mParams);
				if ( hasSound(act.mSound) )
				{
					OgreOggISound* s = getSound(act.mSound); 
					if ( !e->mEffectName.empty() && !e->mFilterName.empty() ) 
						_attachEffectToSoundImpl(s, e->mSlotID, e->mEffectName, e->mFilterName);
					else
						_attachFilterToSoundImpl(s, e->mFilterName);
				}
				// Delete
				OGRE_DELETE_T(e, efxProperty, Ogre::MEMCATEGORY_GENERAL);
			}
			break;
		case LQ_DETACH_EFX:
			{
				efxProperty* e = static_cast<efxProperty*>(act.mParams);
				if ( hasSound(act.mSound) )
				{
					OgreOggISound* s = getSound(act.mSound); 
					if ( e->mSlotID!=255 ) 
						_detachEffectFromSoundImpl(s, e->mSlotID);
					else
						_detachFilterFromSoundImpl(s);
				}
				// Delete
				OGRE_DELETE_T(e, efxProperty, Ogre::MEMCATEGORY_GENERAL);
			}
			break;
		case LQ_SET_EFX_PROPERTY:
			{
				efxProperty* e = static_cast<efxProperty*>(act.mParams);
				if ( hasSound(act.mSound) )
				{
					OgreOggISound* s = getSound(act.mSound); 
					_setEFXSoundPropertiesImpl(s, e->mAirAbsorption, e->mRolloff, e->mConeHF);
				}
				// Delete
				OGRE_DELETE_T(e, efxProperty, Ogre::MEMCATEGORY_GENERAL);
			}
			break;
#endif
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_requestSoundAction(const SoundAction& action)
	{
		// If user has requested a mutex be used for every action,
		// action is performed immediately and blocks main thread.
		if ( mForceMutex || action.mImmediately )
		{
#ifdef POCO_THREAD
			Poco::Mutex::ScopedLock l(mMutex);
#else
			boost::recursive_mutex::scoped_lock lock(mMutex);
#endif
			_performAction(action);
			return;
		}

		if ( !mActionsList ) return;

		mActionsList->push(action);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggSoundManager::_processQueuedSounds()
	{
		if ( !mActionsList ) return;

		SoundAction act;
		// Perform sound requests 
		while ( mActionsList->pop(act) )
		{
			_performAction(act);
		}
	}
#endif
}
