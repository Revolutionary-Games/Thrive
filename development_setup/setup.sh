#!/bin/bash

################################################################################
# Constants
################################################################################
# Find the script's directory (http://stackoverflow.com/a/246128/1184818)
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# The toolchain file
TOOLCHAIN_FILE="$DIR/toolchain.cmake"

# The default root directory of the mingw environment
DEFAULT_MINGW_ENV="/opt/mingw-w64"

################################################################################
# Libraries and Tools to install
################################################################################
LIBRARIES=('boost' 'googletest' 'ogre')

################################################################################
# Usage
################################################################################
USAGE="Usage: $(basename $0) [MINGW_ENV]

    MINGW_ENV is the directory to install mingw to and defaults to 
    $DEFAULT_MINGW_ENV
    
"

################################################################################
# Parse arguments
################################################################################
if [ $# -eq 0 ]; then
    MINGW_ENV=$DEFAULT_MINGW_ENV
elif [ $# -eq 1 ]; then
    MINGW_ENV=$1
elif [ $# -gt 1 ]; then
    echo $USAGE
    exit 1
fi

WORKING_DIR=$MINGW_ENV/temp

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
# Make paths absolute
################################################################################
make_absolute() {
    local __pathVar=$1
    eval $__pathVar=`readlink -fn ${!__pathVar}`
}

make_absolute MINGW_ENV

################################################################################
# Prepare mingw-w64
################################################################################

# Prepare working dir
mkdir -p $WORKING_DIR
if [ $? -gt 0 ]; then
    echo "Error creating directory, aborting."
    exit 1
fi

# Download mingw
if [[ IS_LINUX && IS_64_ENV ]]; then
    OS="linux64"
    MINGW_ARCHIVE_FORMAT="tar.xz"
    MINGW_UNPACK_CMD="tar --directory $WORKING_DIR -xf"
elif IS_WINDOWS; then
    OS="win32"
    MINGW_ARCHIVE_FORMAT="7z"
    MINGW_UNPACK_CMD="7z -o $WORKING_DIR -x"
fi


MINGW_REMOTE_DIR="http://downloads.sourceforge.net/project/mingw-w64/Toolchains%20targetting%20Win64/Personal%20Builds/rubenvb/gcc-4.7-release"
MINGW_ARCHIVE="x86_64-w64-mingw32-gcc-4.7.2-release-${OS}_rubenvb.${MINGW_ARCHIVE_FORMAT}"
wget -O $WORKING_DIR/$MINGW_ARCHIVE $MINGW_REMOTE_DIR/$MINGW_ARCHIVE
if [ $? -gt 0 ]; then
    echo "Error downloading mingw archive, aborting. You may want to delete $MINGW_ENV now."
    exit 1
fi

# Unpack mingw
echo "Unpacking mingw to $MINGW_ENV ..."
$MINGW_UNPACK_CMD $WORKING_DIR/$MINGW_ARCHIVE
mv $WORKING_DIR/mingw64/* $MINGW_ENV/

# Create install dir
mkdir -p $MINGW_ENV/install

# Install libraries
#for LIBRARY in ${LIBRARIES[@]}
#do
#    echo "Installing library $LIBRARY"
#    $DIR/$LIBRARY/init.sh $MINGW_ENV
#done

