#!/usr/bin/env ruby
# frozen_string_literal: true
require_relative '../RubySetupSystem/RubyCommon'

puts 'Extracting .pot file'
runOpen3Checked 'pybabel', 'extract', '-F', '../locale/babelrc', '-k', 'LineEdit', '-k', 'text', '-k', 'DisplayName', '-k', 'window_title', '-k', 'dialog_text', '-k', 'Translate', '-o', '../locale/messages.pot', '../.'
puts 'Done extracting'

puts 'Extracting .po files'
LOCALES = %w[en fr]

LOCALES.each{|locale|
    puts 'Extracting ' + locale + '.po'
    runOpen3Checked 'msgmerge', '--update', '--backup=none', '../locale/' + locale + '.po', '../locale/messages.pot'
}
success 'Done extracting .po files'
