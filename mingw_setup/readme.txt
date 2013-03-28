What's this?
============

In this directory, you will find some scripts that will help you setting up a
system for building Thrive.

Important Note: If you run into any trouble with the setup scripts, please 
post them at

    http://thrivegame.forum-free.ca/t1101-build-system-discussion

so that we can improve the scripts. Thank you.


Windows 7 + Code::Blocks
========================

This is the only combination tested so far and probably the most popular. If 
you would like to use another Windows version, please refer to the "Other 
Windows Platforms" section. If you prefer another IDE, go to "Other Windows 
IDEs".

The setup script will install:

* MinGW-w64 with GCC 4.8

* Boost libraries 1.51.0

* Ogre SDK 1.8.1

To set up the build system follow the steps listed below. If you are not 
interested in the gory details, you can ignore everything but the bullet
points.

0. Requirements
---------------

* At least 5 GB of free hard drive space

* About 30-60 minutes, depending on the speed of your PC and your internet
  connection

1. Enabling Powershell to run the setup script
----------------------------------------------

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

2. Install CMake
----------------

* Download CMake from

    http://www.cmake.org/cmake/resources/software.html

* Run the installer and check "Add CMake to system path" during installation

The setup script requires CMake to be available on the command line. That's
why we need to add it to the PATH environment variable.

3. Run the setup script
-----------------------

* Right-click on "setup.ps1" and select "Run with Powershell"

* In the file dialog that should pop up, select a directory of your choice.
  C:\MinGW is recommended if you don't have another MinGW installation there
  already.

* This will take quite a while, so be patient. When the script is complete, 
  a message box will inform you.

If you want to install the build environment to another directory, you need to
have another compiler installed. A standard mingw installation in C:\MinGW or 
a Visual Studio installation (so that msvc.exe is in the system path) should
do the trick.

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

6. Invoke CMake
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

7. Building Thrive
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


8. Running Thrive
-----------------

* In Code::Blocks, select "install" as the build target and click on the 
  "Build" button.

* Go to your build directory and start Thrive.exe

Unfortunately, I haven't yet found a clean way to start (and debug) Thrive
from within Code::Blocks due to the way Windows finds its shared libraries.


Other IDEs
==========

CMake offers generators for a range of different build procedures. All the 
ones referring to "MinGW Makefiles" should work similar to the Code::Blocks
one.

If you would like to use Visual Studio, I can't render much assistance. You 
can try and find information on how to use MinGW with VS, but the prospects
seem grim.


Other Windows Platforms
=======================

Windows versions prior to Windows 7 may not have Powershell installed by 
default. You will need to find and install the appropriate Powershell version
for your system. Then the above steps should apply as well.


Linux - Cross Compiling for Windows
===================================

The setup.sh script takes one argument, the path to the build environment 
installation directory. It defaults to /opt/mingw-w64.

Once done, you can let CMake know about the toolchain like this:

    cmake -DCMAKE_TOOLCHAIN_FILE=/opt/mingw-w64/cmake/toolchain.cmake $SRC_DIR


Linux - Native Build
====================

Coming soon. Although, if you would like to do this, you probably already 
know how. A few quick pointers:

* Ogre is required, ideally version 1.8+. An older version from your package 
  manager might work, it might not.

* Use the latest GCC you can get your hands on, ideally 4.8.

