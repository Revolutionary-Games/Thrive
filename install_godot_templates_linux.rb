#!/usr/bin/env ruby
# This script downloads and installs godot export templates for current version on Linux
require 'fileutils'
require 'tmpdir'
require 'httparty'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'

require_relative 'godot_version'

INSTALL_TARGET = File.join(File.expand_path('~'), '.local/share/godot/templates/',
                           GODOT_VERSION_FULL).freeze

info "Downloading templates to: #{INSTALL_TARGET}"

FileUtils.mkdir_p INSTALL_TARGET

def download(target)
  url = "https://downloads.tuxfamily.org/godotengine/#{GODOT_VERSION}/" \
        "mono/Godot_v#{GODOT_VERSION}-stable_mono_export_templates.tpz"

  puts "Downloading #{url}"
  puts 'This may take many minutes as the download is large'
  response = nil

  File.open(target, 'wb') do |file|
    response = HTTParty.get(url, stream_body: true) do |fragment|
      file.write(fragment)
    end
  end

  onError 'Failed to download the godot file' unless response.success?

  success 'Download finished'
end

Dir.mktmpdir do |dir|
  file_path = File.join dir, 'godot_mono_templates.tpz'

  download file_path

  temp_unzip = File.join dir, 'unzip'
  templates_folder = File.join temp_unzip, 'templates'

  FileUtils.mkdir_p temp_unzip

  puts 'Extracting templates'
  runOpen3Checked 'unzip', file_path, '-d', temp_unzip
  puts 'Done extracting'

  onError "extracting didn't create templates folder" unless File.exist? templates_folder

  puts 'Moving unzipped templates to right place'

  Dir.each_child(templates_folder) do |x|
    FileUtils.mv File.join(templates_folder, x), INSTALL_TARGET, force: true
  end

  puts 'Finished copying'
end

success 'Done installing templates'
