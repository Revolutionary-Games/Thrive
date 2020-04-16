#!/usr/bin/env ruby
# This script first builds using msbuild treating warnings as errors
# and then runs some custom line length checks
require 'find'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'

MAX_LINE_LENGTH = 120

if runSystemSafe('msbuild', 'Thrive.sln', '/t:Clean,Build', '/warnaserror') != 0
  error "\nBuild generated warnings or errors."
  exit 1
end

# Skip some files that would otherwise be processed
def skip_file?(path)
  path =~ %r{/ThirdParty/}i || path =~ %r{^\.\/\.\/} || path =~ /GlobalSuppressions.cs/
end

# Different handle functions
def handle_gd_file(_path)
  error 'GD scripts should not exist'
  true
end

def handle_cs_file(path)
  errors = false
  original = File.read(path)
  line_number = 0

  original.each_line do |line|
    line_number += 1

    if line.include? "\t"
      error "Line #{line_number} contains a tab"
      errors = true
    end

    # For some reason this reports 1 too high
    length = line.length - 1

    if length > MAX_LINE_LENGTH
      error "Line #{line_number} is too long. #{length} > #{MAX_LINE_LENGTH}"
      errors = true
    end
  end

  errors
end

# Forwards the file handling to a specific handler function if
# something should be done with the file type
def handle_file(path)
  if path =~ /\.gd$/
    handle_gd_file path
  elsif path =~ /\.cs$/
    handle_cs_file path
  else
    false
  end
end

def run
  issues_found = false
  Find.find('.') do |path|
    # path = path[2..-1]
    next if skip_file? path

    begin
      if handle_file path
        puts 'Problems found in file (see above): ' + path
        puts ''
        issues_found = true
      end
    rescue StandardError => e
      puts 'Failed to handle path: ' + path
      puts 'Error: ' + e.message
      raise e
    end
  end

  issues_found
end

if run
  error 'Code format issues detected'
  exit 2
else
  success 'No code format issues found'
  exit 0
end
