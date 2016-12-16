#############
# Constants #
#############

$DIR = Split-Path $MyInvocation.MyCommand.Path

$TOOLCHAIN_TEMPLATE = Join-Path $DIR toolchain_win.cmake.in


#################
# Include utils #
#################

. (Join-Path $DIR utils.ps1)


##################################
# Ask for MinGW installation dir #
##################################

if (Test-Path (Join-Path $DIR debugging)) {
    # This is just here so you can skip the annoying file dialog
    # when debugging the setup script
    $MINGW_ENV = Join-Path $DIR test_win
}
else {
    $MINGW_ENV = Select-Directory -Title "Select installation directory for build environment" -Directory $DIR
}


If ([string]::IsNullOrEmpty($MINGW_ENV)) {
    exit 1
}

mkdir (Join-Path $MINGW_ENV cmake) -force | out-null
mkdir (Join-Path $MINGW_ENV install) -force | out-null



#####################
# Install libraries #
#####################
#
$LIBRARIES =  $LIBRARIES =  "7zip","cmake", "mingw", "boost", "ogre_dependencies", "ogre", "bullet", "ogg", "vorbis", "OpenAl", "TinyXML", "cegui_dependencies", "cegui","cAudio", "ffmpeg"
foreach ($LIBRARY in $LIBRARIES) {
    $INSTALL_SCRIPT = Join-Path $DIR (Join-Path $LIBRARY install.ps1)
    & $INSTALL_SCRIPT $MINGW_ENV
    Write-Output "`n`n`n"
}


################################
# Inform user that we are done #
################################

$shell = new-object -comobject wscript.shell
$shell.popup("Setup script done. See readme.txt for further instructions.",0,"Thrive build setup complete",0)
