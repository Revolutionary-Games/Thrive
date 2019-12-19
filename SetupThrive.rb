#!/usr/bin/env ruby

# Setup script for Thrive.

require 'English'

# RubySetupSystem Bootstrap
if !File.exist? 'RubySetupSystem/RubySetupSystem.rb'
  puts 'Initializing RubySetupSystem'
  system 'git submodule init && git submodule update --recursive'

  if $CHILD_STATUS.exitstatus != 0
    abort('Failed to initialize or update git submodules. ' \
          'Please make sure git is in path and ' \
          'you have an ssh key setup for your github account')
  end
else
  # Make sure RubySetupSystem is up to date
  # This may make debugging RubySetupSystem harder so feel free to comment out
  system 'git submodule update --init'
end

require 'fileutils'

require_relative 'RubySetupSystem/RubyCommon.rb'

def checkRunFolder(suggested)
  onError('Not ran from Thrive base directory!') unless File.exist?('SetupThrive.rb')

  thirdPartyFolder = File.join suggested, 'ThirdParty'

  FileUtils.mkdir_p thirdPartyFolder
  FileUtils.mkdir_p File.join suggested, 'build', 'ThirdParty'

  thirdPartyFolder
end

def projectFolder(baseDir)
  File.expand_path File.join(baseDir, '../')
end

require_relative 'RubySetupSystem/RubySetupSystem.rb'
require_relative 'RubySetupSystem/Libraries/SetupLeviathan.rb'

leviathan = Leviathan.new(
  # Use this if you always want the latest commit
  # version: "develop",
  version: '8968d19a7d9d0f0a4cf0229eb89474124cc5e87c',
  # Doesn't actually work, but leviathan doesn't install with sudo by
  # default, or install at all for that matter
  noInstallSudo: true
)

puts ''
puts ''

info 'Running the engine compilation'

installer = Installer.new([leviathan])

installer.run

info 'Thrive folder setup'

onError "'thrive' folder is missing" unless File.exist? ProjectDir

success 'Thrive folder exists'

Dir.chdir(ProjectDir) do
  system 'git pull'

  warning 'Failed to pull thrive repo' if $CHILD_STATUS.exitstatus > 0

  runOpen3Checked('git', 'submodule', 'update', '--recursive')

  info 'Checking assets'

  runOpen3Checked('git', 'lfs', 'pull')

  success 'git lfs pull ran successfully. Assets should be fine.'

  FileUtils.mkdir_p 'build'
end

# Symlink the textures and fonts from assets to make local previewing of the GUI easier
if OS.windows?
  info 'Creating junctions for assets to be referenced from gui ' \
       'html without running cmake every time'
  runSystemSafe 'cmd', '/c', 'mklink', '/J',
                convertPathToWindows(File.join(ProjectDir, 'Textures')),
                convertPathToWindows(File.join(ProjectDir, 'assets', 'textures'))
  runSystemSafe 'cmd', '/c', 'mklink', '/J',
                convertPathToWindows(File.join(ProjectDir, 'Fonts')),
                convertPathToWindows(File.join(ProjectDir, 'assets', 'fonts'))
  runSystemSafe 'cmd', '/c', 'mklink', '/J',
                convertPathToWindows(File.join(ProjectDir, 'JSVendor')),
                convertPathToWindows(File.join(ProjectDir, 'ThirdParty/Leviathan/bin/Data',
                                               'JSVendor'))
else
  unless File.exist? File.join(ProjectDir, 'Textures')
    FileUtils.ln_sf File.join(ProjectDir, 'assets', 'textures'),
                    File.join(ProjectDir, 'Textures')
  end

  unless File.exist? File.join(ProjectDir, 'Fonts')
    FileUtils.ln_sf File.join(ProjectDir, 'assets', 'fonts'),
                    File.join(ProjectDir, 'Fonts')
  end

  unless File.exist? File.join(ProjectDir, 'JSVendor')
    FileUtils.ln_sf File.join(ProjectDir, 'ThirdParty/Leviathan/bin/Data', 'JSVendor'),
                    File.join(ProjectDir, 'JSVendor')
  end
end

success 'Thrive folder and assets are good to go'

info 'Compiling thrive'

# Build directory is made earlier
Dir.chdir(File.join(ProjectDir, 'build')) do
  unless runCMakeConfigure []
    onError 'Failed to configure Thrive. Are you using a broken version, ' \
            'or did a dependency fail to install?'
  end

  onError 'Failed to compile Thrive' unless TC.runCompiler
end

success 'Done compiling thrive'

if OS.windows?
  info 'Open build/Thrive.sln and start coding'
else
  info "run the game with 'cd #{ProjectDir}/build/bin && ./Thrive'"
end

puts ''
info 'NOTE: when changing the scripts or assets you must rerun cmake to make it move the ' \
     'changed files to the build folder'

success 'Done'

exit 0
