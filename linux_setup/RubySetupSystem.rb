# coding: utf-8
# A ruby script for downloading and installing C++ project dependencies
# Made by Henri Hyyryläinen

# TODO: make cmake use extra find paths on windows and test

require_relative 'RubyCommon.rb'
require_relative 'DepGlobber.rb'

require 'fileutils'
require 'etc'
require 'os'
require 'pathname'

# Used by: verifyVSProjectRuntimeLibrary
require 'nokogiri' if OS.windows?
# Required for installs on windows
require 'win32ole' if OS.windows?


### Setup variables
CMakeBuildType = "RelWithDebInfo"
CompileThreads = Etc.nprocessors

# If set to true will install CEGUI editor
# Note: this doesn't work
InstallCEED = false

# If set to false won't install libs that need sudo
DoSudoInstalls = true

# If true dependencies won't be updated from remote repositories
SkipPullUpdates = false

# If true skips all dependencies
OnlyMainProject = false

# If true skips the main project
OnlyDependencies = false

# If true new version of depot tools and breakpad won't be fetched on install
NoBreakpadUpdateOnWindows = false

# On windows visual studio will be automatically opened if required
AutoOpenVS = true

# Visual studio version on windows, required for forced 64 bit builds
VSVersion = "Visual Studio 14 2015 Win64"
VSToolsEnv = "VS140COMNTOOLS"

# TODO create a variable for running the package manager on linux if possible

### Commandline handling
# TODO: add this


# This verifies that CurrentDir is good and assigns it to CurrentDir
CurrentDir = checkRunFolder Dir.pwd

ProjectDir = projectFolder CurrentDir

ProjectDebDir = File.join ProjectDir, "libraries"

ProjectDebDirLibs = File.join ProjectDebDir, "lib"

ProjectDebDirBinaries = File.join ProjectDebDir, "bin"

ProjectDebDirInclude = File.join ProjectDebDir, "include"



info "Running in dir '#{CurrentDir}'"

if BuildPlatform == "windows"
  puts "This is not properly tested so be careful"
  
end

puts "Using #{CompileThreads} threads to compile, configuration: #{CMakeBuildType}"



## Install runner
class Installer
  # basedepstoinstall Is an array of BaseDep derived objects that install
  # the required libraries
  def initialize(basedepstoinstall)

    @Libraries = basedepstoinstall

    if not @Libraries.kind_of?(Array)
      onError("Installer passed something else than an array")
    end
    
  end

  # Adds an extra library
  def addLibrary(lib)

    @Libraries.push lib
  end

  # Runs the whole thing
  # calls onError if fails
  def run()

    if not SkipPullUpdates and not OnlyMainProject
      info "Retrieving dependencies"

      @Libraries.each do |x|

        x.Retrieve
        
      end

      success "Successfully retrieved all dependencies. Beginning compile"
    end

    if not OnlyMainProject

      info "Configuring dependencies"

      @Libraries.each do |x|

        x.Setup
        x.Compile
        x.Install
        
      end

      info "Dependencies done, configuring main project"
      
    end

    if OnlyDependencies

      success "All done. Skipping main project"
      exit 0
    end
    
    
  end
  
end

# Path helper
# For breakpad depot tools
class PathModifier
  def initialize(newpathentry)
    
    @OldPath = ENV["PATH"]

    abort "Failed to get env path" if @OldPath == nil

    if BuildPlatform == "linux"
      
      newpath = newpathentry + ":" + @OldPath
      
    else

      newpath = @OldPath + ";" + newpathentry
      
    end

    info "Setting path to: #{newpath}"
    ENV["PATH"] = newpath

  end

  def Restore()
    info "Restored old path"
    ENV["PATH"] = @OldPath
  end
end


#### Windows stuff

# Run visual studio environment configure .bat file
def bringVSToPath()
  if not File.exist? "#{ENV[VSToolsEnv]}VsMSBuildCmd.bat"
    onError "VsMSBuildCMD.bat is missing check is VSToolsEnv variable correct in Setup.rb" 
  end
  "call \"#{ENV[VSToolsEnv]}VsMSBuildCmd.bat\""
end

# Makes sure that the wanted value is specified for all targets that match the regex
def verifyVSProjectRuntimeLibrary(projFile, matchRegex, wantedRuntimeLib)
  # Very parameters
  abort "Call verifyVSProjectRuntimeLibrary only on windows!" if not OS.windows?
  onError "Project file: #{projFile} doesn't exist" if not File.exist? projFile
  
  # Load xml with nokogiri
  doc = File.open(projFile) { |f| Nokogiri::XML(f) }
  
  doc.css("Project ItemDefinitionGroup").each do |group|
    if not matchRegex.match group['Condition'] 
      next
    end
    
    info "Checking that project target '#{group['Condition']}' Has RuntimeLibrary of type #{wantedRuntimeLib}"
    
    libType = group.at_css("ClCompile RuntimeLibrary")
    
    if not libType
      warning "Couldn't verify library type. Didn't find RuntimeLibrary node"
      next
    end
    
    if libType.content != wantedRuntimeLib
      
      onError "In file '#{projFile}' target '#{group['Condition']}' "+
        "Has RuntimeLibrary of type #{libType.content} which is not #{wantedRuntimeLib}. "+
        "Please open the visual studio solution in the folder and modify the Runtime Library to be #{wantedRuntimeLib}." +
        "If you don't know how google: 'visual studio set project runtime library'"
    end
  end
  
  success "All targets had correct runtime library types"
