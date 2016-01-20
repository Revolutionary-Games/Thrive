#!/bin/sh

# detect if running from thrive folder
InsideFolder=${PWD##*/}
OriginalFolder=$(pwd)

if [ -f ./thriveversion.ver -o "$InsideFolder" = "thrive" ]; then

    # Running from thrive folder
    StartingDirectory=$(dirname "$(pwd)")
    
else
    
    StartingDirectory=$(pwd)
fi


# Variable setup

OS=$(lsb_release -si)


THREADS=1
DEBUG=0
VERBOSE=0

THRIVE_BRANCH="master"

# Parse arguments
getopt --test > /dev/null
if [ $? != 4 ]; then
    echo "I’m sorry, 'getopt --test' failed in this environment."
    exit 1
fi

SHORT=dfj:t:v
LONG=debug,force,threads:,verbose

PARSED=$(getopt --options $SHORT --longoptions $LONG --name "$0" -- "$@")
if [ $? != 0 ]; then
    exit 2
fi
eval set -- "$PARSED"

while true; do
    case "$1" in
        -d|--debug)
            DEBUG=1
            shift
            ;;
        -v|--verbose)
            VERBOSE=1
            shift
            ;;
        -j|-t|--threads)
            THREADS="$2"
            shift 2
            ;;
        --)
            shift
            break
            ;;
        *)
            echo "Programming error"
            exit 3
            ;;
    esac
done

echo "Using $THREADS threads to compile"
echo "Running in folder $StartingDirectory"
cd "$StartingDirectory"

MakeArgs="-j $THREADS"

if [ $DEBUG = 1 -o $VERBOSE = 1 ]; then

    MakeArgs="$MakeArgs -d"
    
fi

PackageManager="dnf install -y "
PackagesToInstall="bullet-devel boost gcc-c++ libXaw-devel freetype-devel freeimage-devel \
 zziplib-devel boost-devel ois-devel tinyxml-devel glm-devel ffmpeg-devel ffmpeg-libs openal-soft-devel"
CommonPackages="cmake make git mercurial svn"

if [ "$OS" = "Fedora" ]; then

    echo "Creating CEGUI project folder for $OS"
   
elif [ "$OS" = "Ubuntu" ]; then

    PackageManager="apt-get install -y "
         
    PackagesToInstall="bullet-dev boost-dev build-essential automake libtool libfreetype6-dev \
 libfreeimage-dev libzzip-dev libxrandr-dev libxaw7-dev freeglut3-dev libgl1-mesa-dev \
 libglu1-mesa-dev libois-dev libboost-thread-dev tinyxml-dev glm-dev ffmpeg-dev libavutil-dev libopenal-dev"
else
         
    echo "Unkown linux OS \"$OS\""
    exit 2
fi

PackagesToInstall="$PackagesToInstall $CommonPackages"

echo "Installing prerequisite libraries, be prepared to type password for sudo"
eval "sudo $PackageManager $PackagesToInstall"

if [ $? -eq 0 ]; then
    echo "Prerequisites installed successfully"
else
    echo "Package manager failed to install required packages, install \"$PackagesToInstall\" manually"
    exit 1
fi

echo "Cloning repositories and creating build files..."

echo "Ogre..."

mkdir -p ogreBuild
cd ogreBuild

# Main repo
if [ -d ogre ]; then

    cd ogre
    hg pull
    
else
    hg clone https://bitbucket.org/sinbad/ogre ogre
    cd ogre
fi

hg update v2-0

cd ..

# TODO: allow using this if wanted
# Dependencies repo


# Build file
mkdir -p build

echo "cmake_minimum_required(VERSION 2.8.11)

# Let's try forcing static FreeImage

set(FreeImage_USE_STATIC_LIBS TRUE) 

# depencies must be first 
#add_subdirectory(ogredeps)

# actual ogre
add_subdirectory(ogre)" > CMakeLists.txt


# Run cmake
cd build
cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DOGRE_BUILD_RENDERSYSTEM_GL3PLUS=ON -DOGRE_BUILD_COMPONENT_OVERLAY=OFF -DOGRE_BUILD_COMPONENT_PAGING=OFF -DOGRE_BUILD_COMPONENT_PROPERTY=OFF -DOGRE_BUILD_COMPONENT_TERRAIN=OFF -DOGRE_BUILD_COMPONENT_VOLUME=OFF -DOGRE_BUILD_PLUGIN_BSP=OFF -DOGRE_BUILD_PLUGIN_CG=OFF -DOGRE_BUILD_PLUGIN_OCTREE=OFF -DOGRE_BUILD_PLUGIN_PCZ=OFF -DOGRE_BUILD_SAMPLES=OFF

