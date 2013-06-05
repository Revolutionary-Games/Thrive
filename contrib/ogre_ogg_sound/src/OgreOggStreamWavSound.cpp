/**
* @file OgreOggStreamWavSound.cpp
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

#include "OgreOggStreamWavSound.h"
#include <string>
#include <iostream>
#include "OgreOggSoundManager.h"

namespace OgreOggSound
{

	/*/////////////////////////////////////////////////////////////////*/
	OgreOggStreamWavSound::OgreOggStreamWavSound(const Ogre::String& name, const Ogre::SceneManager& scnMgr) : OgreOggISound(name, scnMgr)
	, mLoopOffsetBytes(0)
	, mStreamEOF(false)
	, mLastOffset(0.f)
	{
		for ( int i=0; i<NUM_BUFFERS; i++ ) mBuffers[i]=AL_NONE;	
		mFormatData.mFormat=0;
		mStream = true;	   
	}
	/*/////////////////////////////////////////////////////////////////*/
	OgreOggStreamWavSound::~OgreOggStreamWavSound()
	{		
		// Notify listener
		if ( mSoundListener ) mSoundListener->soundDestroyed(this);
	
		_release();
		for ( int i=0; i<NUM_BUFFERS; i++ ) mBuffers[i]=0;
		if (mFormatData.mFormat) OGRE_FREE(mFormatData.mFormat, Ogre::MEMCATEGORY_GENERAL);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_openImpl(Ogre::DataStreamPtr& fileStream)
	{
		// WAVE descriptor vars
		char*			sound_buffer=0;
		int				bytesRead=0;
		ChunkHeader		c;

		// Store stream pointer
		mAudioStream = fileStream;

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
					// Should be 16 unless compressed ( compressed NOT supported )
					if ( mFormatData.mFormat->mHeaderSize>=16 )
					{
						// PCM == 1
						if (mFormatData.mFormat->mFormatTag==0x0001 || mFormatData.mFormat->mFormatTag==0xFFFE)
						{
							// Samples check..
							if ( (mFormatData.mFormat->mBitsPerSample!=16) && (mFormatData.mFormat->mBitsPerSample!=8) )
							{
								OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "BitsPerSample NOT 8/16!", "OgreOggStreamWavWavSound::_openImpl()");
							}

							// Calculate extra WAV header info
							long int extraBytes = mFormatData.mFormat->mHeaderSize - (sizeof(WaveHeader) - 20);

							// If WAVEFORMATEXTENSIBLE read attributes
							if (mFormatData.mFormat->mFormatTag==0xFFFE)
							{
								extraBytes-=static_cast<long>(mAudioStream->read(&mFormatData.mSamples, 2));
								extraBytes-=static_cast<long>(mAudioStream->read(&mFormatData.mChannelMask, 2));
								extraBytes-=static_cast<long>(mAudioStream->read(&mFormatData.mSubFormat, 16));
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
							OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Compressed wav NOT supported", "OgreOggStreamWavWavSound::_openImpl()");
						}
					}
					else
					{
						OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Wav NOT PCM", "OgreOggStreamWavWavSound::_openImpl()");
					}
				}
				else
				{
					OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Invalid format", "OgreOggStreamWavWavSound::_openImpl()");
				}
			}
			else
			{
				OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Not a valid WAVE file", "OgreOggStreamWavWavSound::_openImpl()");
			}
		}
		else
		{
			OGRE_EXCEPT(Ogre::Exception::ERR_FILE_NOT_FOUND, "Not a valid RIFF file!", "OgreOggStreamWavSound::_openImpl()");
		}

		// Create OpenAL buffer
		alGetError();
		alGenBuffers(NUM_BUFFERS, mBuffers);
		if ( alGetError()!=AL_NO_ERROR )
			OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Unable to create OpenAL buffer.", "OgreOggStreamWavSound::_openImpl()");

		// Check format support
		if (!_queryBufferInfo())
		{
			OGRE_EXCEPT(Ogre::Exception::ERR_INTERNAL_ERROR, "Format NOT supported", "OgreOggStreamWavWavSound::_openImpl()");
		}

		// Calculate length in seconds
		mPlayTime = static_cast<float>(((mAudioEnd-mAudioOffset)*8.f) / static_cast<float>((mFormatData.mFormat->mSamplesPerSec * mFormatData.mFormat->mChannels * mFormatData.mFormat->mBitsPerSample)));

