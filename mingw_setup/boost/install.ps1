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

$REMOTE_DIR="http://downloads.sourceforge.net/project/boost/boost/1.63.0"

$ARCHIVE="boost_1_63_0.7z"

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


#############################################
# Create user config for boost build system #
#############################################

$MINGW_BIN_DIR = (Join-Path $MINGW_ENV bin).replace("\", "\\")

$USER_CONFIG = "
using gcc : 5.4 : $MINGW_BIN_DIR\\g++.exe
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

pushd (Join-Path $WORKING_DIR "boost_1_63_0\tools\build")

Write-Output "Running bootstrap to generate build tool b2..."        
& .\bootstrap.bat


Write-Output "Building boost.build..."        
& .\b2 install --prefix=$WORKING_DIR\boostbuild

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

& .\b2 $ARGUMENTS

Write-Output "Copying header files..."      

#I desperately tried using Copy-Item to copy header files but every attempt always failed to copy everything so I'm calling an xcopy 
$cmd = "xcopy `$WORKING_DIR\boost_1_63_0\boost\*` `$MINGW_ENV\install\include\boost\*` /s/h/e/k/f/c"
$cmd = $ExecutionContext.InvokeCommand.ExpandString($cmd)
cmd.exe /c $cmd

popd
