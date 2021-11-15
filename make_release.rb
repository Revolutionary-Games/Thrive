#!/usr/bin/env ruby
# frozen_string_literal: true

# This script exports the game with godot and packages that up for distribution
# Requires "godot" to be in PATH and the right version
require 'optparse'
require 'sha3'
require 'os'
require 'zlib'
require 'json'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'

require_relative 'scripts/dehydrate'
require_relative 'scripts/version_reader'
require_relative 'scripts/steam_version_helpers'

FileUtils.mkdir_p 'builds'

ALL_TARGETS = ['Linux/X11', 'Windows Desktop', 'Windows Desktop (32-bit)', 'Mac OSX'].freeze
DEVBUILD_TARGETS = ['Linux/X11', 'Windows Desktop'].freeze
BASE_BUILDS_FOLDER = File.realpath 'builds'

README_FILE = 'builds/README.txt'
REVISION_FILE = 'builds/revision.txt'

LINUX_EXTRA_FILES = [['assets/misc/Thrive.desktop', 'Thrive.desktop'],
                     ['assets/misc/thrive_logo_big.png', 'Thrive.png']].freeze

# Files that will never be considered for dehydrating
DEHYDRATE_IGNORE_FILES = [
  'source.7z',
  'revision.txt',
  'ThriveAssetsLICENSE.txt',
  'GodotLicense.txt',
  'gpl.txt',
  'LICENSE.txt',
  'README.txt'
].freeze

LICENSE_FILES = [
  ['LICENSE.txt', 'LICENSE.txt'],
  ['gpl.txt', 'gpl.txt'],
  ['assets/LICENSE.txt', 'ThriveAssetsLICENSE.txt'],
  ['assets/README.txt', 'ThriveAssetsREADME.txt'],
  ['doc/GodotLicense.txt', 'GodotLicense.txt']
].freeze

SOURCE_ITEMS = [
  'default_bus_layout.tres', 'default_env.tres', 'Directory.Build.props', 'export_presets.cfg',
  'GlobalSuppressions.cs', 'LICENSE.txt', 'project.godot', 'stylecop.json', 'StyleCop.ruleset',
  'Thrive.csproj', 'Thrive.sln', 'Thrive.sln.DotSettings', 'doc', 'docker/ci',
  'docker/jsonlint', 'Properties', 'shaders', 'simulation_parameters', 'src', 'test',
  'third_party', 'README.md'
].freeze

SET_EXECUTE_FOR_MAC = false

SPECIAL_BUILDS = { steam:
                     {
                       prepare_compile: lambda {
                         warning 'This is the Steam build. This can only be distributed '\
                                 'by Revolutionary Games Studio (under a special license) '\
                                 'due to Steam being incompatible with GPL license!'
                         @reprint_messages.append 'WARNING: This is the Steam version, see'\
                                                  ' above the licensing caveats'
                         enable_steam_build
                       },
                       target_suffix: '_steam',
                       godot_executable: 'godot-steam',
                       folder_prepare: lambda { |target_folder|
                         copy_steam_resources target_folder
                       },
                       mac_zip_postprocess: lambda {|target_folder, target_file|
                                            }
                     } }.freeze

@options = {
  custom_targets: false,
  targets: ALL_TARGETS,
  retries: 2,
  zip: true,
  include_source: true,
  special: nil
}

# Messages to print again after the end
@reprint_messages = []

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
  opts.on('-s', '--[no-]source', 'Include or exclude source code') do |b|
    @options[:include_source] = b
  end
  opts.on('--special type', 'Enable special build mode') do |s|
    @options[:special] = s.to_sym
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

VALID_TARGETS = @options[:dehydrate] ? DEVBUILD_TARGETS : ALL_TARGETS

# Make sure godot ignores the builds folder in terms of imports
File.write 'builds/.gdignore', '' unless File.exist? 'builds/.gdignore'

if @options[:special]
  onError 'dehydrate option conflicts with special' if @options[:dehydrate]

  unless SPECIAL_BUILDS.include? @options[:special]
    onError "invalid special build specified: #{@options[:special]}"
  end

  SPECIAL_BUILDS[@options[:special]][:prepare_compile].call

  @options[:include_source] = false
  @options[:zip] = false
else
  # Ensure normal build
  disable_steam_build
end

if @options[:dehydrate]
  puts 'Making dehydrated devbuilds'

  @options[:targets] = DEVBUILD_TARGETS unless @options[:custom_targets]
end

if @options[:include_source]

  puts 'Release includes source code'

  zip_target = 'builds/source.7z'

  @extra_included_files = LICENSE_FILES + [
    [zip_target, 'source.7z']
  ]

  puts 'Collecting source code...'

  File.unlink zip_target if File.exist? zip_target

  puts 'Zipping source code...'

  runOpen3Checked(p7zip, 'a', '-mx=9', '-ms=on', zip_target, *SOURCE_ITEMS)

  success 'Source code prepared for release'
