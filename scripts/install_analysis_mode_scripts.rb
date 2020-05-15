#!/usr/bin/env ruby
# Installs the quick access analysis mode scripts
require 'fileutils'

FAST_BUILD_FOLDER = File.join(__dir__, 'fast_build')
TARGET_FOLDER = File.join(__dir__, '../')

%w[ena dis].each do |item|
  file = File.join FAST_BUILD_FOLDER, item
  target = File.join TARGET_FOLDER, item

  File.unlink target if File.exist? target

  FileUtils.cp file, target
  FileUtils.chmod('+x', target)
  puts "Installed file: #{target}"
end
