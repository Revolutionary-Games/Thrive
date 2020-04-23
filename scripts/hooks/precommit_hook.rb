#!/usr/bin/env ruby
# Runs the check_formatting.rb script before committing
require 'English'

system './check_formatting.rb'

exit $CHILD_STATUS.exitstatus
