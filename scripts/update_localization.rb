#!/usr/bin/env ruby
# frozen_string_literal: true

# List of locales, edit this to add new ones:
LOCALES = %w[bg ca cs da de en eo es_AR es et fi fr frm he hu id ko la lb_LU it nl nl_BE
             pl pt_BR pt_PT ru si_LK sr_Cyrl sr_Latn sv th_TH tr uk lt lv zh_CN zh_TW].freeze

# Weblate disagrees with gettext tools regarding where to wrap
# https://github.com/Revolutionary-Games/Thrive/issues/2679
# For now we use 77 column wrapping as that is *mostly* the same
# If 78 was used instead, it would give slightly less changes from Weblate PRs
# but normal gettext editors would need manual configuration (as 77 is like the standard
# line width in gettext)
LINE_WRAP_SETTINGS = ['--width=77'].freeze

require 'optparse'
require_relative '../bootstrap_rubysetupsystem'
require_relative '../RubySetupSystem/RubyCommon'

@options = {
  pot_suffix: '.pot',
  po_suffix: '.po'
}

OptionParser.new do |opts|
  opts.banner = "Usage: #{$PROGRAM_NAME} [options]"

  opts.on('--pot-suffix suffix',
          'Set custom .pot suffix') do |suffix|
    @options[:pot_suffix] = suffix
  end
  opts.on('--po-suffix suffix',
          'Set custom .po suffix') do |suffix|
    @options[:po_suffix] = suffix
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

ROOT_FOLDER = File.expand_path(File.join(File.dirname(__FILE__), '../'))
LOCALE_FOLDER = File.join ROOT_FOLDER, 'locale'

puts "Detected Thrive root folder: #{ROOT_FOLDER}"

Dir.chdir(LOCALE_FOLDER) do
  puts 'Extracting .pot file'

  runOpen3Checked 'pybabel', 'extract', '-F', File.join(LOCALE_FOLDER, 'babelrc'), '-k',
                  'LineEdit', '-k', 'text', '-k', 'DisplayName', '-k', 'Description', '-k',
                  'ProcessesDescription', '-k', 'window_title', '-k', 'dialog_text', '-k',
                  'placeholder_text', '-k', 'hint_tooltip', '-k',
                  'TranslationServer.Translate', '-o',
                  File.join(LOCALE_FOLDER, "messages#{@options[:pot_suffix]}"),
                  '../simulation_parameters', '../assets', '../src'

  success 'Done extracting .pot file'

  info 'Extracting .po files'

  LOCALES.each do |locale|
    target = locale + @options[:po_suffix]

    if File.exist? target
      puts "Extracting #{locale}.po"
      runOpen3Checked 'msgmerge', '--update', '--backup=none', *LINE_WRAP_SETTINGS,
                      target,
                      "messages#{@options[:pot_suffix]}"
    else
      puts "Creating new file #{locale}.po"

      runOpen3Checked 'msginit', '-l', locale, '--no-translator', *LINE_WRAP_SETTINGS,
                      '-o', target, '-i',
                      "messages#{@options[:pot_suffix]}"
    end
  end
end

success 'Done extracting .po files'
