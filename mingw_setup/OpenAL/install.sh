#!/bin/bash

. utils.sh

MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/openAl

REMOTE_DIR="http://kcat.strangesoft.net/openal-releases"

ARCHIVE="openal-soft-1.15.1.tar.bz2"

TOOLCHAIN_FILE=$MINGW_ENV/cmake/toolchain.cmake

mkdir -p $WORKING_DIR

################################################################################
# Download
################################################################################

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "OpenAL archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading OpenAL"
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
    echo "Could not unpack OpenAL If the archive is corrupted, delete 

       $WORKING_DIR/$ARCHIVE 

to force a new download.
    "
fi

# untar the sources again
ARCHIVE="openal-soft-1.15.1.tar"
7za x -y -o$WORKING_DIR $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Could not unpack OpenAL If the archive is corrupted, delete 

       $WORKING_DIR/$ARCHIVE 

to force a new download.
    "
fi

mkdir -p $WORKING_DIR/build
cd $WORKING_DIR/build
cmake \
    -DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE \
    -DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install\
    $WORKING_DIR/openal-soft-1.15.1
make -j4 && make install


