#!/usr/bin/env ruby
# frozen_string_literal: true

# This script first builds using dotnet treating warnings as errors
# and then runs some custom line length checks
require 'optparse'
require 'find'
require 'digest'
require 'nokogiri'
require 'set'
require 'os'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'
require_relative 'scripts/fast_build/toggle_analysis_lib'
require_relative 'scripts/check_file_list'
require_relative 'scripts/po_helpers'
require_relative 'scripts/json_helpers'
require_relative 'scripts/steam_version_helpers'

MAX_LINE_LENGTH = 120
DUPLICATE_THRESHOLD = 105

LOCALIZATION_UPPERCASE_EXCEPTIONS = ['Cancel'].freeze

# Pretty generous, so can't detect like small models with only a few
# vertices, as text etc. is on a single line
SCENE_EMBEDDED_LENGTH_HEURISTIC = 920

VALID_CHECKS = %w[compile files inspectcode cleanupcode duplicatecode localization].freeze
DEFAULT_CHECKS = %w[compile files inspectcode cleanupcode localization].freeze

LOCALE_TEMP_SUFFIX = '.temp_check'
TRAILING_SPACE = /(?<=\S)[\t ]+$/.freeze
GIT_MERGE_CONFLICT_MARKERS = /^<{7}\s+\S+\s*$/.freeze
NODE_NAME_REGEX = /\[node\s+name="([^"]+)"/.freeze
NODE_NAME_UPPERCASE_REQUIRED_LENGTH = 25
NODE_NAME_UPPERCASE_ACRONYM_ALLOWED_LENGTH = 4

CFG_VERSION_LINE = %r{[/_]version="([\d.]+)"}.freeze
ASSEMBLY_VERSION_FILE = 'Properties/AssemblyInfo.cs'
ASSEMBLY_VERSION_REGEX = /AssemblyVersion\("([\d.]+)"\)/.freeze
ASSEMBLY_EXTRA_VERSION_REGEX = /AssemblyInformationalVersion\("([^"]*)"\)/.freeze

REQUIREMENTS_TXT_FILE = 'docker/jsonlint/requirements.txt'
PIP_BABEL_THRIVE_VERSION = /^Babel-Thrive\s*([\d.]+)/.freeze
REQUIREMENTS_BABEL_THRIVE_VERSION = /^Babel-Thrive==([\d.]+)$/.freeze

EMBEDDED_FONT_SIGNATURE = 'sub_resource type="DynamicFont"'

JET_BRAINS_CACHE = '.jetbrains-cache'

OUTPUT_MUTEX = Mutex.new
TOOL_RESTORE_MUTEX = Mutex.new
BUILD_MUTEX = Mutex.new

# Bom bytes
BOM = [239, 187, 191].freeze

