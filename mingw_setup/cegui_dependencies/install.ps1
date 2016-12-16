param(
    [string]$MINGW_ENV
)

######################
# CEGUI Dependencies #
######################

Write-Output "--- Installing CEGUI Dependencies ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\cegui_deps

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


$REMOTE_DIR="https://bitbucket.org/cegui/cegui-dependencies/get"

$COMMIT = "051a02c23494"
$ARCHIVE="051a02c.zip"
$EXTRACTED_NAME = "cegui-cegui-dependencies-" + $COMMIT
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
        "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install/dependencies",
        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",
        "-DCEGUI_BUILD_TOLUAPP:bool=false",
        "-DCEGUI_BUILD_LUA:bool=false",
        "-DCEGUI_BUILD_XERCES:bool=FALSE",
        "-DCEGUI_BUILD_EXPAT:bool=FALSE",
        "-DCEGUI_BUILD_PCRE:bool=FALSE",
        "-DCEGUI_BUILD_ZLIB:bool=FALSE",
        "-DCEGUI_BUILD_EFFECTS11:bool=FALSE",
        "-DCEGUI_BUILD_GLEW:bool=FALSE",
        "-DCEGUI_BUILD_TINYXML:bool=FALSE",
        "-DCEGUI_BUILD_MINIZIP:bool=FALSE",
        "-DCEGUI_BUILD_GLFW:bool=FALSE",
        "-DCEGUI_BUILD_GLM:bool=TRUE",
        "-DCEGUI_BUILD_FREEIMAGE:bool=TRUE",
        "$WORKING_DIR/$EXTRACTED_NAME"

    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "MinGW Makefiles" $ARGUMENTS

    & $MINGW_ENV/bin/mingw32-make -j4 all
    
    $BIN_LOC = "$MINGW_ENV/temp/cegui_deps/build-" + $BUILD_TYPE + "/dependencies/bin/*"
    $BIN_TARGET = "$MINGW_ENV/install/bin/" + $BUILD_TYPE
    Copy-Item $BIN_LOC -destination $BIN_TARGET -Recurse -Force
    
    #Install GLM headers
    $GLM_H = "$MINGW_ENV/temp/cegui_deps/cegui-cegui-dependencies-" + $COMMIT + "/src/glm-0.9.4.5/glm"
    $GLM_H_TARGET = "$MINGW_ENV/install/include/"
    Copy-Item $GLM_H -destination $GLM_H_TARGET -Recurse -Force
    
    
    
    popd
	
}


