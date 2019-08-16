#!/usr/bin/env ruby
# This script imports the assets and shaders to BSF format. All the imported versions
# are placed under "assets"
# Depending on locale you may have to run this as: LC_ALL=en_GB.utf8 ./ImportAssets.rb
# to not get shader compiler errors
require 'fileutils'
require 'open3'

require_relative 'RubySetupSystem/RubyCommon'

editor = "ThirdParty/Leviathan/build/bin/LeviathanEditor"

if OS.windows?
  editor += ".exe"
end

if !File.exists? editor

  onError "LeviathanEditor is not at expected path"
end

editor = File.realpath editor

info "Importing assets. When importing shaders, this may take multiple minutes."

Open3.popen3(editor, "--import", File.realpath("shaders"), File.realpath("assets"),
             "--import", File.realpath("assets/raw_assets"), File.realpath("assets"),
             chdir: File.realpath(File.dirname editor)) {
  |stdin, stdout, stderr, wait_thr|

  outThread = Thread.new{
    stdout.each {|line|
      puts line
    }
  }

  errThread = Thread.new{
    stderr.each {|line|
      puts (line).red
    }
  }

  exit_status = wait_thr.value
  outThread.join
  errThread.join

  if exit_status != 0
    onError "Failed to run import with LeviathanEditor"
  end
}

success "Done importing"
