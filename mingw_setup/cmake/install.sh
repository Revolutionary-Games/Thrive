#!/bin/bash

# Import utilities
. utils.sh


MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/mingw

mkdir -p $WORKING_DIR


################################################################################
# Check for necessary commands
################################################################################
if [[ ! `commandExists tar` ]]; then
    echo "No suitable command to unpack archive found. Aborting."
    exit 1
fi

################################################################################
# Prepare mingw-w64
################################################################################

# Download mingw
REMOTE_DIR="http://downloads.sourceforge.net/project/mingw-w64/Toolchains%20targetting%20Win32/Personal%20Builds/rubenvb/gcc-4.7-release"
ARCHIVE="i686-w64-mingw32-gcc-4.7.4-release-linux64_rubenvb.tar.xz"

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "Archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading MinGW..."
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading mingw archive. Aborting."
        rm $WORKING_DIR/$ARCHIVE
        exit 1
    fi
fi

# Unpack mingw
tar --directory $WORKING_DIR -xf $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Error unpacking archive. Try deleting $WORKING_DIR/$ARCHIVE and redownloading."
    exit 1
fi
rsync -avh $WORKING_DIR/mingw32/* $MINGW_ENV/

