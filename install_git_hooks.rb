#!/usr/bin/env ruby
# Installs git hooks to not commit code that doesn't pass automated checks
require 'fileutils'

PRECOMMIT_SCRIPT = 'scripts/hooks/precommit_hook.rb'.freeze

HOOKS_FOLDER = '.git/hooks'.freeze

PRECOMMIT_FINAL_TARGET = File.join HOOKS_FOLDER, 'pre-commit'

unless File.exist? PRECOMMIT_SCRIPT
  puts "Didn't find the precommit script"
  exit 2
end

def install_precommit
  File.unlink PRECOMMIT_FINAL_TARGET if File.exist? PRECOMMIT_FINAL_TARGET

  FileUtils.cp PRECOMMIT_SCRIPT, PRECOMMIT_FINAL_TARGET
  FileUtils.chmod('+x', PRECOMMIT_FINAL_TARGET)
  puts "Installed hook: #{PRECOMMIT_FINAL_TARGET}"
end

install_precommit
