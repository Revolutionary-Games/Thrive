#!/usr/bin/env ruby
require 'highline'
require_relative '../RubySetupSystem/RubyCommon'

puts "This will reset all translations made in this branch."
puts "Run this script from the root Thrive folder."
puts "Are you sure you want to continue?"
waitForKeyPress()
if (File.exist?('Thrive.sln'))
    currentBranch = `git rev-parse --abbrev-ref HEAD`
    system "git stash"
    system "git checkout master"
    system "git pull"
    system "git checkout #{currentBranch}"
    system "git stash pop"
    system "git checkout master locale/"
    system "ruby scripts/update_localization.rb"
    poeditEditor = which('poedit')
    if which('poedit').nil?  
      coreEditor = `git config --global core.editor`
        if (coreEditor.empty?)
            visualEditor = `git config --global core.visual`
            if (visualEditor.empty?)
                if (Gem.win_platform?)
                    editor = "notepad.exe"
                else
                    editor = "vi"
                end
            else
                editor = visualEditor
            end
        else
            editor = coreEditor
        end
    else
        editor = poeditEditor
    end

    system(editor, "locale/en.po")
else
    puts "I told you to run this script from the root Thrive folder!!"
end
