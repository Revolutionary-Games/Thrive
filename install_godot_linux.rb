#!/usr/bin/env ruby
# This script downloads and installs godot (headless) systemwide on Linux
# needs most likely to be ran with sudo
# Requires the httparty gem
# TODO: allow downloading the non-headless version as well
require 'English'
require 'fileutils'
require 'tmpdir'
require 'httparty'

require_relative 'godot_version'

INSTALL_TARGET = '/usr/local/bin/'.freeze

def download(godot_folder, target)
  url = "https://downloads.tuxfamily.org/godotengine/#{GODOT_VERSION}/mono/" \
        "#{godot_folder}.zip"

  puts "Downloading #{url}"
  response = nil

  File.open(target, 'wb') do |file|
    response = HTTParty.get(url, stream_body: true) do |fragment|
      file.write(fragment)
    end
  end

  raise StandardError, 'Failed to download the godot file' unless response.success?

  puts 'Download finished'
end

def unzip(file, unzip_target)
  FileUtils.mkdir_p unzip_target

  system 'unzip', file, '-d', unzip_target

  raise StandardError, 'Failed to unzip the downloaded file' if $CHILD_STATUS.exitstatus != 0
end

def create_symlink(godot_executable)
  raise StandardError, "Couldn't detect godot executable name" unless godot_executable

  unless File.exist? File.join(INSTALL_TARGET, godot_executable)
    raise StandardError, "Copy didn't create the godot executable"
  end

  FileUtils.ln_sf File.join(INSTALL_TARGET, godot_executable),
                  File.join(INSTALL_TARGET, 'godot')
end

def move_files(created_godot_folder)
  FileUtils.mkdir_p INSTALL_TARGET

  godot_executable = nil

  Dir.each_child(created_godot_folder) do |x|
    if x =~ /mono_linux/ && File.executable?(File.join(created_godot_folder, x))
      godot_executable = x
      puts "Detected Godot executable name is: #{godot_executable}"
    end

    FileUtils.mv File.join(created_godot_folder, x), INSTALL_TARGET, force: true
    puts "Created file #{INSTALL_TARGET}#{x}"
  end

  create_symlink godot_executable
end

Dir.mktmpdir do |dir|
  file_path = File.join dir, 'godot_mono.zip'
  godot_folder = "Godot_v#{GODOT_VERSION}-stable_mono_linux_headless_64"

  download godot_folder, file_path

  unzip_target = File.join dir, 'unzipped'
  unzip file_path, unzip_target

  created_godot_folder = File.join unzip_target, godot_folder

  unless File.exist? created_godot_folder
    raise StandardError, "Expected godot folder wasn't created"
  end

  move_files created_godot_folder

  system 'godot', '--version'

  puts "Godot is now installed and should work with the command 'godot'"
end