@options = {
  checks: DEFAULT_CHECKS,
  skip_file_types: [],
  parallel: true,
  restore_tools: true
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
  opts.on('-p', '--[no-]restore-tools',
          'Automatically restore dotnet tools or skip that') do |b|
    @options[:restore_tools] = b
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

info "Starting formatting checks with the following checks: #{@options[:checks]}"

# Helper functions

def ide_file?(path)
  path =~ %r{/\.vs/} || path =~ %r{/\.idea/}
end

def explicitly_ignored?(path)
  path =~ %r{/ThirdParty/}i || path =~ %r{/third_party/} || path =~ /GlobalSuppressions.cs/ ||
    path =~ %r{/RubySetupSystem/}
end

def cache?(path)
  path =~ %r{/\.mono/} || path =~ %r{/\.import/} || path =~ %r{/builds/} || path =~ %r{/\.git/}
end

# Skip some files that would otherwise be processed
def skip_file?(path)
  explicitly_ignored?(path) || path =~ %r{^\./\./} || cache?(path) || ide_file?(path)
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

@game_version = nil

def game_version
  return @game_version if @game_version

  found = false

  File.foreach(ASSEMBLY_VERSION_FILE, encoding: 'utf-8') do |line|
    next unless line

    matches = line.match(ASSEMBLY_VERSION_REGEX)

    if matches
      @game_version = matches[1]
      found = true
      next
    end

    matches = line.match(ASSEMBLY_EXTRA_VERSION_REGEX)

    if matches && matches[1].length.positive?
      puts 'additional version check'

      raise 'AssemblyInformationalVersion must start with a dash' if matches[1][0] != '-'
    end
  end

  return @game_version if found

  raise 'Could not find AssemblyVersion'
end

@pip_name = nil

def pip_name
  return @pip_name if @pip_name

  @pip_name = which 'pip'
  return @pip_name if @pip_name

  @pip_name = which 'pip3'
  return @pip_name if @pip_name

  raise 'Could not find pip nor pip3.'
end

@installed_babel_thrive_version = nil

def installed_babel_thrive_version
  return @installed_babel_thrive_version if @installed_babel_thrive_version

  _, piplist = runOpen3CaptureOutput pip_name, 'list'
  matches = piplist.match(PIP_BABEL_THRIVE_VERSION)

  return @installed_babel_thrive_version = matches[1] if matches

  raise 'Could not find Babel-Thrive.'
end

@required_babel_thrive_version = nil

def required_babel_thrive_version
  return @required_babel_thrive_version if @required_babel_thrive_version

  requirements = File.read(REQUIREMENTS_TXT_FILE)
  matches = requirements.match(REQUIREMENTS_BABEL_THRIVE_VERSION)

  return @required_babel_thrive_version = matches[1] if matches

  raise "Could not read required Babel-Thrive verion from #{REQUIREMENTS_TXT_FILE}."
end

def install_dotnet_tools
  unless @options[:restore_tools]
    puts 'Skipping restoring dotnet tools, hopefully they are up to date'
  end

  TOOL_RESTORE_MUTEX.synchronize do
    if @tools_restored
      puts 'Tools already restored'
      return
    end

    @tools_restored = true

    info 'Restoring dotnet tools to make sure they are up to date'
    runOpen3Checked('dotnet', 'tool', 'restore')
  end
end

def contains_merge_marker(line)
  line =~ GIT_MERGE_CONFLICT_MARKERS
end

def merge_error(line_number)
  error "Line #{line_number + 1} contains a merge conflict marker"
end

def file_begins_with_bom(path)
  raw_data = File.binread(path, 3)

  # Unpack as raw bytes for comparison
  potential_bom = raw_data.unpack('CCC')

  potential_bom == BOM
end

# Different handle functions for file checks
def handle_gd_file(_path)
  OUTPUT_MUTEX.synchronize do
    error 'GD scripts should not exist'
  end
  true
end

def handle_mo_file(_path)
  OUTPUT_MUTEX.synchronize do
    error '.mo files are not allowed'
  end
  true
end

def handle_cs_file(path)
  errors = false

  # Check for BOM first
  unless file_begins_with_bom path
    OUTPUT_MUTEX.synchronize do
      error 'File should begin with UTF-8 BOM'
      errors = true
    end
  end

  original = File.read(path, encoding: 'utf-8')
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
  contents = File.read(path, encoding: 'utf-8')
  if contains_merge_marker(contents)
    OUTPUT_MUTEX.synchronize do
      error 'Contains a merge conflict marker'
    end
    return true
  end

  digest_before = Digest::MD5.hexdigest contents

  unless jsonlint_on_file(path)
    OUTPUT_MUTEX.synchronize do
      error 'JSONLint failed on file'
    end
    return true
  end

  digest_after = Digest::MD5.hexdigest File.read(path, encoding: 'utf-8')

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

  File.foreach(path, encoding: 'utf-8').with_index do |line, line_number|
    if contains_merge_marker(line)
      OUTPUT_MUTEX.synchronize do
        merge_error(line_number)
        errors = true
      end
    end

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

  File.foreach(path, encoding: 'utf-8').with_index do |line, line_number|
    # For some reason this reports 1 too high
    length = line.length - 1

    if contains_merge_marker(line)
      OUTPUT_MUTEX.synchronize do
        merge_error(line_number)
        errors = true
      end
    end

    if length > SCENE_EMBEDDED_LENGTH_HEURISTIC
      OUTPUT_MUTEX.synchronize do
        error "Line #{line_number + 1} probably has an embedded resource. "\
              "Length #{length} is over heuristic value of #{SCENE_EMBEDDED_LENGTH_HEURISTIC}"
        errors = true
      end
    end

    if line.include? EMBEDDED_FONT_SIGNATURE
      OUTPUT_MUTEX.synchronize do
        error "Line #{line_number + 1} contains embedded font. "\
              "Don't embed fonts in scenes, instead place font resources in a separate .tres"
        errors = true
      end
    end

    matches = line.match(NODE_NAME_REGEX)

    if matches
      name = matches[1]

      if name =~ /\s/
        error "Line #{line_number + 1} contains a name (#{name}) that has a space."
        errors = true
      end

      if name.include? '_'
        error "Line #{line_number + 1} contains a name (#{name}) that has an underscore."
        errors = true
      end

      # Single word names can be without upper case letters so we use a length heuristic here
      if name.length > NODE_NAME_UPPERCASE_REQUIRED_LENGTH && name.downcase == name
        error "Line #{line_number + 1} contains a name (#{name}) that has no capital letters."
        errors = true
      end

      # Short acronyms are ignored here if the name starts with a capital
      # TODO: in the future might only want to allow node names that start with a
      # lowercase letter
      if name.upcase == name && (name.length > NODE_NAME_UPPERCASE_ACRONYM_ALLOWED_LENGTH ||
                                 name !~ /^[A-Z]/)
        error "Line #{line_number + 1} contains a name (#{name}) that doesn't " \
              "contain lowercase letters (and isn't a short acronym)."
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

def handle_export_presets(path)
  errors = false
  found = false

  required_version = game_version

  File.foreach(path, encoding: 'utf-8').with_index do |line, line_number|
    if contains_merge_marker(line)
      OUTPUT_MUTEX.synchronize do
        merge_error(line_number)
        errors = true
      end
    end

    matches = line.match(CFG_VERSION_LINE)

    if matches

      found = true

      if matches[1] != required_version
        OUTPUT_MUTEX.synchronize do
          error "Line #{line_number + 1} has incorrect version. "\
                "#{matches[1]} is not equal to #{required_version}"
          errors = true
        end
      end
    end
  end

  unless found
    OUTPUT_MUTEX.synchronize do
      error 'No line specifying version numbers was found'
      errors = true
    end
  end

  errors
end

def handle_po_file(path)
  errors = false

  is_english = path.end_with? 'en.po'
  in_header = true
  last_msgstr = nil
  last_msgid = nil
  seen_message = false

  msg_ids = Set[]

  File.foreach(path, encoding: 'utf-8').with_index do |line, line_number|
    if contains_merge_marker(line)
      OUTPUT_MUTEX.synchronize do
        merge_error(line_number)
        errors = true
      end
    end

    if is_english && line.match(FUZZY_TRANSLATION_REGEX)
      OUTPUT_MUTEX.synchronize do
        error "Line #{line_number + 1} has fuzzy (marked needs changes) translation, not allowed for en"
        errors = true
      end
    end

    # Could only check in headers, but checking everywhere just adds
    # only 200 milliseconds to total runtime so all lines are checked
    if line.match TRAILING_SPACE
      OUTPUT_MUTEX.synchronize do
        error "Line #{line_number + 1} has trailing space"
        errors = true
      end
    end

    matches = line.match(MSG_ID_REGEX)

    if matches

      unless in_header
        if is_english && (!last_msgstr || last_msgstr.strip.empty?)
          OUTPUT_MUTEX.synchronize do
            error "Line #{line_number + 1} previous message (#{last_msgid}) is blank"
            errors = true
          end
        end

        # TODO: might need a specific whitelist
        if last_msgid && last_msgstr && (last_msgid == last_msgstr) &&
           last_msgstr.include?('_')
          OUTPUT_MUTEX.synchronize do
            error "Line #{line_number + 1} previous message (#{last_msgid}) " \
                  'is the same as the message key'
            errors = true
          end
        end
      end

      last_msgid = matches[1]
      last_msgstr = ''

      next if in_header

      unless last_msgid
        OUTPUT_MUTEX.synchronize do
          error "Line #{line_number + 1} has empty msgid"
          errors = true
        end
      end

      if last_msgid.upcase != last_msgid &&
         !LOCALIZATION_UPPERCASE_EXCEPTIONS.include?(last_msgid)
        OUTPUT_MUTEX.synchronize do
          error "Line #{line_number + 1} has message with non-uppercase characters " \
                " (#{last_msgid})"
          errors = true
        end
      end

      if last_msgid.include? ' '
        OUTPUT_MUTEX.synchronize do
          error "Line #{line_number + 1} has message with with a space " \
                " (#{last_msgid})"
          errors = true
        end
      end

      if msg_ids.include? last_msgid
        OUTPUT_MUTEX.synchronize do
          error "Line #{line_number + 1} has duplicate msgid, #{last_msgid} " \
                'already appeared in the file'
          errors = true
        end
      else
        msg_ids.add last_msgid
      end

      next
    end

    matches = line.match(/^msgstr "(.*)"$/)

    matches ||= line.match(/^"(.*)"$/)

    if matches
      seen_message = true if in_header

      last_msgstr += matches[1]
      next
    end

    # Blank / comment
    in_header = false if in_header && seen_message
  end

  # TODO: solve code duplication with this
  if is_english && (!last_msgstr || last_msgstr.strip.empty?)
    OUTPUT_MUTEX.synchronize do
      error "previous message (last in file) (#{last_msgid}) is blank"
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
  elsif path =~ /\.po$/
    handle_po_file path
  elsif path =~ /\.mo$/
    handle_mo_file path
  elsif path =~ /export_presets.cfg$/
    handle_export_presets path
  else
    false
  end
end

# Run functions for the specific checks

def run_compile
  status = nil
  output = nil

  BUILD_MUTEX.synchronize do
    # Make sure in analysis mode before running build
    perform_analysis_mode_check true, quiet: true

    status, output = runOpen3CaptureOutput('dotnet', 'build', 'Thrive.sln', '/t:Clean,Build',
                                           '/warnaserror')
  end

  if status != 0
    OUTPUT_MUTEX.synchronize  do
      info 'Build output from dotnet:'
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
          puts "Problems found in file (see above): #{path}"
          puts ''
        end
        issues_found = true
      end
    rescue StandardError => e
      OUTPUT_MUTEX.synchronize do
        puts "Failed to handle path: #{path}"
        puts "Error: #{e.message}"
      end
      raise e
    end
  end

  if steam_build_enabled?
    OUTPUT_MUTEX.synchronize do
      puts 'Steam build is enabled, it should not be enabled when committing'
    end
    issues_found = true
  end

  return unless issues_found

  OUTPUT_MUTEX.synchronize do
    error 'Code format issues detected'
  end
  exit 2
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

  install_dotnet_tools

  params = ['dotnet', 'tool', 'run', 'jb', 'inspectcode', 'Thrive.sln',
            '-o=inspect_results.xml', '--build', "--caches-home=#{JET_BRAINS_CACHE}"]

  params.append "--include=#{@includes.join(';')}" if @includes

  BUILD_MUTEX.synchronize do
    runOpen3Checked(*params)
  end

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

def run_cleanup_code
  return if skip_jetbrains?

  install_dotnet_tools

  old_diff = runOpen3CaptureOutput 'git', '-c', 'core.safecrlf=false', 'diff', '--stat'

  params = ['dotnet', 'tool', 'run', 'jb', 'cleanupcode', 'Thrive.sln',
            '--profile=full_no_xml', "--caches-home=#{JET_BRAINS_CACHE}"]

  # TODO: we could probably split the includes into 4 groups to speed up things as the
  # cleanup tool doesn't run in parallel by default (so we could run 4 processes at once)
  params.append "--include=#{@includes.join(';')}" if @includes

  # Sometimes the code inspections fails completely and caches bad data if ran at the same time
  BUILD_MUTEX.synchronize do
    runOpen3Checked(*params)
  end

  new_diff = runOpen3CaptureOutput 'git', '-c', 'core.safecrlf=false', 'diff', '--stat'

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

  warning 'Dupfinder is deprecated. See: '\
          'https://github.com/Revolutionary-Games/Thrive/issues/2953'

  params = [duplicate_code_executable, '-o=duplicate_results.xml', '--show-text',
            "--discard-cost=#{DUPLICATE_THRESHOLD}", '--discard-literals=false',
            '--exclude-by-comment="NO_DUPLICATE_CHECK"', 'Thrive.csproj']

  # Duplicate code needs to always process all files to detect code
  # from an existing file being duplicated in a new file

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

def cleanup_temp_check_locales
  Dir['locale/**/*' + LOCALE_TEMP_SUFFIX].each do |f|
    File.unlink f
  end
end

def run_localization_checks
  cleanup_temp_check_locales

  # Create duplicates of all .po files for msgmerge
  Dir['locale/**/*.po'].each do |f|
    FileUtils.cp f, f + LOCALE_TEMP_SUFFIX
  end

  status, output = runOpen3CaptureOutput(
    'ruby', 'scripts/update_localization.rb', '--pot-suffix',
    '.pot' + LOCALE_TEMP_SUFFIX, '--po-suffix', '.po' + LOCALE_TEMP_SUFFIX
  )

  if status != 0
    OUTPUT_MUTEX.synchronize do
      puts output
      onError 'Failed to run translation generation to check if current files are up to date'
    end
  end

  issues_found = false

  Dir['locale/**/*' + LOCALE_TEMP_SUFFIX].each do |f|
    original = f.gsub LOCALE_TEMP_SUFFIX, ''
    updated = f

    File.open(original, encoding: 'utf-8') do |original_reader|
      File.open(updated, encoding: 'utf-8') do |updated_reader|
        # Check that headers are in the right order
        original_header_order = read_gettext_header_order original_reader
        updated_header_order = read_gettext_header_order updated_reader

        if original_header_order != updated_header_order
          OUTPUT_MUTEX.synchronize do
            puts "Headers are in wrong order in #{original}"
            error "Header order should be: #{original_header_order}, but " \
                  "it is: #{updated_header_order}"
            issues_found = true
          end
        end

        loop do
          original_message = find_next_msg_id original_reader
          updated_message = find_next_msg_id updated_reader

          if original_message.nil? && updated_message.nil?
            # Both files ended
            break
          end

          next unless original_message != updated_message

          OUTPUT_MUTEX.synchronize do
            puts "When comparing #{original}, with freshly updated: #{updated}:"
            error "Original (committed) file has msgid: #{original_message}, while it " \
                  "should have #{updated_message} at this point in the file"
            issues_found = true
          end
          break
        end
      end
    end
  end

  cleanup_temp_check_locales

  return unless issues_found

  OUTPUT_MUTEX.synchronize do
    error 'Translations are not up to date.'
    puts "Babel-Thrive version installed: #{installed_babel_thrive_version}; "\
          "required: #{required_babel_thrive_version}"
    if installed_babel_thrive_version == required_babel_thrive_version
      warning 'Please verify your Babel-Thrive version meets the requirement '\
            'and rerun "scripts/update_localization.rb"'
    else
      warning 'Mismatching Babel-Thrive version detected. Please update '\
              '"pip install -r docker/jsonlint/requirements.txt --user" '\
              'and rerun "scripts/update_localization.rb"'
    end
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
  elsif check == 'localization'
    run_localization_checks
  else
    OUTPUT_MUTEX.synchronize do
      puts "Valid checks: #{VALID_CHECKS}"
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
