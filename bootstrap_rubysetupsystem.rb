#!/usr/bin/env ruby
# Makes sure git submodules are loaded (for RubySetupSystem to work)

require 'English'

# RubySetupSystem Bootstrap
if !File.exist? 'RubySetupSystem/RubySetupSystem.rb'
  puts 'Initializing RubySetupSystem'
  system 'git submodule init && git submodule update --recursive'

  if $CHILD_STATUS.exitstatus != 0
    abort('Failed to initialize or update git submodules. ' \
          'Please make sure git is in path and ' \
          'you have an ssh key setup for your github account')
  end
else
  # TODO: this might turn out to be too slow to do all the time in the formatting check
  # Make sure RubySetupSystem is up to date
  # This may make debugging RubySetupSystem harder so feel free to comment out
  system 'git submodule update --init'
end
