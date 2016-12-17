#!/usr/bin/env ruby
#
# This is a packaging script for Thrive linux releases
# It creates two packaes one universal with literally every library ever in it
# and one with only the Thrive specific libraries

require 'fileutils'
require 'nokogiri'

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

require_relative '../linux_setup/RubySetupSystem'

Dir.chdir(CurrentDir) do

  if not File.exists? "CMakeLibraryList.xml"

    onError "Selected build folder doesn't contain library lists file: CMakeLibraryList.xml "
    
  end
  
end

info "Running Thrive packaging script for linux"

doc = File.open(File.join(CurrentDir, "CMakeLibraryList.xml")) { |f| Nokogiri::XML(f) }


ThriveVersion = doc.at_xpath("//version").content.strip

LibraryList = doc.at_xpath("//libraries").content.split(';')

CEGUIVersion = doc.at_xpath("//CEGUI_version").content.strip

info "For version #{ThriveVersion} with #{LibraryList.count} libraries and "+
     "CEGUI #{CEGUIVersion}"


success "Package completed"


