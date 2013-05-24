#!/bin/bash

# Import utilities
. utils.sh


MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/irrKlang

mkdir -p $WORKING_DIR


################################################################################
# Check for necessary commands
################################################################################
if [[ ! `commandExists tar` ]]; then
    echo "No suitable command to unpack archive found. Aborting."
    exit 1
fi

################################################################################
# Download irrKlang headers
################################################################################

# Download irrKlang
REMOTE_DIR="http://www.ambiera.at/downloads"
ARCHIVE="irrKlang-1.4.0b.zip"

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "Archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading irKlang..."
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading mingw archive. Aborting."
        rm $WORKING_DIR/$ARCHIVE
        exit 1
    fi
fi

# Unpack mingw
7za x -y  -o$WORKING_DIR $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Error unpacking archive. Try deleting $WORKING_DIR/$ARCHIVE and redownloading."
    exit 1
fi

#Copy libraries
rsync -avh $WORKING_DIR/irrKlang-1.4.0/include/* $MINGW_ENV/install/include

################################################################################
# Download irrKlang compiled libraries for gcc4.7
################################################################################

# Download irrKlang
REMOTE_DIR="http://www.ambiera.at/downloads"
ARCHIVE="irrklang-1.4.0-gcc4.7.zip"

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "Archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading irKlang..."
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading mingw archive. Aborting."
        rm $WORKING_DIR/$ARCHIVE
        exit 1
    fi
fi

# Unpack mingw
7za x -y  -o$WORKING_DIR $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Error unpacking archive. Try deleting $WORKING_DIR/$ARCHIVE and redownloading."
    exit 1
fi


rsync -avh $WORKING_DIR/irrklang-1.4.0-gcc4.7/lib/Win32-gcc/* $MINGW_ENV/install/lib
rsync -avh $WORKING_DIR/irrklang-1.4.0-gcc4.7/bin/win32-gcc-4.7/*.dll $MINGW_ENV/install/lib

