param(
    [string]$MINGW_ENV
)

#########
# CEGUI #
#########

Write-Output "--- Installing CEGUI ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")


############################
# Create working directory #
############################

$WORKING_DIR = Join-Path $MINGW_ENV temp\cegui

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

$REMOTE_DIR="https://bitbucket.org/cegui/cegui/get"
$LIB_NAME="869014de5669"
$EXTRACTED_NAME = "cegui-cegui-869014de5669"

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
    $DEPS_LOC = "$MINGW_ENV/temp/cegui_deps/build-" + $BUILD_TYPE + "/dependencies"
    $DEPS_TARGET = "$WORKING_DIR/" + $EXTRACTED_NAME
    Copy-Item $DEPS_LOC -destination $DEPS_TARGET -Recurse -Force
    

    $BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"

    mkdir $BUILD_DIR -force

    pushd $BUILD_DIR

    $LIB_SRC = Join-Path $WORKING_DIR $EXTRACTED_NAME
    
    $MINGW_ENV = $MINGW_ENV -replace "\\", "/"
    
    # hacky way to add tinyxml dependency, the cegui_deps didn't seem to build it correctly so we do it manually
    $ARGUMENTS =
        "-DCMAKE_INSTALL_PREFIX=$MINGW_ENV/install",
        "-DCMAKE_BUILD_TYPE=$BUILD_TYPE",
        "-DOGRE_HOME:path=$MINGW_ENV/OgreSDK",
        "-DCEGUI_BUILD_XMLPARSER_TINYXML:bool=TRUE",
		"-DCEGUI_BUILD_RENDERER_OPENGL:bool=FALSE",
		"-DCEGUI_BUILD_RENDERER_OPENGL3:bool=FALSE",
		"-DCEGUI_BUILD_RENDERER_IRRLICHT:bool=FALSE",
		"-DCEGUI_BUILD_RENDERER_DIRECTFB:bool=FALSE",
		"-DCEGUI_BUILD_RENDERER_DIRECT3D9:bool=FALSE",
		"-DCEGUI_BUILD_RENDERER_DIRECT3D10:bool=FALSE",
		"-DCEGUI_BUILD_RENDERER_DIRECT3D11:bool=FALSE",
		"-DCEGUI_BUILD_RENDERER_NULL:bool=FALSE",
        "-DCEGUI_BUILD_RENDERER_OGRE:bool=TRUE",
		"-DCEGUI_BUILD_RENDERER_OPENGLES:bool=FALSE",
		"-DCEGUI_STRING_CLASS=1",
		"-DCEGUI_SAMPLES_ENABLED:bool=FALSE",
        "-DCMAKE_CXX_FLAGS:string=-msse2 -std=c++0x",
        "-DCEGUI_OPTION_DEFAULT_XMLPARSER:string=TinyXMLParser",
        "-DCEGUI_TINYXML_HAS_2_6_API:bool=TRUE",
        "-DTINYXML_H_PATH:path=$MINGW_ENV/install/include/tinyxml",
     #   -"DBoost_INCLUDE_DIR:path=$MINGW_ENV/install/include/boost",
        "-DTINYXML_LIB_DBG:filepath=$MINGW_ENV/install/lib/libtinyxml.a", #This should be $MINGW_ENV/install... but that causes strange error.
        "$LIB_SRC"

        Write-Output $MINGW_ENV/install/lib/libtinyxml.a
        
    & (Join-Path $MINGW_ENV cmake\bin\cmake) -G "CodeBlocks - MinGW Makefiles" $ARGUMENTS

    & $MINGW_ENV/bin/mingw32-make -j4 install


    popd

}
#Debug thinks that it fails and doesn't install properly atm so we do it manually.
#Copy-Item "$MINGW_ENV/temp/cegui/build-Debug/bin/libCEGUIBase-9999_d.dll" -destination "$MINGW_ENV/install/lib/libCEGUIBase-9999_d.dll" -Recurse -Force
#Copy-Item "$MINGW_ENV/temp/cegui/build-Debug/bin/libCEGUICommonDialogs-9999_d.dll" -destination "$MINGW_ENV/install/lib/libCEGUICommonDialogs-9999_d.dll" -Recurse -Force
#Copy-Item "$MINGW_ENV/temp/cegui/build-Debug/bin/libCEGUIOgreRenderer-9999_d.dll" -destination "$MINGW_ENV/install/lib/libCEGUIOgreRenderer-9999_d.dll" -Recurse -Force