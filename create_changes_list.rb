#!/usr/bin/env ruby
# frozen_string_literal: true

require 'English'

# This scripts builds a list of changed files against master and last commit
# Running this before check_formatting.rb makes it check just the changed files
require_relative 'scripts/check_file_list'

ORIGIN = 'origin'
BRANCH = 'master'

system 'git', 'fetch', ORIGIN, BRANCH

if $CHILD_STATUS.exitstatus != 0
  puts 'Failed to fetch'
  exit 1
end

data = ''

new_data = `git diff-tree --no-commit-id --name-only -r HEAD..#{ORIGIN}/#{BRANCH}`.strip

data += "#{new_data}\n" if new_data != ''

new_data = `git diff-tree --no-commit-id --name-only -r HEAD`.strip

data += "#{new_data}\n" if new_data != ''

new_data = `git diff --name-only --cached`.strip

data += "#{new_data}\n" if new_data != ''

new_data = `git diff --name-only`.strip

data += "#{new_data}\n" if new_data != ''

File.write ONLY_FILE_LIST, data
puts "Created list of changed files in #{ONLY_FILE_LIST}"
