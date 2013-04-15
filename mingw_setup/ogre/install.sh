#!/bin/bash

. utils.sh

MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/ogre

REMOTE_DIR="http://downloads.sourceforge.net/project/ogre/ogre/1.8/1.8.1"

ARCHIVE="OgreSDK_MinGW_v1-8-1.exe"

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

rsync -avh $WORKING_DIR/OgreSDK_MinGW_v1-8-1/* $MINGW_ENV/OgreSDK
