#!/usr/bin/env ruby
# This script imports the assets and shaders to BSF format. All the imported versions
# are placed under "assets"
# Depending on locale you may have to run this as: LC_ALL=en_GB.utf8 ./ImportAssets.rb
# to not get shader compiler errors
require 'fileutils'
require 'open3'

require_relative 'RubySetupSystem/RubyCommon'

ASSETS_REPO = 'https://github.com/Revolutionary-Games/Thrive-Raw-Assets.git'.freeze
ASSETS_FOLDER = '../Thrive-Raw-Assets'.freeze

unless File.exist? ASSETS_FOLDER
  info "Raw assets folder (#{ASSETS_FOLDER}) doesn't exist. Trying to clone it."
  puts 'If you are using ssh to access Github you should manually clone it instead...'

  runOpen3Checked('git', 'clone', ASSETS_REPO, ASSETS_FOLDER)

  onError 'Failed to clone assets repo' unless File.exist? ASSETS_FOLDER
end

puts 'Note: this script does not automatically pull the raw assets repo. ' \
     'Make sure you are using the right commit manually.'

info "Doing git lfs pull to make sure it's up to date"

Dir.chdir(ASSETS_FOLDER) do
  runOpen3Checked('git', 'lfs', 'pull')
end

success 'Done making sure raw assets repo is good'

editor = 'ThirdParty/Leviathan/build/bin/LeviathanEditor'

editor += '.exe' if OS.windows?

onError 'LeviathanEditor is not at expected path' unless File.exist? editor

editor = File.realpath editor

info 'Importing assets. When importing shaders, this may take multiple minutes.'

Open3.popen3(editor, '--import', File.realpath('shaders'), File.realpath('assets'),
             '--import', File.realpath(File.join(ASSETS_FOLDER, 'raw_assets')),
             File.realpath('assets'),
             chdir: File.realpath(File.dirname(editor))) do |_stdin, stdout, stderr, wait_thr|

  out_thread = Thread.new do
    stdout.each do |line|
      puts line
    end
  end

  err_thread = Thread.new do
    stderr.each do |line|
      puts line.red
    end
  end

  exit_status = wait_thr.value
  out_thread.join
  err_thread.join

  onError 'Failed to run import with LeviathanEditor' if exit_status != 0
end

info "NOTE: import failures aren't reported as exit codes so this script " \
     "can't currently detect failures (other than serious ones)"
success 'Done importing'
