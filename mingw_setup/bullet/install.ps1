param(
    [string]$MINGW_ENV
)

##########
# Bullet #
##########

Write-Output "--- Installing Bullet ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\bullet

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


$REMOTE_DIR="https://github.com/bulletphysics/bullet3/archive/"

$LIB_NAME="619cfa2f15"
$EXTRACTED_NAME = "bullet3-619cfa2f1542d33bcd83e204ccc3747f5ec24f0a"
$ARCHIVE=$LIB_NAME + ".zip"

$DESTINATION = Join-Path $WORKING_DIR $ARCHIVE



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

#Release causes this gcc bug:https://github.com/bulletphysics/bullet3/issues/312  http://sourceforge.net/p/tdm-gcc/bugs/232/
#$BUILD_TYPES = @("Release", "Debug")
$BUILD_TYPES = @("Debug")

foreach ($BUILD_TYPE in $BUILD_TYPES) {

    $BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"

    mkdir $BUILD_DIR -force

    pushd $BUILD_DIR

    $ARGUMENTS =
        "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install",
        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",
        "-DBUILD_MULTITHREADING=ON",
        "-DBUILD_CPU_DEMOS=OFF",
        "-DBUILD_DEMOS=OFF",
        "-DBUILD_EXTRAS=OFF",
        "-DUSE_GLUT=OFF",
        "-DCMAKE_CXX_FLAGS:string=-msse2",
        "-DINSTALL_LIBS=ON",
        "$WORKING_DIR/$EXTRACTED_NAME"

    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "MinGW Makefiles" $ARGUMENTS

    & $MINGW_ENV/bin/mingw32-make -j4 install

    popd

}


