param(
    [string]$MINGW_ENV
)

##########
# Vorbis #
##########

Write-Output "--- Installing Vorbis ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\Vorbis

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

$REMOTE_DIR="http://downloads.xiph.org/releases/vorbis"

$ARCHIVE="libvorbis-1.3.3.tar.gz"


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

#unpack .tar.gz
$DESTINATION = Join-Path $WORKING_DIR $ARCHIVE

Write-Output "Unpacking archive..."

$ARGUMENTS = "x",
             "-y",
             "-o$WORKING_DIR",
             $DESTINATION
             
& $7z $ARGUMENTS | out-null

#unpack .tar
$DESTINATION = Join-Path $WORKING_DIR "libvorbis-1.3.3.tar"

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

mkdir "$WORKING_DIR/build" -force

pushd "$WORKING_DIR/build"


#find the location where the script is run from (for custom cmakelists.txt)
$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

    
$ARGUMENTS =
    "-DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE",
    "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install",
    "-DVORBIS_SRC_DIR=$WORKING_DIR/libvorbis-1.3.3",
    "$PSScriptRoot"
    
   
& (Join-Path $MINGW_ENV cmake\bin\cmake) -G "MinGW Makefiles" $ARGUMENTS

& $MINGW_ENV/bin/mingw32-make -j4 install

popd
