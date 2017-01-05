param(
    [string]$MINGW_ENV
)

##########
# FFMPEG #
##########

Write-Output "--- Installing FFMPEG ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")

############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\ffmpeg

mkdir $WORKING_DIR -force | out-null

###################
# Check for 7-Zip #
###################

$7z = Join-Path $MINGW_ENV "temp\7zip\7za.exe"
if (-Not (Get-Command $7z -errorAction SilentlyContinue))
{
    return $false
}

####################
# Download archive #
####################


#$REMOTE_DIR="https://github.com/Revolutionary-Games/ogre-ffmpeg-videoplayer/archive"
#
#$LIB_NAME="master"
#
#$ARCHIVE=$LIB_NAME + ".zip"
#
#$OGRE_FFMPEG_DESTINATION = Join-Path $WORKING_DIR $LIB_NAME
#$OGRE_FFMPEG_DESTINATION_FILE = $ARCHIVE
#if (-Not (Test-Path $OGRE_FFMPEG_DESTINATION_FILE)) {
#    Write-Output "Downloading archive..."
#    $CLIENT = New-Object System.Net.WebClient
#    $CLIENT.DownloadFile("$REMOTE_DIR/$ARCHIVE", $OGRE_FFMPEG_DESTINATION_FILE)
#}
#else {
#    Write-Output "Found archive file, skipping download."
#}

#Download FFMPEG dependency libraries
$FFMPEG_DEV_DESTINATION = Join-Path $WORKING_DIR "ffmpeg-20161230-6993bb4-win32-dev"
$FFMPEG_DEV_DESTINATION_FILE = $FFMPEG_DEV_DESTINATION + ".zip"
if (-Not (Test-Path $FFMPEG_DEV_DESTINATION_FILE)) {
    Write-Output "Downloading archive..."
    $CLIENT = New-Object System.Net.WebClient
    $CLIENT.DownloadFile("https://ffmpeg.zeranoe.com/builds/win32/dev/ffmpeg-20161230-6993bb4-win32-dev.zip", $FFMPEG_DEV_DESTINATION_FILE)
}
else {
    Write-Output "Found archive file, skipping download."
}
#Download FFMPEG dependency binaries
$FFMPEG_BIN_DESTINATION = Join-Path $WORKING_DIR "ffmpeg-20161230-6993bb4-win32-shared"
$FFMPEG_BIN_DESTINATION_FILE = $FFMPEG_BIN_DESTINATION + ".zip"
if (-Not (Test-Path $FFMPEG_BIN_DESTINATION_FILE)) {
    Write-Output "Downloading archive..."
    $CLIENT = New-Object System.Net.WebClient
    $CLIENT.DownloadFile("https://ffmpeg.zeranoe.com/builds/win32/shared/ffmpeg-20161230-6993bb4-win32-shared.zip", $FFMPEG_BIN_DESTINATION_FILE)
}
else {
    Write-Output "Found archive file, skipping download."
}




##########
# Unpack #
##########

Write-Output "Unpacking archive..."


$ARGUMENTS = "x",
             "-y",
             "-o$WORKING_DIR",
             $FFMPEG_DEV_DESTINATION_FILE
             
& $7z $ARGUMENTS | out-null

$ARGUMENTS = "x",
             "-y",
             "-o$WORKING_DIR",
             $FFMPEG_BIN_DESTINATION_FILE
             
& $7z $ARGUMENTS | out-null

###########
# Compile #
###########

Write-Output "Compiling..."

