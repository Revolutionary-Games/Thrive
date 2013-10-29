#!/bin/bash

. utils.sh

MINGW_ENV=$1

WORKING_DIR=$MINGW_ENV/temp/ogre_dependencies

BUILD_TYPES=("Debug" "Release")

REPOSITORY_NAME="ali1234"

REMOTE_DIR="https://bitbucket.org/${REPOSITORY_NAME}/ogredeps/get"

COMMIT_ID="4e9b0c98f4c3"

ARCHIVE="${COMMIT_ID}.zip"

ARCHIVE_TOP_LEVEL_DIR="${REPOSITORY_NAME}-ogredeps-${COMMIT_ID}"

TOOLCHAIN_FILE=$MINGW_ENV/cmake/toolchain.cmake

mkdir -p $WORKING_DIR

################################################################################
# Download
################################################################################

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "OGRE dependencies archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading OGRE dependencies"
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading OGRE dependencies archive. Aborting."
        rm $WORKING_DIR/$ARCHIVE
        exit 1
    fi
fi


################################################################################
# Unpack, compile, install
################################################################################

# unzip the sources
7za x -y -o$WORKING_DIR $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Could not unpack OGRE dependencies. If the archive is corrupted, delete 

       $WORKING_DIR/$ARCHIVE 

to force a new download.
    "
fi

# Fix case sensitive cmake module path
rsync -a $WORKING_DIR/$ARCHIVE_TOP_LEVEL_DIR/cmake/* $WORKING_DIR/$ARCHIVE_TOP_LEVEL_DIR/CMake

for BUILD_TYPE in ${BUILD_TYPES[@]}
do
    BUILD_DIR=$WORKING_DIR/build-$BUILD_TYPE
    # Compile
    mkdir -p $BUILD_DIR
    cd $BUILD_DIR
    cmake \
        -DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE \
        -DCMAKE_INSTALL_PREFIX=${MINGW_ENV}/install \
        -DCMAKE_C_FLAGS=-shared-libgcc \
        -DCMAKE_BUILD_TYPE=$BUILD_TYPE \
        -DOGRE_COPY_DEPENDENCIES=OFF \
        $WORKING_DIR/$ARCHIVE_TOP_LEVEL_DIR
    make -j4 && make install

    # Fix case sensitive paths for Ogre's FindXYZ scripts
    rsync -a $MINGW_ENV/install/lib/$BUILD_TYPE/* $MINGW_ENV/install/lib/`echo ${BUILD_TYPE,,}`
    rsync -a $MINGW_ENV/install/bin/$BUILD_TYPE/* $MINGW_ENV/install/bin/`echo ${BUILD_TYPE,,}`

    # For some reason, OIS.dll is installed into bin, move it to lib
    rsync -a $MINGW_ENV/install/bin/`echo ${BUILD_TYPE,,}`/OIS* $MINGW_ENV/install/lib/`echo ${BUILD_TYPE,,}`

    # OGRE's cmake script looks for freeimage, not FreeImage (*sigh*)
    case "$BUILD_TYPE" in
        "Debug") rsync -a $MINGW_ENV/install/lib/debug/libFreeImage_d.a $MINGW_ENV/install/lib/debug/libfreeimage_d.a
        ;;
        "Release") rsync -a $MINGW_ENV/install/lib/release/libFreeImage.a $MINGW_ENV/install/lib/release/libfreeimage.a
        ;;
    esac
done
