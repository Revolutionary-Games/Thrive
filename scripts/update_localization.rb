#!/usr/bin/env ruby
# frozen_string_literal: true
require_relative '../RubySetupSystem/RubyCommon'

puts 'Extracting .pot file'
runOpen3Checked 'pybabel', 'extract', '-F', '../locale/babelrc', '-k', 'LineEdit', '-k', 'text', '-k', 'window_title', '-k', 'dialog_text', '-k', 'Translate', '-o', '../locale/messages.pot', '../.'
puts 'Done extracting'

puts 'Extracting .po files'
runOpen3Checked 'msgmerge', '--update', '--backup=none', '../locale/en.po', '../locale/messages.pot'
runOpen3Checked 'msgmerge', '--update', '--backup=none', '../locale/fr.po', '../locale/messages.pot'
success 'Done extracting .po files'
