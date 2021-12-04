# frozen_string_literal: true

require 'fileutils'

require_relative 'folder_detector'

STEAM_CLIENT_FILE_PATH = 'src\steam\SteamClient.cs'

CSPROJ_COMPILE_LINE = '<Compile Include='

def thrive_csproj
  "#{detect_thrive_folder}Thrive.csproj"
end

def steam_build_enabled?
  File.read(thrive_csproj, encoding: 'utf-8').include? STEAM_CLIENT_FILE_PATH
end

def enable_steam_build
  if steam_build_enabled?
    puts 'Steam build already enabled'
    return
  end

  original = thrive_csproj
  temp = "#{original}.tmp"

  process_adding_client_line original, temp

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

def process_adding_client_line(from, to)
  added = false
  found_compile = false

  File.open(to, 'w') do |writer|
    File.foreach(from, encoding: 'utf-8').with_index do |line, line_number|
      if added
        writer.write line
        next
      end

      if found_compile && !line.include?(CSPROJ_COMPILE_LINE)
        puts "Inserting special compile file after line #{line_number}"
        writer.puts "    <Compile Include=\"#{STEAM_CLIENT_FILE_PATH}\" />"
        added = true
      elsif line.include?(CSPROJ_COMPILE_LINE) && !found_compile
        puts "Found first compile file reference on line #{line_number + 1}"
        found_compile = true
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

      writer.write line
    end
  end
end
