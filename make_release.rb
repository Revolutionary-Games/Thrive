#!/usr/bin/env ruby
# frozen_string_literal: true

# This script exports the game with godot and packages that up for distribution
# Requires "godot" to be in PATH and the right version
require 'optparse'
require 'sha3'
require 'os'
require 'zlib'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'

FileUtils.mkdir_p 'builds'

ALL_TARGETS = ['Linux/X11', 'Windows Desktop', 'Windows Desktop (32-bit)', 'Mac OSX'].freeze
DEVBUILD_TARGETS = ['Linux/X11', 'Windows Desktop'].freeze
BASE_BUILDS_FOLDER = File.realpath 'builds'
THRIVE_VERSION_FILE = 'Properties/AssemblyInfo.cs'
DEHYDRATE_CACHE = 'builds/dehydrate_cache'
DEVBUILDS_FOLDER = 'builds/devbuilds'

DEHYDRATE_FILE_SIZE_THRESSHOLD = 100_000

LICENSE_FILES = [
  ['LICENSE.txt', 'LICENSE.txt'],
  ['gpl.txt', 'gpl.txt'],
  ['assets/LICENSE.txt', 'ThriveAssetsLICENSE.txt'],
  ['assets/README.md', 'ThriveAssetsREADME.txt'],
  ['doc/GodotLicense.txt', 'GodotLicense.txt']
].freeze

@options = {
  custom_targets: false,
  targets: ALL_TARGETS,
  retries: 2,
  zip: true
}

OptionParser.new do |opts|
  opts.banner = "Usage: #{$PROGRAM_NAME} [options]"

  opts.on('-t', '--targets target1,target2', Array,
          'Export targets to use. Default is all') do |targets|
    @options[:custom_targets] = true
    @options[:targets] = targets
  end
  opts.on('-r', '--retries count', Integer,
          'How many export retries to do to avoid spurious failures') do |r|
    @options[:retries] = r
  end
  opts.on('-z', '--[no-]zip', 'Disables packaging the exported game') do |b|
    @options[:zip] = b
  end
  opts.on('-d', '--[no-]dehydrated', 'Makes dehydrated devbuilds') do |b|
    @options[:dehydrate] = b
    @options[:zip] = !b
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

if @options[:dehydrate]
  puts 'Making dehydrated devbuilds'

  @options[:targets] = DEVBUILD_TARGETS unless @options[:custom_targets]
end

VALID_TARGETS = @options[:dehydrate] ? DEVBUILD_TARGETS : ALL_TARGETS

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

def git_commit
  `git rev-parse --verify HEAD`.strip
end

def pck_tool
  if OS.windows?
    'godotpcktool.exe'
  else
    'godotpcktool'
  end
end

THRIVE_VERSION = !@options[:dehydrate] ? find_thrive_version : git_commit

def target_mac?(target)
  target =~ /mac/i
end

# Returns the extension needed for the target
def get_target_extension(target)
  # "thrive_linux.x86_64"
  if target_mac? target
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

def gzip_to_target(source, target)
  Zlib::GzipWriter.open(target) do |writer|
    File.open(source)  do |reader|
      while (chunk = reader.read(16 * 1024))
        writer.write chunk
      end
    end
  end
end

# Dehydrates a file by moving it to the dehydrate cache if needed
def dehydrate_file(file)
  # TODO: remove
  puts "Dehydrating: #{file}"

  hash = SHA3::Digest::SHA256.file(file).hexdigest

  target = File.join DEHYDRATE_CACHE, "#{hash}.gz"

  # Only copy to the dehydrate cache if hash doesn't exist
  unless File.exist? target
    puts "Copying #{file} (#{hash}) to dehydrate cache"
    gzip_to_target file, target
  end

  # TODO: make a json dehydrated cache

  FileUtils.rm file
end

# Checks if a file needs to be dehydrated
def check_dehydrate_file(file)
  return if file =~ /.+\.pck$/

  return if File.size(file) < DEHYDRATE_FILE_SIZE_THRESSHOLD

  dehydrate_file file
end

def devbuild_package(target, target_name, target_folder, target_file)
  puts "Performing devbuild package on: #{target_folder}"

  FileUtils.mkdir_p DEVBUILDS_FOLDER
  FileUtils.mkdir_p DEHYDRATE_CACHE
  FileUtils.mkdir_p 'builds/temp_extracted'

  extract_folder = "builds/temp_extracted/#{target_name}"

  FileUtils.mkdir_p extract_folder

  # Start by extracting the big files to be dehydrated
  if runSystemSafe(pck_tool, '--action', 'extract', File.join(target_folder, 'Thrive.pck'),
                   '-o', extract_folder, '--min-size-filter',
                   DEHYDRATE_FILE_SIZE_THRESSHOLD.to_s) != 0
    onError 'Failed to run extract. Do you have the right godotpcktool version?'
  end

  # And remove them from the .pck
  if runSystemSafe(pck_tool, '--action', 'repack', File.join(target_folder, 'Thrive.pck'),
                   '--max-size-filter', (DEHYDRATE_FILE_SIZE_THRESSHOLD - 1).to_s) != 0
    onError 'Failed to run repack'
  end

  # Dehydrate always all the unextracted files
  Dir.glob(File.join(extract_folder, '**/*'), File::FNM_DOTMATCH) do |file|
    raise "found file doesn't exist" unless File.exist? file
    next if File.directory? file

    dehydrate_file file
  end

  # No longer need the temp files
  FileUtils.rm_rf extract_folder, secure: true

  # Dehydrate other files
  Dir.glob(File.join(target_folder, '**/*'), File::FNM_DOTMATCH) do |file|
    raise "found file doesn't exist" unless File.exist? file
    next if File.directory? file

    check_dehydrate_file file
  end

  # Then do a normal zip after the contents have been adjusted
  final_file = zip_package target, target_name, target_folder, target_file

  # And move it to the devbuilds folder for the upload script
  FileUtils.mv final_file, DEVBUILDS_FOLDER
  message = "Created devbuild: #{File.join(DEVBUILDS_FOLDER, final_file)}"
  puts message
  @reprint_messages.append '', message
end

def zip_package(target, target_name, target_folder, target_file)
  if target_mac? target
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
  final_file
end

def package(target, target_name, target_folder, target_file)
  if target_mac? target
    puts 'Including licenses in mac .zip'

    Dir.chdir(target_folder) do
      runOpen3Checked(*['zip', '-u', target_file, LICENSE_FILES.map { |i| i[1] }].flatten)
    end
    puts 'Zip updated'
  end

  if @options[:dehydrate]
    devbuild_package target, target_name, target_folder, target_file
  else
    if @options[:zip] == false
      info 'Skipping packaging created folder'
      @reprint_messages.append "Created folder: #{target_folder}"
      return
    end

    zip_package target, target_name, target_folder, target_file
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
    if runOpen3('godot', '--export', target, target_file).success?
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
  unless VALID_TARGETS.include? target
    onError "specified target (#{target}) is not valid. Valid targets: #{VALID_TARGETS}"
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
