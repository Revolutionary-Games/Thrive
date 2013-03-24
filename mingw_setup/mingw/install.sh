#!/bin/bash

# Import utilities
. utils.sh

MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/mingw

mkdir -p $WORKING_DIR

################################################################################
# Check for 64 or 32 bit environment
################################################################################
ARCH=`uname -m`

case "$ARCH" in
    "x86_64")
        IS_64_ENV=true
    ;;
    "i686")
        IS_64_ENV=false
    ;;
    *)
        echo "Unknown architecture \"$ARCH\", aborting"
        exit 1
    ;;
esac

################################################################################
# Windows or Linux?
################################################################################
KERNEL=`uname -s`

IS_LINUX=false
IS_WINDOWS=false

case "$KERNEL" in
    "Linux")
        IS_LINUX=true
    ;;
    "*MSYS*")
        IS_WINDOWS=true
    ;;
esac


################################################################################
# Check for necessary commands
################################################################################
if [[ $IS_LINUX && ! `commandExists tar` ]]; then
    echo "No suitable command to unpack archive found. Aborting."
    exit 1
fi

if [[ $IS_WINDOWS && ! `commandExists 7z` ]]; then
    echo "Please install 7zip and try again."
    exit 1
fi

################################################################################
# Prepare mingw-w64
################################################################################

# Download mingw
if [[ IS_LINUX && IS_64_ENV ]]; then
    OS="linux64"
    ARCHIVE_FORMAT="tar.xz"
    UNPACK_CMD="tar --directory $WORKING_DIR -xf"
elif IS_WINDOWS; then
    OS="win32"
    ARCHIVE_FORMAT="7z"
    UNPACK_CMD="7za x -o$WORKING_DIR"
else
    echo "I don't know which mingw archive to download for your system. Aborting."
    exit 1
fi

REMOTE_DIR="http://downloads.sourceforge.net/project/mingw-w64/Toolchains%20targetting%20Win32/Personal%20Builds/rubenvb/gcc-4.8-dw2-release"
ARCHIVE="i686-w64-mingw32-gcc-dw2-4.8.0-${OS}_rubenvb.${ARCHIVE_FORMAT}"

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
$UNPACK_CMD $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Error unpacking archive. Try deleting $WORKING_DIR/$ARCHIVE and redownloading."
    exit 1
fi
rsync -avh $WORKING_DIR/mingw32-dw2/* $MINGW_ENV/

# Create install dir
mkdir -p $MINGW_ENV/install