end

def runWindowsAdmin(cmd)
  shell = WIN32OLE.new('Shell.Application')
  
  shell.ShellExecute("ruby.exe", 
                     "\"#{CurrentDir}/Helpers/WinInstall.rb\" " +
                     "\"#{cmd.gsub( '"', '\\"')}\"", 
                     "#{Dir.pwd}", 'runas')
  
  # TODO: find a proper way to wait here
  info "Please wait while the install script runs and then press any key to continue"
  system "pause"
end

def askToRunAdmin(cmd)
  puts "."
  puts "."
  info "You need to open a new cmd window as administrator and run the following command: "
  info cmd
  info "Sorry, windows is such a pain in the ass"
  system "pause"
end

### Linux only stuff
def getLinuxOS()

  osrelease = `lsb_release -is`.strip

  onError "Failed to run lsb_release" if osrelease.empty?

  osrelease

end


### Standard stuff

# CMake configure
def runCMakeConfigure(additionalArgs)
    
  if BuildPlatform == "linux"
        
    system "cmake .. -DCMAKE_BUILD_TYPE=#{CMakeBuildType} #{additionalArgs}"
        
  else
        
    system "cmake .. -G \"#{VSVersion}\" #{additionalArgs}"
        
  end
end

# Running make or msbuild
def runCompiler(threads)
    
  if BuildPlatform == "linux"
        
    system "make -j #{threads}"
        
  else
    
    #system "start \"ms\" \"MSBuild.exe\" "
    # Would use this if used project.sln file: /target:ALL_BUILD 
    system "#{bringVSToPath} && MSBuild.exe ALL_BUILD.vcxproj /maxcpucount:#{threads} /p:Configuration=RelWithDebInfo"
        
  end
end

# Running platform standard cmake install
def runInstall()
    
  if BuildPlatform == "linux"
        
    system "sudo make install"
        
  else
    
    info "Running install script as Administrator"

    # Requires admin privileges
    runWindowsAdmin("#{bringVSToPath} && MSBuild.exe INSTALL.vcxproj /p:Configuration=RelWithDebInfo")

  end
end

def runGlobberAndCopy(glob, targetFolder)
  onError "globbing for library failed #{glob.LibName}" if not glob.run
  
  FileUtils.cp_r glob.getResult, targetFolder
end

def isInSubdirectory(directory, possiblesub)

  path = Pathname.new(possiblesub)

  if path.fnmatch?(File.join(directory, '**'))
    true
  else
    false
  end
  
end

def createDependencyTargetFolder()

  FileUtils.mkdir_p ProjectDebDirLibs
  
  FileUtils.mkdir_p ProjectDebDirBinaries
  
  FileUtils.mkdir_p ProjectDebDirInclude
  
end


def createLinkIfDoesntExist(source, linkfile)

  if File.exist? linkfile
    return
  end

  FileUtils.ln_sf source, linkfile
  
end

### Download settings ###
class BaseDep
  def initialize(name, foldername)

    @Name = name
    
    @Folder = File.join(CurrentDir, foldername)
    @FolderName = foldername
    
  end

  def RequiresClone
    not File.exist?(@Folder)
  end
  
  def Retrieve
    info "Retrieving #{@Name}"

    Dir.chdir(CurrentDir) do
      
      if self.RequiresClone
        
        info "Cloning #{@Name} into #{@Folder}"

        if not self.DoClone
          onError "Failed to clone repository"
        end

      end

      if not File.exist?(@Folder)
        onError "Retrieve Didn't create a folder for #{@Name} at #{@Folder}"
      end

      if not self.Update
        # Not fatal
        warning "Failed to update dependency #{@Name}"
      end

    end
    
    success "Successfully retrieved #{@Name}"
  end

  def Update
    Dir.chdir(@Folder) do
      self.DoUpdate
    end
  end

  def Setup
    info "Setting up build files for #{@Name}"
    Dir.chdir(@Folder) do
      if not self.DoSetup
        onError "Setup failed for #{@Name}. Is a dependency missing? or some other cmake error?"
      end
    end
    success "Successfully created project files for #{@Name}"
  end
  
  def Compile
    info "Compiling #{@Name}"
    Dir.chdir(@Folder) do
      if not self.DoCompile
        onError "#{@Name} Failed to Compile. Are you using a broken version? or has the setup process"+
                " changed between versions"
      end
    end
    success "Successfully compiled #{@Name}"
  end

  def Install
    info "Installing #{@Name}"
    Dir.chdir(@Folder) do
      if not self.DoInstall
        onError "#{@Name} Failed to install. Did you type in your sudo password?"
      end
    end
    success "Successfully installed #{@Name}"
  end
end


