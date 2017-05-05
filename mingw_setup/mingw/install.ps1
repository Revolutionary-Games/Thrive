param(
    [string]$MINGW_ENV
)

#########
# MinGW #
#########

Write-Output "--- Installing MinGW ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\mingw

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

##$REMOTE_DIR="https://sourceforge.net/projects/mingw-w64/files/Toolchains%20targetting%20Win32/Personal%20Builds/mingw-builds/4.9.3/threads-win32/dwarf/"
##$ARCHIVE="i686-4.9.3-release-win32-dwarf-rt_v4-rev1.7z"
$REMOTE_DIR="https://sourceforge.net/projects/mingw-w64/files/Toolchains%20targetting%20Win32/Personal%20Builds/mingw-builds/5.4.0/threads-win32/dwarf/"
$ARCHIVE="i686-5.4.0-release-win32-dwarf-rt_v5-rev0.7z"

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
Copy-Item (Join-Path $WORKING_DIR "mingw32\*") -destination $MINGW_ENV -Recurse -Force
if(!(Test-Path -Path "C:\mingw\bin" )){
    New-Item "C:\mingw\bin" -type directory
    New-Item "C:\mingw\i686-w64-mingw32" -type directory
    New-Item "C:\mingw\lib" -type directory
    New-Item "C:\mingw\libexec" -type directory
}
Copy-Item (Join-Path $WORKING_DIR "mingw32\bin\*") -destination "C:\mingw\bin" -Recurse -Force
Copy-Item (Join-Path $WORKING_DIR "mingw32\i686-w64-mingw32\*") -destination "C:\mingw\i686-w64-mingw32" -Recurse -Force
Copy-Item (Join-Path $WORKING_DIR "mingw32\lib\*") -destination "C:\mingw\lib" -Recurse -Force
Copy-Item (Join-Path $WORKING_DIR "mingw32\libexec\*") -destination "C:\mingw\libexec" -Recurse -Force
