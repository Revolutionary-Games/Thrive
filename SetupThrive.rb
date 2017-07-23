#!/usr/bin/env ruby
# coding: utf-8
# Setup script for Thrive.

# RubySetupSystem Bootstrap
if not File.exists? "RubySetupSystem/RubySetupSystem.rb"
  puts "Initializing RubySetupSystem"
  system "git submodule init --recursive && git submodule update --recursive"

  if $?.exitstatus != 0
    abort("Failed to initialize or update git submodules. " +
          "Please make sure git is in path and " +
          "you have an ssh key setup for your github account")
  end
end

require 'fileutils'


require_relative 'RubySetupSystem/RubyCommon.rb'

def checkRunFolder(suggested)

  versionFile = File.join(suggested, "thriveversion.ver")

  onError("Not ran from Thrive base directory!") if not File.exist?(versionFile)

  thirdPartyFolder = File.join suggested, "ThirdParty"

  FileUtils.mkdir_p thirdPartyFolder
  FileUtils.mkdir_p File.join suggested, "build", "ThirdParty"
  
  thirdPartyFolder
  
end

def projectFolder(baseDir)

  File.expand_path File.join(baseDir, "../")
  
end

def parseExtraArgs

end

require_relative 'RubySetupSystem/RubySetupSystem.rb'
require_relative 'RubySetupSystem/Libraries/SetupLeviathan.rb'

leviathan = Leviathan.new(
  version: "develop",
  # Doesn't actually work, but leviathan doesn't install with sudo by
  # default, or install at all for that matter
  noInstallSudo: true
)

puts ""
puts ""

info "Running the engine compilation"

installer = Installer.new([leviathan])

installer.run

info "Thrive folder setup"

if not File.exist? ProjectDir

  onError "'thrive' folder is missing"

end

success "Thrive folder exists"

Dir.chdir(ProjectDir) do
  
  system "git pull"

  if $?.exitstatus > 0

    warning "Failed to pull thrive repo"
    
  end

  systemChecked "git submodule update --recursive"

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

  info "Building luajit"

  Dir.chdir(File.join(CurrentDir, "thrive", "contrib/lua/luajit/src")) do

    # Make sure XCFLAGS+= -DLUAJIT_ENABLE_LUA52COMPAT is uncommented
    outdata = File.read("Makefile").gsub(/#XCFLAGS\+= -DLUAJIT_ENABLE_LUA52COMPAT/,
                                       "XCFLAGS+= -DLUAJIT_ENABLE_LUA52COMPAT")

    File.open("Makefile", 'w') do |out|
      out << outdata
    end  
    
    runCompiler CompileThreads
    
    onError "Failed to compile luajit" if $?.exitstatus > 0
    
  end

  success "luajit is ok"
  
  FileUtils.mkdir_p "build"

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

info "run the game with '#{CurrentDir}/thrive/build/Thrive'"

success "Done"

exit 0