# Copies files to a directory following all symlinks but also copying the symlinks if their
# names are different
# Also if stripfiles is true will run strip on each of the files
def copyPossibleSymlink(path, target, stripfiles = false, log = false)

  if not File.exist?(path)
    warning "Skipping copying non-existant file: #{path}" if log
    return
  end

  info "Copying file: #{path} to #{target}" if log 
  
  if File.lstat(path).symlink?
    
    link = File.join(File.dirname(path), File.readlink(path))

  else
    link = nil
  end

  if link

    info "File #{path} is a symlink to #{link}" if log

    if not File.basename(link) == File.basename(path)

      # Symlink target has a different name, so copy the symlink
      #FileUtils.cp path, target
      linkname = File.join(target, File.basename(path)) 
      FileUtils.ln_sf File.basename(link), linkname

      if not File.lstat(linkname).symlink?
        onError "Link creation failed (#{linkname} => #{File.basename(link)})"
      end
      
      info "Created symlink #{linkname} pointing to #{File.basename(link)}" if log
    end

    # Follow the link
    info "Following symlink to #{link}" if log
    copyPossibleSymlink(link, target, stripfiles, log)
    
  else

    info "Copying plain file: #{path}" if log
    
    # Plain old file
    FileUtils.cp path, target
    
    if stripfiles
      system "strip #{File.join(target, File.basename(path))}"
      info "Stripped file #{File.join(target, File.basename(path))}" if log
    end
  end
end

HandledLibraries = []

# Copies a dependency library to target directory following all symlinks along the way
# Ignores some common things that CMake adds that aren't actually libraries
def copyDependencyLibraries(libs, target, strip, log)

  libs.each do |lib|

    # Skip empty stuff
    if not lib or lib.empty? or lib == "optimized" or lib == "debug" or lib =~ /-l.*/
      next
    end

    # Skip duplicates
    if HandledLibraries.include? lib
      next
    end

    onError "Dependency library file #{lib} doesn't exist" if not File.exists? lib

    copyPossibleSymlink(lib, target, strip, log)
    HandledLibraries.push lib
    
  end
end

# Finds library matching regex and returns that folder
def findLibraryFolder(libs, regex)

  libs.each do |lib|

    if lib =~ regex

      return File.dirname lib
      
    end
  end

  "didn't find library matching regex"
end

# Finds CEGUI plugins and adds them to the list
def findCEGUIPlugins(libs, ceguiversion)

  ceguiDir = findLibraryFolder(libs, /.*CEGUIBase.*/i)

  ceguiDir = File.join(ceguiDir, "cegui-#{ceguiversion}.0")
  
  if not File.exists? ceguiDir
    onError "Failed to find CEGUI root directory for dynamic libs (#{ceguiDir})"
  end

  info "Looking for CEGUI plugins in #{ceguiDir}"

  Dir.chdir(ceguiDir) do

    Dir["*.so"].each do |ceguilib|

      libs.push File.absolute_path(ceguilib)
      
    end
    
  end
  
end

# Finds Ogre plugins and adds them to the list
# Plugin names is an array containing names like 'RenderSystem_GL' and 'Plugin_ParticleFX'
def findOgrePlugins(libs, pluginnames)

  ogreDir = File.join(findLibraryFolder(libs, /.*OgreMain.*/i), "OGRE")

  if not File.exists? ogreDir
    onError "Failed to find Ogre root directory for plugins (#{ogreDir})"
  end

  pluginnames.each do |lib|

    # This is a symbolic link but the dependency copy function should figure it out
    file = File.absolute_path(File.join(ogreDir, lib + ".so"))

    onError "Ogre library #{file} doesn't exist" if not File.exists? file
    
    libs.push file
    
  end
  
end

# Boost thread library is a ld script. This finds the actual libraries
def findRealBoostThread(libs)

  boostDir = findLibraryFolder(libs, /.*boost_thread.*/i)

  if not File.exists? boostDir
    onError "Failed to find boost_thread directory for getting actual libraries (#{boostDir})"
  end
  
  # Copy the actual files
  Dir.chdir(boostDir) do

    Dir["libboost_thread.*"].each do |lib|

      libs.push File.absolute_path(lib)
      
    end
    
  end
end

# Uses ldd on a file to find dependency libraries
def lddFindLibraries(binary)

  result = []

  libs = `ldd "#{binary}"`

  libs.each_line do |line|

    line.strip!

    if line.empty?
      next
    end

    if match = line.match(/\s+=>\s+(.*?\.so[^\s]*)/i)
      
      lib = match.captures[0]

      # Skip non-existing filles
      if not File.exist? lib or not Pathname.new(lib).absolute?
        next
      end

      if not isGoodLDDFound lib
        next
      end

      # And finally skip ones that are in the staging or build directory
      if not isInSubdirectory(CurrentDir, lib)

        puts "ldd found library: " + lib 

        result.push lib
        
      end
    end
  end

  result
end


#
#### Library Install Definitions ###
# These are all the libraries that this script can install
#

