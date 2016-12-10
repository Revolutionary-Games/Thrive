param(
    [string]$MINGW_ENV
)

########
# Ogre #
########

Write-Output "--- Installing Ogre SDK ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\ogre

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


$REPOSITORY_NAME="sinbad"

$REMOTE_DIR="https://bitbucket.org/sinbad/ogre/get"
             
            
$COMMIT_ID="9fba1c8ac0be"

$ARCHIVE="$COMMIT_ID.zip"

$ARCHIVE_TOP_LEVEL_DIR="$REPOSITORY_NAME-ogre-$COMMIT_ID"

$DESTINATION = Join-Path $WORKING_DIR $ARCHIVE

if (-Not (Test-Path $DESTINATION)) {
    Write-Output "Downloading archive..."
    $CLIENT = New-Object System.Net.WebClient
    $CLIENT.DownloadFile("$REMOTE_DIR/$ARCHIVE", $DESTINATION)
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
             $DESTINATION
             
& $7z $ARGUMENTS | out-null


###########
# Compile #
###########

Write-Output "Compiling..."

$env:Path += (Join-Path $MINGW_ENV bin) + ";"

$TOOLCHAIN_FILE="$MINGW_ENV/cmake/toolchain.cmake"

$BUILD_TYPES = @("Debug", "Release")

foreach ($BUILD_TYPE in $BUILD_TYPES) {

    $BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"

    mkdir $BUILD_DIR -force

    pushd $BUILD_DIR
#-mstackrealign is needed as sse2 assumes stack is aligned but gcc doesn't allign stack by default.
#this flag can cause some performance issues in which case alternative solutions are possible
#See http://www.peterstock.co.uk/games/mingw_sse/
    $ARGUMENTS =
        "-DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE",
        "-DOGRE_DEPENDENCIES_DIR=$MINGW_ENV/install",
        "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/OgreSDK",
        "-DOGRE_THREAD_PROVIDER=1",
        "-DOGRE_CONFIG_THREADS=1",
        "-DOGRE_USE_BOOST=ON",
        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",
        "-DOGRE_BUILD_RENDERSYSTEM_D3D9=OFF",
        "-DOGRE_BUILD_RENDERSYSTEM_D3D11=OFF",
        "-DOGRE_BUILD_SAMPLES=OFF",
        "-DOGRE_BUILD_TOOLS=OFF",
        "-DCMAKE_CXX_FLAGS:string=-mstackrealign -msse2 -std=c++0x",
        "-DDirectX_DXERR_LIBRARY=$MINGW_ENV/x86_64-mingw32/lib/libdxerr9.a",
        "$WORKING_DIR/sinbad-ogre-$COMMIT_ID"

    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "MinGW Makefiles" $ARGUMENTS

    & $MINGW_ENV/bin/mingw32-make -j4 install | Tee-Object -FilePath compileroutput.txt

    popd

}


