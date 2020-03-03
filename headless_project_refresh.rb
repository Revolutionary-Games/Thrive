#!/usr/bin/env ruby
# This script fixes the project for running on headless CI build
require 'English'

# This doesn't work very well

puts 'Running solution build with godot, this failing is not serious at this point'
system(%(timeout --kill-after 15s 45s godot -e --build-solutions -q))

# if $CHILD_STATUS.exitstatus != 0
#   abort "Failed to compile C# project with Godot"
# end