class Newton < BaseDep
  def initialize
    super("Newton Dynamics", "newton-dynamics")
  end

  def DoClone
    system "git clone https://github.com/MADEAPPS/newton-dynamics.git"
    $?.exitstatus == 0
  end

  def DoUpdate
    system "git checkout master"
    system "git pull origin master"
    $?.exitstatus == 0
  end

  def DoSetup
    
    if BuildPlatform == "windows"
      
      return File.exist? "packages/projects/visualStudio_2015_dll/build.sln"
    else
      FileUtils.mkdir_p "build"

      Dir.chdir("build") do
        
        runCMakeConfigure "-DNEWTON_DEMOS_SANDBOX=OFF"
        return $?.exitstatus == 0
      end
    end      
  end
  
  def DoCompile
    if BuildPlatform == "windows"
      cmdStr = "#{bringVSToPath} && MSBuild.exe \"packages/projects/visualStudio_2015_dll/build.sln\" " +
               "/maxcpucount:#{CompileThreads} /p:Configuration=release /p:Platform=\"x64\""
      system cmdStr
      return $?.exitstatus == 0
    else
      Dir.chdir("build") do
        
        runCompiler CompileThreads
        
      end
      return $?.exitstatus == 0
    end
  end
  
  def DoInstall
    
    # Copy files to ProjectDir dependencies folder
    createDependencyTargetFolder

    runGlobberAndCopy(Globber.new("Newton.h", File.join(@Folder, "coreLibrary_300/source")),
                          ProjectDebDirInclude)
    
    if BuildPlatform == "linux"

      runGlobberAndCopy(Globber.new("libNewton.so", File.join(@Folder, "build/lib")),
                            ProjectDebDirLibs)

    else

      runGlobberAndCopy(Globber.new("newton.dll",
                                    File.join(@Folder, "coreLibrary_300/projects/windows")),
                            ProjectDebDirBinaries)

      runGlobberAndCopy(Globber.new("newton.lib",
                                    File.join(@Folder, "coreLibrary_300/projects/windows")),
                            ProjectDebDirLibs)
    end
    true
  end
end

class OpenAL < BaseDep
  def initialize
    super("OpenAL Soft", "openal-soft")
    onError "Use OpenAL from package manager on linux" if BuildPlatform != "windows"
  end

  def DoClone
    system "git clone https://github.com/kcat/openal-soft.git"
    $?.exitstatus == 0
  end

  def DoUpdate
    system "git checkout master"
    system "git pull origin master"
    $?.exitstatus == 0
  end

  def DoSetup
    FileUtils.mkdir_p "build"

    Dir.chdir("build") do
      
      runCMakeConfigure "-DALSOFT_UTILS=OFF -DALSOFT_EXAMPLES=OFF -DALSOFT_TESTS=OFF"
    end
    
    $?.exitstatus == 0
  end
  
  def DoCompile

    Dir.chdir("build") do
      runCompiler CompileThreads
    end
    $?.exitstatus == 0
  end
  
  def DoInstall
    return false if not DoSudoInstalls
    
    Dir.chdir("build") do
      runInstall
      
      if BuildPlatform == "windows" and not File.exist? "C:/Program Files/OpenAL/include/OpenAL"
        # cAudio needs OpenAL folder in include folder, which doesn't exist. 
        # So we create it here
        askToRunAdmin("mklink /D \"C:/Program Files/OpenAL/include/OpenAL\" " + 
                      "\"C:/Program Files/OpenAL/include/AL\"")
      end
    end
    $?.exitstatus == 0
  end
end

class CAudio < BaseDep
  def initialize
    super("cAudio", "cAudio")
  end

  def DoClone
    #system "git clone https://github.com/R4stl1n/cAudio.git"
    # Official repo is broken
    system "git clone https://github.com/hhyyrylainen/cAudio.git"
    $?.exitstatus == 0
  end

  def DoUpdate
    system "git checkout master"
    system "git pull origin master"
    $?.exitstatus == 0
  end

  def DoSetup
    FileUtils.mkdir_p "build"

    Dir.chdir("build") do
      
      if BuildPlatform == "windows"
        # The bundled ones aren't compatible with our compiler setup 
        # -DCAUDIO_DEPENDENCIES_DIR=../Dependencies64
        runCMakeConfigure "-DCAUDIO_BUILD_SAMPLES=OFF -DCAUDIO_DEPENDENCIES_DIR=\"C:/Program Files/OpenAL\" " +
                          "-DCMAKE_INSTALL_PREFIX=./Install"
      else
        runCMakeConfigure "-DCAUDIO_BUILD_SAMPLES=OFF"
      end
    end
    
    $?.exitstatus == 0
  end
  
  def DoCompile

    Dir.chdir("build") do
      runCompiler CompileThreads
    end
    $?.exitstatus == 0
  end
  
  def DoInstall
    
    Dir.chdir("build") do
      if BuildPlatform == "windows"
        
        system "#{bringVSToPath} && MSBuild.exe INSTALL.vcxproj /p:Configuration=RelWithDebInfo"
        
        # And then to copy the libs
        
        FileUtils.mkdir_p File.join(CurrentDir, "cAudio")
        FileUtils.mkdir_p File.join(CurrentDir, "cAudio", "lib")
        FileUtils.mkdir_p File.join(CurrentDir, "cAudio", "bin")
        
        FileUtils.cp File.join(@Folder, "build/bin/RelWithDebInfo", "cAudio.dll"),
                     File.join(CurrentDir, "cAudio", "bin")

        FileUtils.cp File.join(@Folder, "build/lib/RelWithDebInfo", "cAudio.lib"),
                     File.join(CurrentDir, "cAudio", "lib")
        
        FileUtils.copy_entry File.join(@Folder, "build/Install/", "include"),
                             File.join(CurrentDir, "cAudio", "include")
        
      else
        return true if not DoSudoInstalls
        runInstall
      end
    end
    $?.exitstatus == 0
  end
