param(
    [string]$MINGW_ENV
)

#########
# cmake #
#########

Write-Output "--- Installing cmake ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\cmake

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

$REMOTE_DIR="https://cmake.org/files/v3.4/"
$ARCHIVE="cmake-3.4.1-win32-x86.zip"

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

$ARGUMENTS = "x",
             "-y",
             "-o$WORKING_DIR",
             $DESTINATION
             
Write-Output "Unpacking archive..."

& $7z $ARGUMENTS | out-null

Write-Output "Installing..."

Copy-Item (Join-Path $WORKING_DIR "cmake-3.4.1-win32-x86\*") -destination (Join-Path $MINGW_ENV cmake) -Recurse -Force


##################################
# Configure CMake toolchain file #
##################################

$ESCAPED_MINGW_ENV = $MINGW_ENV.replace("\", "/")

$ARGUMENTS = "-DTOOLCHAIN_TEMPLATE=$TOOLCHAIN_TEMPLATE",
             "-DMINGW_ENV=$ESCAPED_MINGW_ENV",
             "-P",
             "$DIR\../configure_toolchain.cmake"

& (Join-Path $MINGW_ENV cmake\bin\cmake) $ARGUMENTS #| out-null
