#!/bin/bash

. utils.sh

MINGW_ENV=$1

BUILD_TYPE=Debug

WORKING_DIR=$MINGW_ENV/temp/ogre

REMOTE_DIR="http://downloads.sourceforge.net/project/ogre/ogre/1.8/1.8.1"

ARCHIVE="ogre_src_v1-8-1.exe"

TOOLCHAIN_FILE=$MINGW_ENV/cmake/toolchain.cmake

OGRE_SRC_DIR=$WORKING_DIR/ogre_src_v1-8-1

mkdir -p $WORKING_DIR

################################################################################
# Download
################################################################################

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "OGRE archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading OGRE"
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading OGRE archive. Aborting."
        rm $WORKING_DIR/$ARCHIVE
        exit 1
    fi
fi


################################################################################
# Unpack, compile, install
################################################################################

# untar the sources
7za x -y -o$WORKING_DIR $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Could not unpack OGRE If the archive is corrupted, delete 

       $WORKING_DIR/$ARCHIVE 

to force a new download.
    "
fi

mkdir -p $WORKING_DIR/build
cd $WORKING_DIR/build
cmake \
    -DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE \
    -DOGRE_DEPENDENCIES_DIR=$MINGW_ENV/install \
    -DCMAKE_INSTALL_PREFIX=$MINGW_ENV/OgreSDK \
    -DOGRE_CONFIG_THREADS=1 \
    -DOGRE_USE_BOOST=ON \
    -DCMAKE_BUILD_TYPE=$BUILD_TYPE \
    -DOGRE_BUILD_RENDERSYSTEM_D3D9=OFF \
    -DDirectX_DXERR_LIBRARY=$MINGW_ENV/x86_64-mingw32/lib/libdxerr9.a \
    $WORKING_DIR/ogre_src_v1-8-1
make -j4 && make install

# Fix case sensitive directory names
rsync -a $MINGW_ENV/OgreSDK/lib/$BUILD_TYPE/* $MINGW_ENV/OgreSDK/lib/`echo ${BUILD_TYPE,,}`
rsync -a $MINGW_ENV/OgreSDK/bin/$BUILD_TYPE/* $MINGW_ENV/OgreSDK/bin/`echo ${BUILD_TYPE,,}`