#if HAVE_EFX
		// Upload to XRAM buffers if available
		if ( OgreOggSoundManager::getSingleton().hasXRamSupport() )
			OgreOggSoundManager::getSingleton().setXRamBuffer(NUM_BUFFERS, mBuffers);
#endif
		// Calculate loop offset in bytes
		// Set BEFORE sound loaded
		if ( mLoopOffset>0.f )
		{
			if ( mLoopOffset<mPlayTime ) 
			{
				// Calculate offset in bytes aligned to block align
				mLoopOffsetBytes = static_cast<unsigned long>((mLoopOffset * (mFormatData.mFormat->mSamplesPerSec * mFormatData.mFormat->mChannels * mFormatData.mFormat->mBitsPerSample))/8);
				mLoopOffsetBytes -= mLoopOffsetBytes % mFormatData.mFormat->mBlockAlign;
			}
			else			
			{
				Ogre::LogManager::getSingleton().logMessage("**** OgreOggStreamWavSound::open() ERROR - loop time invalid! ****", Ogre::LML_NORMAL);
				mLoopOffset=0.f;
			}
		}
		
		// Notify listener
		if ( mSoundListener ) mSoundListener->soundLoaded(this);
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggStreamWavSound::isMono()
	{
		if ( !mInitialised ) return false;

		return ( (mFormat==AL_FORMAT_MONO16) || (mFormat==AL_FORMAT_MONO8) );
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggStreamWavSound::_queryBufferInfo()
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
	void OgreOggStreamWavSound::_release()
	{
		if ( mSource!=AL_NONE )
		{
			ALuint src=AL_NONE;
			setSource(src);
		}
		for (int i=0; i<NUM_BUFFERS; i++)
		{
			if (mBuffers[i]!=AL_NONE)
				alDeleteBuffers(1, &mBuffers[i]);
		}
		mPlayPosChanged = false;
		mPlayPos = 0.f;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_prebuffer()
	{
		if (mSource==AL_NONE) return;

		int i=0;
		while ( i<NUM_BUFFERS )
		{
			if ( _stream(mBuffers[i]) )
				alSourceQueueBuffers(mSource, 1, &mBuffers[i++]);
			else
				break;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::setSource(ALuint& src)
	{
		if (src!=AL_NONE)
		{
			// Set source
			mSource=src;

			// Fill data buffers
			_prebuffer();

			// Init source
			_initSource();
		}
		else
		{
			// Unqueue buffers
			_dequeue();

			// Set source
			mSource=src;

			// Cancel initialisation
			mInitialised = false;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_updateAudioBuffers()
	{
		if(mSource == AL_NONE || !mPlay) return;

		ALenum state;
		alGetSourcei(mSource, AL_SOURCE_STATE, &state);

		if (state == AL_PAUSED) return;

		// Ran out of buffer data?
		if (state == AL_STOPPED)
		{
			if(mStreamEOF)
			{
				stop();
				return;
			}
			else
			{
				// Clear audio data already played...
				_dequeue();

				// Fill with next chunk of audio...
				_prebuffer();

				// Play...
				alSourcePlay(mSource);
			}
		}

		int processed;

		alGetSourcei(mSource, AL_BUFFERS_PROCESSED, &processed);

		while(processed--)
		{
			ALuint buffer;
			ALint size, bits, channels, freq;

			alSourceUnqueueBuffers(mSource, 1, &buffer);

			// Get buffer details
			alGetBufferi(buffer, AL_SIZE, &size);
			alGetBufferi(buffer, AL_BITS, &bits);
			alGetBufferi(buffer, AL_CHANNELS, &channels);
			alGetBufferi(buffer, AL_FREQUENCY, &freq);    

			// Update offset (in seconds)
			mLastOffset += ((ALuint)size/channels/(bits/8)) / (ALfloat)freq;
			if ( mLastOffset>=mPlayTime )
			{
				mLastOffset = mLastOffset-mPlayTime;
				
				/**	This is the closest we can get to a loop trigger.
				@remarks 
					If played data size exceeds audio data size trigger callback.
				*/
				if ( mSoundListener ) mSoundListener->soundLooping(this);
			}

			if ( _stream(buffer) ) 
			{
				alSourceQueueBuffers(mSource, 1, &buffer);
			}
		}

		// Handle play position change
		if ( mPlayPosChanged )
		{
			_updatePlayPosition();
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::setLoopOffset(float startTime)
	{
		// Store requested loop time
		mLoopOffset = startTime;

		// Is sound ready?
		if ( !mAudioStream.isNull() )
		{
			// Check valid loop point
			if ( mLoopOffset>=mPlayTime ) 
			{
				Ogre::LogManager::getSingleton().logMessage("**** OgreOggStreamWavSound::setLoopOffset() ERROR - loop time invalid! ****", Ogre::LML_CRITICAL);
				return;
			}

			// Calculate offset in bytes block aligned
			mLoopOffsetBytes = static_cast<unsigned long>(mLoopOffset * (mFormatData.mFormat->mSamplesPerSec));
			mLoopOffsetBytes -= mLoopOffsetBytes % mFormatData.mFormat->mBlockAlign;
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	bool OgreOggStreamWavSound::_stream(ALuint buffer)
	{
		std::vector<char> audioData;
		char* data;
		int  bytes = 0;
		int  result = 0;

		// Create buffer
		data = OGRE_ALLOC_T(char, mBufferSize, Ogre::MEMCATEGORY_GENERAL);
		memset(data, 0, mBufferSize);
		
		// Read only what was asked for
		while( !mStreamEOF && (static_cast<int>(audioData.size()) < mBufferSize) )
		{
			size_t currPos = mAudioStream->tell();
			// Is looping about to occur?
			if ( (currPos+mBufferSize) > mAudioEnd )
			{
				// Calculate remaining data size
				size_t remaining = mAudioEnd-currPos;
				// Read up to a buffer's worth of data
				if ( remaining )
					bytes = static_cast<int>(mAudioStream->read(data, remaining));
				// If set to loop wrap to start of stream
				if ( mLoop )
				{
					mAudioStream->seek(mAudioOffset + mLoopOffsetBytes);
				}
				else
				{
					mStreamEOF=true;
					// EOF - finish.
					if (bytes==0) break;
				}
				// Append to end of buffer
				audioData.insert(audioData.end(), data, data + bytes);
				// Keep track of read data
				result+=bytes;
			}
			else
			{
				// Read up to a buffer's worth of data
				bytes = static_cast<int>(mAudioStream->read(data, mBufferSize));
				// EOF check
				if (mAudioStream->eof())
				{
					// If set to loop wrap to start of stream
					if ( mLoop )
					{
						mAudioStream->seek(mAudioOffset);
						/**	This is the closest we can get to a loop trigger.
						If, whilst filling the buffers, we need to wrap the stream
						pointer, trigger the loop callback if defined.
						NOTE:- The accuracy of this method will be affected by a number of
						parameters, namely the buffer size, whether the sound has previously
						given up its source (therefore it will be re-filling all buffers, which,
						if the sound was close to eof will likely get triggered), and the quality
						of the sound, lower quality will hold a longer section of audio per buffer.
						In ALL cases this trigger will happen BEFORE the audio audibly loops!!
						*/		
						// Notify listener
						if ( mSoundListener ) mSoundListener->soundLooping(this);
					}
					else
					{
						mStreamEOF=true;
						// EOF - finish.
						if (bytes==0) break;
					}
				}
				// Append to end of buffer
				audioData.insert(audioData.end(), data, data + bytes);
				// Keep track of read data
				result+=bytes;
			}
		}

		// EOF
		if(result == 0)
		{
			OGRE_FREE(data, Ogre::MEMCATEGORY_GENERAL);
			return false;
		}

		alGetError();
		// Copy buffer data
		alBufferData(buffer, mFormat, &audioData[0], static_cast<ALsizei>(audioData.size()), mFormatData.mFormat->mSamplesPerSec);

		// Cleanup
		OGRE_FREE(data, Ogre::MEMCATEGORY_GENERAL);

		return true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_dequeue()
	{
		if(mSource == AL_NONE)
			return;

		int queued=0;

		alGetError();

		/** Check current state
		@remarks
			Fix for bug where prebuffering a streamed sound caused a buffer problem
			resulting in only 1st buffer repeatedly looping. This is because alSourceStop() 
			doesn't function correctly if the sources state hasn't previously been set!!???
		*/
		ALenum state;
		alGetSourcei(mSource, AL_SOURCE_STATE, &state);

		// Force mSource to change state so the call to alSourceStop() will mark buffers correctly.
		if (state == AL_INITIAL)
			alSourcePlay(mSource);

		// Stop source to allow unqueuing
		alSourceStop(mSource);

		// Get number of buffers queued on source
		alGetSourcei(mSource, AL_BUFFERS_PROCESSED, &queued);

		// Remove number of buffers from source
		while (queued--)
		{
			ALuint buffer;
			alSourceUnqueueBuffers(mSource, 1, &buffer);

			// Any problems?
			if ( alGetError() ) 
			{
				Ogre::LogManager::getSingleton().logMessage("*** Unable to unqueue buffers");
			}
		}
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_pauseImpl()
	{
		if(mSource == AL_NONE) return;

		alSourcePause(mSource);
		
		// Notify listener
		if ( mSoundListener ) mSoundListener->soundPaused(this);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_playImpl()
	{
		if(isPlaying())	return;

		// Grab a source if not already attached
		if (mSource == AL_NONE)
			if ( !OgreOggSoundManager::getSingleton()._requestSoundSource(this) )
				return;

		// Play source
		alGetError();
		alSourcePlay(mSource);
		if ( alGetError() )
		{
			Ogre::LogManager::getSingleton().logMessage("Unable to play sound");
			return;
		}

		// Set play flag
		mPlay = true;
		
		// Notify listener
		if ( mSoundListener ) mSoundListener->soundPlayed(this);
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::setPlayPosition(float seconds)
	{
		if(seconds < 0) return;

		// Wrap
		if ( seconds>mPlayTime ) 
			do { seconds-=mPlayTime; } while ( seconds>mPlayTime );

		// Store play position
		mPlayPos = seconds;

		// Set flag
		mPlayPosChanged = true;
	}
	/*/////////////////////////////////////////////////////////////////*/
	float OgreOggStreamWavSound::getPlayPosition()
	{ 
		if ( !mSource ) return -1.f;

		float time=0.f;
		alGetSourcef(mSource, AL_SEC_OFFSET, &time);

		if ( (mLastOffset+time)>=mPlayTime )
			return (mLastOffset+time) - mPlayTime;
		else
			return mLastOffset+time;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_updatePlayPosition()
	{
		if ( mSource==AL_NONE ) 
			return;

		// Get state
		bool playing = isPlaying();
		bool paused = isPaused();

		// Stop playback
		pause();

		// mBufferSize is 1/4 of a second
		size_t dataOffset = static_cast<size_t>(mPlayPos * mBufferSize * 4);
		mAudioStream->seek(mAudioOffset + dataOffset);

		// Unqueue audio
		_dequeue();

		// Fill buffers
		_prebuffer();

		// Set state
		if		(playing) play();
		else if	(paused) pause();

		// Set flag
		mPlayPosChanged = false;
		mLastOffset = mPlayPos;
		mStreamEOF=false;
	}
	/*/////////////////////////////////////////////////////////////////*/
	void OgreOggStreamWavSound::_stopImpl()
	{
		if(mSource != AL_NONE)
		{
			// Remove audio data from source
			_dequeue();

			// Stop playback
			mPlay=false;

			// Reset stream pointer
			mAudioStream->seek(mAudioOffset);
			mLastOffset=0;
			mStreamEOF=false;

			// Reload audio data
			_prebuffer();

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
	}
}
