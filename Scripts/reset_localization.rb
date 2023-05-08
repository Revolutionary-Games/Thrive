#!/usr/bin/env ruby
# frozen_string_literal: true

# TODO: this needs to be converted to C#
# https://github.com/Revolutionary-Games/Thrive/issues/3765

require 'os'
require_relative '../RubySetupSystem/RubyCommon'

def editor
  return 'poedit' unless which('poedit').nil?

  core_editor = `git config --global core.editor`
  return core_editor.strip unless core_editor.empty?

  visual_editor = `git config --global core.visual`
  return visual_editor.strip unless visual_editor.empty?

  if OS.windows?
    'notepad.exe'
  else
    'vi'
  end
end

puts 'This will reset all translations made in this branch.'
puts 'Run this script from the root Thrive folder.'
puts 'Are you sure you want to continue?'
waitForKeyPress
unless File.exist?('Thrive.sln')
  puts 'I told you to run this script from the root Thrive folder!'
  exit
end
has_changes = system('git diff-index --quiet HEAD --') == false
if has_changes
  puts 'You have uncommitted changes. Please commit your changes before running this script.'
  exit
end
current_branch = `git rev-parse --abbrev-ref HEAD`.strip!
runOpen3Checked('git', 'checkout', 'master')
runOpen3Checked('git', 'pull')
runOpen3Checked('git', 'checkout', current_branch)
runOpen3Checked('git', 'checkout', 'master', 'locale/')
runOpen3Checked('git', 'add', 'locale/')
runOpen3Checked('git', 'commit', '-m', '"Reset locales to master"')
runOpen3Checked('git', 'merge', 'master', '--no-ff')
runOpen3Checked('ruby', 'scripts/update_localization.rb')
system(editor, 'locale/en.po')
