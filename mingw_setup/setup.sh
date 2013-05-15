#!/bin/bash

. utils.sh

################################################################################
# Constants
################################################################################
# The script's directory
DIR=`getScriptDirectory`

# The toolchain template
TOOLCHAIN_TEMPLATE="$DIR/toolchain_linux.cmake.in"

# The default root directory of the mingw environment
DEFAULT_MINGW_ENV="/opt/mingw"

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

################################################################################
# Make paths absolute
################################################################################
make_absolute MINGW_ENV

################################################################################
# Prepare directories
################################################################################

mkdir -p $MINGW_ENV
mkdir -p $MINGW_ENV/install
mkdir -p $MINGW_ENV/cmake


################################################################################
# Configure CMake toolchain file
################################################################################
cmake -DTOOLCHAIN_TEMPLATE=$TOOLCHAIN_TEMPLATE -DMINGW_ENV=$MINGW_ENV -P $DIR/configure_toolchain.cmake

################################################################################
# Install libraries
################################################################################
LIBRARIES=('mingw' 'boost' 'ogre_dependencies' 'ogre' 'bullet' 'irrKlang')

for LIBRARY in ${LIBRARIES[@]}
do
    $DIR/$LIBRARY/install.sh $MINGW_ENV
    if [ $? -ne 0 ]; then
        exit 1
    fi
done