end

class AngelScript < BaseDep
  def initialize
    super("AngelScript", "angelscript")
    @WantedURL = "http://svn.code.sf.net/p/angelscript/code/tags/2.31.2"

    if @WantedURL[-1, 1] == '/'
      abort "Invalid configuraion in Setup.rb AngelScript tag has an ending '/'. Remove it!"
    end
  end

  def DoClone
    system "svn co #{@WantedURL} angelscript"
    $?.exitstatus == 0
  end

  def DoUpdate

    # Check is tag correct
    match = `svn info`.strip.match(/.*URL:\s?(.*angelscript\S+).*/i)

    abort("'svn info' unable to find URL with regex") if !match
    
    currenturl = match.captures[0]

    if currenturl != @WantedURL
      
      info "Switching AngelScript tag from #{currenturl} to #{@WantedURL}"
      
      system "svn switch #{@WantedURL}"
      onError "Failed to switch svn url" if $?.exitstatus > 0
    end
    
    system "svn update"
    $?.exitstatus == 0
  end

  def DoSetup
    if BuildPlatform == "windows"
      
      return File.exist? "sdk/angelscript/projects/msvc2015/angelscript.sln"
    else
      return true
    end
  end
  
  def DoCompile

    if BuildPlatform == "linux"
      Dir.chdir("sdk/angelscript/projects/gnuc") do
        
        system "make -j #{CompileThreads}"
        
      end
      $?.exitstatus == 0
    else
      
      info "Verifying that angelscript solution has Runtime Library = MultiThreadedDLL"
      verifyVSProjectRuntimeLibrary "sdk/angelscript/projects/msvc2015/angelscript.vcxproj", 
                                    %r{Release\|x64}, "MultiThreadedDLL"  
      
      success "AngelScript solution is correctly configured. Compiling"
      
      cmdStr = "#{bringVSToPath} && MSBuild.exe \"sdk/angelscript/projects/msvc2015/angelscript.sln\" " +
               "/maxcpucount:#{CompileThreads} /p:Configuration=Release /p:Platform=\"x64\""
      system cmdStr
      return $?.exitstatus == 0
    end
  end
  
  def DoInstall

    # Copy files to Project folder
    createDependencyTargetFolder

    # First header files and addons
    FileUtils.cp File.join(@Folder, "sdk/angelscript/include", "angelscript.h"),
                 ProjectDebDirInclude

    addondir = File.join(ProjectDebDirInclude, "add_on")

    FileUtils.mkdir_p addondir

    # All the addons from
    # `ls -m | awk 'BEGIN { RS = ","; ORS = ", "}; NF { print "\""$1"\""};'`
    addonnames = Array[
      "autowrapper", "contextmgr", "datetime", "debugger", "scriptany", "scriptarray",
      "scriptbuilder", "scriptdictionary", "scriptfile", "scriptgrid", "scripthandle",
      "scripthelper", "scriptmath", "scriptstdstring", "serializer", "weakref"
    ]

    addonnames.each do |x|

      FileUtils.copy_entry File.join(@Folder, "sdk/add_on/", x),
                           File.join(addondir, x)
    end

    # Then the library
    if BuildPlatform == "linux"

      FileUtils.cp File.join(@Folder, "sdk/angelscript/lib", "libangelscript.a"),
                   ProjectDebDirLibs
      
    else
      FileUtils.cp File.join(@Folder, "sdk/angelscript/lib", "angelscript64.lib"),
                   ProjectDebDirLibs
    end
    true
  end
end

