#!/usr/bin/env ruby
# frozen_string_literal: true

# List of locales, edit this to add new ones:
LOCALES = %w[en fr].freeze

require_relative '../RubySetupSystem/RubyCommon'

ROOT_FOLDER = File.expand_path(File.join(File.dirname(__FILE__), '../'))
LOCALE_FOLDER = File.join ROOT_FOLDER, 'locale'

puts "Detected Thrive root folder: #{ROOT_FOLDER}"

Dir.chdir(LOCALE_FOLDER) do
  puts 'Extracting .pot file'

  runOpen3Checked 'pybabel', 'extract', '-F', File.join(LOCALE_FOLDER, 'babelrc'), '-k',
                  'LineEdit', '-k', 'text', '-k', 'DisplayName', '-k', 'window_title', '-k',
                  'dialog_text', '-k', 'Translate', '-o',
                  File.join(LOCALE_FOLDER, 'messages.pot'), '../src',
                  '../simulation_parameters', '../assets'

  success 'Done extracting .pot file'

  info 'Extracting .po files'

  LOCALES.each do |locale|
    puts "Extracting #{locale}.po"
    runOpen3Checked 'msgmerge', '--update', '--backup=none',
                    locale + '.po',
                    'messages.pot'
  end
end

success 'Done extracting .po files'
