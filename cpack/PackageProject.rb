#!/usr/bin/env ruby
#
# This is a packaging script for Thrive linux releases
# It creates two packages one universal with literally every library ever in it
# (and one with only the Thrive specific libraries, todo: do this)

require 'fileutils'
require 'nokogiri'

# Setup
# If true 'strip' is used to get rid of debugging info
StripFiles = true

# If false will skip compressing the staging folder, useful for checking which files would
# be included without having to compress them
ZipIt = true


if ARGV.count > 1

  onError "Expected 0 or 1 argument. The argument is the build directory to use"
  
end

def checkRunFolder(suggestedfolder)

  if ARGV.count > 0
    ARGV[0]
  else
    File.expand_path("../build", suggestedfolder)
  end
end

def projectFolder(baseDir)

  return baseDir
  
end

require_relative '../linux_setup/RubySetupSystem'

Dir.chdir(CurrentDir) do

  if not File.exists? "CMakeLibraryList.xml"

    onError "Selected build folder doesn't contain library lists file 'CMakeLibraryList.xml' "
    
  end
  
end

info "Running Thrive packaging script for linux"

doc = File.open(File.join(CurrentDir, "CMakeLibraryList.xml")) { |f| Nokogiri::XML(f) }

ThriveVersion = doc.at_xpath("//version").content.strip

LibraryList = doc.at_xpath("//libraries").content.split(';')

CEGUIVersion = doc.at_xpath("//CEGUI_version").content.strip

info "For version #{ThriveVersion} with #{LibraryList.count} libraries and "+
     "CEGUI #{CEGUIVersion}"


# Constant name stuff (these aren't configuration options)
PackageName = "Thrive-#{ThriveVersion}"

ZipName = "#{PackageName}.7z"

# Add Ogre plugins to the library list
findOgrePlugins(LibraryList, ["RenderSystem_GL", "RenderSystem_GL3Plus", "Plugin_ParticleFX"])

# Add CEGUI plugins to the list
findCEGUIPlugins(LibraryList, CEGUIVersion)

# Find actual libboost_thread files
findRealBoostThread(LibraryList)


# Create staging folder
Dir.chdir(CurrentDir) do

  FileUtils.mkdir_p PackageName
  FileUtils.mkdir_p File.join(PackageName, "bin")
  
end

TargetRoot = File.join(CurrentDir, PackageName)

info "Copying all the files to staging directory at: #{TargetRoot}"

info "Copying core files"

# Copy all required files
Dir.chdir(CurrentDir) do

  copyPossibleSymlink("Thrive", File.join(TargetRoot, "bin/"), StripFiles, true)
  copyPossibleSymlink("liblua.so", File.join(TargetRoot, "bin/"), StripFiles, true)

end

success "Core Thrive binaries copied"

info "Copying direct dependencies"

puts LibraryList

copyDependencyLibraries(LibraryList, File.join(TargetRoot, "bin/"), StripFiles, true)

success "Copied direct libraries"

# Use ldd to find more dependencies
lddfound = lddFindLibraries File.join(TargetRoot, "bin/Thrive")

info "Copying #{lddfound.count} libraries found by ldd on Thrive binary"

copyDependencyLibraries(lddfound, File.join(TargetRoot, "bin/"), StripFiles, true)


# Find dependencies of dynamic Ogre libraries
lddfound = lddFindLibraries File.join(TargetRoot, "bin/Plugin_ParticleFX.so")

info "Copying #{lddfound.count} libraries found by ldd on random things"

copyDependencyLibraries(lddfound, File.join(TargetRoot, "bin/"), StripFiles, true)

success "Copied ldd found libraries"

info "Copied #{HandledLibraries.count} libraries to staging directory"


info "Copying assets"


# Assets
# TODO: see if these could be symlinks
FileUtils.cp_r File.join(CurrentDir, "../assets/fonts"), TargetRoot
FileUtils.cp_r File.join(CurrentDir, "../assets/gui"), TargetRoot
FileUtils.cp_r File.join(CurrentDir, "../assets/materials"), TargetRoot
FileUtils.cp_r File.join(CurrentDir, "../assets/models"), TargetRoot
FileUtils.cp_r File.join(CurrentDir, "../assets/sounds"), TargetRoot
FileUtils.cp_r File.join(CurrentDir, "../assets/videos"), TargetRoot

