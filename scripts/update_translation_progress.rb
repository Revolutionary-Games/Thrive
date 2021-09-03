#!/usr/bin/env ruby
# frozen_string_literal: true

require 'json'

require_relative '../RubySetupSystem/RubyCommon'

require_relative 'po_helpers'
require_relative 'json_helpers'

TOP_LEVEL_FILE_FOR_FOLDER_DETECT = 'Thrive.sln'

def calculate_stats_for(path)
  in_header = true
  last_msgstr = ''
  seen_message = false
  fuzzy = false
  next_fuzzy = false
  header_just_ended = false
  last_already_blank = false

  blank_translations = 0
  total_translations = 0

  File.foreach(path, encoding: 'utf-8') do |line|
    if line.match(FUZZY_TRANSLATION_REGEX)
      # The fuzzy applies to the next message id we see as we always process the previous
      # message when we see the start of the next one
      next_fuzzy = true
    end

    matches = line.match(MSG_ID_REGEX)

    if matches
      next if in_header

      if header_just_ended
        header_just_ended = false
        next
      end

      if (!last_msgstr || last_msgstr.strip.empty?) || fuzzy
        blank_translations += 1
        last_already_blank = true
      else
        last_already_blank = false
      end

      last_msgstr = ''

      fuzzy = next_fuzzy
      next_fuzzy = false

      total_translations += 1
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
    if in_header && seen_message
      in_header = false
      last_msgstr = ''
      header_just_ended = true
    end
  end

  if !last_already_blank && (last_msgstr.strip.empty? || fuzzy)
    puts "Last is fuzzy or blank (#{next_fuzzy}) #{last_msgstr}"
    blank_translations += 1
  end

  puts "Complete: #{total_translations - blank_translations} of #{total_translations}"

  (total_translations - blank_translations) / total_translations.to_f
end

def run
  base_path = ''
  unless File.exist? TOP_LEVEL_FILE_FOR_FOLDER_DETECT
    base_path = '../'
    unless File.exist? "#{base_path}TOP_LEVEL_FILE_FOR_FOLDER_DETECT"
      puts 'Failed to find the top level Thrive folder'
      exit 2
    end
  end

  progress = {}
  stats = { TranslationProgress: progress }

  Dir["#{base_path}locale/**/*.po"].each do |f|
    # Dir["#{base_path}locale/**/frm.po"].each do |f|
    value = calculate_stats_for f
    puts "Progress of #{f}: #{value * 100}%"
    progress[File.basename(f, File.extname(f))] = value
  end

  json_file = "#{base_path}simulation_parameters/common/translations_info.json"

  File.write json_file, JSON.generate(stats)

  unless jsonlint_on_file(json_file)
    puts 'Failed to run jsonlint'
    exit 2
  end
end

run
