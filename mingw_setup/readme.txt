What's this?
============

In the directory of this readme, you will find some scripts that will help you setting up a
system for building Thrive on Windows.


Important Note: If you run into any trouble with the setup scripts, please 
post them at
    http://thrivegame.forum-free.ca/t999-development-troubleshooting
    
Or if you have feedback for how to make the process better:
    
    http://thrivegame.forum-free.ca/t1101-build-system-discussion

Thank you!


Windows 7/8 + Code::Blocks
========================

If you would like to use an older Windows version, please refer to the "Other 
Windows Platforms" section. We currently only have experience with using codeblocks
due to its good support of make-files and mingw. Other IDEs may be possible to use
but we can't currently help with setting that up.

The setup script will install Ogre, Mingw and other required libraries.
For a full list refer to the readme in the root of the repository.

To set up the build system follow the steps listed below. If you are not 
interested in the gory details, you can ignore everything but the bullet
points.


0. Requirements
---------------

* At least 300 MB of free hard drive space on C:\ drive specifially and 3 GB free space on the drive on which you want to put the dependencies (can also be C:/)

* About 30-60 minutes, depending on the speed of your PC and your internet
  connection


1. Install CMake
----------------

* Download CMake from

    http://www.cmake.org/cmake/resources/software.html

* Run the installer


2a. Downloading required libraries
----------------------------------------------
Step 2a is optional but recommended for beginners to the project, this will require the 3 GB of dependencies to be on your C:/ drive.
If you choose step 2a, you should skip forward to step 5. after completion.

*  Download the archive found here:
    
    https://mega.nz/#!JVRw3IrS!K1s5y4dlbVc2HFfUlea_fZBDKjg_34u_5_Hk4W3wgRM  

*  Extract to C:\mingw

This skips the compilation of required libraries and instead downloads precompiled ones.

Skip to step 5.


2b. Enabling Powershell to run the setup script
----------------------------------------------

Note that this and the following steps should only be performed if you skip 2a.

* Open "Windows Powershell" as administrator by opening the start menu, 
  and entering "powershell" into the search line. Then right click on
  "Windows Powershell" and select "Run as Administrator".

* At the prompt that should pop up, type in 

    Set-ExecutionPolicy Unrestricted

  hit enter and confirm.

Powershell has very strict execution policies by default. Initially, you 
cannot execute scripts you downloaded (such as ours) or even scripts you wrote
yourself. To change that, we have to explicitly set the execution policy to
"Unrestricted". For security reasons, only the administrator can do that.


3. Run the setup script
-----------------------

* Right-click on "setup.ps1" and select "Run with Powershell"

* In the file dialog that should pop up, select a directory of your choice.
  C:\MinGW is recommended, but otherwise 300 MB will be copied to C:/MinGW regardless.

* This will take quite a while, so be patient. When the script is complete, 
  a message box will inform you.

You will also have to point Code::Blocks to your custom directory in step (7).

4. Optional, but recommended: Reset Powershell Execution Policy
---------------------------------------------------------------

* If you are paranoid (or share your PC with a naive person), you can now
  reset Powershell's execution policy to something more secure. Follow the
  steps outline in (1), but instead of "Unrestricted", set the policy to
  "Restricted"

5. Install Code::Blocks
-----------------------

* Download Code::Blocks from 

    http://www.codeblocks.org/downloads/26

  and install it

If you don't install Code::Blocks, CMake won't be able to generate a project
file in the next step.


6. Install tortoise svn or just svn
---------------

* Download TortoiseSVN from

    http://tortoisesvn.net/downloads.html
    
    and install it


7. Get the assets
---------------

* If you Installed tortoise SVN

    Right click somewhere in the git repository and click SVN Checkout.
    Under URL enter: http://assets.revolutionarygamesstudio.com/
    Edit the last part of Checkout directory to "/assets" instead of "/trive_assets"
    Click checkout and it will prompt you for a user
    For password and username simply enter 'thrive' and 'thrive'

* If you are instead using commandline SVN

    svn co http://assets.revolutionarygamesstudio.com ./assets
    
8. Invoke CMake
---------------

* Start the CMake GUI from your start menu

* Set the source code directory to Thrive's root directory. That should be one
  directory above where this readme is located.

* Set the build directory to an empty directory where you want to put the 
  compiled binaries. I like to use a "build" subdirectory of the project

* Click on "Configure"

* Select "CodeBlocks - MinGW Makefiles" as generator and "Specify toolchain 
  file for cross-compiling", then click "Next"

* On the next page, browse to C:\MinGW\cmake\toolchain.cmake (or wherever you
  installed the build environment) and click "Finish"

* Click "Generate" to generate the Code::Blocks project files

The toolchain file was configured during the setup script to contain paths to 
the compiler executable and all accompanying tools. It's usually used for 
cross-compiling, but it's convenient for us, too.

9. Building Thrive
---------------------------

* Open "Thrive.cbp" in your selected build directory with Code::Blocks

* If you didn't install to C:\MinGW, Code::Blocks will probably complain
  about not being able to find the compiler. If it asks you for a default
  compiler, select "Gnu GCC Compiler". Then goto "Settings -> Compiler" and
  select the tab "Toolchain Executables". Point it to your installation 
  directory. Note that you may have to remove the mingw-32 prefix from the
  default executables (EXCEPT mingw32-make).

* If all is well, you can just click on the "Build" button (a tiny cog) to
  build Thrive


10. Running Thrive
-----------------

* In Code::Blocks, go to Project->Properties->Build Targets then select Install on the left and change Type: from GUI Application to Console Application. Next, change Output Filename to "dist\bin\Thrive.exe" (without the quotes). If asks you to replace the file, replace it. Finally, click okay and exit.

* Select "install" as the build target and click on the 
  "Build and Run" button.

* You can also go to your build/dist/bin directory and start Thrive.exe
Note that the build/Thrive.exe will not work as it is not placed with the 
necessary DLL files.

* An ogre config will show up when you start Thrive. Selecting a non-0 value for FSAA anti aliasing may or may not prevent a current issue with flickering on windows.


Older Windows Platforms
=======================

Windows versions prior to Windows 7 may not have Powershell installed by 
default. You will need to find and install the appropriate Powershell version
for your system. Then the above steps should apply as well.


Linux - Cross Compiling for Windows
===================================

 [ THIS FEATURE IS CURRENTLY DEPRECATED ]
 It may return in the future.

The setup.sh script takes one argument, the path to the build environment 
installation directory. It defaults to /opt/mingw-w64.

Once done, you can let CMake know about the toolchain like this:

    cmake -DCMAKE_TOOLCHAIN_FILE=/opt/mingw-w64/cmake/toolchain.cmake $SRC_DIR


Troubleshooting
===============

CMake complains about missing googletest and / or luabind
---------------------------------------------------------
Those two dependencies are included as git submodules, which are not 
automatically cloned. If you are using git from the command line, go to
the project directory and issue the commands "git submodule init", followed by 
"git submodule update".

For other clients such as TortoiseGit, look for options like 
"Submodule Update" or similar.

Building works, but when running Thrive, it complains about missing DLLs
------------------------------------------------------------------------
Make sure to install Thrive before running it. The install target copies all
necessary files to the "dist" subdirectory in the build directory.

For Code::Blocks, you can select the install target in a dropdown near the 
build button.