class Breakpad < BaseDep
  def initialize
    super("Google Breakpad", "breakpad")
    @DepotFolder = File.join(CurrentDir, "depot_tools")
    @CreatedNewFolder = false
  end

  def RequiresClone
    if File.exist?(@DepotFolder) and File.exist?(@Folder)
      return false
    end
    
    true
  end
  
  def DoClone

    # Depot tools
    system "git clone https://chromium.googlesource.com/chromium/tools/depot_tools.git"
    return false if $?.exitstatus > 0

    if not File.exist?(@Folder)
      
      FileUtils.mkdir_p @Folder
      @CreatedNewFolder = true
      
    end
    
    true
  end

  def DoUpdate
    
    if BuildPlatform == "windows" and NoBreakpadUpdateOnWindows
      info "Windows: skipping Breakpad update"
      if not File.exist?("src")
        @CreatedNewFolder = true
      end
      return true
    end

    # Update depot tools
    Dir.chdir(@DepotFolder) do
      system "git checkout master"
      system "git pull origin master"
    end

    if $?.exitstatus > 0
      return false
    end

    if not @CreatedNewFolder
      
      if not File.exist?("src")
        # This is set to true if we created an empty folder but we didn't get to the pull stage
        @CreatedNewFolder = true
      else
        Dir.chdir(@Folder) do
          # The first source subdir is the git repository
          Dir.chdir("src") do
            system "git checkout master"
            system "git pull origin master"
            system "gclient sync"
          end
        end
      end
    end
    
    true
  end

  def DoSetup
    
    if not @CreatedNewFolder
      return true
    end
    
    # Bring the depot tools to path
    pathedit = PathModifier.new(@DepotFolder)

    # Get source for breakpad
    Dir.chdir(@Folder) do

      system "fetch breakpad"

      if $?.exitstatus > 0
        pathedit.Restore
        onError "fetch breakpad failed"
      end
      
      Dir.chdir("src") do

        # Configure script
        if BuildPlatform == "windows"
          system "src/tools/gyp/gyp.bat src/client/windows/breakpad_client.gyp –no-circular-check"
        else
          system "./configure"
        end
        
        if $?.exitstatus > 0
          pathedit.Restore
          onError "configure breakpad failed" 
        end
      end
    end

    pathedit.Restore
    true
  end
  
  def DoCompile

    # Bring the depot tools to path
    pathedit = PathModifier.new(@DepotFolder)

    # Build breakpad
    Dir.chdir(File.join(@Folder, "src")) do
      
      if BuildPlatform == "linux"
        system "make -j #{CompileThreads}"
        
        if $?.exitstatus > 0
          pathedit.Restore
          onError "breakpad build failed" 
        end
      else
        
        info "Please open the solution at and compile breakpad client in Release and x64. " +
             "Remember to disable treat warnings as errors first: "+
             "#{CurrentDir}/breakpad/src/src/client/windows/breakpad_client.sln"
        
        system "start #{CurrentDir}/breakpad/src/src/client/windows/breakpad_client.sln" if AutoOpenVS
        system "pause"
      end
    end
    
    pathedit.Restore
    true
  end
  
  def DoInstall

    # Create target folders
    FileUtils.mkdir_p File.join(CurrentDir, "Breakpad", "lib")
    FileUtils.mkdir_p File.join(CurrentDir, "Breakpad", "bin")

    breakpadincludelink = File.join(CurrentDir, "Breakpad", "include")
    
    if BuildPlatform == "windows"

      askToRunAdmin "mklink /D \"#{breakpadincludelink}\" \"#{File.join(@Folder, "src/src")}\""
      
      FileUtils.copy_entry File.join(@Folder, "src/src/client/windows/Release/lib"),
                           File.join(CurrentDir, "Breakpad", "lib")
      
      
      
    # Might be worth it to have windows symbols dumbed on windows, if the linux dumber can't deal with pdbs
    #FileUtils.cp File.join(@Folder, "src/src/tools/linux/dump_syms", "dump_syms"),
    #             File.join(CurrentDir, "Breakpad", "bin")
      
    else
      
      # Need to delete old file before creating a new symlink
      File.delete(breakpadincludelink) if File.exist?(breakpadincludelink)
      FileUtils.ln_s File.join(@Folder, "src/src"), breakpadincludelink
      
      FileUtils.cp File.join(@Folder, "src/src/client/linux", "libbreakpad_client.a"),
                   File.join(CurrentDir, "Breakpad", "lib")

      FileUtils.cp File.join(@Folder, "src/src/tools/linux/dump_syms", "dump_syms"),
                   File.join(CurrentDir, "Breakpad", "bin")

      FileUtils.cp File.join(@Folder, "src/src/processor", "minidump_stackwalk"),
                   File.join(CurrentDir, "Breakpad", "bin")
    end
    true
  end
end

