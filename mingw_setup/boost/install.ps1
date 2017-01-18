param(
    [string]$MINGW_ENV
)

#########
# Boost #
#########

Write-Output "--- Installing Boost Libraries ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\boost

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

$REMOTE_DIR="http://downloads.sourceforge.net/project/boost/boost/1.53.0"

$ARCHIVE="boost_1_53_0.7z"

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


##################
# Download tools #
##################

$TOOLS_REMOTE_DIR="http://downloads.sourceforge.net/project/boost/boost-binaries/1.50.0"

$TOOLS_ARCHIVE="boost_1_50_tools.zip"

$TOOLS_DESTINATION = Join-Path $WORKING_DIR $TOOLS_ARCHIVE

if (-Not (Test-Path $TOOLS_DESTINATION)) {
    Write-Output "Downloading tools..."
    $CLIENT = New-Object System.Net.WebClient
    $CLIENT.DownloadFile("$TOOLS_REMOTE_DIR/$TOOLS_ARCHIVE", $TOOLS_DESTINATION)
}
else {
    Write-Output "Found tools archive file, skipping download."
}


################
# Unpack tools #
################

Write-Output "Unpacking tools..."             

$ARGUMENTS = "x",
             "-y",
             "-o$WORKING_DIR\tools",
             $TOOLS_DESTINATION

& $7z $ARGUMENTS  | out-null

Copy-Item (Join-Path $WORKING_DIR "tools\bin\bjam.exe") -destination (Join-Path $WORKING_DIR "boost_1_53_0\bjam.exe")


#############################################
# Create user config for boost build system #
#############################################

$MINGW_BIN_DIR = (Join-Path $MINGW_ENV bin).replace("\", "\\")

$USER_CONFIG = "
using gcc : 4.7 : $MINGW_BIN_DIR\\g++.exe
        :
        <rc>$MINGW_BIN_DIR\\windres.exe
        <archiver>$MINGW_BIN_DIR\\ar.exe
;
"

$USER_CONFIG_FILE = Join-Path $WORKING_DIR "user-config.jam"

Set-Content $USER_CONFIG_FILE $USER_CONFIG


#########
# Build #
#########

pushd (Join-Path $WORKING_DIR "boost_1_53_0")

Write-Output "Building libraries..."             

$ARGUMENTS  =
    "address-model=32",
    "toolset=gcc" ,
    "target-os=windows",
    "variant=release",
    "threading=multi",
    "threadapi=win32",
    "link=shared",
    "runtime-link=shared",
    "--prefix=$MINGW_ENV/install",
    "--user-config=$WORKING_DIR/user-config.jam",
    "--without-mpi",
    "--without-python",
    "-sNO_BZIP2=1",
    "-sNO_ZLIB=1",
    "--layout=tagged",
    "install"

& .\bjam $ARGUMENTS

popd
