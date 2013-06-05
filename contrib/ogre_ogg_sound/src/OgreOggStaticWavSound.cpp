/**
* @file OgreOggStaticWavSound.cpp
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

#include "OgreOggStaticWavSound.h"
#include <string>
#include <iostream>
#include "OgreOggSoundManager.h"

namespace OgreOggSound
{

	/*/////////////////////////////////////////////////////////////////*/
			OgreOggStaticWavSound::OgreOggStaticWavSound(const Ogre::String& name,const Ogre::SceneManager& scnMgr) : OgreOggISound(name, scnMgr)
		,mAudioName("")
		,mPreviousOffset(0)
		,mBuffer(0)
		{
			mStream=false;
			mFormatData.mFormat=0;
			mBufferData.clear();
		}
	/*/////////////////////////////////////////////////////////////////*/
			OgreOggStaticWavSound::~OgreOggStaticWavSound()
	{
		// Notify listener
		if ( mSoundListener ) mSoundListener->soundDestroyed(this);

		_release();
		mBufferData.clear();
		if (mFormatData.mFormat) OGRE_FREE(mFormatData.mFormat, Ogre::MEMCATEGORY_GENERAL);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_openImpl(Ogre::DataStreamPtr& fileStream)
	{
		// WAVE descriptor vars
		char*			sound_buffer=0;
		int				bytesRead=0;
		ChunkHeader		c;

		// Store stream pointer
		mAudioStream = fileStream;

		// Store file name
		mAudioName = mAudioStream->getName();

		// Allocate format structure
		mFormatData.mFormat = OGRE_NEW_T(WaveHeader, Ogre::MEMCATEGORY_GENERAL);

		// Read in "RIFF" chunk descriptor (4 bytes)
		mAudioStream->read(mFormatData.mFormat, sizeof(WaveHeader));

		// Valid 'RIFF'?
		if ( mFormatData.mFormat->mRIFF[0]=='R' && mFormatData.mFormat->mRIFF[1]=='I' && mFormatData.mFormat->mRIFF[2]=='F' && mFormatData.mFormat->mRIFF[3]=='F' )
		{
			// Valid 'WAVE'?
			if ( mFormatData.mFormat->mWAVE[0]=='W' && mFormatData.mFormat->mWAVE[1]=='A' && mFormatData.mFormat->mWAVE[2]=='V' && mFormatData.mFormat->mWAVE[3]=='E' )
			{
				// Valid 'fmt '?
				if ( mFormatData.mFormat->mFMT[0]=='f' && mFormatData.mFormat->mFMT[1]=='m' && mFormatData.mFormat->mFMT[2]=='t' && mFormatData.mFormat->mFMT[3]==' ' )
				{
					// SmFormatData.mFormat->uld be 16 unless compressed ( compressed NOT supported )
					if ( mFormatData.mFormat->mHeaderSize>=16 )
					{
						// PCM == 1
						if (mFormatData.mFormat->mFormatTag==0x0001 || mFormatData.mFormat->mFormatTag==0xFFFE)
						{
							// Samples check..
							if ( (mFormatData.mFormat->mBitsPerSample!=16) && (mFormatData.mFormat->mBitsPerSample!=8) )
							{
								OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "BitsPerSample NOT 8/16!", "OgreOggStaticWavSound::_openImpl()");
							}

							// Calculate extra WAV header info
							unsigned long int extraBytes = mFormatData.mFormat->mHeaderSize - (sizeof(WaveHeader) - 20);

							// If WAVEFORMATEXTENSIBLE read attributes
							if (mFormatData.mFormat->mFormatTag==0xFFFE)
							{
								extraBytes-=static_cast<unsigned long int>(mAudioStream->read(&mFormatData.mSamples, 2));
								extraBytes-=static_cast<unsigned long int>(mAudioStream->read(&mFormatData.mChannelMask, 2));
								extraBytes-=static_cast<unsigned long int>(mAudioStream->read(&mFormatData.mSubFormat, 16));
							}
		
							// Skip
							mAudioStream->skip(extraBytes);

							do
							{
								// Read in chunk header
								mAudioStream->read(&c, sizeof(ChunkHeader));

								// 'data' chunk...
								if ( c.chunkID[0]=='d' && c.chunkID[1]=='a' && c.chunkID[2]=='t' && c.chunkID[3]=='a' )
								{
									// Store byte offset of start of audio data
									mAudioOffset = static_cast<unsigned long>(mAudioStream->tell());

									// Check data size
									int fileCheck = c.length % mFormatData.mFormat->mBlockAlign;

									// Store end pos
									mAudioEnd = mAudioOffset+(c.length-fileCheck);

									// Allocate array
									sound_buffer = OGRE_ALLOC_T(char, mAudioEnd-mAudioOffset, Ogre::MEMCATEGORY_GENERAL);

									// Read entire sound data
									bytesRead = static_cast<int>(mAudioStream->read(sound_buffer, mAudioEnd-mAudioOffset));

									// Jump out
									break;
								}
								// Unsupported chunk...
								else
									mAudioStream->skip(c.length);
							}
							while ( mAudioStream->eof() || c.chunkID[0]!='d' || c.chunkID[1]!='a' || c.chunkID[2]!='t' || c.chunkID[3]!='a' );							
						}
						else 
						{
							OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Compressed wav NOT supported!", "OgreOggStaticWavSound::_openImpl()");
						}
					}
					else
					{
						OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Wav NOT PCM!", "OgreOggStaticWavSound::_openImpl()");
					}
				}
				else
				{
					OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Invalid Format!", "OgreOggStaticWavSound::_openImpl()");
				}
			}
			else
			{
				OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Not a valid WAVE file!", "OgreOggStaticWavSound::_openImpl()");
			}
		}
		else
		{
			OGRE_EXCEPT(Ogre::Exception::ERR_FILE_NOT_FOUND, "Not a valid RIFF file!", "OgreOggStaticWavSound::_openImpl()");
		}


		// Create OpenAL buffer
		alGetError();
		alGenBuffers(1, &mBuffer);
		if ( alGetError()!=AL_NO_ERROR )
		{
			OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Unable to create OpenAL buffer.", "OgreOggStaticWavSound::_openImpl()");
			return;
		}

#if HAVE_EFX
		// Upload to XRAM buffers if available
		if ( OgreOggSoundManager::getSingleton().hasXRamSupport() )
			OgreOggSoundManager::getSingleton().setXRamBuffer(1, &mBuffer);
#endif

		// Check format support
		if (!_queryBufferInfo())
			OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Format NOT supported.", "OgreOggStaticWavSound::_openImpl()");

		// Calculate length in seconds
		mPlayTime = static_cast<float>(((mAudioEnd-mAudioOffset)*8.f) / static_cast<float>((mFormatData.mFormat->mSamplesPerSec * mFormatData.mFormat->mChannels * mFormatData.mFormat->mBitsPerSample)));

		alGetError();
		alBufferData(mBuffer, mFormat, sound_buffer, static_cast<ALsizei>(bytesRead), mFormatData.mFormat->mSamplesPerSec);
		if ( alGetError()!=AL_NO_ERROR )
		{
			OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Unable to load audio data into buffer!", "OgreOggStaticWavSound::_openImpl()");
			return;
		}
		OGRE_FREE(sound_buffer, Ogre::MEMCATEGORY_GENERAL);

		// Register shared buffer
		OgreOggSoundManager::getSingleton()._registerSharedBuffer(mAudioName, mBuffer);

		// Notify listener
		if ( mSoundListener ) mSoundListener->soundLoaded(this);
	}

	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_openImpl(const Ogre::String& fName, ALuint& buffer)
	{
		// Set buffer
		mBuffer = buffer;

		// Filename
		mAudioName = fName;

		// Notify listener
		if ( mSoundListener ) mSoundListener->soundLoaded(this);
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool	OgreOggStaticWavSound::isMono() 
	{
		if ( !mInitialised ) return false;

		return ( (mFormat==AL_FORMAT_MONO16) || (mFormat==AL_FORMAT_MONO8) );
	}

	/*/////////////////////////////////////////////////////////////////*/
	bool	OgreOggStaticWavSound::_queryBufferInfo()
	{
		if ( !mFormatData.mFormat ) return false;

		switch(mFormatData.mFormat->mChannels)
		{
		case 1:
			{
				if ( mFormatData.mFormat->mBitsPerSample==8 )
				{
					// 8-bit mono
					mFormat = AL_FORMAT_MONO8;

					// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
					mBufferSize = mFormatData.mFormat->mSamplesPerSec/4;
				}
				else
				{
					// 16-bit mono
					mFormat = AL_FORMAT_MONO16;

					// Queue 250ms of audio data
					mBufferSize = mFormatData.mFormat->mAvgBytesPerSec >> 2;

					// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
					mBufferSize -= (mBufferSize % mFormatData.mFormat->mBlockAlign);
				}
			}
			break;
		case 2:
			{
				if ( mFormatData.mFormat->mBitsPerSample==8 )
				{
					// 8-bit stereo
					mFormat = AL_FORMAT_STEREO8;

					// Set BufferSize to 250ms (Frequency * 2 (8bit stereo) divided by 4 (quarter of a second))
					mBufferSize = mFormatData.mFormat->mSamplesPerSec >> 1;

					// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
					mBufferSize -= (mBufferSize % 2);
				}
				else
				{
					// 16-bit stereo
					mFormat = AL_FORMAT_STEREO16;

					// Queue 250ms of audio data
					mBufferSize = mFormatData.mFormat->mAvgBytesPerSec >> 2;

					// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
					mBufferSize -= (mBufferSize % mFormatData.mFormat->mBlockAlign);
				}
			}
			break;
		case 4:
			{
				// 16-bit Quad surround
				mFormat = alGetEnumValue("AL_FORMAT_QUAD16");
				if (!mFormat) return false;

				// Queue 250ms of audio data
				mBufferSize = mFormatData.mFormat->mAvgBytesPerSec >> 2;

				// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
				mBufferSize -= (mBufferSize % mFormatData.mFormat->mBlockAlign);
			}
			break;
		case 6:
			{
				// 16-bit 5.1 surround
				mFormat = alGetEnumValue("AL_FORMAT_51CHN16");
				if (!mFormat) return false;

				// Queue 250ms of audio data
				mBufferSize = mFormatData.mFormat->mAvgBytesPerSec >> 2;

				// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
				mBufferSize -= (mBufferSize % mFormatData.mFormat->mBlockAlign);
			}
			break;
		case 7:
			{
				// 16-bit 7.1 surround
				mFormat = alGetEnumValue("AL_FORMAT_61CHN16");
				if (!mFormat) return false;

				// Queue 250ms of audio data
				mBufferSize = mFormatData.mFormat->mAvgBytesPerSec >> 2;

				// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
				mBufferSize -= (mBufferSize % mFormatData.mFormat->mBlockAlign);
			}
			break;
		case 8:
			{
				// 16-bit 8.1 surround
				mFormat = alGetEnumValue("AL_FORMAT_71CHN16");
				if (!mFormat) return false;

				// Queue 250ms of audio data
				mBufferSize = mFormatData.mFormat->mAvgBytesPerSec >> 2;

				// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
				mBufferSize -= (mBufferSize % mFormatData.mFormat->mBlockAlign);
			}
			break;
		default:
			{
				// Error message
				Ogre::LogManager::getSingleton().logMessage("*** --- Unable to determine number of channels: defaulting to 16-bit stereo");

				// 16-bit stereo
				mFormat = AL_FORMAT_STEREO16;

				// Queue 250ms of audio data
				mBufferSize = mFormatData.mFormat->mAvgBytesPerSec >> 2;

				// IMPORTANT : The Buffer Size must be an exact multiple of the BlockAlignment ...
				mBufferSize -= (mBufferSize % mFormatData.mFormat->mBlockAlign);
			}
			break;
		}
		return true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_release()
	{
		ALuint src=AL_NONE;
		setSource(src);
		OgreOggSoundManager::getSingleton()._releaseSharedBuffer(mAudioName, mBuffer);
		mPlayPosChanged = false;
		mPlayPos = 0.f;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_prebuffer()
	{
		if (mSource==AL_NONE) return;

		// Queue buffer
		alSourcei(mSource, AL_BUFFER, mBuffer);
	}

	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::setSource(ALuint& src)
	{
		if (src!=AL_NONE)
		{
			// Attach new source
			mSource=src;

			// Load audio data onto source
			_prebuffer();

			// Init source properties
			_initSource();
		}
		else
		{
			// Validity check
			if ( mSource!=AL_NONE )
			{
				// Need to stop sound BEFORE unqueuing
				alSourceStop(mSource);

				// Unqueue buffer
				alSourcei(mSource, AL_BUFFER, 0);
			}

			// Attach new source
			mSource=src;

			// Cancel initialisation
			mInitialised = false;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_pauseImpl()
	{
		if ( mSource==AL_NONE ) return;

		alSourcePause(mSource);

		// Notify listener
		if ( mSoundListener ) mSoundListener->soundPaused(this);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_playImpl()
	{
		if(isPlaying())
			return;

		if (mSource == AL_NONE)
			if ( !OgreOggSoundManager::getSingleton()._requestSoundSource(this) )
				return;

		// Pick up position change
		if ( mPlayPosChanged )
			setPlayPosition(mPlayPos);

		alSourcePlay(mSource);
		mPlay = true;

		// Notify listener
		if ( mSoundListener ) mSoundListener->soundPlayed(this);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_stopImpl()
	{
		if ( mSource==AL_NONE ) return;

		alSourceStop(mSource);
		alSourceRewind(mSource);
		mPlay=false;
		mPreviousOffset=0;

		if (mTemporary)
		{
			OgreOggSoundManager::getSingleton()._destroyTemporarySound(this);
		}
		// Give up source immediately if specfied
		else if (mGiveUpSource) 
			OgreOggSoundManager::getSingleton()._releaseSoundSource(this);

		// Notify listener
		if ( mSoundListener ) mSoundListener->soundStopped(this);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::loop(bool loop)
	{
		OgreOggISound::loop(loop);

		if(mSource != AL_NONE)
		{
			alSourcei(mSource,AL_LOOPING, loop);
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void	OgreOggStaticWavSound::_updateAudioBuffers()
	{
		if(mSource == AL_NONE || !mPlay)
			return;

		ALenum state;
		alGetSourcei(mSource, AL_SOURCE_STATE, &state);

		if (state == AL_STOPPED)
		{
			stop();
		}
		else
		{
			ALint bytes=0;

			// Use byte offset to work out current position
			alGetSourcei(mSource, AL_BYTE_OFFSET, &bytes);

			// Has the audio looped?
			if ( mPreviousOffset>bytes )
			{
				// Notify listener
				if ( mSoundListener ) mSoundListener->soundLooping(this);
			}

			// Store current offset position
			mPreviousOffset=bytes;
		}
	}
}