class Ogre < BaseDep
  def initialize
    super("Ogre", "ogre")
  end

  def RequiresClone
    if BuildPlatform == "windows"
      return (not File.exist?(@Folder) or not File.exist?(File.join(@Folder, "Dependencies")))
    else
      return (not File.exist? @Folder)
    end
  end
  
  def DoClone
    if BuildPlatform == "windows"

      system "hg clone https://bitbucket.org/sinbad/ogre"
      if $?.exitstatus > 0
        return false
      end
      
      Dir.chdir(@Folder) do

        system "hg clone https://bitbucket.org/cabalistic/ogredeps Dependencies"
      end
      return $?.exitstatus == 0
    else
      system "hg clone https://bitbucket.org/sinbad/ogre"
      return $?.exitstatus == 0
    end
  end

  def DoUpdate
    
    if BuildPlatform == "windows"
      Dir.chdir("Dependencies") do
        system "hg pull"
        system "hg update"
        
        if $?.exitstatus > 0
          return false
        end
      end
    end
    
    system "hg pull"
    system "hg update v2-0"
    $?.exitstatus == 0
  end

  def DoSetup
    
    # Dependencies compile
    additionalCMake = ""
    
    if BuildPlatform == "windows"
      Dir.chdir("Dependencies") do
        
        system "cmake . -DOGREDEPS_BUILD_SDL2=OFF" 
        
        system "#{bringVSToPath} && MSBuild.exe ALL_BUILD.vcxproj /maxcpucount:#{CompileThreads} /p:Configuration=Debug"
        onError "Failed to compile Ogre dependencies " if $?.exitstatus > 0
        
        runCompiler CompileThreads
        onError "Failed to compile Ogre dependencies " if $?.exitstatus > 0

        info "Please open the solution SDL2 in Release and x64: "+
             "#{@Folder}/Dependencies/src/SDL2/VisualC/SDL_VS2013.sln"
        
        system "start #{@Folder}/Dependencies/src/SDL2/VisualC/SDL_VS2013.sln" if AutoOpenVS
        system "pause"
        
        additionalCMake = "-DSDL2MAIN_LIBRARY=..\SDL2\VisualC\Win32\Debug\SDL2main.lib " +
                          "-DSD2_INCLUDE_DIR=..\SDL2\include"
        "-DSDL2_LIBRARY_TEMP=..\SDL2\VisualC\Win32\Debug\SDL2.lib"
        
      end
    end
    
    FileUtils.mkdir_p "build"
    
    Dir.chdir("build") do

      runCMakeConfigure "-DOGRE_BUILD_RENDERSYSTEM_GL3PLUS=ON " +
                        "-DOGRE_BUILD_RENDERSYSTEM_D3D9=OFF -DOGRE_BUILD_RENDERSYSTEM_D3D11=OFF "+
                        "-DOGRE_BUILD_COMPONENT_OVERLAY=OFF " +
                        "-DOGRE_BUILD_COMPONENT_PAGING=OFF -DOGRE_BUILD_COMPONENT_PROPERTY=OFF " +
                        "-DOGRE_BUILD_COMPONENT_TERRAIN=OFF -DOGRE_BUILD_COMPONENT_VOLUME=OFF "+
                        "-DOGRE_BUILD_PLUGIN_BSP=OFF -DOGRE_BUILD_PLUGIN_CG=ON " +
                        "-DOGRE_BUILD_PLUGIN_OCTREE=OFF -DOGRE_BUILD_PLUGIN_PCZ=OFF -DOGRE_BUILD_SAMPLES=OFF " + 
                        additionalCMake
    end
    
    $?.exitstatus == 0
  end
  
  def DoCompile
    Dir.chdir("build") do
      if BuildPlatform == "windows"
        system "#{bringVSToPath} && MSBuild.exe ALL_BUILD.vcxproj /maxcpucount:#{CompileThreads} /p:Configuration=Release"
        system "#{bringVSToPath} && MSBuild.exe ALL_BUILD.vcxproj /maxcpucount:#{CompileThreads} /p:Configuration=RelWithDebInfo"
      else
        runCompiler CompileThreads
      end
    end
    
    $?.exitstatus == 0
  end
  
  def DoInstall

    Dir.chdir("build") do
      
      if BuildPlatform == "windows"

        system "#{bringVSToPath} && MSBuild.exe INSTALL.vcxproj /p:Configuration=RelWithDebInfo"
        ENV["OGRE_HOME"] = "#{@Folder}/build/ogre/sdk"
        
      else
        return true if not DoSudoInstalls
        runInstall
      end
    end

    $?.exitstatus == 0
  end
end

# Windows only CEGUI dependencies
class CEGUIDependencies < BaseDep
  def initialize
    super("CEGUI Dependencies", "cegui-dependencies")
  end

  def DoClone

    system "hg clone https://bitbucket.org/cegui/cegui-dependencies"
    $?.exitstatus == 0
  end

  def DoUpdate
    system "hg pull"
    system "hg update default"
    $?.exitstatus == 0
  end

  def DoSetup

    FileUtils.mkdir_p "build"

    if InstallCEED
      python = "ON"
    else
      python = "OFF"
    end

    Dir.chdir("build") do
      runCMakeConfigure "-DCEGUI_BUILD_PYTHON_MODULES=#{python} "
    end
    
    $?.exitstatus == 0
  end
  
  def DoCompile

    Dir.chdir("build") do
      system "#{bringVSToPath} && MSBuild.exe ALL_BUILD.vcxproj /maxcpucount:#{CompileThreads} /p:Configuration=Debug"
      system "#{bringVSToPath} && MSBuild.exe ALL_BUILD.vcxproj /maxcpucount:#{CompileThreads} /p:Configuration=RelWithDebInfo"
    end
    $?.exitstatus == 0
  end
  
  def DoInstall

    FileUtils.copy_entry File.join(@Folder, "build", "dependencies"),
                         File.join(CurrentDir, "cegui", "dependencies")
    $?.exitstatus == 0
  end
end

