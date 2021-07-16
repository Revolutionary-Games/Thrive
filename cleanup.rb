#!/usr/bin/env ruby
# Performs a full cleanup on the local version. Godot Editor needs to be closed while running.

# frozen_string_literal: true

require 'fileutils'

puts 'Deleting .import'

FileUtils.rm_rf '.import'

puts 'Deleting .mono'

FileUtils.rm_rf '.mono'

puts 'Doing git reset --hard HEAD'

system 'git reset --hard HEAD'
