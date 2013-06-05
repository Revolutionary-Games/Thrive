#!/bin/bash

. utils.sh

MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/ogg

REMOTE_DIR="http://downloads.xiph.org/releases/ogg"

ARCHIVE="libogg-1.3.1.tar.gz"

TOOLCHAIN_FILE=$MINGW_ENV/cmake/toolchain.cmake

DIR=`getScriptDirectory`

CMAKE_LISTS=$DIR/CMakeLists.txt

mkdir -p $WORKING_DIR

################################################################################
# Download
################################################################################

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "Ogg archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading Ogg"
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading Ogg archive. Aborting."
        rm $WORKING_DIR/$ARCHIVE
        exit 1
    fi
fi


################################################################################
# Unpack, compile, install
################################################################################

# untar the sources
tar --directory $WORKING_DIR -xf $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Could not unpack Ogg. If the archive is corrupted, delete 

       $WORKING_DIR/$ARCHIVE 

to force a new download.
    "
fi

mkdir -p $WORKING_DIR/build
cd $WORKING_DIR/build
cmake \
    -DCMAKE_TOOLCHAIN_FILE=$MINGW_ENV/cmake/toolchain.cmake \
    -DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install \
    -DOGG_SRC_DIR=$WORKING_DIR/libogg-1.3.1 \
    $DIR
make -j4 && make install


