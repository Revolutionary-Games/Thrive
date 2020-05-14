#!/usr/bin/env ruby
# Toggles analysis on or off by modifying the build solution
require_relative 'toggle_analysis_lib'

if ARGV.length != 1
  error "Excepted a single parameter: 'yes' or 'no'"
  exit 2
end

case ARGV[0]
when /yes/i
  @analysis_mode = true
when /no/i
  @analysis_mode = false
else
  onError 'unknown parameter given'
end

perform_analysis_mode_check @analysis_mode
