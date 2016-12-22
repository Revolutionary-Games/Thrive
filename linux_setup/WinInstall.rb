# Installs a target on Windows
# This is a separate script so that it is enough to run just this script as administrator
require_relative 'RubySetupSystem'

abort "invalid argumenst" if ARGV.count != 1

info "Running install: #{ARGV[0]}"
system "#{ARGV[0]}"

if $?.exitstatus > 0
  # Failed
  error "Installation Failed!"
  system "pause"
  abort "error"
end

success "Installation completed"