cd "$StartingDirectory"
echo "Done"

echo "CEGUI..."

if [ -d cegui ]; then

    cd cegui
    hg pull
    
else
    hg clone https://bitbucket.org/cegui/cegui cegui
    cd cegui
fi

hg update default

mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DCEGUI_BUILD_APPLICATION_TEMPLATES=OFF -DCEGUI_BUILD_PYTHON_MODULES=OFF -DCEGUI_SAMPLES_ENABLED=OFF

cd "$StartingDirectory"

echo "Done"

echo "OgreFFMPEG"

if [ -d ogre-ffmpeg-videoplayer ]; then

    cd ogre-ffmpeg-videoplayer
    git checkout master
    git pull origin master
    
else

    #Official repo
    #git clone https://github.com/scrawl/ogre-ffmpeg-videoplayer.git ogre-ffmpeg-videoplayer
    # Currently working hhyyrylainen's fork
    git clone https://github.com/hhyyrylainen/ogre-ffmpeg-videoplayer.git ogre-ffmpeg-videoplayer
    cd ogre-ffmpeg-videoplayer
fi


mkdir -p build
cd build
cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DBUILD_VIDEOPLAYER_DEMO=OFF
cd "$StartingDirectory"

echo "Done"


echo "cAudio"

if [ -d cAudio ]; then

    cd cAudio
    #Workaround for broken latest version
    #git checkout master
    #git pull origin master

    
else

    git clone https://github.com/wildicv/cAudio.git
    cd cAudio
fi

#Workaround for broken build with latest version
git checkout 22ff1a97a9a820c72726463708590adfae77008c

mkdir -p build
cd build
cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo


echo "Done"


echo "Compiling. This may take a long time!"

echo "Ogre..."

cd "$StartingDirectory/ogreBuild/build"
eval "make $MakeArgs"


echo "Done"

echo "CEGUI..."

cd "$StartingDirectory/cegui/build"
eval "make $MakeArgs"

echo "Done"

echo "OgreFFMPEG..."

cd "$StartingDirectory/ogre-ffmpeg-videoplayer/build"
eval "make $MakeArgs"

echo "Done"

echo "cAudio..."

cd "$StartingDirectory/cAudio/build"
eval "make $MakeArgs"

echo "Done"


echo "Installing dependencies"

echo "Installing Ogre, prepare for sudo password"
cd "$StartingDirectory/ogreBuild/build"
sudo make install

echo "Installing CEGUI, prepare for sudo password"
cd "$StartingDirectory/cegui/build"
sudo make install

echo "Installing OgreFFMPEG, prepare for sudo password"
cd "$StartingDirectory/ogre-ffmpeg-videoplayer/build"
sudo make install

echo "Installing cAudio, prepare for sudo password"
cd "$StartingDirectory/cAudio/build"
sudo make install

echo "Done"


echo "Setting up Thrive"
cd "$StartingDirectory"

echo "Getting code"

if [ -d thrive ]; then

    cd thrive
    
else

    git clone https://github.com/Revolutionary-Games/Thrive.git thrive
    cd thrive
    git submodule update --init --recursive
fi

git checkout $THRIVE_BRANCH
git pull --recurse-submodules origin $THRIVE_BRANCH
git submodule update --recursive

echo "Getting assets"

if [ -d assets ]; then
    (
        cd assets
        svn up
    )   
else

    svn checkout http://crovea.net/svn/thrive_assets/ assets
fi

echo "Making all the links"
ln -sf assets/cegui_examples cegui_examples
ln -sf assets/definitions definitions
ln -sf assets/fonts fonts
ln -sf assets/gui gui
ln -sf assets/materials materials
ln -sf assets/models models
ln -sf assets/sounds sounds

echo "Copying Ogre resources file"
cp ogre_cfg/resources.cfg build/resources.cfg

echo "Copying complelety pointless Ogre files"

cp /usr/local/share/OGRE/plugins.cfg build/plugins.cfg



echo "Compiling Thrive"
mkdir -p build
cd build
cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DCMAKE_EXPORT_COMPILE_COMMANDS=ON
eval "make $MakeArgs"

echo "."
echo "."
echo "."

cd "$StartingDirectory"

echo "Done, run the game with '$StartingDirectory/thrive/build/Thrive'"
cd "$OriginalFolder"

