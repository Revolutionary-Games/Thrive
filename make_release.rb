#!/usr/bin/env ruby
# This script exports the game with godot and packages that up for distribution
# Requires "godot" to be in PATH and the right version
require 'optparse'
require 'sha3'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'

FileUtils.mkdir_p 'builds'

ALL_TARGETS = ['Linux/X11', 'Windows Desktop', 'Windows Desktop (32-bit)', 'Mac OSX'].freeze
BASE_BUILDS_FOLDER = File.realpath 'builds'
THRIVE_VERSION_FILE = 'Properties/AssemblyInfo.cs'.freeze

LICENSE_FILES = [
  ['LICENSE.txt', 'LICENSE.txt'],
  ['gpl.txt', 'gpl.txt'],
  ['assets/LICENSE.txt', 'ThriveAssetsLICENSE.txt'],
  ['assets/README.md', 'ThriveAssetsREADME.txt'],
  ['doc/GodotLicense.txt', 'GodotLicense.txt']
].freeze

@options = {
  targets: ALL_TARGETS,
  retries: 2,
  zip: true
}

OptionParser.new do |opts|
  opts.banner = "Usage: #{$PROGRAM_NAME} [options]"

  opts.on('-t', '--targets target1,target2', Array,
          'Export targets to use. Default is all') do |targets|
    @options[:targets] = targets
  end
  opts.on('-r', '--retries count', Integer,
          'How many export retries to do to avoid spurious failures') do |r|
    @options[:retries] = r
  end
  opts.on('-z', '--[no-]zip', 'Disables packaging the exported game') do |b|
    @options[:zip] = b
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

ASSEMBLY_VERSION = /AssemblyVersion\(\"([\d\.]+)\"\)/.freeze
INFORMATIONAL_VERSION = /AssemblyInformationalVersion\(\"([^\"]*)\"\)/.freeze

# Messages to print again after the end
@reprint_messages = []

# Reads thrive version from the code
def find_thrive_version
  version = nil
  additional_version = nil

  File.open(THRIVE_VERSION_FILE, 'r') do |f|
    f.each_line do |line|
      line.match(ASSEMBLY_VERSION) do |match|
        version = match[1]
      end
      line.match(INFORMATIONAL_VERSION) do |match|
        additional_version = match[1]
      end
    end
  end

  raise 'Failed to find AssemblyVersion for thrive' unless version

  "#{version}#{additional_version}"
end

THRIVE_VERSION = find_thrive_version

def is_target_mac(target)
  target =~ /mac/i
end

# Returns the extension needed for the target
def get_target_extension(target)
  # "thrive_linux.x86_64"
  if is_target_mac target
    '.zip'
  elsif target =~ /windows/i
    '.exe'
  elsif target =~ /linux/i
    ''
  else
    ''
  end
end

# Copies license information to a target folder
def prepare_licenses(target_folder)
  LICENSE_FILES.each do |l|
    FileUtils.cp l[0], File.join(target_folder, l[1])
  end
end

def package(target, target_name, target_folder, target_file)
  if is_target_mac target
    puts 'Including licenses in mac .zip'

    Dir.chdir(target_folder) do
      runOpen3Checked(*['zip', '-u', target_file, LICENSE_FILES.map { |i| i[1] }].flatten)
    end
    puts 'Zip updated'
  end

  if @options[:zip] == false
    info 'Skipping packaging created folder'
    @reprint_messages.append "Created folder: #{target_folder}"
    return
  end

  if is_target_mac target
    puts 'Mac target is already zipped, moving it instead'

    final_file = target_folder + '.zip'

    File.unlink(final_file) if File.exist? final_file

    FileUtils.mv target_file, final_file
  else
    info 'Packaging for release...'
    final_file = target_folder + '.7z'

    # TODO: clean build option
    # File.unlink(final_file) if File.exist? final_file

    Dir.chdir(BASE_BUILDS_FOLDER) do
      if runSystemSafe(p7zip, 'a', "#{target_name}.7z", target_name) != 0
        onError 'Failed to package the target: ' + target_folder
      end
    end
  end

  onError "Final file creation failed (#{final_file})" unless File.exist? final_file

  puts ''
  message1 = 'Done: ' + final_file
  message2 = 'SHA3: ' + SHA3::Digest::SHA256.file(final_file).hexdigest

  @reprint_messages.append '', message1, message2

  success message1
  puts message2
end

# Performs the actual export for a target
def perform_export(target)
  puts ''
  info "Starting export for target: #{target}"

  target_name = "Thrive_#{THRIVE_VERSION}_" + target.gsub(%r{[\s/]}i, '_').downcase

  target_folder = File.join BASE_BUILDS_FOLDER, target_name

  FileUtils.mkdir_p target_folder

  puts "Exporting to folder: #{target_folder}"

  target_file = File.join(target_folder, 'Thrive' + get_target_extension(target))

  attempts = 1 + @options[:retries]

  success = false

  (1..attempts).each do |attempt|
    system('godot', '--export', target, target_file)
    if $CHILD_STATUS.exitstatus.zero?
      success = true
      break
    end

    # Failed
    error 'Exporting failed.'
    puts 'Retrying...' if attempt < attempts
  end

  if success
    success 'Godot export succeeded'
  else
    puts ''
    onError 'Exporting failed too many times'
  end

  prepare_licenses target_folder

  package target, target_name, target_folder, target_file
end

puts "Starting exporting thrive #{THRIVE_VERSION} to the specified targets"

@exported_something = false

@options[:targets].each do |target|
  unless ALL_TARGETS.include? target
    onError "specified target (#{target}) is not valid. Valid targets: #{ALL_TARGETS}"
  end

  perform_export target
  @exported_something = true
end

puts ''

unless @reprint_messages.empty?
  info 'Reprint messages:'
  @reprint_messages.each do |m|
    puts m
  end

  puts ''
end

if @exported_something
  success 'Finished exporting the specified targets'
else
  warning 'No export targets were selected'
end
