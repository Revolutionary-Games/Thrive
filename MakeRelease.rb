#!/usr/bin/env ruby
# Creates release packages for Thrive.
# You MUST compile thrive in RelWithDebInfo with release cmake options before running this
require 'os'

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


props = ReleaseProperties.new("Thrive-#{VERSION}")

props.addExecutable "Thrive"
props.addExecutable "ThriveServer"

props.addFile File.join ProjectDir, "gpl.txt"
props.addFile File.join ProjectDir, "LICENSE.txt"
props.addFile File.join CurrentDir, "README.txt"

# Run the packaging
runMakeRelease(props)
