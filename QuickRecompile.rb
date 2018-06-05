#!/usr/bin/env ruby
# This script runs the setup with options that make recompiling much faster (but it doesn't
# update everything!)
require_relative 'RubySetupSystem/RubyCommon.rb'

system("ruby SetupThrive.rb --no-packagemanager --no-subproject-deps")
exit $?.exitstatus == 0