FileUtils.cp_r File.join(CurrentDir, "../scripts"), TargetRoot

success "Assets copied"


info "Copying documentation and creating scripts"

info "Creating launch scripts"

# Launch links

File.open(File.join(TargetRoot, "launch.sh"), 'w') {
  |file| file.write(<<-eos)
#!/bin/sh
SCRIPTPATH=$( cd $(dirname $0) ; pwd -P )
( cd "$SCRIPTPATH/bin"
LD_LIBRARY_PATH="$(pwd)"
export LD_LIBRARY_PATH
./Thrive
)
eos
}

systemChecked "chmod +x \"#{File.join(TargetRoot, "launch.sh")}\""

# Source code setup script
FileUtils.mkdir_p File.join(TargetRoot, "source_build")
FileUtils.cp File.join(CurrentDir, "../SetupThrive.rb"), File.join(TargetRoot, "source_build")

File.open(File.join(TargetRoot, "source_build/README.md"), 'w') {
  |file| file.write(<<-eos)
Contained in this directory is a script that downloads and setups Thrive project build.
You should probably ignore it if you don't plan on doing development on the C++ side of Thrive.
Note: the script requires root, so you should read through it before running it
eos
}

info "Copying Ogre scripts"

File.open(File.join(TargetRoot, "bin/plugins.cfg"), 'w') {
  |file| file.write(<<-eos)
# Defines plugins to load
PluginFolder=./
 Define plugins
# Plugin=RenderSystem_Direct3D9
# Plugin=RenderSystem_Direct3D11
Plugin=RenderSystem_GL
Plugin=RenderSystem_GL3Plus
# Plugin=RenderSystem_GLES
# Plugin=RenderSystem_GLES2
 Plugin=Plugin_ParticleFX
# Plugin=Plugin_CgProgramManager
eos
}

Dir.chdir(File.join(CurrentDir, "../ogre_cfg")) do
  
  FileUtils.cp "resources.cfg", File.join(TargetRoot, "bin")

end

# Info files
FileUtils.cp File.join(CurrentDir, "../LICENSE.txt"), TargetRoot
FileUtils.cp File.join(CurrentDir, "../README.md"),
             File.join(TargetRoot, "REPOSITORY_README.md")
FileUtils.cp File.join(CurrentDir, "../gpl.txt"), TargetRoot

FileUtils.cp File.join(CurrentDir, "../cpack/Linux_package_readme.md"),
             File.join(TargetRoot, "README.md")

# Version file
FileUtils.cp File.join(CurrentDir, "../thriveversion.ver"), TargetRoot

FileUtils.touch(File.join(TargetRoot, "package.version.#{ThriveVersion}"))

Dir.chdir(File.join(CurrentDir, "..")) do

  File.open(File.join(TargetRoot, "revision.txt"), 'w') {
    |file| file.write("Package time: " + `date --iso-8601=seconds` + "\n\n" + `git log -n 1`)
  }
end


info "Copying documentation"

# documentation
FileUtils.cp_r File.join(CurrentDir, "doc"), TargetRoot

success "Done"

success "Done copying"

info "Deleting log and settings files if they exist"

FileUtils.rm_f File.join(TargetRoot, "bin/cAudioEngineLog.html")
FileUtils.rm_f File.join(TargetRoot, "bin/CEGUI.log")
FileUtils.rm_f File.join(TargetRoot, "bin/default")
FileUtils.rm_f File.join(TargetRoot, "bin/ogre.cfg")



if ZipIt
  
  info "Creating a zip of the staging folder"

  Dir.chdir(CurrentDir) do

    systemChecked "7za a '#{ZipName}' '#{PackageName}'"
    
  end

  success "Zip completed"
  
else

  warning "Skipping zip, deleting existing one if one exists";
  FileUtils.rm_f File.join(CurrentDir, ZipName)
  
end

success "Package #{ZipName} completed"


