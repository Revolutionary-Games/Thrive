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
precompiled package from our [build server](ftp://91.250.119.121/jenkins).
After unpacking, you will find the scripts in the aptly named `scripts` 
subdirectory.

Be sure to have a look at the [styleguide](www.github.com/Revolutionary-Games/Thrive/blob/master/doc/style_guide.dox),
both for guidelines on code formatting and git usage.

### C++ Programmers
To compile Thrive yourself, you will not only need to clone this git 
repository, but also the Subversion [asset repository](91.250.119.121/scm/svn/thrive_assets/trunk).
The best place to put the assets is in your code repository's `assets` 
subdirectory. If, for whatever reason, you want to check it out to another
place, you will have to modify the `ASSET_DIRECTORY` variable in the CMake 
setup. If you want to know why we split the code and assets into two different
repositories in the first place, please refer to GitHub issue #32.

Windows developers should follow the procedure outlined in the [mingw setup 
guide](www.github.com/Revolutionary-Games/Thrive/blob/master/mingw_setup/readme.txt).

For the time being, Linux developers will have to manually set up their build 
environment.

Be sure to have a look at the [styleguide](www.github.com/Revolutionary-Games/Thrive/blob/master/doc/style_guide.dox),
both for guidelines on code formatting and git usage.

### Modellers and Sound Engineers
To work on the art assets, you can download a precompiled package from our
[build server](ftp://91.250.119.121/jenkins). After unpacking, create a 
subdirectory `testing` and place your assets in there. If they are used 
anywhere in the game, they will be picked up by Thrive.

More detailed instructions for contributing art assets will follow soon.
