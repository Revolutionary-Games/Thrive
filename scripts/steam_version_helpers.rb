# frozen_string_literal: true

require 'fileutils'

require_relative 'folder_detector'

STEAM_CLIENT_FILE_PATH = 'src\steam\SteamClient.cs'
STEAMWORKS_REFERENCE_START = '<Reference Include="Steamworks.NET">'

CSPROJ_COMPILE_LINE = '<Compile Include='
CSPROJ_SYSTEM_REFERENCE = '<Reference Include="System"'

def path_to_steam_assembly(platform)
  case platform
  when /linux/i || /osx/i
    'third_party\\linux\\Steamworks.NET.dll'
  when /windows/i
    'third_party\\windows\\Steamworks.NET.dll'
  else
    raise "unknown platform for steam assembly path: #{platform}"
  end
end

def thrive_csproj
  "#{detect_thrive_folder}Thrive.csproj"
end

def steam_build_enabled?
  File.read(thrive_csproj, encoding: 'utf-8').include? STEAM_CLIENT_FILE_PATH
end

def steamworks_referenced?(platform)
  File.read(thrive_csproj, encoding: 'utf-8').include? steam_assembly_reference(platform)
end

def steam_assembly_reference(platform)
  "#{STEAMWORKS_REFERENCE_START}<HintPath>#{path_to_steam_assembly(platform)}"\
    '</HintPath></Reference>'
end

def enable_steam_build(platform)
  if steam_build_enabled?
    puts 'Steam build already enabled'

    unless steamworks_referenced?(platform)
      puts 'Adding correct platform reference'
      disable_steam_build

      puts 'Re-enabling steam build with right platform...'
      enable_steam_build platform
    end

    return
  end

  original = thrive_csproj
  temp = "#{original}.tmp"

  process_adding_client_line original, temp, platform

  FileUtils.mv temp, original, force: true

  puts 'Enabled Steam build'
end

def disable_steam_build
  unless steam_build_enabled?
    puts 'Steam build is not enabled'
    return
  end

  original = thrive_csproj
  temp = "#{original}.tmp"

  process_removing_client_line original, temp

  FileUtils.mv temp, original, force: true

  puts 'Disabled Steam build'
end

def process_adding_client_line(from, to, platform)
  added = false
  found_compile = false

  added_steamworks = false

  File.open(to, 'w') do |writer|
    File.foreach(from, encoding: 'utf-8').with_index do |line, line_number|
      if added && added_steamworks
        writer.write line
        next
      end

      if found_compile && !line.include?(CSPROJ_COMPILE_LINE) && !added
        puts "Inserting special compile file after line #{line_number}"
        writer.puts "    <Compile Include=\"#{STEAM_CLIENT_FILE_PATH}\" />"
        added = true
      elsif line.include?(CSPROJ_COMPILE_LINE) && !found_compile
        puts "Found first compile file reference on line #{line_number + 1}"
        found_compile = true
      end

      if !added_steamworks && line.include?(CSPROJ_SYSTEM_REFERENCE)
        puts "Found system reference on line #{line_number + 1}, adding steamworks reference"
        writer.puts "    #{steam_assembly_reference(platform)}"
      end

      writer.write line
    end
  end
end

def process_removing_client_line(from, to)
  File.open(to, 'w') do |writer|
    File.foreach(from, encoding: 'utf-8').with_index do |line, line_number|
      if line.include? STEAM_CLIENT_FILE_PATH
        puts "Removed steam client compile reference on line #{line_number + 1}"
        next
      end

      if line.include? STEAMWORKS_REFERENCE_START
        puts "Removed steamworks assembly reference on line #{line_number + 1}"
        next
      end

      writer.write line
    end
  end
end
