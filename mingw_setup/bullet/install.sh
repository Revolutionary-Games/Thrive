#!/bin/bash

. utils.sh

MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/bullet

REMOTE_DIR="https://bullet.googlecode.com/files"

ARCHIVE="bullet-2.81-rev2613.zip"

TOOLCHAIN_FILE=$MINGW_ENV/cmake/toolchain.cmake

mkdir -p $WORKING_DIR

################################################################################
# Download
################################################################################

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "Bullet archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading Bullet"
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading Bullet archive. Aborting."
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
    echo "Could not unpack Bullet If the archive is corrupted, delete 

       $WORKING_DIR/$ARCHIVE 

to force a new download.
    "
fi

mkdir -p $WORKING_DIR/build
cd $WORKING_DIR/build
cmake \
    -DCMAKE_TOOLCHAIN_FILE=$MINGW_ENV/cmake/toolchain.cmake \
    -DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install \
    -DBUILD_CPU_DEMOS=OFF \
    -DBUILD_DEMOS=OFF \
    -DBUILD_EXTRAS=OFF \
    -DUSE_GLUT=OFF \
    -DINSTALL_LIBS=ON \
    $WORKING_DIR/bullet-2.81-rev2613
make -j4 && make install