#$env:Path += (Join-Path $MINGW_ENV bin) + ";"
#
#$TOOLCHAIN_FILE="$MINGW_ENV/cmake/toolchain.cmake"
#
#$BUILD_TYPES = @("Debug", "Release")
#
#foreach ($BUILD_TYPE in $BUILD_TYPES) {
#
#    $BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"
#
#    mkdir $BUILD_DIR -force
#
#    pushd $BUILD_DIR
#
#    $ARGUMENTS =
#        "-DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE",
#        "-DOGRE_HOME:path=$MINGW_ENV/OgreSDK",
#        "-DFFMPEG_LIBRARIES=$FFMPEG_DEV_DESTINATION/lib",
#        "-DFFMPEG_INCLUDE_DIRS=$FFMPEG_DEV_DESTINATION/include",
#        "-DAVCODEC_LIBRARIES=$FFMPEG_DEV_DESTINATION/lib",
#        "-DAVCODEC_INCLUDE_DIRS=$FFMPEG_DEV_DESTINATION/include",
#        "-DAVFORMAT_LIBRARIES=$FFMPEG_DEV_DESTINATION/lib",
#        "-DAVFORMAT_INCLUDE_DIRS=$FFMPEG_DEV_DESTINATION/include",
#        "-DAVUTIL_LIBRARIES=$FFMPEG_DEV_DESTINATION/lib",
#        "-DAVUTIL_INCLUDE_DIRS=$FFMPEG_DEV_DESTINATION/include",
#        "-DSWSCALE_LIBRARIES=$FFMPEG_DEV_DESTINATION/lib",
#        "-DSWSCALE_INCLUDE_DIRS=$FFMPEG_DEV_DESTINATION/include",
#        "-DSWRESAMPLE_LIBRARIES=$FFMPEG_DEV_DESTINATION/lib",
#        "-DSWRESAMPLE_INCLUDE_DIRS=$FFMPEG_DEV_DESTINATION/include",
#        "-DCMAKE_CXX_FLAGS:string=-mstackrealign -msse2",
#        "-DBUILD_VIDEOPLAYER_DEMO=OFF",
#        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",
#        "$WORKING_DIR/ogre-ffmpeg-videoplayer-master"
#
#    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "CodeBlocks - MinGW Makefiles" $ARGUMENTS
#
#    & $MINGW_ENV/bin/mingw32-make -j4 all | Tee-Object -FilePath compileroutput.txt
#
#    Copy-Item "$BUILD_DIR/libogre-ffmpeg-videoplayer.a" -destination "$MINGW_ENV/install/lib/$BUILD_TYPE/libogre-ffmpeg-videoplayer.a" -Recurse -Force
#    popd
#
#}
Copy-Item "$FFMPEG_DEV_DESTINATION/lib/libavcodec.dll.a" -destination "$MINGW_ENV/install/lib/libavcodec.dll.a" -Recurse -Force
Copy-Item "$FFMPEG_DEV_DESTINATION/lib/libavformat.dll.a" -destination "$MINGW_ENV/install/lib/libavformat.dll.a" -Recurse -Force
Copy-Item "$FFMPEG_DEV_DESTINATION/lib/libavutil.dll.a" -destination "$MINGW_ENV/install/lib/libavutil.dll.a" -Recurse -Force
Copy-Item "$FFMPEG_DEV_DESTINATION/lib/libswscale.dll.a" -destination "$MINGW_ENV/install/lib/libswscale.dll.a" -Recurse -Force
Copy-Item "$FFMPEG_DEV_DESTINATION/lib/libswresample.dll.a" -destination "$MINGW_ENV/install/lib/libswresample.dll.a" -Recurse -Force
Copy-Item "$FFMPEG_BIN_DESTINATION/bin/avcodec-57.dll" -destination "$MINGW_ENV/install/bin/avcodec-57.dll" -Recurse -Force
Copy-Item "$FFMPEG_BIN_DESTINATION/bin/avformat-57.dll" -destination "$MINGW_ENV/install/bin/avformat-57.dll" -Recurse -Force
Copy-Item "$FFMPEG_BIN_DESTINATION/bin/avutil-55.dll" -destination "$MINGW_ENV/install/bin/avutil-55.dll" -Recurse -Force
Copy-Item "$FFMPEG_BIN_DESTINATION/bin/swscale-4.dll" -destination "$MINGW_ENV/install/bin/swscale-4.dll" -Recurse -Force
Copy-Item "$FFMPEG_BIN_DESTINATION/bin/swresample-2.dll" -destination "$MINGW_ENV/install/bin/swresample-2.dll" -Recurse -Force


Copy-Item "$FFMPEG_DEV_DESTINATION/include/*" "$MINGW_ENV/install/include/" -Recurse -Force

