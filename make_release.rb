#!/usr/bin/env ruby
# This script exports the game with godot and packages that up for distribution
# Requires "godot" to be in PATH and the right version
require 'optparse'
require 'sha3'

require_relative 'RubySetupSystem/RubyCommon'

FileUtils.mkdir_p 'builds'

ALL_TARGETS = ['Linux/X11', 'Windows Desktop', 'Windows Desktop (32-bit)', 'Mac OSX'].freeze
BASE_BUILDS_FOLDER = File.realpath 'builds'
THRIVE_VERSION_FILE = 'Properties/AssemblyInfo.cs'.freeze

@options = {
  targets: ALL_TARGETS,
  retries: 1
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
end.parse!

ASSEMBLY_VERSION = /AssemblyVersion\(\"([\d\.]+)\"\)/.freeze
INFORMATIONAL_VERSION = /AssemblyInformationalVersion\(\"([^\"]*)\"\)/.freeze

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

# Returns the extension needed for the target
def get_target_extension(target)
  # "thrive_linux.x86_64"
  if target =~ /mac/i
    '.zip'
  elsif target =~ /windows/i
    '.exe'
  elsif target =~ /linux/i
    ''
  else
    ''
  end
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

  if target =~ /mac/i
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
  success 'Done: ' + final_file
  puts 'SHA3: ' + SHA3::Digest::SHA256.file(final_file).hexdigest
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
if @exported_something
  success 'Finished exporting the specified targets'
else
  warning 'No export targets were selected'
end
