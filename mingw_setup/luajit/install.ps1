param(
    [string]$MINGW_ENV
)

##########
# FFMPEG #
##########

Write-Output "--- Installing luajit ---"

$DIR = Split-Path $MyInvocation.MyCommand.Path

#################
# Include utils #
#################

. (Join-Path "$DIR\.." "utils.ps1")

############################
# Create working directory #
############################

$WORKING_DIR = "$DIR\..\..\contrib\lua\luajit"

mkdir $WORKING_DIR -force | out-null


###########
# Compile #
###########

Write-Output "Compiling..."






#$BUILD_DIR = Join-Path $WORKING_DIR "build-$BUILD_TYPE"
pushd $WORKING_DIR

$env:Path += ";${MINGW_ENV}/bin"



& $MINGW_ENV/bin/mingw32-make -j4 all | Tee-Object -FilePath compileroutput.txt
popd



