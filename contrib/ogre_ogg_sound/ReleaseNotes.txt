Release Notes:-

OgreOggSound is a wrapper around OpenAL to quickly add 2D/3D .ogg/.wav audio to applications. 
It is designed to seemlessly integrate into OGRE applications and handles static and streamed sounds with optional multi-threaded stream support via BOOST/POCO threads.

Features List:

* .ogg file format support

* uncompressed .wav file support

* In memory and streaming support
	* Load whole sound into memory
	* Stream sound from a file

* Optional multithreaded streaming 
	* using BOOST Threads

* Multichannel audio support

* Full 2D/3D audio support
	* spatialized sound support using mono sound files
	* 2D/multichannel support 

* Full control over 3D parameters
	* All 3D properties exposed for customisation
	* Global attenuation model configurable
	* Global sound speed configurable
	* Global doppler effect configurable

* Playback seeking 

* Cue points - Set 'jump-to' points within sounds

* Configurable loop points
	* By default a sound would loop from start to end, however a user can customise this by offsetting the start point of the loop per sound.

* Temporary sounds
	* Allows creation and automatic destruction of single-play/infrequent sounds.

* Source management
	* Sources are pooled 
	* Sources are automatically re-used when a sound requests to play
	* Sounds are re-activated if temporarily stopped.

* OGRE integration support
	* Sound objects are derived from MovableObject
	* can be attached directly into scene graph via SceneNodes
	* Automatically updates transformations

* Audio capturing support to WAV file.

* XRAM hardware buffer support 
	* Currently experimental

* EFX effect support
	* Support for attaching EFX filters/effects to sounds if hardware supported
	* Support for EAX room reverb presets
	
* Volume control

* Pitch control

* Loop control

* Fully Documented