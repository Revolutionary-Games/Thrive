#!/bin/bash

. utils.sh

MINGW_ENV=$1
make_absolute MINGW_ENV

WORKING_DIR=$MINGW_ENV/temp/boost

REMOTE_DIR="http://downloads.sourceforge.net/project/boost/boost/1.51.0"

ARCHIVE="boost_1_51_0.tar.gz"

mkdir -p $WORKING_DIR

################################################################################
# Download
################################################################################

if [ -e $WORKING_DIR/$ARCHIVE ]; then
    echo "Boost archive file found, skipping download. If you want to redownload it, please delete ${WORKING_DIR}/${ARCHIVE}."
else
    echo "Downloading Boost"
    wget -O $WORKING_DIR/$ARCHIVE $REMOTE_DIR/$ARCHIVE
    if [ $? -gt 0 ]; then
        echo "Error downloading Boost archive. Aborting."
        rm $WORKING_DIR/$ARCHIVE
        exit 1
    fi
fi

################################################################################
# Unpack, compile, install
################################################################################

# untar the boost sources
# tar -x --directory=$WORKING_DIR -f $WORKING_DIR/$ARCHIVE
if [ $? -ne 0 ]; then
    echo "Could not unpack boost. If the archive is corrupted, delete \n \
    \n \
    \t $WORKING_DIR/$ARCHIVE \n \
    \n \
    to force a new download.
    "
fi
cd $WORKING_DIR/boost_1_51_0/

# build the bjam program provided with Boost
if $IS_LINUX; then
    ./bootstrap.sh --without-icu
    BIN_EXTENSION=""
    MINGW_BIN_DIR=$MINGW_ENV/bin/
elif $IS_WINDOWS; then
    BIN_EXTENSION=".exe"
    MINGW_BIN_DIR=`posixToWinPath $MINGW_ENV/bin/ | sed 's/\\\\/\\\\\\\\/g'`
    echo "Dir: $MINGW_BIN_DIR"
    ./bootstrap.sh --with-toolset=mingw --without-icu
    sed -e s/gcc/mingw/ project-config.jam > project-config.jam
fi

echo "using gcc : 4.8 : ${MINGW_BIN_DIR}i686-w64-mingw32-g++$BIN_EXTENSION
        :
        <rc>${MINGW_BIN_DIR}i686-w64-mingw32-windres$BIN_EXTENSION
        <archiver>${MINGW_BIN_DIR}i686-w64-mingw32-ar$BIN_EXTENSION
;" > $WORKING_DIR/user-config.jam

# build boost
./bjam address-model=32 toolset=gcc target-os=windows variant=release threading=multi threadapi=win32\
    link=shared runtime-link=shared --prefix=$MINGW_ENV/install --user-config=$WORKING_DIR/user-config.jam -j 2\
    --without-mpi --without-python -sNO_BZIP2=1 -sNO_ZLIB=1 --layout=tagged install

cd -
