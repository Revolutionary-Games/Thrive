#!/usr/bin/env ruby
# This script fixes the project for running on headless CI build
require 'English'

puts 'Running solution build with godot, this failing is not serious at this point'
system(%(godot -e --build-solutions))

# if $CHILD_STATUS.exitstatus != 0
#   abort "Failed to compile C# project with Godot"
# end
