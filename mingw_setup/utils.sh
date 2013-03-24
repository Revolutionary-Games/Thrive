

# Checks whether a shell command is available
# Usage:
#
#    commandExists $CMD
#
# Example:
#
#    if [ `commandExists cd` ]; then
#        echo "Found cd"
#    else
#        echo "cd not available"
#    fi
function commandExists()
{
    local command=$1
    type -P $command
}


# Downloads a file with progress indicator
# Usage:
#
#    download $URL [$DESTINATION_FILENAME]
#
function download()
{
    # http://fitnr.com/showing-file-download-progress-using-wget.html
    local url=$1
    local destination=$2
    echo -n "    "
    if [ $destination ]; then
        wget --progress=dot $url  -O  $destination 2>&1 | \
            grep --line-buffered -o "[0-9]*%" | \
            xargs -L1 echo -en "\b\b\b\b";echo
    else 
        wget --progress=dot $url 2>&1 | \
            grep --line-buffered -o "[0-9]*%" | \
            xargs -L1 echo -en "\b\b\b\b";echo
    fi
    echo -ne "\b\b\b\b"
    echo " DONE"
}

# Gets the directory of the current script
# (http://stackoverflow.com/a/246128/1184818)
#
# Usage:
#
#    # Note the back quotes
#    DIR=`getScriptDirectory`
#
function getScriptDirectory() {
    echo "$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
}

# Makes a path absolute
# Usage:
#
#    DIR=test
#    make_absolute DIR
#
function make_absolute() {
    local __pathVar=$1
    eval $__pathVar=`readlink -fn ${!__pathVar}`
}


function runCMakeString() {
    local string=$1
    cmake -P <(echo "$string")
}
