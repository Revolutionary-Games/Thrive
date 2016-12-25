#!/bin/sh

function init() {
	# detect if running from thrive folder
	InsideFolder=${PWD##*/}
	OriginalFolder=$(pwd)

	# Define colors for output
	ERROR="\033[1;31m"
	GOOD="\033[1;32m"
	INFO="\033[1;36m"
	COMMAND="\033[1;37m"
	NC="\033[0m" # No Color

	if [ -f ./thriveversion.ver -o "$InsideFolder" = "thrive" ]; then
		# Running from thrive folder
		StartingDirectory=$(dirname "$(pwd)")

	else
		StartingDirectory=$(pwd)
	fi

	# Variable setup
		#OS Check
	x=`lsb_release`
	if [[ "$X" =~ LSB.Version.* ]]; then
		OS=$(lsb_release -is) #For reasonable operating systems
	else
		echo "$ERROR lsb_release not available, quitting. $NC" 
		exit 1
	fi
	
	THREADS=$(nproc)
	DEBUG=0
	VERBOSE=0

	THRIVE_BRANCH="master"

	# Parse arguments
	getopt --test > /dev/null
	if [ $? != 4 ]; then
	    echo -e "$ERROR I’m sorry, 'getopt --test' failed in this environment. $NC"
	    exit 1
	fi

	SHORT=dfj:t:v
	LONG=debug,force,threads:,verbose

	PARSED=$(getopt --options $SHORT --longoptions $LONG --name "$0" -- "$@")
	if [ $? != 0 ]; then
	    exit 2
	fi
	eval set -- "$PARSED"

	while true; do
	    case "$1" in
		-d|--debug)
		    DEBUG=1
		    shift
		    ;;
		-v|--verbose)
		    VERBOSE=1
		    shift
		    ;;
		-j|-t|--threads)
		    THREADS="$2"
		    shift 2
		    ;;
		--)
		    shift
		    break
		    ;;
		*)
		    echo -e "$ERROR Programming error $NC"
		    exit 3
		    ;;
	    esac
	done

	echo -e -e "$INFO Using $THREADS threads to compile $NC"
	echo -e "$INFO Running in folder $StartingDirectory $NC"
	cd "$StartingDirectory"

	MakeArgs="-j $THREADS"

	if [ $DEBUG = 1 -o $VERBOSE = 1 ]; then
	    MakeArgs="$MakeArgs -d"
	fi
}

function install_Packages() {

    CommonPackages="cmake make git mercurial svn"
    
    # My fedora version string contains bunch of junk after like "SION_ID=24\nIANT_ID=Workstation"
    # So this is matched with a regex
	if [ $OS = "Fedora" ]; then
		PackageManager="dnf install -y "
		PackagesToInstall="bullet-devel boost gcc-c++ libXaw-devel freetype-devel freeimage-devel \
                zziplib-devel boost-devel ois-devel tinyxml-devel glm-devel ffmpeg-devel ffmpeg-libs \
                openal-soft-devel libatomic Cg"

	elif [ $OS = "Ubuntu" ]; then
		PackageManager="apt-get install -y "
		PackagesToInstall="bullet-dev boost-dev build-essential automake libtool libfreetype6-dev \
			libfreeimage-dev libzzip-dev libxrandr-dev libxaw7-dev freeglut3-dev libgl1-mesa-dev \
			libglu1-mesa-dev libois-dev libboost-thread-dev tinyxml-dev glm-dev ffmpeg-dev libavutil-dev libopenal-dev"

	elif [ $OS = "Arch" ]; then
		PackageManager="pacman -S --noconfirm --color auto --needed"
		PackagesToInstall="bullet boost automake libtool freetype2 \
			freeimage zziplib libxrandr libxaw freeglut libgl \
			ois tinyxml glm ffmpeg openal"
		x=`pacman -Qs gcc-multilib`
		if [ -n "$x" ]; then
			PackagesToInstall="$PackagesToInstall gcc-multilib autoconf automake binutils \
				bison fakeroot file findutils flex gawk gettext grep groff gzip libtool \
				m4 make pacman patch pkg-config sed sudo texinfo util-linux which"
		else
			PackagesToInstall="$PackagesToInstall base-devel"
		fi

	else
		echo -e "$ERROR Unkown linux OS \"$OS\" $NC"
		exit 2
	fi

    echo -e "$INFO Creating CEGUI project folder for $OS $NC"
    
	PackagesToInstall="$CommonPackages $PackagesToInstall"

	echo -e "$INFO Installing prerequisite libraries, be prepared to type password for sudo $NC"
	eval "sudo $PackageManager $PackagesToInstall"

	if [ $? -eq 0 ]; then
		echo -e "$GOOD Prerequisites installed successfully $NC"

	else
		echo -e "$ERROR Package manager failed to install required packages, install \"$PackagesToInstall\" manually $NC"
		exit 1
	fi
}

