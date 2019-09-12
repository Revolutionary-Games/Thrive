#!/usr/bin/env ruby
# Creates release packages for Thrive.
# You MUST compile thrive in RelWithDebInfo with release cmake options before running this
require 'os'
require "fileutils"

if OS.mac?
  onError "unsupported platform"
end

require_relative 'RubySetupSystem/ReleaseCreator'


# Customize options for Thrive
VERSION = File.read("src/thrive_version.h").match(/Thrive_VERSIONS\s+"(.*)"/).captures[0]

if !VERSION
  onError "Failed to detect version from 'thrive_version.h'"
end

# Create a readme file
readmeText = "This is Thrive release #{VERSION} for "

if OS.windows?
  readmeText += "Windows\n\n"

  readmeText += "To run the game run 'bin/Thrive.exe'."
elsif OS.linux?
  readmeText += "Linux\n\n"

  readmeText += "To run the game run 'bin/Thrive' executable."
else
  onError "unknown platform"
end

readmeText += "\n\nVisit http://revolutionarygamesstudio.com/ for more info"


File.write File.join(CurrentDir, "README.txt"), readmeText

# Create a helper script for importing assets

File.write File.join(CurrentDir, "AssetImportHelper.rb"), <<-END
#!/usr/bin/env ruby
# Helper script for running asset import with a precompiled Thrive version
require 'open3'
require 'fileutils'
require 'os'

editor = "bin/LeviathanEditor"

if OS.windows?
  editor += ".exe"
end

if !File.exists? editor

  puts "LeviathanEditor is not at expected path"
  exit 1
end

editor = File.realpath editor

FileUtils.mkdir_p "raw assets"
FileUtils.mkdir_p "processed assets"

puts "Importing assets. When importing shaders, this may take multiple minutes."

Open3.popen3(editor, "--import", File.realpath("raw assets"),
             File.realpath("processed assets"),
             chdir: File.realpath(File.dirname editor)) {
  |stdin, stdout, stderr, wait_thr|

  outThread = Thread.new{
    stdout.each {|line|
      puts line
    }
  }

  errThread = Thread.new{
    stderr.each {|line|
      puts line
    }
  }

  exit_status = wait_thr.value
  outThread.join
  errThread.join

  if exit_status != 0
    puts "Failed to run import with LeviathanEditor"
    exit 1
  end
}

puts "Finished running import. See the log above for (potential) failures."
END

FileUtils.chmod("+x", File.join(CurrentDir, "AssetImportHelper.rb"))

props = ReleaseProperties.new("Thrive-#{VERSION}")

props.addExecutable "Thrive"
props.addExecutable "ThriveServer"
props.addExecutable "LeviathanEditor"

props.addFile File.join ProjectDir, "gpl.txt"
props.addFile File.join ProjectDir, "LICENSE.txt"
props.addFile File.join CurrentDir, "README.txt"
props.addFile File.join CurrentDir, "AssetImportHelper.rb"

# Run the packaging
runMakeRelease(props)
