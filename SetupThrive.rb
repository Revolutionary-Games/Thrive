#!/usr/bin/env ruby
# coding: utf-8
# Setup script for Thrive.

# RubySetupSystem Bootstrap
if not File.exists? "RubySetupSystem/RubySetupSystem.rb"
  puts "Initializing RubySetupSystem"
  system "git submodule init && git submodule update --recursive"

  if $?.exitstatus != 0
    abort("Failed to initialize or update git submodules. " +
          "Please make sure git is in path and " +
          "you have an ssh key setup for your github account")
  end
else
  # Make sure RubySetupSystem is up to date
  # This may make debugging RubySetupSystem harder so feel free to comment out
  system "git submodule update"
end

require 'fileutils'


require_relative 'RubySetupSystem/RubyCommon.rb'

def checkRunFolder(suggested)

  versionFile = File.join(suggested, "thriveversion.ver")

  onError("Not ran from Thrive base directory!") if not File.exist?("SetupThrive.rb")

  thirdPartyFolder = File.join suggested, "ThirdParty"

  FileUtils.mkdir_p thirdPartyFolder
  FileUtils.mkdir_p File.join suggested, "build", "ThirdParty"
  
  thirdPartyFolder
  
end

def projectFolder(baseDir)

  File.expand_path File.join(baseDir, "../")
  
end

def parseExtraArgs

  if ARGV.length > 1

    onError("Unrecognized command line options.\n" +
            "Expected only username in addition to other arguments. Got: #{ARGV.join(' ')}")
    
  end
  
  $svnUser = ARGV[0]
  ARGV.shift

  # Handle provided password
  if $svnUser.count ':'
    split = $svnUser.split ':'
    $svnUser = split[0]
    $svnPassword = split[1]
  end
  
end

require_relative 'RubySetupSystem/RubySetupSystem.rb'
require_relative 'RubySetupSystem/Libraries/SetupLeviathan.rb'

if !$svnUser
  $svnUser = "thrive"
  $svnPassword = "thrive"
end

WantedURL = "https://#{$svnUser}@boostslair.com/svn/thrive_assets"

leviathan = Leviathan.new(
  # Use this if you always want the latest commit
  # version: "develop",
  version: "02599f1c44a37d6d84bf9cdd1051b8debdd4815c",
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

  runOpen3Checked("git", "submodule", "update", "--recursive")

  info "Checking assets"

  isInteractive = $stdout.isatty

  puts "Interactive mode is: " + isInteractive.to_s

  if not File.exist? "assets"
    
    info "Getting assets"

    params = ["svn", "checkout", "--username", $svnUser]

    if !isInteractive
      params.push "--non-interactive"
    end

    if $svnPassword
      params.push("--password", $svnPassword)
    end
    
    params.push(WantedURL, "assets")

    system(*params)

    if $?.exitstatus != 0
      onError "Failed to get thrive assets repository"
    end
    
  else

    info "Updating assets"

    Dir.chdir("assets") do

      verifySVNUrl(WantedURL)

      if $svnPassword
        system("svn", "up", "--username", $svnUser, "--password", $svnPassword)
      else
        system "svn up"
      end
      onError "Failed to update thrive assets" if $?.exitstatus > 0
      
    end
    
  end
  
  success "Assets are good to go"

  # info "Building luajit"

  # Dir.chdir(File.join(ProjectDir, "contrib/lua/luajit/src")) do

  #   # Make sure XCFLAGS+= -DLUAJIT_ENABLE_LUA52COMPAT is uncommented
  #   outdata = File.read("Makefile").gsub(/#XCFLAGS\+= -DLUAJIT_ENABLE_LUA52COMPAT/,
  #                                      "XCFLAGS+= -DLUAJIT_ENABLE_LUA52COMPAT")

  #   File.open("Makefile", 'w') do |out|
  #     out << outdata
  #   end  
    
  #   runCompiler $compileThreads
    
  #   onError "Failed to compile luajit" if $?.exitstatus > 0
    
  # end

  # success "luajit is ok"
  
  FileUtils.mkdir_p "build"

end

# Symlink the textures and fonts from assets to make local previewing of the GUI easier
if OS.windows?
  info "Creating junctions for assets to be referenced from gui " +
       "html without running cmake every time"
  runSystemSafe "cmd", "/c", "mklink", "/J",
                convertPathToWindows(File.join(ProjectDir, "Textures")),
                convertPathToWindows(File.join(ProjectDir, "assets", "textures"))
  runSystemSafe "cmd", "/c", "mklink", "/J",
                convertPathToWindows(File.join(ProjectDir, "Fonts")),
                convertPathToWindows(File.join(ProjectDir, "assets", "fonts"))
  runSystemSafe "cmd", "/c", "mklink", "/J",
                convertPathToWindows(File.join(ProjectDir, "JSVendor")),
                convertPathToWindows(File.join(ProjectDir, "ThirdParty/Leviathan/bin/Data",
                                               "JSVendor"))  
else
  if !File.exists? File.join(ProjectDir, "Textures")
    FileUtils.ln_sf File.join(ProjectDir, "assets", "textures"),
                    File.join(ProjectDir, "Textures")
  end

  if !File.exists? File.join(ProjectDir, "Fonts")
    FileUtils.ln_sf File.join(ProjectDir, "assets", "fonts"),
                    File.join(ProjectDir, "Fonts")
  end

  if !File.exists? File.join(ProjectDir, "JSVendor")
    FileUtils.ln_sf File.join(ProjectDir, "ThirdParty/Leviathan/bin/Data", "JSVendor"),
                    File.join(ProjectDir, "JSVendor")
  end
end

success "Thrive folder and assets are good to go"


info "Compiling thrive"


# Build directory is made earlier

Dir.chdir(File.join(ProjectDir, "build")) do

  if !runCMakeConfigure []
    onError "Failed to configure Thrive. Are you using a broken version, " +
            "or did a dependency fail to install?"
  end

  if !TC.runCompiler
    onError "Failed to compile Thrive"
  end
  
end

success "Done compiling thrive"

if OS.windows?
  info "Open build/Thrive.sln and start coding"
else
  info "run the game with '#{CurrentDir}/thrive/build/Thrive'"
end

puts ""
info "NOTE: when changing the scripts or assets you must rerun cmake to make it move the " + 
     "changed files to the build folder"

success "Done"

exit 0

