param(
    [string]$MINGW_ENV
)

###############
# Google Test #
###############

Write-Output "--- Installing Google Test Library ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\gtest

mkdir $WORKING_DIR -force | out-null


####################
# Download archive #
####################

$REMOTE_DIR="http://googletest.googlecode.com/files"

$ARCHIVE="gtest-1.6.0.zip"

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

Unzip $DESTINATION $WORKING_DIR


Copy-Item (Join-Path $WORKING_DIR "gtest-1.6.0") -destination (Join-Path $MINGW_ENV "gtest") -Recurse -Force
