#!/usr/bin/env ruby
# frozen_string_literal: true

require 'os'
require_relative '../RubySetupSystem/RubyCommon'

def editor
  return 'poedit' unless which('poedit').nil?

  core_editor = `git config --global core.editor`
  return core_editor unless core_editor.nil?

  visual_editor = `git config --global core.visual`
  return visual_editor unless visual_editor.nil?

  if OS.win?
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
current_branch = `git rev-parse --abbrev-ref HEAD`.strip!
has_changes = system('git diff-index --quiet HEAD --')
runOpen3Checked('git', 'stash')
runOpen3Checked('git', 'checkout', 'master')
runOpen3Checked('git', 'pull')
runOpen3Checked('git', 'checkout', current_branch)
runOpen3Checked('git', 'stash', 'pop') if has_changes
runOpen3Checked('git', 'checkout', 'master', 'locale/')
runOpen3Checked('ruby', 'scripts/update_localization.rb')
runOpen3Checked(editor, 'locale/en.po')
