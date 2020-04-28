#!/usr/bin/env ruby
# This script first builds using msbuild treating warnings as errors
# and then runs some custom line length checks
require 'optparse'
require 'find'
require 'digest'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'

MAX_LINE_LENGTH = 120

VALID_CHECKS = %w[compile files].freeze

OUTPUT_MUTEX = Mutex.new

@options = {
  checks: VALID_CHECKS,
  skip_file_types: [],
  parallel: true
}

OptionParser.new do |opts|
  opts.banner = "Usage: #{$PROGRAM_NAME} [options]"

  opts.on('-c', '--checks check1,check2', Array,
          'Select checks to do. Default is all') do |checks|
    @options[:checks] = checks
  end
  opts.on('-s', '--skip filetype1,filetype2', Array,
          'Skips files checks on the specified types') do |skip|
    @options[:skip_file_types] = skip
  end
  opts.on('-p', '--[no-]parallel', 'Run different checks in parallel (default)') do |b|
    @options[:parallel] = b
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

info "Starting formatting checks with the following checks: #{@options[:checks]}"

# Helper functions

# Skip some files that would otherwise be processed
def skip_file?(path)
  path =~ %r{/ThirdParty/}i || path =~ %r{^\.\/\.\/} || path =~ /GlobalSuppressions.cs/ ||
    path =~ %r{/RubySetupSystem/} || path =~ %r{/\.mono/}
end

def file_type_skipped?(path)
  if @options[:skip_file_types].include? File.extname(path)[1..-1]
    OUTPUT_MUTEX.synchronize do
      puts "Skipping file '#{path}'"
    end
    true
  else
    false
  end
end

# Different handle functions for file checks
def handle_gd_file(_path)
  OUTPUT_MUTEX.synchronize do
    error 'GD scripts should not exist'
  end
  true
end

def handle_cs_file(path)
  errors = false
  original = File.read(path)
  line_number = 0

  OUTPUT_MUTEX.synchronize do
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
  end

  errors
end

def handle_json_file(path)
  digest_before = Digest::MD5.hexdigest File.read(path)

  if runSystemSafe('jsonlint', '-i', path, '--indent', '    ') != 0
    OUTPUT_MUTEX.synchronize do
      error 'JSONLint failed on file'
    end
    return true
  end

  digest_after = Digest::MD5.hexdigest File.read(path)

  if digest_before != digest_after
    OUTPUT_MUTEX.synchronize do
      error 'JSONLint made formatting changes'
    end
    true
  else
    false
  end
end

# Forwards the file handling to a specific handler function if
# something should be done with the file type
def handle_file(path)
  return false if file_type_skipped? path

  if path =~ /\.gd$/
    handle_gd_file path
  elsif path =~ /\.cs$/
    handle_cs_file path
  elsif path =~ %r{scripts/.*\.json$}
    handle_json_file path
  else
    false
  end
end

# Run functions for the specific checks

def run_compile
  status, output = runOpen3CaptureOutput('msbuild', 'Thrive.sln', '/t:Clean,Build',
                                         '/warnaserror')

  if status != 0
    OUTPUT_MUTEX.synchronize  do
      info 'Build output from msbuild:'
      puts output
      error "\nBuild generated warnings or errors."
    end
    exit 1
  end
end

def run_files
  issues_found = false
  Find.find('.') do |path|
    # path = path[2..-1]
    next if skip_file? path

    begin
      if handle_file path
        OUTPUT_MUTEX.synchronize  do
          puts 'Problems found in file (see above): ' + path
          puts ''
        end
        issues_found = true
      end
    rescue StandardError => e
      OUTPUT_MUTEX.synchronize do
        puts 'Failed to handle path: ' + path
        puts 'Error: ' + e.message
      end
      raise e
    end
  end

  return unless issues_found

  OUTPUT_MUTEX.synchronize do
    error 'Code format issues detected'
  end
  exit 2
end

run_check = proc { |check|
  if check == 'compile'
    run_compile
  elsif check == 'files'
    run_files
  else
    OUTPUT_MUTEX.synchronize do
      onError "Unknown check type: #{check}"
    end
  end
}

if @options[:parallel]
  threads = @options[:checks].map do |check|
    Thread.new do
      run_check.call check
    end
  end

  threads.map(&:join)
else
  @options[:checks].each do |check|
    run_check.call check
  end
end

success 'No code format issues found'
exit 0