function prepare_Deps() {
	echo -e "$INFO Cloning repositories and creating build files... $NC"
	prepare_Ogre
	prepare_CEGUI
	prepare_cAudio
}

function prepare_Ogre() {
	mkdir -p ogreBuild
	cd ogreBuild

	# Main repo
	if [ -d ogre ]; then
		cd ogre
		hg pull

	else
		hg clone https://bitbucket.org/sinbad/ogre ogre
		cd ogre
	fi

	hg update v2-0

	cd ..

	# TODO: allow using this if wanted
	# Dependencies repo


	# Build file
	mkdir -p build

	echo "cmake_minimum_required(VERSION 2.8.11)
	# Let's try forcing static FreeImage
	set(FreeImage_USE_STATIC_LIBS TRUE)
	# depencies must be first
	#add_subdirectory(ogredeps)
	# actual ogre
	add_subdirectory(ogre)" > CMakeLists.txt


	# Run cmake
	cd build
	check cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DOGRE_BUILD_RENDERSYSTEM_GL3PLUS=ON -DOGRE_BUILD_COMPONENT_OVERLAY=OFF -DOGRE_BUILD_COMPONENT_PAGING=OFF -DOGRE_BUILD_COMPONENT_PROPERTY=OFF -DOGRE_BUILD_COMPONENT_TERRAIN=OFF -DOGRE_BUILD_COMPONENT_VOLUME=OFF -DOGRE_BUILD_PLUGIN_BSP=OFF -DOGRE_BUILD_PLUGIN_CG=ON -DOGRE_BUILD_PLUGIN_OCTREE=OFF -DOGRE_BUILD_PLUGIN_PCZ=OFF -DOGRE_BUILD_SAMPLES=OFF

	cd "$StartingDirectory"
	echo -e "$GOOD Done $NC"
}

function prepare_CEGUI() {
	echo -e "$INFO CEGUI... $NC"

	if [ -d cegui ]; then
		cd cegui
		hg pull

	else
		hg clone https://bitbucket.org/cegui/cegui cegui
		cd cegui
	fi

    # Let's use this commit for now
    hg update 869014de5669
	#hg update default

	mkdir build
	cd build

	check cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DCEGUI_BUILD_APPLICATION_TEMPLATES=OFF -DCEGUI_BUILD_PYTHON_MODULES=OFF -DCEGUI_SAMPLES_ENABLED=OFF -DCEGUI_STRING_CLASS=1 -DCEGUI_USE_FRIBIDI=OFF -DCEGUI_BUILD_RENDERER_OPENGLES=OFF

	cd "$StartingDirectory"

	echo -e "$GOOD Done $NC"
}

function prepare_cAudio() {
	echo -e "$INFO cAudio $NC"

	if [ -d cAudio ]; then
		cd cAudio

		git checkout master
		git pull origin master

	else
        # Official repo
		#git clone https://github.com/wildicv/cAudio.git
        # The official repo doesn't merge pull requests so here's a working fork
        git clone https://github.com/hhyyrylainen/cAudio
		cd cAudio
	fi

	mkdir -p build
	cd build
	check cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DCAUDIO_BUILD_SAMPLES=OFF

	echo -e "$GOOD Done $NC"
}

function build_Deps() {
	echo -e "$INFO Compiling. This may take a long time! $NC"
	build_Ogre
	build_CEGUI
	build_cAudio
}

function build_Ogre() {
	echo -e "$INFO Ogre... $NC"

	cd "$StartingDirectory/ogreBuild/build"
	check eval "make $MakeArgs"

	echo -e "$GOOD Done $NC"
}

