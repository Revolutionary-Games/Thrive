Thrive
======

This is the code repository for Thrive. For more information, visit
http://www.revolutionarygames.com.


Getting Involved
----------------
Depending on what you want to contribute, you need to take different steps
to get your development environment set up.

### Script Authors
If you only want to modify the Lua scripts, you can just download a 
precompiled package from our build server at ftp://nimbal.net/jenkins.
After unpacking, you will find the scripts in the aptly named `scripts` 
subdirectory.

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage.

### C++ Programmers
To compile Thrive yourself, you will not only need to clone this git 
repository, but also the Subversion [asset repository][asset_repository],
username and password both "anonymous".  The best place to put the assets 
is in your code repository's `assets` subdirectory. If, for whatever 
reason, you want to check it out to another place, you will have to modify 
the `ASSET_DIRECTORY` variable in the CMake setup. Offloading art assets 
into Subversion was necessary due to size constraints for GitHub repositories.

Windows developers should follow the procedure outlined in the [mingw setup 
guide]().

For the time being, Linux developers will have to manually set up their build 
environment. The dependencies at the time of this writing are

* gcc 4.7 or newer
* boost 1.52
* Ogre 1.8
* Bullet 2.81
* OpenAL Soft 1.15.1

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage.

### Modellers and Sound Engineers
To work on the art assets, you can download a precompiled package from our
build server at ftp://nimbal.net/jenkins. After unpacking, create a 
subdirectory `testing` and place your assets in there. If they are used 
anywhere in the game, they will be picked up by Thrive.

More detailed instructions for contributing art assets will follow soon.


[asset_repository]: http://nimbal.net/scm/svn/thrive_assets/trunk "Asset Repository"
[mingw]: http://www.github.com/Revolutionary-Games/Thrive/blob/master/mingw_setup/readme.txt "MinGW setup guide"
[styleguide]: http://www.github.com/Revolutionary-Games/Thrive/blob/master/doc/style_guide.dox "Styleguide"