# Depends on Ogre to be installed
class CEGUI < BaseDep
  def initialize
    super("CEGUI", "cegui")
  end

  def DoClone

    system "hg clone https://bitbucket.org/cegui/cegui"
    $?.exitstatus == 0
  end

  def DoUpdate
    system "hg pull"
    #system "hg update default"

    # TODO: allow configuring this commit
    system "hg update 6510156"
    
    $?.exitstatus == 0
  end

  def DoSetup

    FileUtils.mkdir_p "build"

    if InstallCEED
      python = "ON"
    else
      python = "OFF"
    end

    Dir.chdir("build") do
      # Use UTF-8 strings with CEGUI (string class 1)
      runCMakeConfigure "-DCEGUI_STRING_CLASS=1 " +
                        "-DCEGUI_BUILD_APPLICATION_TEMPLATES=OFF -DCEGUI_BUILD_PYTHON_MODULES=#{python} " +
                        "-DCEGUI_SAMPLES_ENABLED=OFF -DCEGUI_BUILD_RENDERER_DIRECT3D11=OFF -DCEGUI_BUILD_RENDERER_OGRE=ON " +
                        "-DCEGUI_BUILD_RENDERER_OPENGL=OFF -DCEGUI_BUILD_RENDERER_OPENGL3=OFF"
    end
    
    $?.exitstatus == 0
  end
  
  def DoCompile

    Dir.chdir("build") do
      runCompiler CompileThreads 
    end
    $?.exitstatus == 0
  end
  
  def DoInstall

    return true if not DoSudoInstalls or BuildPlatform == "windows"
    
    Dir.chdir("build") do
      runInstall
    end
    $?.exitstatus == 0
  end
end

class SFML < BaseDep
  def initialize
    super("SFML", "SFML")
  end

  def DoClone
    system "git clone https://github.com/SFML/SFML.git"
    $?.exitstatus == 0
  end

  def DoUpdate
    system "git checkout master"
    system "git pull origin master"
    $?.exitstatus == 0
  end

  def DoSetup
    FileUtils.mkdir_p "build"

    Dir.chdir("build") do
      runCMakeConfigure ""
    end
    
    $?.exitstatus == 0
  end
  
  def DoCompile

    Dir.chdir("build") do
      
      if BuildPlatform == "windows"
        system "#{bringVSToPath} && MSBuild.exe ALL_BUILD.vcxproj /maxcpucount:#{CompileThreads} /p:Configuration=Debug"
      end
      
      runCompiler CompileThreads
    end
    $?.exitstatus == 0
  end
  
  def DoInstall

    return true if not DoSudoInstalls or BuildPlatform == "windows"
    
    Dir.chdir("build") do
      runInstall
    end
    $?.exitstatus == 0
  end

  def LinuxPackages
    if Linux == "Fedora"
      return Array["xcb-util-image-devel", "systemd-devel", "libjpeg-devel", "libvorbis-devel",
                   "flac-devel"]
    else
      onError "LinuxPackages not done for this linux system"
    end
  end
end


### LDD found libraries that should be included in full package
# Used to filter ldd results
def isGoodLDDFound(lib)

  case lib
  when /.*swresample.*/i
    true
  when /.*vorbis.*/i
    true
  when /.*theora.*/i
    true
  when /.*opus.*/i
    true
  when /.*pcre.*/i
    true
  when /.*ogg.*/i
    true
  when /.*tinyxml.*/i
    true
  when /.*avcodec.*/i
    true
  when /.*avformat.*/i
    true
  when /.*avutil.*/i
    true
  when /.*swscale.*/i
    true
  when /.*rtmp.*/i
    true
  when /.*gsm.*/i
    true
  when /.*soxr.*/i
    true
  when /.*vpx.*/i
    true
  when /.*x2.*/i
    true
  when /.*libstdc++.*/i
    true
  when /.*jpeg.*/i
    true
  when /.*jxrglue.*/i
    true
  when /.*IlmImf.*/i
    true
  when /.*Imath.*/i
    true
  when /.*Half.*/i
    true
  when /.*Iex.*/i
    true
  when /.*IlmThread.*/i
    true
  when /.*openjp.*/i
    true
  when /.*libraw.*/i
    true
  when /.*png.*/i
    true
  when /.*freeimage.*/i
    true
  when /.*gnutls.*/i
    true
  when /.*atomic.*/i
    true
  when /.*zzip.*/i
    true
  when /.*Cg.*/i
    true
  when /.*va.*/i
    true
  when /.*xvid.*/i
    true
  when /.*zvbi.*/i
    true
  when /.*amr.*/i
    true
  when /.*mfx.*/i
    true
  when /.*aac.*/i
    true
  # nvidia stuff for ffmpeg
  when /.*nvcu.*/i
    true
  when /.*cuda.*/i
    true
  when /.*nvidia-fatbinary.*/i
    true
  when /.*vdpau.*/i
    true
  when /.*twolame.*/i
    true
  when /.*h26.*/i
    true
  when /.*mp3.*/i
    true
  when /.*bluray.*/i
    true
  when /.*OpenCL.*/i
    true
  when /.*webp.*/i
    true
  when /.*schroedinger.*/i
    true
  when /.*Xaw.*/i
    true
  when /.*numa.*/i
    true
  when /.*hogweed.*/i
    true
  when /.*jasper.*/i
    true
  else
    false
  end
end

