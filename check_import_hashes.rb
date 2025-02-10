#!/usr/bin/ruby
# Detect new file hashes in Godot import folder
# frozen_string_literal: true

require 'json'
require 'digest'

GODOT_IMPORT_FOLDER = '.godot/**/*.*'
DB = 'hashes.json'

File.write(DB, '{}') unless File.exist? DB

@db = JSON.parse(File.read(DB))

new_files = []

puts 'Calculating hashes...'

Dir.glob(GODOT_IMPORT_FOLDER) do |file|
  # Skip folders
  next unless File.file? file

  # Assume we have enough RAM to hold the biggest files in memory
  hash = Digest::SHA2.hexdigest File.read(file)

  # puts "#{hash} #{file}"

  unless @db.key? hash
    new_files.append [hash, file]
    @db[hash] = file
  end
end

puts 'Finished calculating hashes'

File.write(DB, JSON.dump(@db))
puts 'Wrote new DB'

new_files.each  do |file|
  puts "New hash seen for file: #{file[1]} (#{file[0]})"
end
