# frozen_string_literal: true

require 'fileutils'
require 'tmpdir'
require 'httparty'

require_relative '../bootstrap_rubysetupsystem'
require_relative '../RubySetupSystem/RubyCommon'

require_relative 'godot_version'

INSTALL_TARGET ||= File.join(File.expand_path('~'), '.local/share/godot/templates/',
                             GODOT_VERSION_FULL).freeze

def download(url, target)
  puts "Downloading #{url}"
  puts 'This may take many minutes as the download is large'
  response = nil

  File.open(target, 'wb') do |file|
    response = HTTParty.get(url, stream_body: true) do |fragment|
      if [301, 302].include?(fragment.code)
        puts 'got a redirect'
      elsif fragment.code == 200
        file.write(fragment)
      else
        raise StandardError, "Non-success status code while downloading #{fragment.code}"
      end
    end
  end

  onError 'Failed to download the godot file' unless response.success?

  success 'Download finished'
end

def extract_templates(templates_file, temp_dir)
  temp_unzip = File.join temp_dir, 'unzip'
  templates_folder = File.join temp_unzip, 'templates'

  FileUtils.mkdir_p temp_unzip

  puts 'Extracting templates'
  runOpen3Checked 'unzip', templates_file, '-d', temp_unzip
  puts 'Done extracting'

  onError "extracting didn't create templates folder" unless File.exist? templates_folder

  templates_folder
end

def run_template_download(url, template_file_name)
  FileUtils.mkdir_p INSTALL_TARGET

  info "Downloading templates to: #{INSTALL_TARGET}"

  Dir.mktmpdir do |dir|
    file_path = File.join dir, template_file_name

    download url, file_path

    templates_folder = extract_templates file_path, dir

    puts 'Moving unzipped templates to right place'

    Dir.each_child(templates_folder) do |x|
      FileUtils.mv File.join(templates_folder, x), INSTALL_TARGET, force: true
    end

    puts 'Finished copying'
  end

  success 'Done installing templates'
end
