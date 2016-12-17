#!/usr/bin/env ruby
# coding: utf-8
# Setup script for Thrive. Windows mode is experimental and isn't tested

require 'fileutils'

require_relative 'linux_setup/RubyCommon.rb'

def checkRunFolder(suggestedfolder)

  if File.exist? "thriveversion.ver" or File.basename(Dir.getwd) == "thrive"

    # Inside thrive folder
    info "Running from inside thrive folder"

    return File.expand_path("..", Dir.pwd)
    
  else

    # Outside, install thrive here
    info "Running outside thrive folder. Thrive folder will be created here"

    return Dir.pwd
    
  end
end

ThriveBranch = "master"
#ThriveBranch = "ruby_setup"
SkipPackageManager = false

require_relative 'linux_setup/RubySetupSystem.rb'

# Install packages
if BuildPlatform == "linux" and not SkipPackageManager

  LinuxOS = `lsb_release -is`.strip

  onError "failed to run lsb_release" if LinuxOS.empty?

  info "Installing packages"

  CommonPackages = "cmake make git mercurial svn"

  if LinuxOS.casecmp("Fedora") == 0

    PackageManager = "dnf install -y "
    
    PackagesToInstall = "bullet-devel boost gcc-c++ libXaw-devel freetype-devel " +
                        "freeimage-devel zziplib-devel boost-devel ois-devel tinyxml-devel " +
                        "glm-devel ffmpeg-devel ffmpeg-libs openal-soft-devel libatomic Cg"

  elsif LinuxOS.casecmp("Ubuntu") == 0

    PackageManager = "apt-get install -y "
    
	PackagesToInstall = "bullet-dev boost-dev build-essential automake libtool " +
                        "libfreetype6-dev libfreeimage-dev libzzip-dev libxrandr-dev " +
                        "libxaw7-dev freeglut3-dev libgl1-mesa-dev libglu1-mesa-dev " +
                        "libois-dev libboost-thread-dev tinyxml-dev glm-dev ffmpeg-dev " +
                        "libavutil-dev libopenal-dev"

  elsif LinuxOS.casecmp("Arch") == 0

    PackageManager = "pacman -S --noconfirm --color auto --needed"
    
	PackagesToInstall = "bullet boost automake libtool freetype2 freeimage zziplib " +
                        "libxrandr libxaw freeglut libgl ois tinyxml glm ffmpeg openal"
    
	if `pacman -Qs gcc-multilib`
      
	  PackagesToInstall += " gcc-multilib autoconf automake binutils bison fakeroot file " +
                           "findutils flex gawk gettext grep groff gzip libtool m4 make " +
                           "pacman patch pkg-config sed sudo texinfo util-linux which"
	else
      
	  PackagesToInstall += " base-devel"
      
    end
    
  else

    onError "Unknown operating system: #{LinuxOS}"
    
  end

  info "Installing prerequisite libraries, be prepared to type password for sudo"

  system "sudo #{PackageManager} #{CommonPackages} #{PackagesToInstall}"
  onError "Failed to install package manager dependencies" if $?.exitstatus > 0
    
  success "Packages installed"
  
end

installer = Installer.new(
  Array[CAudio.new, Ogre.new,
        # CEGUI uses commit 869014de5669
        CEGUI.new
       ])

installer.run

info "Thrive folder setup"



if not File.exist? File.join(CurrentDir, "thrive")

  info "Thrive folder doesn't exist, cloning from git"

  Dir.chdir(CurrentDir) do
    
    system "git clone https://github.com/Revolutionary-Games/Thrive.git thrive"
    onError "Failed to clone repository" if $?.exitstatus > 0
    
    Dir.chdir("thrive") do

      systemChecked "git submodule update --init --recursive"
      
    end
  end
end

success "Thrive folder exists"

Dir.chdir(File.join(CurrentDir, "thrive")) do
  
  systemChecked "git checkout #{ThriveBranch}"
  systemChecked "git pull --recurse-submodules origin #{ThriveBranch}"
  systemChecked "git submodule update --recursive"

  # submodule init check
  if not File.exists? File.join(CurrentDir, "thrive", "contrib/luabind/luabind", "object.hpp")

    warning "Submodules haven't been initialized, initializing now"
    
    systemChecked "git submodule update --init --recursive"

    success "Submodules are now initialized"

  end

  info "Checking assets"

  if not File.exist? "assets"
    
    info "Getting assets"
    
    system "svn checkout http://assets.revolutionarygamesstudio.com/ assets"
    onError "Failed to get thrive assets repository" if $?.exitstatus > 0
    
  else

    info "Updating assets"

    Dir.chdir("assets") do

      system "svn up"
      onError "Failed to update thrive assets" if $?.exitstatus > 0
      
    end
    
  end
  
  success "Assets are good to go"
  
  FileUtils.mkdir_p "build"
  FileUtils.mkdir_p "build/dist"
  FileUtils.mkdir_p "build/dist/bin"

  info "Making links"

  FileUtils.ln_sf "assets/cegui_examples", "cegui_examples"
  FileUtils.ln_sf "assets/definitions", "definitions"
  FileUtils.ln_sf "assets/fonts", "fonts"
  FileUtils.ln_sf "assets/gui", "gui"
  FileUtils.ln_sf "assets/materials", "materials"
  FileUtils.ln_sf "assets/models", "models"
  FileUtils.ln_sf "assets/sounds", "sounds"
  FileUtils.ln_sf "assets/videos", "videos"

  Dir.chdir("build") do
    FileUtils.ln_sf "dist/bin/Thrive", "Thrive"
  end

  info "Copying Ogre resources file"
  FileUtils.cp "ogre_cfg/resources.cfg", "./build/resources.cfg"

  info "Copying completety pointless Ogre files"

  FileUtils.cp "/usr/local/share/OGRE/plugins.cfg", "./build/plugins.cfg"
  
end

success "Thrive folder and assets are good to go"


info "Compiling thrive"


# Build directory is made earlier

Dir.chdir(File.join(CurrentDir, "thrive", "build")) do

  runCMakeConfigure "-DCMAKE_EXPORT_COMPILE_COMMANDS=ON"

  if $?.exitstatus > 0
    onError "Failed to configure Thrive. Are you using a broken version, " +
            "or did a dependency fail to install?"
  end

  runCompiler CompileThreads
  onError "Failed to compile Thrive " if $?.exitstatus > 0
  
end

success "Done compiling thrive"

# Create a link from liblua.so to fix undefined symbol: _Z13luaL_newstatev
Dir.chdir(File.join(CurrentDir, "thrive", "build")) do

  FileUtils.ln_sf "contrib/lua/liblua.so", "liblua.so"
  
end


info "run the game with '#{CurrentDir}/thrive/build/Thrive'"

success "Done"

exit 0

