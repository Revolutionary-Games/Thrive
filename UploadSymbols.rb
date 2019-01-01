#!/usr/bin/env ruby
# Uploads Breakpad symbols to Thrive Dev Center.
# You MUST run ReleaseCreator before running this
require_relative 'RubySetupSystem/SymbolUploader'


# Customize options for Thrive
VERSION = File.read("src/thrive_version.h").match(/Thrive_VERSIONS\s+"(.*)"/).captures[0]

if !VERSION
  onError "Failed to detect version from 'thrive_version.h'"
end

props = SymbolDestinationProperties.new("https://dev.revolutionarygamesstudio.com/")

# Run the upload. This will ask for user confirmation
runUploadSymbols(props)
