What's this?
============

This is the setup instructions for Thrive on linux. This documentation
is written for Ubuntu and latest version of Fedora, but with slight
changes this should be usable with any linux distribution. You will
need sudo access in order to install the dependencies.

Before starting make sure that you don't have any conflicting
libraries installed. Lua and old versions of Ogre (pre 2.0) are known
to cause issues compiling or running the game. So uninstall these
before starting if possible, or you will have a bad time.


Important Note: If you run into any trouble with the setup scripts, please 
bring them up on the development slack or open a github issue.

If you are a team member you can ask help on the forums:
    [Private Developer Forums](http://forum.revolutionarygamesstudio.com/)

Thank you!


Common Requirements
========================

You will need these for any linux distribution. It is recommended that
you install these with your package manager. Additional dependencies
will be installed by the setup script.

* git
* CMake
* ruby

These ruby gems for running the setup script:

* fileutils
* colorize
* etc
* os

If in the future the script is made to support windows then also these gems are required:

* nokogiri
* win32ole

Fedora example commands
-----------------------

These commands install the required packages on Fedora.

    sudo dnf install git cmake ruby
    gem install fileutils colorize etc os


Ubuntu example commands
-----------------------

    sudo apt-get install build-essential git cmake ruby
    gem install fileutils colorize etc os


Running the Setup Script
========================

0. Clone Thrive
---------------

If you haven't already done so you need to clone the thrive
repository. Additional dependencies will be installed alongside the
thrive directory so it is recommended to clone thrive into a
subdirectory. For example: ~/projects/thrive_build/thrive First go
to [Thrive Github][thrivegh] and click the "Clone or Download" button
to get the link for clone. And replace the url in this command with it:

    git clone git@github.com:Revolutionary-Games/Thrive.git thrive
    cd thrive
    git submodule update --init --recursive
    
1. Run Setup
------------

Then go to the directory and run the setup script.

    cd thrive
    ./SetupThrive.rb
    
If you installed ruby correctly you should now be prompted for your
sudo password and then installation of extra dependencies and thrive
should begin. After the script finishes successfully you can go to the
thrive/build folder and run Thrive with ./Thrive

2. Done
-------

If the game starts successfully then congratulations you are now ready to develop Thrive!


[thrivegh]: https://github.com/Revolutionary-Games/Thrive  "Revolutionary-Games/Thrive"
