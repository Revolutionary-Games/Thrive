param(
    [string]$MINGW_ENV
)

#########
# TinyXML2 #
#########

Write-Output "--- Installing TinyXML2 ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\TinyXML

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

$REMOTE_DIR="http://downloads.sourceforge.net/project/tinyxml/tinyxml/2.6.2"

$LIB_NAME = "tinyxml_2_6_2"

$ARCHIVE=  $LIB_NAME + ".tar.gz"

Write-Output $ARCHIVE

$DESTINATION = Join-Path $WORKING_DIR $ARCHIVE

if (-Not (Test-Path $DESTINATION)) {
    Write-Output "Downloading archive..."
    $CLIENT = New-Object System.Net.WebClient
    $CLIENT.Credentials = New-Object System.Net.NetworkCredential 'thrivebuildsystem', 'autoevo1'
    $CLIENT.DownloadFile("$REMOTE_DIR/$ARCHIVE", $DESTINATION)
}
else {
    Write-Output "Found archive file, skipping download."
}

##########
# Unpack #
##########

Write-Output "Unpacking archive..."

$DESTINATION = (Join-Path $WORKING_DIR $LIB_NAME) + ".tar.gz"

$ARGUMENTS = "x",
             "-y",
             "-o$WORKING_DIR",
             $DESTINATION
             
& $7z $ARGUMENTS | out-null

##########
# Unpack #
##########

Write-Output "Unpacking archive..."

$DESTINATION = (Join-Path $WORKING_DIR $LIB_NAME) + ".tar"

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

$BUILD_TYPES = @("Debug", "Release")

foreach ($BUILD_TYPE in $BUILD_TYPES) {

    $BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"

    mkdir $BUILD_DIR -force

    pushd $BUILD_DIR    

    $SRC_DIR = Join-Path $WORKING_DIR "tinyxml"
    
    #find the location where the script is run from (for custom cmakelists.txt)
    $PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

    
    $ARGUMENTS =
        "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install",
        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",
        "-DTinyXML_SRC_DIR=$SRC_DIR",
        "$PSScriptRoot"

        
        
    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "MinGW Makefiles" $ARGUMENTS

    & $MINGW_ENV/bin/mingw32-make -j4 install


    
    popd

}

#Copy-Item "" -destination (Join-Path $MINGW_ENV install) -Recurse -Force