else
  puts "Release doesn't include source code"
  @extra_included_files = LICENSE_FILES
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

def create_readme
  File.open(README_FILE, 'w') do |file|
    file.puts 'Thrive'
    file.puts ''
    file.puts "This is a compiled version of the game. Run the executable 'Thrive' to play."
    file.puts ''
    file.puts 'Source code is available online: https://github.com/Revolutionary-Games/Thrive'
    file.puts ''
    file.puts 'Exact commit this build is made from is in revision.txt'
  end

  File.open(REVISION_FILE, 'w') do |file|
    file.puts `git log -n 1`.strip
    file.puts ''

    diff = `git diff`.strip

    unless diff.strip.empty?

      file.puts 'dirty, diff:'
      file.puts diff
    end
  end
end

# Copies license information to a target folder (and specified extra files)
def prepare_licenses(target_folder)
  @extra_included_files.each do |l|
    FileUtils.cp l[0], File.join(target_folder, l[1])
  end
end

# Copies commit info to the target folder
def prepare_readme(target_folder)
  FileUtils.cp README_FILE, target_folder
  FileUtils.cp REVISION_FILE, target_folder
end

def copy_extra_linux_resources(target_folder)
  LINUX_EXTRA_FILES.each do |source, target|
    FileUtils.cp source, File.join(target_folder, target)
  end
end

def copy_steam_resources(target_folder)
  File.open(File.join(target_folder, 'README.txt'), 'w') do |file|
    file.puts 'Thrive'
    file.puts ''
    file.puts 'This is the Steam version of the game. Run the executable'\
              " 'Thrive-Launcher' to play."
    file.puts ''
    file.puts 'Source code is available online: https://github.com/Revolutionary-Games/Thrive'
    file.puts 'This version of Thrive is specially licensed and *not* under the GPLv3 license.'
    file.puts ''
    file.puts 'Exact version of this build is in revision.txt'
  end

  FileUtils.cp REVISION_FILE, target_folder
end

def gzip_to_target(source, target)
  Zlib::GzipWriter.open(target) do |writer|
    File.open(source) do |reader|
      while (chunk = reader.read(16 * 1024))
        writer.write chunk
      end
    end
  end
end

def set_mac_execute_bit(target_file)
  puts "Extracting app from #{target_file}"

  work_dir = File.dirname target_file
  base_extract = File.join work_dir, 'Thrive.app'
  path_in_zip = 'Thrive.app/Contents/MacOS/Thrive'
  extracted = File.join base_extract, 'Contents', 'MacOS', 'Thrive'

  runOpen3Checked('unzip', '-o', target_file, path_in_zip, '-d', work_dir)

  onError "Expected file (#{extracted}) didn't get extracted" unless File.exist? extracted

  if File.executable? extracted
    info "File is already executable, but we don't care because mac seems to think "\
         "it isn't executable"
  end

  FileUtils.chmod '+x', extracted

  unless File.executable? extracted
    onError 'Failed to add the execute bit (you are probably trying to run this on '\
            "a filesystem that doesn't support setting the execute bit)"
  end

  Dir.chdir(work_dir) do
    # Zip command doesn't seem to want to update things. 7zip seems to
    # make the executable bit change go into the archive
    runOpen3Checked(p7zip, 'a', target_file, path_in_zip)
  end

  FileUtils.rm_rf base_extract, secure: true
end

