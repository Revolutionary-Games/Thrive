param(
    [string]$MINGW_ENV
)

#########
# cAudio #
#########

Write-Output "--- Installing cAudio ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\cAudio

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


$REMOTE_DIR="https://github.com/R4stl1n/cAudio/archive"

$LIB_NAME="5c932101891e4e63b93b03803d34342fbdb9f0a3"

$ARCHIVE=$LIB_NAME + ".zip"

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
             
& $7z $ARGUMENTS  | out-null


###########
# Compile #
###########

Write-Output "Compiling..."

$env:Path += (Join-Path $MINGW_ENV bin) + ";"

$TOOLCHAIN_FILE="$MINGW_ENV/cmake/toolchain.cmake"

#Debug fails to build if it's built first. 
#Was unable to figure out why, but doing release first works
$BUILD_TYPES = @("Release", "Debug")


foreach ($BUILD_TYPE in $BUILD_TYPES) {

    $BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"
    
    #Fetch the dependencies
    $DEPS_LOC = "$MINGW_ENV/temp/cAudio-5c932101891e4e63b93b03803d34342fbdb9f0a3/Dependencies"

    $BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"

    mkdir $BUILD_DIR -force

    pushd $BUILD_DIR

    $LIB_SRC = Join-Path $WORKING_DIR "cAudio-5c932101891e4e63b93b03803d34342fbdb9f0a3"
    
    $ARGUMENTS =
        "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install",
        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",       
		"-DCAUDIO_DEPENDENCIES_DIR:path=$MINGW_ENV/temp/cAudio/cAudio-5c932101891e4e63b93b03803d34342fbdb9f0a3/Dependencies",
        "$LIB_SRC"

    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "CodeBlocks - MinGW Makefiles" $ARGUMENTS

    & $MINGW_ENV/bin/mingw32-make -j4 install
    
    #DLL's don' seem to be copied automatically
    $INSTALL_TARGET = Join-Path $MINGW_ENV "install/bin/OpenAL32.dll" 
    $OPENAL_DLL = Join-Path $BUILD_DIR "bin/OpenAL32.dll"
    Copy-Item $OPENAL_DLL -destination $INSTALL_TARGET -Recurse -Force
    $INSTALL_TARGET = Join-Path $MINGW_ENV "install/bin/wrap_oal.dll" 
    $WRAPOAL_DLL = Join-Path $BUILD_DIR "bin/wrap_oal.dll"
    Copy-Item $WRAPOAL_DLL -destination $INSTALL_TARGET -Recurse -Force

    
    popd

}
#DLL's don' seem to be copied automatically
$INSTALL_TARGET = Join-Path $MINGW_ENV "install/bin/libcAudio.dll" 
$CAUDIO_DLL = Join-Path $WORKING_DIR "build-Release/bin/release/libcAudio.dll"
Copy-Item $CAUDIO_DLL -destination $INSTALL_TARGET -Recurse -Force
$INSTALL_TARGET = Join-Path $MINGW_ENV "install/bin/libcAudio_d.dll" 
$CAUDIO_DLL = Join-Path $WORKING_DIR "build-Debug/bin/debug/libcAudio_d.dll"
Copy-Item $CAUDIO_DLL -destination $INSTALL_TARGET -Recurse -Force