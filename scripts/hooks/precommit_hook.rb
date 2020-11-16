#!/usr/bin/env ruby
# frozen_string_literal: true

# Runs the check_formatting.rb script before committing
require 'English'

CHANGED_FILE = 'files_to_check.txt'

output = system 'git diff --cached --name-only ' 

File.write output 

system './check_formatting.rb'

File.unlink CHANGED_FILE

exit $CHILD_STATUS.exitstatus