function build_CEGUI() {
	echo -e "$INFO CEGUI... $NC"

	cd "$StartingDirectory/cegui/build"
	check eval "make $MakeArgs"

	echo -e "$GOOD Done $NC"
}

function build_cAudio() {
	echo -e "$INFO cAudio... $NC"

	cd "$StartingDirectory/cAudio/build"
	check eval "make $MakeArgs"

	echo -e "$GOOD Done $NC"
}

function install_Deps() {
	echo -e "$INFO Installing dependencies $NC"
	install_Ogre
    install_CEGUI
    install_cAudio
	echo -e "$GOOD Done $NC"
}

function install_Ogre() {
	echo -e "$INFO Installing Ogre, prepare for sudo password $NC"
	cd "$StartingDirectory/ogreBuild/build"
	check sudo make install
}

function install_CEGUI() {
	echo -e "$INFO Installing CEGUI, prepare for sudo password $NC"
	cd "$StartingDirectory/cegui/build"
	check sudo make install
}

function install_cAudio() {
	echo -e "$INFO Installing cAudio, prepare for sudo password $NC"
	cd "$StartingDirectory/cAudio/build"
	check sudo make install
}

function setup_Thrive() {
	echo -e "$INFO Setting up Thrive $NC"
	cd "$StartingDirectory"

	echo -e "$INFO Getting code $NC"

	if [ -d thrive ]; then
		cd thrive

	else
		git clone https://github.com/Revolutionary-Games/Thrive.git thrive
		cd thrive
		git submodule update --init --recursive
	fi

	git checkout $THRIVE_BRANCH
	git pull --recurse-submodules origin $THRIVE_BRANCH
	git submodule update --recursive

	echo -e "$INFO Getting assets $NC"

	if [ -d assets ]; then
			cd assets
			svn up
			cd ..
	else
		svn checkout http://assets.revolutionarygamesstudio.com/ assets
	fi

	echo -e "$INFO Making all the links $NC"
	ln -sf assets/cegui_examples cegui_examples
	ln -sf assets/definitions definitions
	ln -sf assets/fonts fonts
	ln -sf assets/gui gui
	ln -sf assets/materials materials
	ln -sf assets/models models
	ln -sf assets/sounds sounds
    ln -sf assets/videos videos

	echo -e "$INFO Copying Ogre resources file $NC"
    cp ogre_cfg/resources.cfg ./build/resources.cfg

	echo -e "$INFO Copying completety pointless Ogre files $NC"

	cp /usr/local/share/OGRE/plugins.cfg ./build/plugins.cfg
}

function build_Thrive() {
	echo -e "$INFO Compiling Thrive $NC"
	mkdir -p $StartingDirectory/thrive/build
	cd $StartingDirectory/thrive/build

	check cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo -DCMAKE_EXPORT_COMPILE_COMMANDS=ON

	check eval "make $MakeArgs"
}

function print_final_message() {
	echo -e "$INFO ."
	echo -e "$INFO ."
	echo -e "$INFO . $NC"

	cd "$StartingDirectory"

	echo -e "$GOOD Done, run the game with '$StartingDirectory/thrive/build/Thrive' $NC"
	cd "$OriginalFolder"
}

function check() {
	"$@"
    local status=$?
    if [ $status -ne 0 ]; then
			if [ $1 = "eval" ]; then
				echo -e "$ERROR Error with $COMMAND $2 $ERROR in function $COMMAND ${FUNCNAME[1]}() $NC" >&2
				exit 1

			else
	      echo -e "$ERROR Error with $COMMAND $1 $ERROR in function $COMMAND ${FUNCNAME[1]}() $NC" >&2
				exit 1
			fi

		elif [ $1 = "sudo" ]; then
    	echo -e "$GOOD Everything went fine with $COMMAND $2 $GOOD in function $COMMAND ${FUNCNAME[1]}() $NC"

		else
			echo -e "$GOOD Everything went fine with $COMMAND $1 $GOOD in function $COMMAND ${FUNCNAME[1]}() $NC"
	fi
}

init
install_Packages
prepare_Deps
build_Deps
install_Deps
setup_Thrive
build_Thrive
print_final_message
