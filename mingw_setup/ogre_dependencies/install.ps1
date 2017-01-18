param(
    [string]$MINGW_ENV
)

#####################
# Ogre Dependencies #
#####################

Write-Output "--- Installing Ogre Dependencies ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\ogre_dependencies

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

$REPOSITORY_NAME="ali1234"

$REMOTE_DIR="https://bitbucket.org/$REPOSITORY_NAME/ogredeps/get"

$COMMIT_ID="4e9b0c98f4c3"

$ARCHIVE="$COMMIT_ID.zip"

$ARCHIVE_TOP_LEVEL_DIR="$REPOSITORY_NAME-ogredeps-$COMMIT_ID"

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
             
& $7z $ARGUMENTS


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

    $ARGUMENTS =
        "-DCMAKE_PROGRAM_PATH=$MINGW_ENV/bin/",
        "-DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE",
        "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install",
        "-DCMAKE_C_FLAGS=-shared-libgcc",
        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",
        "-DOGRE_COPY_DEPENDENCIES=OFF",
        "$WORKING_DIR/$ARCHIVE_TOP_LEVEL_DIR"

    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "MinGW Makefiles" $ARGUMENTS

    & $MINGW_ENV/bin/mingw32-make -j4 install

    popd

}


