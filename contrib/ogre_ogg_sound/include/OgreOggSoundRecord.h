/**
* @file OgreOggSoundRecord.h
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
* Implements methods for recording audio 
*/

#pragma once

#include "OgreOggSoundPrereqs.h"

#include <fstream>

namespace OgreOggSound
{
	//! WAVE file format structure
	struct wFormat
	{
		unsigned short 
			nChannels,
			wBitsPerSample,
			nBlockAlign,
			wFormatTag,
			cbSize;
		
		unsigned int
			nSamplesPerSec,
			nAvgBytesPerSec;
	};

	//! WAVE file header information
	struct WAVEHEADER
	{
		char			szRIFF[4];
		int				lRIFFSize;
		char			szWave[4];
		char			szFmt[4];
		int				lFmtSize;
		wFormat			wfex;
		char			szData[4];
		int				lDataSize;
	};

	//! Captures audio data
	/**
	@remarks
		This class can be used to capture audio data to an external file, WAV file ONLY.
		Use control panel --> Sound and Audio devices applet to select input type and volume.
		NOTE:- default file properties are - Frequency: 44.1Khz, Format: 16-bit stereo, Buffer Size: 8820 bytes.
	*/
	class _OGGSOUND_EXPORT OgreOggSoundRecord
	{
	
	public:

		typedef std::vector<Ogre::String> RecordDeviceList;

	private:

		ALCdevice*			mDevice;
		ALCcontext*			mContext;
		ALCdevice*			mCaptureDevice;
		const ALCchar*		mDefaultCaptureDevice;
		ALint				mSamplesAvailable;
		std::ofstream		mFile;
		ALchar*				mBuffer;
		WAVEHEADER			mWaveHeader;
		ALint				mDataSize;
		ALint				mSize;
		RecordDeviceList	mDeviceList;
		Ogre::String		mOutputFile;
		Ogre::String		mDeviceName;
		ALCuint				mFreq;
		ALCenum				mFormat;
		ALsizei				mBufferSize;
		unsigned short		mBitsPerSample;
		unsigned short		mNumChannels;
		bool				mRecording;

		/** Updates recording from the capture device
		*/
		void _updateRecording();
		/** Initialises a capture device ready to record audio data
		@remarks
		Gets a list of capture devices, initialises one, and opens output file
		for writing to.
		*/
		bool _openDevice();

	public:

		OgreOggSoundRecord(ALCdevice& alDevice);
		/** Gets a list of strings describing the capture devices
		*/
		const RecordDeviceList& getCaptureDeviceList();
		/** Creates a capture object
		*/
		bool initCaptureDevice(const Ogre::String& devName="", const Ogre::String& fileName="output.wav", ALCuint freq=44100, ALCenum format=AL_FORMAT_STEREO16, ALsizei bufferSize=8820);
		/** Starts a recording from a capture device
		*/
		void startRecording();
		/** Returns whether a capture device is available
		*/
		bool isCaptureAvailable();
		/** Stops recording from the capture device
		*/
		void stopRecording();
		/** Closes capture device, outputs captured data to a file if available.
		*/
		~OgreOggSoundRecord();

		// Manager friend
		friend class OgreOggSoundManager;
	};

}