def devbuild_package(target, target_name, target_folder, target_file)
  puts "Performing devbuild package on: #{target_folder}"

  FileUtils.mkdir_p DEVBUILDS_FOLDER
  FileUtils.mkdir_p DEHYDRATE_CACHE
  FileUtils.mkdir_p 'builds/temp_extracted'

  extract_folder = "builds/temp_extracted/#{target_name}"

  FileUtils.mkdir_p extract_folder

  pck = File.join(target_folder, 'Thrive.pck')

  # Start by extracting the big files to be dehydrated
  if runSystemSafe(pck_tool, '--action', 'extract', pck,
                   '-o', extract_folder, '--min-size-filter',
                   DEHYDRATE_FILE_SIZE_THRESSHOLD.to_s) != 0
    onError 'Failed to run extract. Do you have the right godotpcktool version?'
  end

  # And remove them from the .pck
  if runSystemSafe(pck_tool, '--action', 'repack', File.join(target_folder, 'Thrive.pck'),
                   '--max-size-filter', (DEHYDRATE_FILE_SIZE_THRESSHOLD - 1).to_s) != 0
    onError 'Failed to run repack'
  end

  pck_cache = DehydrateCache.new extract_folder

  # Dehydrate always all the unextracted files
  Dir.glob(File.join(extract_folder, '**/*'), File::FNM_DOTMATCH) do |file|
    raise "found file doesn't exist" unless File.exist? file
    next if File.directory? file

    dehydrate_file file, pck_cache
  end

  # No longer need the temp files
  FileUtils.rm_rf extract_folder, secure: true

  normal_cache = DehydrateCache.new target_folder

  # Dehydrate other files
  Dir.glob(File.join(target_folder, '**/*'), File::FNM_DOTMATCH) do |file|
    raise "found file doesn't exist" unless File.exist? file
    next if File.directory? file

    # Always ignore some files despite their sizes
    next if DEHYDRATE_IGNORE_FILES.include? file.sub("#{target_folder}/", '')

    check_dehydrate_file file, normal_cache
  end

  normal_cache.add_pck pck, pck_cache

  # Write the cache
  File.write(File.join(target_folder, 'dehydrated.json'), normal_cache.to_json)

  # Then do a normal zip after the contents have been adjusted
  final_file = zip_package target, target_name, target_folder, target_file

  # And move it to the devbuilds folder for the upload script
  FileUtils.mv final_file, DEVBUILDS_FOLDER

  # Write meta file needed for upload
  File.write(File.join(DEVBUILDS_FOLDER, "#{File.basename(final_file)}.meta.json"),
             { dehydrated: { objects: normal_cache.hashes },
               branch: git_branch,
               platform: target,
               version: THRIVE_VERSION }.to_json)

  message = "Created devbuild: #{File.join(DEVBUILDS_FOLDER, final_file)}"
  puts message
  @reprint_messages.append '', message
end

def zip_package(target, target_name, target_folder, target_file)
  if target_mac? target
    puts 'Mac target is already zipped, moving it instead'

    final_file = "#{target_folder}.zip"

    File.unlink(final_file) if File.exist? final_file

    FileUtils.mv target_file, final_file
  else
    info 'Packaging for release...'
    final_file = "#{target_folder}.7z"

    # TODO: clean build option
    # File.unlink(final_file) if File.exist? final_file

    Dir.chdir(BASE_BUILDS_FOLDER) do
      if runSystemSafe(p7zip, 'a', "#{target_name}.7z", target_name) != 0
        onError "Failed to package the target: #{target_folder}"
      end
    end
  end

  onError "Final file creation failed (#{final_file})" unless File.exist? final_file

  puts ''
  message1 = "Done: #{final_file}"
  message2 = "SHA3: #{SHA3::Digest::SHA256.file(final_file).hexdigest}"

  @reprint_messages.append '', message1, message2

  success message1
  puts message2
  final_file
end

def package(target, target_name, target_folder, target_file)
  if target_mac? target
    if @options[:special]
      SPECIAL_BUILDS[@options[:special]][:mac_zip_postprocess].call target_folder, target_file
    else
      puts 'Including licenses in mac .zip'

      Dir.chdir(target_folder) do
        runOpen3Checked(*['zip', '-u', target_file,
                          @extra_included_files.map { |i| i[1] }].flatten,
                        'README.txt', 'revision.txt')
      end

      puts 'Licenses added'
    end

    if SET_EXECUTE_FOR_MAC
      puts 'Trying to set execute bit for .app for mac'

      set_mac_execute_bit target_file

      puts 'Execute bit set'
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

def godot_template_name(target)
  if @options[:special]
    target + SPECIAL_BUILDS[@options[:special]][:target_suffix]
  else
    target
  end
end

# Performs the actual export for a target
def perform_export(target)
  puts ''
  info "Starting export for target: #{target}"

  target_name = "Thrive_#{THRIVE_VERSION}_" + target.gsub(%r{[\s/]}i, '_').downcase
  godot = 'godot'

  if @options[:special]
    target_name += SPECIAL_BUILDS[@options[:special]][:target_suffix]
    godot = SPECIAL_BUILDS[@options[:special]][:godot_executable]
  end

  target_folder = File.join BASE_BUILDS_FOLDER, target_name

  FileUtils.mkdir_p target_folder

  puts "Exporting to folder: #{target_folder}"

  target_file = File.join(target_folder, "Thrive#{get_target_extension(target)}")

  attempts = 1 + @options[:retries]

  success = false

  (1..attempts).each do |attempt|
    if runOpen3(godot, '--export', godot_template_name(target), target_file).success?
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

  if @options[:special]
    SPECIAL_BUILDS[@options[:special]][:folder_prepare].call target_folder
  else
    prepare_licenses target_folder
    prepare_readme target_folder
  end

  copy_extra_linux_resources target_folder if target =~ /linux/i

  package target, target_name, target_folder, target_file
end

puts "Starting exporting thrive #{THRIVE_VERSION} to the specified targets"

create_readme

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
