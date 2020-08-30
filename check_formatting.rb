#!/usr/bin/env ruby
# frozen_string_literal: true

# This script first builds using msbuild treating warnings as errors
# and then runs some custom line length checks
require 'optparse'
require 'find'
require 'digest'
require 'nokogiri'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'
require_relative 'scripts/fast_build/toggle_analysis_lib'

MAX_LINE_LENGTH = 120
DUPLICATE_THRESSHOLD = 110

# Pretty generous, so can't detect like small models with only a few
# vertices, as text etc. is on a single line
SCENE_EMBEDDED_LENGTH_HEURISTIC = 920

VALID_CHECKS = %w[compile files inspectcode cleanupcode duplicatecode].freeze
DEFAULT_CHECKS = %w[compile files inspectcode cleanupcode duplicatecode].freeze

ONLY_FILE_LIST = 'files_to_check.txt'

OUTPUT_MUTEX = Mutex.new
MSBUILD_MUTEX = Mutex.new

@options = {
  checks: DEFAULT_CHECKS,
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
  opts.on('--msbuild MSBUILD', 'Specify msbuild dll to use with jetbrains tools') do |f|
    @options[:msBuild] = f
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

info "Starting formatting checks with the following checks: #{@options[:checks]}"

# Helper functions

def detect_ms_build_dll
  msbuild = which 'msbuild'

  unless msbuild
    OUTPUT_MUTEX.synchronize do
      puts 'Searched paths:'
      pathAsArray.each do |p|
        puts p
      end

      onError 'msbuild not found in PATH'
    end
  end

  File.foreach(msbuild) do |line|
    match = line.match(%r{/mono\s+.+\s(/.*/MSBuild.dll)\s+})

    next unless match

    dll = match.captures[0]

    info "msbuild dll path detected: #{dll}"
    return dll
  end

  onError 'Could not determine MSBuild.dll location, please specify --msbuild ' \
          'parameter with the correct path'
end

def ms_build
  MSBUILD_MUTEX.synchronize do
    return @options[:msBuild] if @options[:msBuild]

    @options[:msBuild] = detect_ms_build_dll
  end
end

def ide_file?(path)
  path =~ %r{/\.vs/} || path =~ %r{/\.idea/}
end

def explicitly_ignored?(path)
  path =~ %r{/ThirdParty/}i || path =~ /GlobalSuppressions.cs/ || path =~ %r{/RubySetupSystem/}
end

def cache?(path)
  path =~ %r{/\.mono/} || path =~ %r{/\.import/} || path =~ %r{/builds/} || path =~ %r{/\.git/}
end

# Skip some files that would otherwise be processed
def skip_file?(path)
  explicitly_ignored?(path) || path =~ %r{^\.\/\.\/} || cache?(path) || ide_file?(path)
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

# Detects if there is a file telling which files to check. Returns nil otherwise
def files_to_include
  return nil unless File.exist? ONLY_FILE_LIST

  includes = []
  File.foreach(ONLY_FILE_LIST).with_index do |line, _num|
    next unless line

    file = line.strip
    next if file.empty?

    includes.append file
  end

  includes
end

@includes = files_to_include

def includes_changes_to(type)
  return false if @includes.nil?

  @includes.each  do |file|
    return true if file.end_with? type
  end

  false
end

def process_file?(filepath)
  if !@includes
    true
  else
    filepath = filepath.sub './', ''
    @includes.each do |file|
      return true if filepath.end_with? file
    end

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

      if !OS.windows? && line.include?("\r\n")
        error "Line #{line_number} contains a windows style line ending (CR LF)"
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

def handle_shader_file(path)
  errors = false

  File.foreach(path).with_index do |line, line_number|
    if line.include? "\t"
      OUTPUT_MUTEX.synchronize do
        error "Line #{line_number + 1} contains a tab"
        errors = true
      end
    end

    # For some reason this reports 1 too high
    length = line.length - 1

    if length > MAX_LINE_LENGTH
      OUTPUT_MUTEX.synchronize do
        error "Line #{line_number + 1} is too long. #{length} > #{MAX_LINE_LENGTH}"
        errors = true
      end
    end
  end

  errors
end

def handle_tscn_file(path)
  errors = false

  File.foreach(path).with_index do |line, line_number|
    # For some reason this reports 1 too high
    length = line.length - 1

    if length > SCENE_EMBEDDED_LENGTH_HEURISTIC
      OUTPUT_MUTEX.synchronize do
        error "Line #{line_number + 1} probably has an embedded resource. "\
              "Length #{length} is over heuristic value of #{SCENE_EMBEDDED_LENGTH_HEURISTIC}"
        errors = true
      end
    end
  end

  errors
end

def handle_csproj_file(path)
  errors = false
  data = File.read(path, encoding: 'utf-8')

  unless data.start_with? '<?xml'
    OUTPUT_MUTEX.synchronize do
      error "File doesn't start with '<?xml' likely due to added BOM"
      errors = true
    end
  end

  # This next check is a bit problematic on Windows so it is skipped
  return errors if OS.windows?

  unless data.end_with? "\n"
    OUTPUT_MUTEX.synchronize do
      error "File doesn't end with a new line"
      errors = true
    end
  end

  errors
end

# Forwards the file handling to a specific handler function if
# something should be done with the file type
def handle_file(path)
  return false if file_type_skipped?(path) || !process_file?(path)

  if path =~ /\.gd$/
    handle_gd_file path
  elsif path =~ /\.cs$/
    handle_cs_file path
  elsif path =~ %r{simulation_parameters/.*\.json$}
    handle_json_file path
  elsif path =~ /\.shader$/
    handle_shader_file path
  elsif path =~ /\.csproj$/
    handle_csproj_file path
  elsif path =~ /\.tscn$/
    handle_tscn_file path
  else
    false
  end
end

# Run functions for the specific checks

def run_compile
  # Make sure in analysis mode before running build
  perform_analysis_mode_check true, quiet: true

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

def inspect_code_executable
  # TODO: 32 bit support if needed
  if OS.windows?
    'inspectcode.exe'
  else
    'inspectcode.sh'
  end
end

def skip_jetbrains?
  if @includes && !includes_changes_to('.cs')
    OUTPUT_MUTEX.synchronize do
      info 'No changes to be checked for .cs files'
    end
    return true
  end

  false
end

def run_inspect_code
  return if skip_jetbrains?

  params = [inspect_code_executable, 'Thrive.sln', '-o=inspect_results.xml']

  params.append "--toolset-path=#{ms_build}" if OS.linux?

  params.append "--include=#{@includes.join(';')}" if @includes

  runOpen3Checked(*params)

  issues_found = false

  doc = Nokogiri::XML(File.open('inspect_results.xml'), &:norecover)

  issue_types = {}

  doc.xpath('//IssueType').each do |node|
    issue_types[node['Id']] = node
  end

  doc.xpath('//Issue').each do |issue|
    type = issue_types[issue['TypeId']]

    next if type['Severity'] == 'SUGGESTION'

    issues_found = true

    OUTPUT_MUTEX.synchronize do
      error "#{issue['File']}:#{issue['Line']} #{issue['Message']} type: #{issue['TypeId']}"
    end
  end

  return unless issues_found

  OUTPUT_MUTEX.synchronize do
    error 'Code inspection detected issues, see inspect_results.xml'
  end
  exit 2
end

def cleanup_code_executable
  # TODO: 32 bit support if needed
  if OS.windows?
    'cleanupcode.exe'
  else
    'cleanupcode.sh'
  end
end

def run_cleanup_code
  return if skip_jetbrains?

  old_diff = runOpen3CaptureOutput 'git', 'diff', '--stat'

  params = [cleanup_code_executable, 'Thrive.sln', '--profile=full_no_xml']

  params.append "--toolset-path=#{ms_build}" if OS.linux?

  params.append "--include=#{@includes.join(';')}" if @includes

  runOpen3Checked(*params)

  new_diff = runOpen3CaptureOutput 'git', 'diff', '--stat'

  return if new_diff == old_diff

  OUTPUT_MUTEX.synchronize do
    error 'Code cleanup performed changes, please stage / check them before committing'
  end
  exit 2
end

def duplicate_code_executable
  if OS.windows?
    'dupfinder.exe'
  else
    'dupfinder.sh'
  end
end

def run_duplicate_finder
  return if skip_jetbrains?

  params = [duplicate_code_executable, '-o=duplicate_results.xml', '--show-text',
            "--discard-cost=#{DUPLICATE_THRESSHOLD}", '--discard-literals=true']

  params.append "--toolset-path=#{ms_build}" if OS.linux?

  if @includes
    params += @includes.select { |item| item =~ /\.cs$/ }.uniq
  else
    params.append 'Thrive.sln'
  end

  runOpen3Checked(*params)

  issues_found = false

  doc = Nokogiri::XML(File.open('duplicate_results.xml'), &:norecover)

  doc.xpath('//Duplicate').each do |duplicate|
    issues_found = true

    OUTPUT_MUTEX.synchronize do
      error "Found duplicate with cost #{duplicate['Cost']}"

      duplicate.xpath('//Fragment').each do |fragment|
        file = fragment.xpath('FileName')[0].content
        start_end = fragment.xpath('LineRange')[0]

        puts "Fragment in file #{file}"
        puts "Lines #{start_end['Start']}--#{start_end['End']}"

        if fragment.xpath('Text')
          puts 'Fragment code:'
          puts fragment.xpath('Text')[0].content
        end

        puts 'End of fragment'
      end

      puts 'End of duplicate'
    end
  end

  return unless issues_found

  OUTPUT_MUTEX.synchronize do
    error 'Duplicate finder found duplicates, see duplicate_results.xml'
  end
  exit 2
end

run_check = proc { |check|
  if check == 'compile'
    run_compile
  elsif check == 'files'
    run_files
  elsif check == 'inspectcode'
    run_inspect_code
  elsif check == 'cleanupcode'
    run_cleanup_code
  elsif check == 'duplicatecode'
    run_duplicate_finder
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
